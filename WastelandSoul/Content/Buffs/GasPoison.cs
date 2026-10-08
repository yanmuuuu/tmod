using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Buffs
{
	/// <summary>
	/// **有害气体**（减益）：壁炉堡垒一直在排放的污染物，整个壁炉世界的大气都被它污染了。
	///
	/// <para/>玩家要求的表现：**没有防毒饰品就缓慢扣血，并且无视生命自然恢复**。
	/// 两个效果分别对应：
	/// <list type="bullet">
	/// <item><c>player.lifeRegen -= 6</c> —— 原版 <c>lifeRegen</c> 每 1 点约等于 0.5 HP/秒，
	/// 所以这里是 **3 HP/秒**：站着不动大约两分半掉一条血，属于"缓慢但持续"的压迫；</item>
	/// <item><c>player.lifeRegenTime = 0</c> —— 原版的自然恢复靠这个计时器累积，
	/// 把它清空就等于**自然恢复完全不生效**（戴着再生类饰品也一样掉血）。</item>
	/// </list>
	///
	/// <para/>免疫来源：<see cref="Content.Items.Accessories.GasMask"/>（防毒面具）。
	/// 判定的地方在 <see cref="FireplaceAtmospherePlayer"/>。
	/// </summary>
	public class GasPoison : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
			BuffID.Sets.IsATagBuff[Type] = false;       // 不是"标签类"减益，护士不能清除它（在壁炉里会立刻重新挂上）
		}

		public override void Update(Player player, ref int buffIndex)
		{
			// ⚠️ 数值按玩家反馈调过：第一版 -6（3 HP/秒）会被战士那类自带恢复的饰品抵消掉，
			// 于是"扣血"完全看不出来。这里加到 -18（约 9 HP/秒，一分钟掉 540），
			// 任何自然恢复都盖不住它；要削弱就调这一个数。
			player.lifeRegen -= 18;
			player.lifeRegenTime = 0f;

			// 视觉：身上时不时飘出灰绿色的气团
			if (!Main.dedServ && Main.rand.NextBool(7)) {
				Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Smoke);
				dust.velocity *= 0.4f;
				dust.velocity.Y -= 0.6f;
				dust.noGravity = true;
				dust.scale = 0.9f;
				dust.color = new Microsoft.Xna.Framework.Color(150, 170, 120);
			}
		}
	}
}
