using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace WastelandSoul.Common.Configs
{
	/// <summary>
	/// 模组配置：会出现在 tModLoader 的「模组配置」界面（QoL 模组如「更好的体验」也能从这里打开各模组的配置）。
	/// <para/>用 ServerSide：这些选项会影响刷怪/掉落，多人时以服务器为准并自动下发给客户端。
	/// <para/>配置项的显示名走自动本地化键
	/// <c>Mods.WastelandSoul.Configs.WastelandConfig.&lt;成员名&gt;.Label</c>
	/// （英文在主模组，中文在汉化补丁 WastelandSoulCN）。
	/// </summary>
	public class WastelandConfig : ModConfig
	{
		/// <summary>全局实例，供各处读取。</summary>
		public static WastelandConfig Instance { get; private set; }

		public override ConfigScope Mode => ConfigScope.ServerSide;

		public override void OnLoaded()
		{
			Instance = this;
		}

		[DefaultValue(true)]
		public bool NoLootOnSelfDestruct { get; set; } = true;

		// ---------- 读取用的安全封装（配置尚未加载时回落到默认值）----------

		public static bool SelfDestructGivesNoLoot => Instance?.NoLootOnSelfDestruct ?? true;
	}
}
