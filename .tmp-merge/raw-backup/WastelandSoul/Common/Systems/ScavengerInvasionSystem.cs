using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WastelandSoul.Common.Configs;
using WastelandSoul.Content.NPCs.Bosses.Scavenger;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 清道夫定期来袭。
	/// <para/>世界创建后每 5 天一次：当天 19:30 传出预警（耳边刺耳的电流声、空气里的血腥气味），20:30 正式生成。
	/// <para/>玩家真正击败它、推进剧情后停止定期来袭；之后想再打就用「清道夫信号传感器」召唤。
	/// </summary>
	public class ScavengerInvasionSystem : ModSystem
	{
		/// <summary>来袭周期（天）。世界创建当天记为第 1 天。（可在模组配置里调整）</summary>
		public const int DefaultCycleDays = 5;

		/// <summary>夜晚起点 19:30——此时 <see cref="Main.time"/> 归零。</summary>
		private const double NightStartHour = 19.5;

		/// <summary>预警时刻：19:30。</summary>
		private const float WarningHour = 19.5f;

		/// <summary>生成时刻：20:30。</summary>
		private const float SpawnHour = 20.5f;

		/// <summary>世界天数（第 1 天 = 世界创建当天，每天黎明 +1）。</summary>
		public static int WorldDay = 1;

		// ---------- 以下为运行期状态，不存盘 ----------
		private static bool warningDoneTonight;
		private static bool spawnDoneTonight;
		private static bool wasNight;

		public override void ClearWorld()
		{
			WorldDay = 1;
			ResetTonight();
			wasNight = false;
		}

		private static void ResetTonight()
		{
			warningDoneTonight = false;
			spawnDoneTonight = false;
		}

		// ==================== 存档与同步 ====================

		public override void SaveWorldData(TagCompound tag)
		{
			tag["day"] = WorldDay;
		}

		public override void LoadWorldData(TagCompound tag)
		{
			WorldDay = tag.GetInt("day");

			if (WorldDay < 1) {
				WorldDay = 1;
			}

			ResetTonight();
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(WorldDay);
		}

		public override void NetReceive(BinaryReader reader)
		{
			WorldDay = reader.ReadInt32();
			ResetTonight();
		}

		// ==================== 主循环 ====================

		public override void PostUpdateWorld()
		{
			bool isNight = !Main.dayTime;

			// 黎明换日
			if (!isNight && wasNight) {
				WorldDay++;
				ResetTonight();
			}

			wasNight = isNight;

			if (!isNight) {
				return;
			}

			// 已被击败并推进剧情后，不再自然来袭（想继续刷材料就用信号传感器）
			if (WastelandStorySystem.scavengerDefeated) {
				return;
			}

			if (WorldDay <= 0 || WorldDay % DefaultCycleDays != 0) {
				return;
			}

			double nightTime = Main.time;

			// 预警：各端按同样的天数与时间推导，因此单机/联机表现一致
			if (!warningDoneTonight) {
				if (nightTime >= TicksAt(SpawnHour)) {
					// 读档时已经过了生成时刻，预警已经没有意义
					warningDoneTonight = true;
				}
				else if (nightTime >= TicksAt(WarningHour)) {
					warningDoneTonight = true;
					ShowWarning();
				}
			}

			// 生成：只由服务端 / 单机执行
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			if (!spawnDoneTonight && nightTime >= TicksAt(SpawnHour)) {
				spawnDoneTonight = true;
				SpawnScavenger();
			}
		}

		/// <summary>把「几点几分」换算成入夜后的 tick 数（19:30 = 0）。</summary>
		private static double TicksAt(float hour)
		{
			return (hour - NightStartHour) * 3600.0;
		}

		// ==================== 预警与生成 ====================

		private static void ShowWarning()
		{
			if (Main.dedServ) {
				return;
			}

			// 联机时的广播方案待定（见文档「联机暂时搁置」）
			Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.ScavengerWarning"), 186, 58, 58);
		}

		private static void SpawnScavenger()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			// 同一时间只允许一只清道夫
			if (Scavenger.AnyAlive()) {
				return;
			}

			// 蓄水池抽样：在所有存活玩家里等概率挑一个
			int chosen = -1;
			int alive = 0;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];

				if (!player.active || player.dead || player.ghost) {
					continue;
				}

				alive++;

				if (Main.rand.NextBool(alive)) {
					chosen = i;
				}
			}

			if (chosen < 0) {
				return;
			}

			Player target = Main.player[chosen];
			float side = Main.rand.NextBool() ? 1f : -1f;
			Vector2 position = target.Center + new Vector2(side * Main.rand.NextFloat(620f, 900f), -420f);

			int index = NPC.NewNPC(new EntitySource_SpawnNPC(), (int)position.X, (int)position.Y, ModContent.NPCType<Scavenger>());

			if (index >= 0 && index < Main.maxNPCs) {
				Main.npc[index].netUpdate = true;
			}

			if (Main.dedServ) {
				return;
			}

			Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.ScavengerArrived"), 226, 90, 70);
		}
	}
}
