using InnoVault;
using InnoVault.Models3D.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace WastelandSoul.Common.Models
{
	// ====================================================================================
	// 3D 管线最小验证（InnoVault `Models3D`）
	//
	// 目的：证明本工程能真的把一份自己生成的 OBJ 加载成 `Vault3DModel` 并在游戏里画出来。
	// 玩家手里拿着「芯片」（Chip）时，头顶会转一个 3D 机械小机器人。
	//
	// 模型不是手画的、也没用 Blender：`tools/gen_3d_models.py` 直接用 Python 写出
	// OBJ + MTL + 一张程序化贴图。见该脚本头部的说明。
	//
	// ★ 加载期线程安全（这是硬性要求，踩过事故）
	//   tModLoader 的模组加载跑在**线程池工作线程**上，FNA3D 的图形调用只能在主线程。
	//   第一版粒子系统把 `new Texture2D(Main.instance.GraphicsDevice, ...)` 写在 `Load()` 里，
	//   客户端启动直接 `ThreadStateException: most FNA3D audio/graphics functions must be
	//   called on the main thread` → `Disabling Mod: WastelandSoul`。
	//
	//   所以这里：
	//     * `[VaultLoaden]` 走的 `ObjModelLoader` 只解析**纯文本几何**（CPU 顶点数组），
	//       不创建 GPU 资源 —— 安全；
	//     * MTL 里**故意不写 `map_Kd`**：`ObjModelLoader.TryLoadTexture` 用的是
	//       `AssetRequestMode.ImmediateLoad`，写了就会在加载线程上建贴图。贴图改由
	//       `AttachTexture()` 在**第一次绘制（主线程）**时请求一次；
	//     * 整个 `Model3DInstance`（以及 `Model3DRenderer` 自己的 BasicEffect /
	//       层级 RenderTarget）都在**第一次绘制**时懒创建。
	//
	// ★ SpriteBatch 批次状态
	//   `Model3DRenderer` 是 InnoVault 的 `RenderHandle`，它自己在
	//   `DrawAfterPlayers` / `DrawBeforeTiles` 里管好 End → 3D → Begin 与层级 RT 的
	//   绑定/恢复。我们**不要**自己去 Begin/End —— 那会和它抢批次。
	//   这与 `Common/Effects/WastelandFx.cs` 里 WastelandFxSystem 自己 Begin/End 的
	//   写法**不同**：那边是纯 SpriteBatch 粒子，必须自己开批次；这边渲染器已经把
	//   批次契约封装了。两边共同遵守的原则是：**不要留下未闭合的批次**。
	// ====================================================================================

	/// <summary>
	/// 3D 探针资源。`[VaultLoaden]` 的路径省略扩展名，`Model3DLoadenHandle`
	/// 会按 `.obj` → `.gltf` 的顺序探测。
	/// <para/>⚠️ 资源扫描只在客户端跑，专用服务器上这个属性始终是 <c>null</c>；
	/// 客户端加载失败时拿到的是 <c>Vault3DModel.Empty</c>。**两种都要判**。
	/// 写成带 setter 的属性而不是字段，免得编译器对「只被反射赋值」的字段报警（0 warnings 要求）。
	/// </summary>
	public static class Wasteland3DProbeAssets
	{
		/// <summary>探针模型：11 个长方体 / 132 三角形 / 264 个去重顶点。</summary>
		[VaultLoaden("Assets/Models/Wasteland3DProbe")]
		public static Vault3DModel Probe { get; set; }
	}

	/// <summary>持握「芯片」时在头顶绘制一个旋转的 3D 机械小机器人。
	/// <para/>纯客户端视觉、纯贴图/几何，**不新增任何本地化键**（只有一条 <c>Logger.Info</c> 证据行）。
	/// </summary>
	public class Wasteland3DProbeSystem : ModSystem
	{
		private static Model3DInstance _instance;
		private static bool _textureTried;
		private static Vector2 _anchor;

		/// <summary>贴图在模组内的路径（不带扩展名）。</summary>
		private const string TexturePath = "WastelandSoul/Assets/Models/Wasteland3DProbe";

		/// <summary>每单位多少像素：探针模型高约 0.96 单位 → 屏幕上约 35px。</summary>
		private const float Scale = 36f;

		public override void Unload()
		{
			// 卸载跑在**加载线程**上：这里只做纯托管清理，绝不碰 GPU。
			// BasicEffect / RenderTarget 由 InnoVault 的 Model3DSystem 通过
			// Main.QueueMainThreadAction 在主线程释放。
			if (_instance != null) {
				Model3DRenderer.UnregisterPersistent(_instance);
				_instance = null;
			}

			_anchor = Vector2.Zero;
			_textureTried = false;
		}

		/// <summary>玩家是否正握着芯片。</summary>
		private static bool HoldingChip()
		{
			Player player = Main.LocalPlayer;

			if (player == null || !player.active || player.dead || player.HeldItem == null) {
				return false;
			}

			return player.HeldItem.type == ModContent.ItemType<Content.Items.Materials.Chip>();
		}

		/// <summary>逻辑帧：把显示锚点定在本地玩家头顶。</summary>
		public override void PostUpdatePlayers()
		{
			if (Main.dedServ || !HoldingChip()) {
				return;
			}

			_anchor = Main.LocalPlayer.Top + new Vector2(0f, -52f);
		}

		/// <summary>
		/// 主线程绘制钩子。**这是唯一允许创建图形资源的地方。**
		/// <para/>`PostDrawTiles` 跑在主线程、且在 `Model3DRenderer` 的
		/// `DrawAfterPlayers` 之前，所以在本帧内注册的常驻实例当帧就会画出来。
		/// <para/>绝不在这里自己 Begin/End：批次契约归 `Model3DRenderer` 管。
		/// </summary>
		public override void PostDrawTiles()
		{
			if (Main.dedServ || Main.gameMenu) {
				return;
			}

			Model3DInstance instance = EnsureInstance();

			if (instance == null) {
				return;
			}

			bool holding = HoldingChip();
			instance.Visible = holding;

			if (holding) {
				instance.Position = _anchor;
			}
		}

		/// <summary>
		/// 懒建实例与贴图。**只能从绘制路径调用** —— 里面会请求贴图（ImmediateLoad），
		/// 并让渲染器创建 BasicEffect 与层级 RenderTarget，全是主线程独占的图形调用。
		/// </summary>
		private static Model3DInstance EnsureInstance()
		{
			if (_instance != null) {
				return _instance;
			}

			Vault3DModel model = Wasteland3DProbeAssets.Probe;

			if (model == null || !model.IsValid) {
				// 资源还没到 / 加载失败。不缓存失败，下一帧继续试。
				return null;
			}

			AttachTexture(model);

			Model3DInstance created = new Model3DInstance(model) {
				// AfterPlayers：画在玩家精灵之后，头顶的东西不会被身体遮住
				Layer = Model3DLayer.AfterPlayers,
				Scale = new Vector3(Scale),
				// BasicEffect 的方向光：不开光照，方块面会糊成一片纯色
				LightingEnabled = true,
				// OBJ 来源的绕序不一定和 XNA 一致，InnoVault 文档也建议关掉剔除
				CullBackface = false,
				// 变换放在 PreDrawInstance 里刷：这个回调每个**渲染**帧都走一次，
				// 高刷屏上自转才顺滑（逻辑帧只有 60Hz）
				PreDrawInstance = static (in Model3DDrawContext ctx) => {
					ctx.Instance.Rotation = new Vector3(0f, Main.GlobalTimeWrappedHourly * 0.9f, 0f);
				},
			};

			if (!Model3DRenderer.RegisterPersistent(created)) {
				// 渲染器还没起来（渲染句柄未注册时 Instance 为 null）。
				// 同样不缓存失败。
				return null;
			}

			_instance = created;
			LogModelInfo(model);
			return _instance;
		}

		/// <summary>把加载结果写进日志 —— 这是"真的加载到了模型"的可核对证据。</summary>
		private static void LogModelInfo(Vault3DModel model)
		{
			Mod mod = ModContent.GetInstance<WastelandSoul>();
			string text = string.Format(
				"[WastelandSoul] 3D 探针加载成功：{0}｜{1} 个网格分组 / {2} 顶点 / {3} 三角形 / {4} 个材质 / 骨骼={5} / 动画={6}",
				model.Name, model.Groups.Count, model.VertexCount, model.TriangleCount,
				model.Materials.Count, model.Skeletons.Count, model.Clips.Count);

			mod.Logger.Info(text);

			if (model.Diagnostic != null && model.Diagnostic.Entries.Count > 0) {
				mod.Logger.Info("[WastelandSoul] 3D 探针导入诊断：" + model.Diagnostic.Format());
			}
		}

		/// <summary>
		/// 把生成好的 PNG 贴图挂到各材质的 <c>DiffuseTexture</c> 上。
		/// <para/>必须在主线程调用（<see cref="AssetRequestMode.ImmediateLoad"/> 会把贴图推上
		/// GPU），所以只在绘制路径里跑。**失败不缓存**，下一帧还会再试 ——
		/// 贴图缺失时模型仍会用 MTL 里的 Kd 纯色画出来，不会整块消失。
		/// </summary>
		private static void AttachTexture(Vault3DModel model)
		{
			if (_textureTried) {
				return;
			}

			try {
				Asset<Texture2D> asset = ModContent.Request<Texture2D>(TexturePath, AssetRequestMode.ImmediateLoad);
				Texture2D texture = asset?.Value;

				if (texture == null) {
					return;
				}

				foreach (Model3DMaterial material in model.Materials.Values) {
					material.DiffuseTexture = texture;
				}

				_textureTried = true;
			} catch (Exception) {
				// 保持 MTL 的 Kd 纯色即可；下一帧重试。
			}
		}
	}
}
