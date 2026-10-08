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

		/// <summary>
		/// 模组包的**唯一**入口（tModLoader 在 <c>ModNet.HandleModPacket</c> 里调它）。
		///
		/// <para/>⚠️ tModLoader 会自己核对"读完的字节数 == 本模组这一段负载的字节数"，
		/// 不相等就抛 <c>IOException: Read underflow N of M bytes caused by WastelandSoul in HandlePacket</c>
		/// —— 玩家实测的联机崩溃就是这里抛出来的（根因见 <see cref="Common.Systems.WastelandNet"/>）。
		/// 所以：
		/// <list type="bullet">
		/// <item>所有读都必须走 <c>Common.Systems.NetReader</c>（显式宽度 + 有界，读不够就记日志返回，不抛异常）；</item>
		/// <item>所有写都必须走 <c>Common.Systems.NetWriter</c>（显式宽度，不再靠重载决议猜）；</item>
		/// <item>⚠️ tModLoader 的 <c>Mod.HandlePacket</c> 只给 <c>(reader, whoAmI)</c> 两个参数，
		/// **拿不到** <c>ModNet.HandleModPacket</c> 里那个 <c>length</c>（那是 internal），
		/// 所以最外层不传字节预算（= 不预判），由 <c>NetReader</c> 在真正读不到时兜底。</item>
		/// </list>
		/// </summary>
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			Common.Systems.WastelandStorySystem.ReceivePacket(reader, whoAmI);
		}
	}
}
