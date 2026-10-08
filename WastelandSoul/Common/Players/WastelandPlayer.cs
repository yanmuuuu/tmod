using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WastelandSoul.Common.Systems;

namespace WastelandSoul.Common.Players
{
	/// <summary>
	/// 玩家个人进度（预留）。
	/// <para/>与世界级的 <see cref="Systems.WastelandStorySystem"/> 分开：这里只记录「这名玩家自己知道/做过什么」，
	/// 用来控制个人对话解锁与任务提示，不会因为别的玩家推进剧情而跳过。
	/// </summary>
	public class WastelandPlayer : ModPlayer
	{
		/// <summary>是否已与智械人正式交谈过（用于首次见面的特殊台词）。</summary>
		public bool metCompanion;

		/// <summary>是否已从智械人处得知「守望者计划」。</summary>
		public bool knowsWatchmanProtocol;

		/// <summary>是否已经听过智械人的第一段记忆回放。</summary>
		public bool heardFirstMemory;

		/// <summary>是否已经发放过开局的「智械核心」。</summary>
		public bool givenCompanionCore;

		/// <summary>已经交付给智械人的灵魂碎片数量（剧情伏笔推进用，最多 4）。</summary>
		public int soulFragmentsDelivered;

		/// <summary>饰品提供的不消耗弹药概率。每帧由饰品重写，在 <see cref="ResetEffects"/> 里清掉。</summary>
		public float ammoSave;

		/// <summary>命中时点燃目标的概率。</summary>
		public float emberOnHit;

		/// <summary>玩家背包里现在带着几枚不同的灵魂碎片（供对话判断）。</summary>
		public int CarriedSoulFragments()
		{
			int count = 0;

			foreach (int type in Content.Items.Soul.SoulFragmentRegistry.AllTypes) {
				if (Player.HasItem(type)) {
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// 进入世界时把「智械核心」放进背包（每个角色只发一次）。
		/// </summary>
		public override void OnEnterWorld()
		{
			if (givenCompanionCore) {
				return;
			}

			givenCompanionCore = true;

			int coreType = ModContent.ItemType<Content.Items.Story.CompanionCore>();

			if (Player.HasItem(coreType)) {
				return;
			}

			Player.QuickSpawnItem(Player.GetSource_Misc("WastelandSoulStart"), coreType);

			if (!Main.dedServ) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.CoreReceived"), 200, 220, 255);
			}
		}

		public override void ResetEffects()
		{
			ammoSave = 0f;
			emberOnHit = 0f;
		}

		public override void PostUpdateEquips()
		{
			if (WastelandStorySystem.endingChoice == WastelandStorySystem.EndingVessel) {
				Player.GetDamage(DamageClass.Generic) += 0.08f;
				Player.lifeRegen += 2;
			}
			else if (WastelandStorySystem.endingChoice == WastelandStorySystem.EndingRefuse) {
				Player.statDefense += 8;
				Player.moveSpeed += 0.08f;
			}
		}

		public override bool CanConsumeAmmo(Item weapon, Item ammo)
		{
			if (ammoSave > 0f && Main.rand.NextFloat() < ammoSave) {
				return false;
			}

			return base.CanConsumeAmmo(weapon, ammo);
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			TryIgnite(target);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			TryIgnite(target);
		}

		private void TryIgnite(NPC target)
		{
			if (emberOnHit > 0f && Main.rand.NextFloat() < emberOnHit) {
				target.AddBuff(BuffID.OnFire3, 180);
			}
		}

		public override void SaveData(TagCompound tag)
		{
			if (metCompanion) {
				tag["metCompanion"] = true;
			}

			if (knowsWatchmanProtocol) {
				tag["watchman"] = true;
			}

			if (heardFirstMemory) {
				tag["heardMemory1"] = true;
			}

			if (givenCompanionCore) {
				tag["givenCore"] = true;
			}

			if (soulFragmentsDelivered > 0) {
				tag["souls"] = soulFragmentsDelivered;
			}
		}

		public override void LoadData(TagCompound tag)
		{
			metCompanion = tag.GetBool("metCompanion");
			knowsWatchmanProtocol = tag.GetBool("watchman");
			heardFirstMemory = tag.GetBool("heardMemory1");
			givenCompanionCore = tag.GetBool("givenCore");
			soulFragmentsDelivered = tag.GetInt("souls");
		}

		/// <summary>
		/// 联机同步用：这几个标志都是**服务端说了算**的（客户端改了会被服务器的旧值覆盖回去），
		/// 所以要先把客户端的当前值抄给"服务器状态副本"，让服务器看得出差异。
		/// </summary>
		public override void CopyClientState(ModPlayer targetCopy)
		{
			WastelandPlayer clone = (WastelandPlayer)targetCopy;

			clone.metCompanion = metCompanion;
			clone.knowsWatchmanProtocol = knowsWatchmanProtocol;
			clone.heardFirstMemory = heardFirstMemory;
			clone.givenCompanionCore = givenCompanionCore;
			clone.soulFragmentsDelivered = soulFragmentsDelivered;
		}

		/// <summary>比对副本，发现差异就把这名玩家的个人进度发给服务端。</summary>
		public override void SendClientChanges(ModPlayer clientPlayer)
		{
			WastelandPlayer clone = (WastelandPlayer)clientPlayer;

			if (clone.metCompanion != metCompanion
				|| clone.knowsWatchmanProtocol != knowsWatchmanProtocol
				|| clone.heardFirstMemory != heardFirstMemory
				|| clone.givenCompanionCore != givenCompanionCore
				|| clone.soulFragmentsDelivered != soulFragmentsDelivered) {
				SyncPlayer(toWho: -1, fromWho: Main.myPlayer, newPlayer: false);
			}
		}
	}
}
