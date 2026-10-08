using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Buffs;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 壁炉世界的**大气与污染水**判定。
	///
	/// <para/>规则（对应设定：堡垒为了保住火种，把外面的土地烧成灰，废气把大气整个污染了）：
	/// <list type="bullet">
	/// <item>只要人在壁炉里、**又没戴防毒面具**（<see cref="Content.Items.Accessories.GasMask"/>），
	/// 就持续挂 <see cref="GasPoison"/>：缓慢掉血且无视自然恢复；</item>
	/// <item>泡在**污染水**里（原版水 + 壁炉世界）额外挂上既有的 <see cref="Pollution"/>（腐蚀护甲）；</item>
	/// <item>再补一点灰绿色浮尘，让"空气是脏的"看得见。</item>
	/// </list>
	///
	/// <para/>=== 联机适配（本轮）：减益改由**服务端权威施加** ===
	/// <list type="number">
	/// <item><b>谁施加</b>：只有 <c>Main.netMode != MultiplayerClient</c>（单机 / 服务端 / 子世界服务端）
	/// 才调 <see cref="Player.AddBuff(int, int)"/>；客户端这一段**完全不碰减益**，
	/// 只保留浮尘这种纯视觉。</item>
	/// <item><b>怎么到客户端</b>：⚠️ 这一条是 Cecil 反编译 <c>Terraria.dll</c> 核过的事实 ——
	/// 原版 <c>Player.AddBuff</c> **只在 <c>Main.netMode == 1</c>（客户端）时**发
	/// <c>MessageID.PlayerBuffs</c>（55 号包），而接收端在 <c>MessageBuffer.GetData</c> 里
	/// 只对 <c>netMode == 1 &amp;&amp; player == myPlayer</c> 调 <c>AddBuff(..., quiet: true)</c>。
	/// 也就是说「服务端自己 AddBuff 就会自动同步给客户端」**并不成立**（全库只有
	/// <c>Player.AddBuff</c> 一处发 55 号包）。所以服务端施加之后必须**自己补发一次**
	/// 55 号包，参数与 <c>Player.AddBuff</c> 内部那一次完全一致（whoAmI / buffType / buffTime）。</item>
	/// <item><b>重挂频率</b>：不再"每帧补 4 帧"。改成一次挂 <see cref="FireplaceAtmospherePlayer.DebuffDuration"/>
	/// （90 tick = 1.5 秒），只在**剩余时间 ≤ <see cref="FireplaceAtmospherePlayer.RefreshThreshold"/>**
	/// （30 tick）时才续挂 —— 也就是每个玩家大约**每秒**一次 AddBuff / 一次同步包，
	/// 既不会掉 buff 也不会刷包。</item>
	/// </list>
	///
	/// <para/>为什么单独开一个 <see cref="ModPlayer"/> 而不是塞进 <c>WastelandPlayer</c>：
	/// 这块逻辑只服务壁炉，分开写以后合并别的分支时冲突面最小。
	/// </summary>
	public class FireplaceAtmospherePlayer : ModPlayer
	{
		/// <summary>
		/// 减益一次挂多久（帧）。**1.5 秒**。
		///
		/// <para/>为什么是这个数：原来污染水那条路就是 90 帧（每帧补，等于一直挂着），
		/// 所以"离开水之后还会留 1.5 秒"的手感与改之前**完全一样**；
		/// 有害气体原来只补 4 帧（离开壁炉几乎立刻消失），现在最多多留 1.5 秒 —— 可接受的代价。
		/// </summary>
		public const int DebuffDuration = 90;

		/// <summary>
		/// 剩余时间**少于**这个值才续挂（帧）。0.5 秒 —— 也就是每个玩家大约**每秒重挂一次**，
		/// 既不会掉 buff，也不会像"每帧调 AddBuff"那样把同步包刷成每帧一个。
		/// </summary>
		public const int RefreshThreshold = 30;

		/// <summary>本帧是否戴着防毒面具（饰品在 <c>UpdateAccessory</c> 里置位）。</summary>
		public bool gasMaskEquipped;

		public override void ResetEffects()
		{
			gasMaskEquipped = false;
		}

		public override void PostUpdateEquips()
		{
			if (Player.dead || !SubworldSystem.IsActive<FireplaceSubworld>()) {
				return;
			}

			// 1) 减益：**服务端权威**。单人时这一支就是本地施加（与改之前的表现一致）；
			//    联机时服务端施加 + 显式广播，客户端只负责显示与结算原版 buff 效果。
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				if (!gasMaskEquipped) {
					ApplyServerDebuff(Player, ModContent.BuffType<GasPoison>(), DebuffDuration);
				}

				// 污染水：泡在水里额外挂污染（岩浆/蜂蜜不算）
				if (Player.wet && !Player.lavaWet && !Player.honeyWet) {
					ApplyServerDebuff(Player, ModContent.BuffType<Pollution>(), DebuffDuration);
				}
			}

			// 2) 大气视觉：玩家附近飘灰绿色浮尘（纯视觉，客户端本地；服务端不产生 dust）
			if (!Main.dedServ && Main.rand.NextBool(10)) {
				Vector2 position = Player.Center + Main.rand.NextVector2Circular(320f, 240f);

				if (!WorldPaint.HasTile((int)(position.X / 16f), (int)(position.Y / 16f))) {
					Dust dust = Dust.NewDustDirect(position, 2, 2, DustID.Smoke);
					dust.velocity = Main.rand.NextVector2Circular(0.4f, 0.4f);
					dust.noGravity = true;
					dust.scale = 1.1f;
					dust.color = new Color(148, 168, 124);
					dust.alpha = 60;
				}
			}
		}

		/// <summary>
		/// 服务端（含单机）施加 / 续挂一个减益，并在联机时把它显式同步给该玩家的客户端。
		///
		/// <para/>续挂判定用**剩余时间**而不是"有没有"：<c>Player.AddBuff</c> 一旦发现这个 buff
		/// 已经存在就会走 <c>AddBuff_TryUpdatingExistingBuffTime</c> 直接 return（IL 已核），
		/// 所以每帧调用既不会延长时间也不会发包，但也没有意义 —— 这里按 <see cref="RefreshThreshold"/>
		/// 把它压成"每秒一次"。
		/// </summary>
		private static void ApplyServerDebuff(Player player, int buffType, int duration)
		{
			if (buffType <= 0) {
				return;
			}

			int index = player.FindBuffIndex(buffType);

			if (index >= 0 && player.buffTime[index] > RefreshThreshold) {
				return;   // 还够久，不重挂（这是"不刷包"的关键）
			}

			player.AddBuff(buffType, duration);

			// 服务端自己加 buff 不会自动广播（见类注释第 2 条），这里补一次原版的 buff 同步包：
			// 参数顺序与 Player.AddBuff 内部那次 SendData(55, ...) 完全一致。
			if (Main.netMode == NetmodeID.Server) {
				NetMessage.SendData(MessageID.PlayerBuffs, -1, -1, null, player.whoAmI, buffType, duration);
			}
		}
	}
}
