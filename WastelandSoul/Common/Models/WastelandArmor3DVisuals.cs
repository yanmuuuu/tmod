using InnoVault;
using InnoVault.Models3D.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;
using WastelandSoul.Common.Configs;
using WastelandSoul.Content.Items.Armor;

namespace WastelandSoul.Common.Models
{
	// ====================================================================================
	// 精钢护甲三件的 3D 穿身视觉（InnoVault `Models3D`）
	//
	// ■ 为什么走"玩家画完再摆模型"这条路
	//   本工程的护甲是普通 `ModItem` + `[AutoloadEquip(EquipType.Head/Body/Legs)]`，
	//   穿身外观由 tModLoader 按 `<类名>_Head.png` / `_Body.png` / `_Legs.png` 找 EquipTexture
	//   贴图（这些贴图现在是**全透明**的，`tools/hide_armor_visuals.py` 干的就是这件事）。
	//   要把它们换成 3D，有两条路：
	//     (a) 接管 EquipTexture / DrawArmorColor —— 要动 tML 的装备贴图体系，风险大；
	//     (b) **不动贴图体系**，在玩家精灵画完之后，按玩家位置/朝向自己把模型摆上去。
	//   这里选 (b)：`Model3DLayer.AfterPlayers` 这一层就是 InnoVault 给"覆盖在玩家身上
	//   的模型（载具、武器等）"准备的（见 `Model3DLayer` 的注释），语义正好对上，
	//   而且完全不需要改护甲物品本身（数值/配方/套装效果一个字节都不碰）。
	//
	// ■ 生命周期：常驻实例，不用每帧 Submit
	//   `Model3DRenderer.RegisterPersistent(instance)` 注册的实例**每帧都会被绘制**，
	//   直到 `UnregisterPersistent`。所以：
	//     * 实例只在"这个玩家真的穿着精钢护甲"时懒建一次（见 TryCreate）；
	//     * 每帧只刷新 `Position / Scale / Rotation / Visible`；
	//     * 没穿 / 开关关掉时把 `Visible` 设成 false —— 渲染器在分桶阶段就会跳过它，
	//       连自己的 RenderTarget 都不会建（`DrawLayerInternal` 在桶为空时直接 return）。
	//   之所以不用每帧 `Submit`（临时实例）：常驻实例的可见性由字段控制，不依赖
	//   提交时机与"帧末清空临时桶"的先后顺序，少一个可能出错的假设。
	//
	//   两个钩子分工（`SyncVisuals`）：
	//     * `PostUpdatePlayers`（逻辑帧，一定在任何绘制之前）→ 只摆位置；
	//     * `PostDrawTiles`（主线程绘制帧）→ 摆位置 + 懒建实例与贴图。
	//   这样"模型跟着玩家跑"不依赖绘制钩子的先后顺序，而 GPU 创建仍然只发生在绘制路径上。
	//
	// ■ 加载期线程安全（硬性要求，踩过事故）
	//   `[VaultLoaden]` 走的 `ObjModelLoader` 只解析**纯文本几何**，不创建 GPU 资源；
	//   MTL 里故意没有 `map_Kd`，所以贴图不会被 `TryLoadTexture`（`ImmediateLoad`）
	//   在加载线程上推上 GPU。贴图改由 `AttachTextureIfNeeded` 在**第一次绘制（主线程）**挂。
	//   本文件里所有 `new Model3DInstance` / 贴图请求都只在绘制路径上发生。
	//
	// ■ SpriteBatch 批次
	//   `Model3DRenderer` 自己管批次（End → 画到自己的 RT → 合成 → Begin），
	//   我们**不要**自己 Begin/End —— 那会和它抢批次，也会违反 check_batch_usage。
	// ====================================================================================

	/// <summary>
	/// 三个 OBJ 资源。
	/// <para/>⚠️ 资源扫描只在客户端跑：专用服务器上拿到的是 <c>Vault3DModel.Empty</c>
	/// （`Model3DLoadenHandle.GetDefaultValue`），客户端加载失败时也是它。**两种都要判**
	/// —— 判空统一用 <c>model == null || !model.IsValid</c>。
	/// <para/>写成带 setter 的属性而不是字段，免得编译器对"只被反射赋值"的字段报警（0 warnings 要求）。
	/// </summary>
	public static class WastelandArmor3DAssets
	{
		/// <summary>头盔：6 个长方体 / 72 三角形。</summary>
		[VaultLoaden("Assets/Models/SalvagedSteelHelmet")]
		public static Vault3DModel Helmet { get; set; }

		/// <summary>胸甲：7 个长方体 / 84 三角形。</summary>
		[VaultLoaden("Assets/Models/SalvagedSteelChestplate")]
		public static Vault3DModel Chestplate { get; set; }

		/// <summary>护腿：7 个长方体 / 84 三角形。</summary>
		[VaultLoaden("Assets/Models/SalvagedSteelGreaves")]
		public static Vault3DModel Greaves { get; set; }
	}

	/// <summary>
	/// ★★★ 摆放参数全部集中在这里 ★★★ —— 第一次进游戏看效果后，**只改这一块**。
	/// <para/>坐标系（`Model3DRenderer.BuildWorldMatrix` 的口径）：
	/// <c>Position</c> 是**世界坐标（像素）**，渲染时自动减掉 <c>Main.screenPosition</c>；
	/// 屏幕 X 向右为 +X，屏幕 Y 向下为 +Y（所以"往上挪"是负数）。
	/// <para/>模型的 Y 轴：OBJ 里是 Y 朝上，InnoVault 导入时翻成 Y 朝下
	/// （`ObjAxisConvention.YUpToYDown`），所以 OBJ 里"在原点上方 0.2 单位"的部件，
	/// 落到这里就是"比锚点高 0.2 单位"。**不要自己再翻一次**。
	/// <para/>方向 / 深度：模型正面（+Z：胸甲核心、头盔面罩）朝向 **屏幕外**，也就是
	/// 默认就能看见零件最丰富的那一面。Z 越大越靠观察者（深度测试 Less + 这个正交投影
	/// 下 z 大的一侧胜出），`Depth` 字段就是给"两件模型互相穿插"时调先后用的。
	/// </summary>
	public static class Armor3DTuning
	{
		// ==================== 全局 ====================

		/// <summary>每 1 个模型单位 = 多少像素。34 是按"胸甲宽 0.52 单位 ≈ 玩家躯干 20px"估的。</summary>
		public const float PixelsPerUnit = 34f;

		/// <summary>绘制层。AfterPlayers = 玩家精灵之后，正好是"穿在身上的东西"。</summary>
		public const Model3DLayer RenderLayer = Model3DLayer.AfterPlayers;

		/// <summary>模型绕 Y 轴的固定朝向（弧度）。0 = 正面朝屏幕外；±1.5708 就是让"侧面"朝屏幕外。</summary>
		public const float Yaw = 0f;

		// ==================== 头盔 ====================

		/// <summary>头盔缩放倍率（乘在 <see cref="PixelsPerUnit"/> 上）。</summary>
		public const float HelmetScale = 1f;

		/// <summary>头盔锚点相对 <c>player.Center</c> 的偏移（像素）。Y 为负 = 往上。</summary>
		public static readonly Vector2 HelmetOffset = new Vector2(0f, -12f);

		/// <summary>头盔 Z 深度微调。</summary>
		public const float HelmetDepth = 0f;

		// ==================== 胸甲 ====================

		/// <summary>胸甲缩放倍率。</summary>
		public const float ChestScale = 1f;

		/// <summary>胸甲锚点相对 <c>player.Center</c> 的偏移（像素）。</summary>
		public static readonly Vector2 ChestOffset = new Vector2(0f, 1f);

		/// <summary>胸甲 Z 深度微调。</summary>
		public const float ChestDepth = 0f;

		// ==================== 护腿 ====================

		/// <summary>护腿缩放倍率。</summary>
		public const float GreavesScale = 1f;

		/// <summary>护腿锚点相对 <c>player.Center</c> 的偏移（像素）。</summary>
		public static readonly Vector2 GreavesOffset = new Vector2(0f, 5f);

		/// <summary>护腿 Z 深度微调。</summary>
		public const float GreavesDepth = 0f;

		// ==================== 动态小动作（不想要就把系数设成 0）====================

		/// <summary>横向速度 → 侧倾（弧度 / 每像素每帧的速度）。符号不对就把这里改成负的。</summary>
		public const float RollPerVelocityX = 0.010f;

		/// <summary>纵向速度 → 前后倾（弧度 / 每像素每帧的速度）。符号不对就把这里改成负的。</summary>
		public const float PitchPerVelocityY = 0.008f;

		/// <summary>走路时上下起伏的幅度（像素）。0 = 完全不抖。</summary>
		public const float WalkBobAmplitude = 1.2f;

		/// <summary>走路起伏的速度（弧度 / 秒）。</summary>
		public const float WalkBobSpeed = 9f;

		/// <summary>速度参与倾斜计算时的上限（像素/帧），免得冲刺时模型翻过去。</summary>
		public const float MaxLeanVelocity = 10f;
	}

	/// <summary>
	/// 精钢护甲三件的 3D 穿身视觉系统。
	/// <para/>客户端视觉 + 由 <see cref="WastelandVisualConfig.Enable3DArmorVisuals"/>（默认 false）控制。
	/// 关闭 / 玩家没穿 / 专用服务器 / 菜单界面，这四种情况都不画。
	/// </summary>
	public class WastelandArmor3DSystem : ModSystem
	{
		/// <summary>玩家槽位上限。<c>Main.player</c> 的长度是 255，这里写死免得静态初始化踩到未就绪的 Main。</summary>
		private const int PlayerSlots = 255;

		/// <summary>贴图在模组内的路径（不带扩展名），主线程懒加载时用。</summary>
		private const string HelmetTexturePath = "WastelandSoul/Assets/Models/SalvagedSteelHelmet";
		private const string ChestplateTexturePath = "WastelandSoul/Assets/Models/SalvagedSteelChestplate";
		private const string GreavesTexturePath = "WastelandSoul/Assets/Models/SalvagedSteelGreaves";

		/// <summary>每个玩家槽位的模型实例（懒建：只有真的穿精钢护甲才会建）。</summary>
		private static readonly PlayerArmorVisual[] Visuals = new PlayerArmorVisual[PlayerSlots];

		/// <summary>失败日志只打一次，避免绘制异常时每帧刷屏。</summary>
		private static bool _errorLogged;

		/// <summary>一个玩家的三件模型实例。任意一件加载/注册失败时对应字段保持 null，下一帧重试。</summary>
		private sealed class PlayerArmorVisual
		{
			public Model3DInstance Helmet;
			public Model3DInstance Chestplate;
			public Model3DInstance Greaves;
		}

		/// <summary>一件护甲的摆放参数。</summary>
		private readonly struct PieceLayout
		{
			public readonly Vector2 Offset;
			public readonly float Scale;
			public readonly float Depth;

			public PieceLayout(Vector2 offset, float scale, float depth)
			{
				Offset = offset;
				Scale = scale;
				Depth = depth;
			}
		}

		private static readonly PieceLayout HelmetLayout =
			new PieceLayout(Armor3DTuning.HelmetOffset, Armor3DTuning.HelmetScale, Armor3DTuning.HelmetDepth);

		private static readonly PieceLayout ChestLayout =
			new PieceLayout(Armor3DTuning.ChestOffset, Armor3DTuning.ChestScale, Armor3DTuning.ChestDepth);

		private static readonly PieceLayout GreavesLayout =
			new PieceLayout(Armor3DTuning.GreavesOffset, Armor3DTuning.GreavesScale, Armor3DTuning.GreavesDepth);

		public override void Unload()
		{
			// 卸载跑在**加载线程**上：这里只做纯托管清理（把实例从渲染器里摘掉），
			// 绝不碰 GPU。BasicEffect / RenderTarget 由 InnoVault 的 Model3DSystem 在主线程释放。
			for (int i = 0; i < Visuals.Length; i++) {
				PlayerArmorVisual visual = Visuals[i];

				if (visual == null) {
					continue;
				}

				Model3DRenderer.UnregisterPersistent(visual.Helmet);
				Model3DRenderer.UnregisterPersistent(visual.Chestplate);
				Model3DRenderer.UnregisterPersistent(visual.Greaves);
				Visuals[i] = null;
			}

			_errorLogged = false;
		}

		/// <summary>
		/// 逻辑帧刷新：把三件模型的位置/可见性对齐到玩家。
		/// <para/>为什么位置刷新放在**更新阶段**而不是只在绘制阶段：逻辑帧一定在
		/// 任何绘制钩子之前，所以"模型跟着玩家跑"这件事不依赖
		/// `PostDrawTiles` 与 `Model3DRenderer.DrawAfterPlayers` 谁先谁后
		/// —— 万一顺序反了，模型也不会有 1 帧延迟（跑动时会看出来"护甲掉在后面"）。
		/// <para/>这里**不创建**任何图形资源（<paramref name="createMissing"/> = false），
		/// 创建统一留给绘制路径上的 <see cref="PostDrawTiles"/>。
		/// </summary>
		public override void PostUpdatePlayers()
		{
			SyncVisuals(createMissing: false);
		}

		/// <summary>
		/// 绘制帧刷新 + 懒建。选 <c>PostDrawTiles</c> 的理由：它跑在**主线程**，
		/// 而且在 `Model3DRenderer` 的 `DrawAfterPlayers` **之前**
		/// （探针 Wasteland3DPreview 就是这么做的），所以这一帧新建的实例当帧就能画出来；
		/// 同时它是本项目约定里唯一允许创建图形资源的地方（贴图 `ImmediateLoad`、
		/// `Model3DInstance` 懒建都在这里）。
		/// </summary>
		public override void PostDrawTiles()
		{
			SyncVisuals(createMissing: true);
		}

		/// <summary>
		/// 遍历所有玩家槽位，把已经存在的实例摆好、把不需要的藏起来。
		/// <para/><paramref name="createMissing"/> 为 true 时才允许懒建实例
		/// （那一步会请求贴图 = 主线程 GPU 调用）。
		/// <para/>整个循环外面套 try/catch：任何异常都只跳过本帧并记一条日志，
		/// 绝不让主线程绘制流程抛出去（那会让 tML 判定 Main engine crash）。
		/// </summary>
		private void SyncVisuals(bool createMissing)
		{
			if (Main.dedServ || Main.gameMenu || Main.player == null) {
				return;
			}

			try {
				bool enabled = WastelandVisualConfig.Armor3DEnabled;
				int count = Math.Min(PlayerSlots, Main.player.Length);

				for (int i = 0; i < count; i++) {
					Player player = Main.player[i];
					bool alive = player != null && player.active && !player.dead;

					if (!enabled || !alive) {
						Hide(i);
						continue;
					}

					bool helmet = IsSalvagedSteelArmor(player.armor[0]);
					bool chestplate = IsSalvagedSteelArmor(player.armor[1]);
					bool greaves = IsSalvagedSteelArmor(player.armor[2]);

					if (!helmet && !chestplate && !greaves) {
						Hide(i);
						continue;
					}

					PlayerArmorVisual visual = Visuals[i];

					// 更新阶段（createMissing=false）不建容器：这一帧还没人需要它，
					// 等绘制阶段真看到"穿着护甲"时再建。
					if (visual == null) {
						if (!createMissing) {
							continue;
						}

						visual = new PlayerArmorVisual();
						Visuals[i] = visual;
					}

					// 动态小动作：走路起伏 + 速度侧倾。三个模型共用同一套值，
					// 免得头盔和胸甲各抖各的、看起来像散架。
					float bob = WalkBob(player);
					float pitch = MathHelper.Clamp(player.velocity.Y, -Armor3DTuning.MaxLeanVelocity, Armor3DTuning.MaxLeanVelocity)
						* Armor3DTuning.PitchPerVelocityY;
					float roll = -MathHelper.Clamp(player.velocity.X, -Armor3DTuning.MaxLeanVelocity, Armor3DTuning.MaxLeanVelocity)
						* Armor3DTuning.RollPerVelocityX;

					if (helmet) {
						if (visual.Helmet == null && createMissing) {
							visual.Helmet = TryCreate(WastelandArmor3DAssets.Helmet, HelmetTexturePath);
						}

						if (visual.Helmet != null) {
							Place(visual.Helmet, player, HelmetLayout, bob, pitch, roll);
						}
					}
					else if (visual.Helmet != null) {
						visual.Helmet.Visible = false;
					}

					if (chestplate) {
						if (visual.Chestplate == null && createMissing) {
							visual.Chestplate = TryCreate(WastelandArmor3DAssets.Chestplate, ChestplateTexturePath);
						}

						if (visual.Chestplate != null) {
							Place(visual.Chestplate, player, ChestLayout, bob, pitch, roll);
						}
					}
					else if (visual.Chestplate != null) {
						visual.Chestplate.Visible = false;
					}

					if (greaves) {
						if (visual.Greaves == null && createMissing) {
							visual.Greaves = TryCreate(WastelandArmor3DAssets.Greaves, GreavesTexturePath);
						}

						if (visual.Greaves != null) {
							Place(visual.Greaves, player, GreavesLayout, bob, pitch, roll);
						}
					}
					else if (visual.Greaves != null) {
						visual.Greaves.Visible = false;
					}
				}
			}
			catch (Exception exception) {
				LogOnce(exception);
			}
		}

		/// <summary>这个装备槽里的物品是不是精钢护甲（六个职业的精钢套装共用一个基类）。</summary>
		private static bool IsSalvagedSteelArmor(Item item)
		{
			return item != null && !item.IsAir && item.ModItem is SalvagedSteelArmor;
		}

		/// <summary>把这个玩家的三件模型全部藏起来（实例保留，不销毁也不重建）。</summary>
		private static void Hide(int index)
		{
			PlayerArmorVisual visual = Visuals[index];

			if (visual == null) {
				return;
			}

			if (visual.Helmet != null) {
				visual.Helmet.Visible = false;
			}

			if (visual.Chestplate != null) {
				visual.Chestplate.Visible = false;
			}

			if (visual.Greaves != null) {
				visual.Greaves.Visible = false;
			}
		}

		/// <summary>
		/// 走路时的上下起伏（像素）。站着 / 在空中 / 幅度设为 0 都是 0
		/// （幅度为 0 时 sin 乘 0 自然就是 0，所以这里**不要**写
		/// <c>if (WalkBobAmplitude &lt;= 0f) return 0f;</c> —— 常量为正常数时那句话
		/// 会被编译器判定为"不可达代码"（CS0162），而本工程要求 0 warnings）。
		/// </summary>
		private static float WalkBob(Player player)
		{
			if (player.velocity.X == 0f || player.velocity.Y != 0f) {
				return 0f;
			}

			return MathF.Sin((float)Main.GlobalTimeWrappedHourly * Armor3DTuning.WalkBobSpeed)
				* Armor3DTuning.WalkBobAmplitude;
		}

		/// <summary>刷新一件模型的摆放（每帧调用，纯字段写入，不涉及 GPU）。</summary>
		private static void Place(Model3DInstance instance, Player player, in PieceLayout layout
			, float bob, float pitch, float roll)
		{
			instance.Visible = true;
			instance.Position = player.Center + layout.Offset + new Vector2(0f, bob);
			instance.Scale = new Vector3(Armor3DTuning.PixelsPerUnit * layout.Scale);
			instance.Depth = layout.Depth;
			instance.Rotation = new Vector3(pitch, Armor3DTuning.Yaw, roll);
		}

		/// <summary>
		/// 懒建一个常驻实例。**只能从绘制路径调用** —— 里面会请求贴图（<c>ImmediateLoad</c>
		/// 会把贴图推上 GPU），并让渲染器在首次绘制时创建 BasicEffect 与层级 RenderTarget，
		/// 全是主线程独占的图形调用。
		/// <para/>资源没到 / 加载失败（<c>Vault3DModel.Empty</c>）/ 渲染器还没起来，都返回 null
		/// 并且**不缓存失败**，下一帧继续试。
		/// </summary>
		private static Model3DInstance TryCreate(Vault3DModel model, string texturePath)
		{
			if (model == null || !model.IsValid) {
				return null;
			}

			AttachTextureIfNeeded(model, texturePath);

			Model3DInstance instance = new Model3DInstance(model) {
				Layer = Armor3DTuning.RenderLayer,
				// 建出来先藏着：这一帧的 Place() 会决定要不要显示
				Visible = false,
				// BasicEffect 的方向光：不开光照，方块面会糊成一片纯色
				LightingEnabled = true,
				// OBJ 来源的绕序不一定和 XNA 一致，InnoVault 文档也建议关掉剔除
				CullBackface = false,
				DepthEnabled = true,
			};

			if (!Model3DRenderer.RegisterPersistent(instance)) {
				// 渲染句柄还没注册（Instance 为 null）或层号非法：同样不缓存失败。
				return null;
			}

			return instance;
		}

		/// <summary>
		/// 把生成好的 PNG 贴图挂到各材质的 <c>DiffuseTexture</c> 上。
		/// <para/>必须在主线程调用（<c>AssetRequestMode.ImmediateLoad</c> 会把贴图推上 GPU）。
		/// 判"已经挂过"用材质自己的 <c>HasTexture</c>，所以**不需要额外的标志位**，
		/// 而且跨世界、跨重载都不会重复请求。
		/// <para/>失败时保持 MTL 里的 Kd 纯色（模型不会整块消失），下一次仍会重试。
		/// </summary>
		private static void AttachTextureIfNeeded(Vault3DModel model, string texturePath)
		{
			foreach (Model3DMaterial material in model.Materials.Values) {
				if (material != null && material.HasTexture) {
					return;
				}
			}

			try {
				Asset<Texture2D> asset = ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad);
				Texture2D texture = asset?.Value;

				if (texture == null) {
					return;
				}

				foreach (Model3DMaterial material in model.Materials.Values) {
					if (material != null) {
						material.DiffuseTexture = texture;
					}
				}
			}
			catch (Exception) {
				// 贴图缺失不该影响几何：保持 Kd 纯色，下一帧重试。
			}
		}

		/// <summary>
		/// 绘制异常只记一次日志（防刷屏）。
		/// <para/>写成**实例**方法就是为了能用 <c>Mod.Logger</c>：<c>Mod</c> 是
		/// <c>ModType</c> 的实例属性，静态方法里访问会报 CS0120。
		/// </summary>
		private void LogOnce(Exception exception)
		{
			if (_errorLogged) {
				return;
			}

			_errorLogged = true;
			Mod.Logger.Warn("[WastelandSoul] 3D 护甲绘制出错，本帧已跳过（后续同类错误不再重复记录）：" + exception);
		}
	}
}
