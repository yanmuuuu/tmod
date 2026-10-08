using System.IO;
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
			// 联机：进世界时先把本机这份个人进度主动推一次。
			// 为什么必须主动推：SendClientChanges 只在"值发生变化"时才发，
			// 而像 givenCompanionCore / soulFragmentsDelivered 这种**上一次存档就置位**的字段
			// 进世界后不会再变，不推的话服务端镜像会一直停在默认值。
			// ⚠️ 必须放在下面那个提前 return **之前**。
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				SyncPlayer(toWho: -1, fromWho: Main.myPlayer, newPlayer: true);
			}

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
		///
		/// <para/>⚠️ 本轮复核结论：这 5 个字段就是**全部**会影响体验的个人进度
		/// （<c>ammoSave</c> / <c>emberOnHit</c> 是每帧由饰品重写的临时值，不需要同步），
		/// 所以同步面本来就是齐的。真正缺的是 <see cref="SyncPlayer"/> —— 见那里的说明。
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

		/// <summary>
		/// 把个人进度发给服务端（**客户端 → 服务端**单向）。
		///
		/// <para/>⚠️ 为什么必须重写它：tModLoader 里 <see cref="ModPlayer.SyncPlayer"/> 的基类是
		/// **空实现**（Cecil 反编译 tModLoader.dll 已核：方法体只有一条 <c>ret</c>），
		/// 也就是说上面 <see cref="SendClientChanges"/> 里那句 <c>SyncPlayer(...)</c>
		/// 在改之前**什么都没发** —— 服务端永远拿不到这名玩家的个人进度
		/// （于是服务端那边 <c>ShouldGrantSoulFragment</c> 之类的判定一直在用默认值）。
		///
		/// <para/>方向也是刻意的：**只**由客户端发给服务端，服务端**不**回发。
		/// 这样服务器的旧副本不可能反过来把玩家自己的进度覆盖掉
		/// （玩家进度以客户端存档为准，服务端只留一份"用来判定"的镜像）。
		///
		/// <para/>=== 🔴 本轮联机崩溃的根因就在这个方法里 ===
		/// 修前的写法是 <c>packet.Write(WastelandStorySystem.PacketKindPlayerProgress);</c>：
		/// 实参虽然是 <c>byte</c> 常量，但 <c>ModPacket</c>/<c>BinaryWriter</c> 的重载决议会选中
		/// <c>Write(int)</c>，于是**写出 4 个字节**；而服务端
		/// <see cref="WastelandStorySystem.ReceivePacket"/> 是用 <c>ReadByte()</c> 读类型字节的，
		/// 只消费 1 个字节 —— 一读一写差 3 字节，服务端每收到一次本包就在
		/// <c>ModNet.HandleModPacket</c> 抛一次
		/// <c>IOException: Read underflow N of M bytes caused by WastelandSoul in HandlePacket</c>，
		/// 并把这条 socket 的报文流位置带偏（日志里那两条 6 / 9 字节的 underflow 就是这么来的）。
		/// 玩家身上的表现就是"网络加载失败、子世界没有方块、被弹回主世界"。
		///
		/// <para/>现在类型字节一律走 <see cref="NetWriter.WriteByte"/>（恒定 1 字节），
		/// 与 <see cref="ReceiveProgress"/> 的字段表逐字节对齐：
		/// <code>
		/// kind(1) + metCompanion(1) + knowsWatchmanProtocol(1) + heardFirstMemory(1)
		///         + givenCompanionCore(1) + soulFragmentsDelivered(4) = 9 字节
		/// </code>
		/// </summary>
		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				return;
			}

			ModPacket packet = Mod.GetPacket();

			if (packet == null) {
				return;
			}

			NetWriter writer = new NetWriter(packet);
			writer.WriteByte(WastelandStorySystem.PacketKindPlayerProgress);
			writer.WriteBool(metCompanion);
			writer.WriteBool(knowsWatchmanProtocol);
			writer.WriteBool(heardFirstMemory);
			writer.WriteBool(givenCompanionCore);
			writer.WriteInt32(soulFragmentsDelivered);

			// 兜底断言：人工登记 9 字节，实际写出不是 9 就写一条【协议漂移】日志。
			WastelandNet.AssertBytes("个人进度 SyncPlayer()", WastelandStorySystem.PacketKindPlayerProgress, 9, writer.Count);

			packet.Send(toWho, fromWho);
		}

		/// <summary>
		/// **服务端**收到某名玩家发来的个人进度。
		/// <para/>发送者身份一律用 <c>HandlePacket</c> 给的 <paramref name="whoAmI"/>，
		/// 不信包里任何"我是几号玩家"的说法（否则可以改别人的进度）。
		/// </summary>
		/// <param name="remaining">
		/// 本模组这一段负载的总字节数（见 <see cref="WastelandNet.ModPayloadLength"/>）。
		/// 只用做兜底：读之前先看够不够，**绝不让 <c>ReadInt32</c> 之类抛 IOException**
		/// —— 抛出去 tModLoader 会丢掉整条 socket 的报文流。
		/// </param>
		public static void ReceiveProgress(NetReader reader, int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers) {
				return;
			}

			Player player = Main.player[whoAmI];

			if (player == null || !player.active) {
				return;
			}

			WastelandPlayer modPlayer = player.GetModPlayer<WastelandPlayer>();

			// ⚠️ 逐字段与 SyncPlayer 的写入顺序一一对应，任何一步字节不够都立刻整体放弃
			// （不写一半：半套进度比不写更容易出怪事）。
			if (!reader.TryReadBool("progress.metCompanion", out bool met)
				|| !reader.TryReadBool("progress.watchman", out bool watchman)
				|| !reader.TryReadBool("progress.heardMemory1", out bool heard)
				|| !reader.TryReadBool("progress.givenCore", out bool core)
				|| !reader.TryReadInt32("progress.souls", out int souls)) {
				return;
			}

			modPlayer.metCompanion = met;
			modPlayer.knowsWatchmanProtocol = watchman;
			modPlayer.heardFirstMemory = heard;
			modPlayer.givenCompanionCore = core;
			// 夹一下范围：这一份只是"用来判定"的镜像，别让改过的客户端塞个天文数字进来
			modPlayer.soulFragmentsDelivered = Utils.Clamp(souls, 0, WastelandMemorySystem.TotalMemories);
		}
	}
}
