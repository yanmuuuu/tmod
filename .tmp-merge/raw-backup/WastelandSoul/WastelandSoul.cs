using System.IO;
using Terraria.ModLoader;

namespace WastelandSoul
{
	/// <summary>
	/// 模组主类。内部名 WastelandSoul，显示名「废土魂穿」。
	/// </summary>
	public class WastelandSoul : Mod
	{
		/// <summary>
		/// 注册非内容类本地化键（对话 / 提示）。
		/// <para/>内容键由 tModLoader 自动注册，但自定义键必须在加载期注册，
		/// 否则运行时 <c>Language.GetTextValue</c> 只会拿到键名。
		/// </summary>
		public override void Load()
		{
			foreach (string key in Common.WastelandText.CustomKeys) {
				GetLocalization(key);
			}
		}

		/// <summary>
		/// 重载模组时把静态缓存清干净。
		/// <para/>不清的后果见 client.log：`WastelandSoul mod class still using memory` /
		/// `AssemblyLoadContext still using memory` —— 状态机的 per-NPC 上下文缓存是
		/// <c>static readonly Dictionary</c>，会被整个 AssemblyLoadContext 钉住。
		/// </summary>
		public override void Unload()
		{
			Content.NPCs.Bosses.Archivist.ArchivistContext.Clear();
			Content.NPCs.Bosses.Scavenger.ScavengerContext.Clear();
			Content.NPCs.Bosses.AshHeart.AshHeartContext.Clear();
			Content.NPCs.Bosses.FireplaceGuardian.FireplaceGuardianContext.Clear();
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			Common.Systems.WastelandStorySystem.ReceivePacket(reader, whoAmI);
		}
	}
}
