using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Buffs
{
	// ====================================================================================
	// 消耗品增益（6 个）。全部是**正向** buff，图标统一 32×32。
	// ⚠️ 这里只做"效果"，持续时间由对应物品的 Item.buffTime 决定（见 Consumables/WastelandConsumables.cs）。
	// ⚠️ 不要在 SetStaticDefaults 里创建任何图形资源（会抛 ThreadStateException 并禁用整个模组）。
	// ====================================================================================

	/// <summary>
	/// 滤芯（防毒）：把面罩里的滤芯换成新的，**免疫有害气体与污染**。
	/// <para/>判定点：本来 <see cref="GasPoison"/> 是由壁炉大气系统无条件挂上的，
	/// 这里在 <c>Update</c> 里把它清掉——效果等同于防毒面具，但只持续一瓶药的时间。
	/// </summary>
	public class GasFilterBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = false;
			Main.pvpBuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			// 立刻清掉已经挂上的两种大气类减益；大气系统下一帧还会再挂，所以每帧都要清
			player.ClearBuff(ModContent.BuffType<GasPoison>());
			player.ClearBuff(ModContent.BuffType<Pollution>());

			player.lifeRegen += 2;   // 顺手给一点恢复，免得刚出污染区就被残血磨死

			// 视觉：面罩边缘偶尔漏出一小撮白气
			if (!Main.dedServ && Main.rand.NextBool(14)) {
				Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Smoke);
				dust.velocity *= 0.3f;
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.color = new Color(200, 220, 210);
			}
		}
	}

	/// <summary>
	/// 掘进液：挖掘速度 +30%、放置/破坏方块的动作也更快，并附带 +1 格挖掘范围。
	/// <para/>数值参考原版「挖掘药水」的量级（原版是 +25%），这里稍微给多一点，
	/// 因为废土的地形（瓦砾、合金残骸）比原版世界硬。
	/// </summary>
	public class MinersSolutionBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.pvpBuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.pickSpeed -= 0.30f;          // 越小越快
			player.tileSpeed += 0.30f;
			player.wallSpeed += 0.30f;
			player.blockRange++;                 // 多挖一格
		}
	}

	/// <summary>
	/// 冷光：夜视 + 微光，并提高刷怪率（看得见，也就意味着更危险）。
	/// <para/>夜视走原版 <see cref="BuffID.NightOwl"/> 的同一套照明逻辑；
	/// dangerSense 让附近的敌人在屏幕外也标出来，避免被废土的伏击打死。
	/// </summary>
	public class ColdSpotlightBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.pvpBuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.nightVision = true;
			player.dangerSense = true;
			player.findTreasure = true;
			player.moveSpeed += 0.05f;   // 一点点机动性，冷光同时也在给你打气

			if (!Main.dedServ && Main.rand.NextBool(10)) {
				Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.IceTorch);
				dust.velocity *= 0.25f;
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}
	}

	/// <summary>
	/// 纳米修复膏：每秒回 2 点血，并且**大幅提高自然恢复速度**（lifeRegenTime 会累积得很快）。
	/// <para/>这是"持续作战"药，不是"一口奶满"药；配合血肉墙之后的战斗节奏设计。
	/// </summary>
	public class NaniteSalveBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.pvpBuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.lifeRegen += 4;        // 约 2 HP/秒
			player.lifeRegenTime += 6f;   // 加速自然恢复的累积

			if (!Main.dedServ && Main.rand.NextBool(8)) {
				Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.HealingPlus);
				dust.velocity *= 0.3f;
				dust.noGravity = true;
				dust.scale = 0.9f;
			}
		}
	}

	/// <summary>
	/// 兽哨余响：召唤伤害 +15%，鞭子挥得更快（-20% 使用时间）也更长（+25% 范围）。
	/// <para/>做成"召唤师专属药剂"，和原版召唤药水（+1 仆从位）错开定位：
	/// 那些药给的是**数量**，这一瓶给的是**质量**。
	/// </summary>
	public class BeastWhistleBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.pvpBuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.GetDamage(DamageClass.Summon) += 0.15f;
			player.whipRangeMultiplier += 0.25f;

			if (!Main.dedServ && Main.rand.NextBool(12)) {
				Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Blood);
				dust.velocity *= 0.2f;
				dust.noGravity = true;
				dust.scale = 0.8f;
				dust.color = new Color(210, 160, 90);
			}
		}
	}

	/// <summary>
	/// 灰烬口粮：吃饱了（原版 WellFed）+ 全伤害 +8% + 自然恢复。
	/// <para/>废土上的食物都很粗糙，但热量是真的；饱食度是很多废土配方的"生活成本"。
	/// </summary>
	public class AshenRationBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.pvpBuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.AddBuff(BuffID.WellFed, 2, true);   // 每帧续 2 帧，不会覆盖更高级的饱食
			player.GetDamage(DamageClass.Generic) += 0.08f;
			player.lifeRegen += 2;
		}
	}
}
