using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Common.Players;
using WastelandSoul.Content.Items.Armor;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树（四）· 护甲：精钢战士套装 → 灰烬合金战士套装 → 炉卫合金战士套装
	//
	// 三级差异（**套装加成换机制，不只是防御数字变大**）：
	//   精钢战士套装（已有） 5/7/5 = 17 防；套装：近战伤害 +10%、近战速度 +10%
	//   灰烬合金战士套装     8/11/8 = 27 防（每件 +8% 近战 / +8% 速度）；
	//                        套装：近战伤害 +12%、近战速度 +12%、**命中 15% 概率点燃**，
	//                        并且整套对污染伤害再打八五折（密封性更好）
	//   炉卫合金战士套装     12/16/12 = 40 防；每件额外 +3% 减伤；
	//                        套装：近战伤害 +15%、近战速度 +15%、**减伤 +6%、免疫击退**
	//
	// 图标 22x22，穿身帧表 40x1120（20 帧 x 56 高），由 tools/gen_upgradetree_art.py 程序化生成。
	// ====================================================================================

	// ====================================================================================
	// 一、灰烬合金战士套装（世纪之花之后 / 秘银砧）
	// ====================================================================================

	[AutoloadEquip(EquipType.Head)]
	public class AshAlloyWarriorHelm : AshAlloyWarriorPiece
	{
		protected override int Defense => 8;
		protected override int AlloyCost => 6;
		protected override int FragmentCost => 3;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Melee) += 0.08f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelWarriorHelm>()
				.AddIngredient<AshHeartAlloyBar>(AlloyCost)
				.AddIngredient<AshHeartFragment>(FragmentCost)
				.AddTile(CraftTile)
				.Register();
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class AshAlloyWarriorPlate : AshAlloyWarriorPiece
	{
		protected override int Defense => 11;
		protected override int AlloyCost => 10;
		protected override int FragmentCost => 5;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Melee) += 0.08f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<AshAlloyWarriorHelm>()
				&& legs.type == ModContent.ItemType<AshAlloyWarriorGreaves>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.AshAlloyWarriorPlate.SetBonus");

			player.GetDamage(DamageClass.Melee) += 0.12f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.12f;

			WastelandUpgradePlayer upgrade = player.GetModPlayer<WastelandUpgradePlayer>();
			upgrade.ashAlloySet = true;                       // 抗污染乘区在 WastelandUpgradePlayer.ModifyHurt

			WastelandGearPlayer gear = player.GetModPlayer<WastelandGearPlayer>();
			gear.gearEmberOnHit += 0.15f;                     // 招牌机制：命中概率点燃
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelWarriorPlate>()
				.AddIngredient<AshHeartAlloyBar>(AlloyCost)
				.AddIngredient<AshHeartFragment>(FragmentCost)
				.AddTile(CraftTile)
				.Register();
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class AshAlloyWarriorGreaves : AshAlloyWarriorPiece
	{
		protected override int Defense => 8;
		protected override int AlloyCost => 8;
		protected override int FragmentCost => 4;

		public override void UpdateEquip(Player player)
		{
			player.GetAttackSpeed(DamageClass.Melee) += 0.08f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelWarriorGreaves>()
				.AddIngredient<AshHeartAlloyBar>(AlloyCost)
				.AddIngredient<AshHeartFragment>(FragmentCost)
				.AddTile(CraftTile)
				.Register();
		}
	}

	// ====================================================================================
	// 二、炉卫合金战士套装（月亮领主前 / 远古操纵机）
	// ====================================================================================

	[AutoloadEquip(EquipType.Head)]
	public class HearthAlloyWarriorHelm : HearthAlloyWarriorPiece
	{
		protected override int Defense => 12;
		protected override int AlloyCost => 8;
		protected override int FragmentCost => 4;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Melee) += 0.09f;
			player.endurance += 0.03f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshAlloyWarriorHelm>()
				.AddIngredient<FireplaceAlloyBar>(AlloyCost)
				.AddIngredient<FireplaceFragment>(FragmentCost)
				.AddIngredient(ItemID.HellstoneBar, 6)
				.AddTile(CraftTile)
				.Register();
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class HearthAlloyWarriorPlate : HearthAlloyWarriorPiece
	{
		protected override int Defense => 16;
		protected override int AlloyCost => 12;
		protected override int FragmentCost => 6;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Melee) += 0.09f;
			player.noKnockback = true;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<HearthAlloyWarriorHelm>()
				&& legs.type == ModContent.ItemType<HearthAlloyWarriorGreaves>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.HearthAlloyWarriorPlate.SetBonus");

			player.GetDamage(DamageClass.Melee) += 0.15f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
			player.endurance += 0.06f;
			player.noKnockback = true;
			player.moveSpeed -= 0.03f;      // 重甲：换来减伤，走路稍沉
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshAlloyWarriorPlate>()
				.AddIngredient<FireplaceAlloyBar>(AlloyCost)
				.AddIngredient<FireplaceFragment>(FragmentCost)
				.AddIngredient(ItemID.HellstoneBar, 6)
				.AddTile(CraftTile)
				.Register();
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class HearthAlloyWarriorGreaves : HearthAlloyWarriorPiece
	{
		protected override int Defense => 12;
		protected override int AlloyCost => 10;
		protected override int FragmentCost => 5;

		public override void UpdateEquip(Player player)
		{
			player.GetAttackSpeed(DamageClass.Melee) += 0.09f;
			player.endurance += 0.03f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshAlloyWarriorGreaves>()
				.AddIngredient<FireplaceAlloyBar>(AlloyCost)
				.AddIngredient<FireplaceFragment>(FragmentCost)
				.AddIngredient(ItemID.HellstoneBar, 6)
				.AddTile(CraftTile)
				.Register();
		}
	}

	// ====================================================================================
	// 公共基类：两档共用的尺寸 / 售价 / 稀有度 / 工作台。抽象类不会被自动加载，不需要贴图。
	// ====================================================================================

	/// <summary>灰烬合金战士套装三件：精钢对应件 + 灰烬之心合金锭 + 灰烬碎片，秘银砧。</summary>
	public abstract class AshAlloyWarriorPiece : ModItem
	{
		protected abstract int Defense { get; }

		protected abstract int AlloyCost { get; }

		protected abstract int FragmentCost { get; }

		protected virtual int CraftTile => TileID.MythrilAnvil;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = WastelandRarityTiers.MidLate;
			Item.defense = Defense;
		}
	}

	/// <summary>炉卫合金战士套装三件：灰烬合金对应件 + 壁炉合金锭 + 壁炉碎片 + 狱石锭，远古操纵机。</summary>
	public abstract class HearthAlloyWarriorPiece : ModItem
	{
		protected abstract int Defense { get; }

		protected abstract int AlloyCost { get; }

		protected abstract int FragmentCost { get; }

		protected virtual int CraftTile => TileID.LunarCraftingStation;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = WastelandRarityTiers.Late;
			Item.defense = Defense;
		}
	}
}
