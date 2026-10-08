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
	/// <para/>为什么单独开一个 <see cref="ModPlayer"/> 而不是塞进 <c>WastelandPlayer</c>：
	/// 这块逻辑只服务壁炉，分开写以后合并别的分支时冲突面最小。
	/// </summary>
	public class FireplaceAtmospherePlayer : ModPlayer
	{
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

			// 1) 有害气体：没有防毒面具就一直侵蚀
			if (!gasMaskEquipped) {
				Player.AddBuff(ModContent.BuffType<GasPoison>(), 4);
			}

			// 2) 污染水：泡在水里额外挂污染（岩浆/蜂蜜不算）
			if (Player.wet && !Player.lavaWet && !Player.honeyWet) {
				Player.AddBuff(ModContent.BuffType<Pollution>(), 90);
			}

			// 3) 大气视觉：玩家附近飘灰绿色浮尘
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
	}
}
