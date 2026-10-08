using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;
using WastelandSoul.Content.Projectiles.UpgradeTrees;

namespace WastelandSoul.Content.Items.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树（六）· 召唤师：锈蚀齿轮哨 → 精钢齿轮哨 → 灰烬之心齿灵哨
	//
	// 三级差异（**不是纯数值 +10%**，三级三种仆从编制）：
	//   锈蚀齿轮哨        1 只**近战冲锋**仆从（撞击型，1 个仆从位），没有任何附加效果
	//   精钢齿轮哨        一次召唤 **2 只**：1 只冲锋齿轮 + 1 只悬浮射击齿轮（共 2 个仆从位），
	//                     射击齿轮吐出的钢钉会让目标**流血 3 秒**
	//   灰烬之心齿灵哨    一次召唤 **2 只环绕齿轮**：绕着主人转圈切割（撞到就**点燃**），
	//                     同时每 45 帧投出一枚会**追踪**的余烬弹（点燃 + 霜冻），
	//                     并且主人身上常驻一层**齿轮护盾**（防御 +5、免疫击退、再减伤 8%）
	//
	// 召唤类一律「物品 + 仆从弹幕 + 专属 Buff」三件套成对出现（见 SummonerUpgradeMinions.cs）。
	// ⚠️ 1.4.4 的 <c>Terraria.Item</c> **没有** <c>summon</c> 这个字段（已用 Cecil 核对过元数据），
	//    召唤判定靠 <c>DamageType = DamageClass.Summon</c> + <c>buffType</c> + <c>shoot</c> 三者。
	// 每级各有自己的配方（在合成界面可见；物品介绍里**不写**制作方法）。
	// ====================================================================================

	/// <summary>
	/// Rusted Gear Whistle（锈蚀齿轮哨）
	/// <para/>定位：这条召唤树的树根。伤害 12、使用时间 36、蓝耗 10；
	/// 吹响一次召唤 1 只 **锈蚀齿轮哨兵**（占 1 个仆从位）：它不会射击，
	/// 而是直接冲向 640 像素内最近的敌人用齿轮咬上去（接触伤害）。
	/// 与废料无人机(11) 相比略重一点、脆一点，好处是材料只要齿轮和碎块，清道夫期就能做。
	/// </summary>
	public class RustedGearWhistle : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 12;
		protected override int UseTime => 36;
		protected override float Knockback => 3f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(gold: 1);
		protected override int ShootType => ModContent.ProjectileType<RustedGearSentry>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<RustedGearSentryBuff>(), 10);
			Item.width = 36;
			Item.height = 36;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RustedGear>(8)
				.AddIngredient<SalvagedSteelChunk>(6)
				.AddIngredient(ItemID.Wood, 10)
				.AddIngredient(ItemID.Gel, 6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Salvaged Steel Gear Whistle（精钢齿轮哨）
	/// <para/>定位：编制从「一只冲锋」变成「一冲一射」的**双联哨**。
	/// 伤害 16、使用时间 32、蓝耗 12；吹响一次**同时**召唤 2 只（共占 2 个仆从位）：
	/// 冲锋齿轮（继承树根的撞击 AI，血量更厚）+ 悬浮射击齿轮（每 60 帧一发钢钉，命中**流血 3 秒**）。
	/// 单只伤害不如同级 Boss 召唤物，但"一只顶在前一只在后"能把仇恨拉成两段。
	/// </summary>
	public class SalvagedSteelGearWhistle : RustedGearWhistle
	{
		protected override int Damage => 16;
		protected override int UseTime => 32;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 2, silver: 50);
		protected override int ShootType => ModContent.ProjectileType<SalvagedSteelCharger>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<SalvagedSteelGearBuff>(), 12);
			Item.width = 40;
			Item.height = 40;
		}

		public override void AddRecipes()
		{
			// 进阶件：树根 + 精钢回火，再用归档者的索引碎片把两只齿轮的指令对齐
			CreateRecipe()
				.AddIngredient<RustedGearWhistle>()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient<ArchivistFragment>(4)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Ash Heart Gear Whistle（灰烬之心齿灵哨）
	/// <para/>定位：这条树的终阶，AI 从"冲锋"整个换成**环绕**。
	/// 伤害 22、使用时间 30、蓝耗 14；一次召唤 2 只**环绕齿灵**（共 2 个仆从位）：
	/// 它们绕着主人转圈，碰到什么就把什么**点燃**；每 45 帧投出一枚**会追踪**的余烬弹（点燃 + 霜冻）。
	/// 额外给主人一层**齿轮护盾**：防御 +5、免疫击退、受到的伤害再降 8%（见 WastelandTreePlayer）。
	/// 与壁炉系的定点哨戒相比：它不站桩，是"贴着主人转的移动火圈"。
	/// </summary>
	public class AshHeartGearWhistle : SalvagedSteelGearWhistle
	{
		protected override int Damage => 22;
		protected override int UseTime => 30;
		protected override int Rarity => WastelandRarityTiers.MidLate;
		protected override int SellPrice => Item.sellPrice(gold: 5);
		protected override int ShootType => ModContent.ProjectileType<AshHeartOrbitSentry>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<AshHeartGearBuff>(), 14);
			Item.width = 44;
			Item.height = 44;
			Item.UseSound = SoundID.Item44;
		}

		public override void AddRecipes()
		{
			// 终阶件：进阶哨 + 灰烬之心合金锭重铸齿轮 + 灰烬结晶做余烬核心
			CreateRecipe()
				.AddIngredient<SalvagedSteelGearWhistle>()
				.AddIngredient<AshHeartAlloyBar>(8)
				.AddIngredient<AshCrystal>(5)
				.AddIngredient<CircuitBoard>(4)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
