using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace WastelandSoul.Common.Configs
{
	/// <summary>
	/// 纯客户端**视觉**开关。
	/// <para/>为什么必须是 ClientSide：这些选项只决定"我这台机器上怎么画"，
	/// 不影响伤害/刷怪/掉落，也不需要服务器同意。ServerSide 会把设置同步给所有人
	/// （<see cref="WastelandConfig"/> 是那种：影响刷怪与掉落，以服务器为准），
	/// 渲染开关放那里语义不对，而且单人以外的玩家没法自己关掉。
	/// <para/>配置项的显示名走自动本地化键
	/// <c>Mods.WastelandSoul.Configs.WastelandVisualConfig.&lt;成员名&gt;.Label</c>
	/// / <c>.Tooltip</c>（英文在主模组 en-US，中文在汉化补丁 WastelandSoulCN）。
	/// </summary>
	public class WastelandVisualConfig : ModConfig
	{
		/// <summary>全局实例，供绘制路径读取。<para/>专用服务器上是 null，读取方必须判空。</summary>
		public static WastelandVisualConfig Instance { get; private set; }

		public override ConfigScope Mode => ConfigScope.ClientSide;

		public override void OnLoaded()
		{
			Instance = this;
		}

		/// <summary>
		/// 把精钢护甲三件（头盔 / 胸甲 / 护腿）画成 3D 模型贴在玩家身上。
		/// <para/><b>默认 false</b>：关闭时完全走原来的行为 —— 护甲的穿身贴图本来就是
		/// 全透明的，所以"关掉"等于"看不见护甲"，这一点不要改。
		/// <para/>打开后由 <c>WastelandSoul.Common.Models.WastelandArmor3DSystem</c>
		/// 在玩家绘制之后按玩家位置摆三个 <c>Model3DInstance</c>。
		/// </summary>
		[DefaultValue(false)]
		public bool Enable3DArmorVisuals { get; set; } = false;

		// ---------- 读取用的安全封装（配置还没加载 / 专用服务器上回落到"关"）----------

		/// <summary>3D 护甲穿身视觉是否启用。</summary>
		public static bool Armor3DEnabled => Instance?.Enable3DArmorVisuals ?? false;
	}
}
