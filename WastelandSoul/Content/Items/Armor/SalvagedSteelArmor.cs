using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Armor
{
	/// <summary>
	/// 精钢防具基类：统一处理尺寸/价值/防御/配方，子类只管数值与增益。
	/// <para/>数值定位：**打史莱姆王之前能得到的最好一套**（全套 15~17 防御，
	/// 略高于铂金 16，低于暗影/猩红），代价是要先打赢 Boss 1。
	/// <para/>注意：抽象类不会被 tModLoader 自动加载，因此不需要贴图。
	/// </summary>
	public abstract class SalvagedSteelArmor : ModItem
	{
		/// <summary>防御力。</summary>
		public abstract int Defense { get; }

		/// <summary>合成所需精钢数量（锭）。</summary>
		public abstract int BarCost { get; }

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Orange;
			Item.defense = Defense;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(BarCost)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	// ==================== 战士 ====================

	[AutoloadEquip(EquipType.Head)]
	public class SalvagedSteelWarriorHelm : SalvagedSteelArmor
	{
		public override int Defense => 5;
		public override int BarCost => 6;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Melee) += 0.06f;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class SalvagedSteelWarriorPlate : SalvagedSteelArmor
	{
		public override int Defense => 7;
		public override int BarCost => 10;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Melee) += 0.06f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<SalvagedSteelWarriorHelm>()
				&& legs.type == ModContent.ItemType<SalvagedSteelWarriorGreaves>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.SalvagedSteelWarriorPlate.SetBonus");
			player.GetDamage(DamageClass.Melee) += 0.10f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.10f;
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class SalvagedSteelWarriorGreaves : SalvagedSteelArmor
	{
		public override int Defense => 5;
		public override int BarCost => 8;

		public override void UpdateEquip(Player player)
		{
			player.GetAttackSpeed(DamageClass.Melee) += 0.08f;
		}
	}

	// ==================== 法师 ====================

	[AutoloadEquip(EquipType.Head)]
	public class SalvagedSteelMageHood : SalvagedSteelArmor
	{
		public override int Defense => 4;
		public override int BarCost => 6;

		public override void UpdateEquip(Player player)
		{
			player.statManaMax2 += 30;
			player.GetDamage(DamageClass.Magic) += 0.06f;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class SalvagedSteelMageRobe : SalvagedSteelArmor
	{
		public override int Defense => 6;
		public override int BarCost => 10;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Magic) += 0.06f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<SalvagedSteelMageHood>()
				&& legs.type == ModContent.ItemType<SalvagedSteelMageLeggings>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.SalvagedSteelMageRobe.SetBonus");
			player.GetDamage(DamageClass.Magic) += 0.10f;
			player.manaCost -= 0.10f;
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class SalvagedSteelMageLeggings : SalvagedSteelArmor
	{
		public override int Defense => 5;
		public override int BarCost => 8;

		public override void UpdateEquip(Player player)
		{
			player.manaCost -= 0.06f;
		}
	}

	// ==================== 射手 ====================

	[AutoloadEquip(EquipType.Head)]
	public class SalvagedSteelRangerVisor : SalvagedSteelArmor
	{
		public override int Defense => 4;
		public override int BarCost => 6;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Ranged) += 0.06f;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class SalvagedSteelRangerVest : SalvagedSteelArmor
	{
		public override int Defense => 7;
		public override int BarCost => 10;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Ranged) += 0.06f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<SalvagedSteelRangerVisor>()
				&& legs.type == ModContent.ItemType<SalvagedSteelRangerLeggings>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.SalvagedSteelRangerVest.SetBonus");
			player.GetDamage(DamageClass.Ranged) += 0.10f;
			player.ammoCost80 = true;
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class SalvagedSteelRangerLeggings : SalvagedSteelArmor
	{
		public override int Defense => 5;
		public override int BarCost => 8;

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.08f;
		}
	}

	// ==================== 召唤师 ====================

	[AutoloadEquip(EquipType.Head)]
	public class SalvagedSteelSummonerCowl : SalvagedSteelArmor
	{
		public override int Defense => 4;
		public override int BarCost => 6;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Summon) += 0.06f;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class SalvagedSteelSummonerTunic : SalvagedSteelArmor
	{
		public override int Defense => 7;
		public override int BarCost => 10;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Summon) += 0.06f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<SalvagedSteelSummonerCowl>()
				&& legs.type == ModContent.ItemType<SalvagedSteelSummonerLeggings>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.SalvagedSteelSummonerTunic.SetBonus");
			player.GetDamage(DamageClass.Summon) += 0.10f;
			player.maxMinions += 1;
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class SalvagedSteelSummonerLeggings : SalvagedSteelArmor
	{
		public override int Defense => 5;
		public override int BarCost => 8;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Summon) += 0.05f;
		}
	}

	// ==================== 盗贼（投掷 / 通用，不依赖任何模组） ====================

	[AutoloadEquip(EquipType.Head)]
	public class SalvagedSteelRogueMask : SalvagedSteelArmor
	{
		public override int Defense => 4;
		public override int BarCost => 6;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Throwing) += 0.06f;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class SalvagedSteelRogueVest : SalvagedSteelArmor
	{
		public override int Defense => 7;
		public override int BarCost => 10;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Throwing) += 0.06f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<SalvagedSteelRogueMask>()
				&& legs.type == ModContent.ItemType<SalvagedSteelRogueLeggings>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.SalvagedSteelRogueVest.SetBonus");
			player.GetDamage(DamageClass.Throwing) += 0.10f;
			player.moveSpeed += 0.10f;
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class SalvagedSteelRogueLeggings : SalvagedSteelArmor
	{
		public override int Defense => 5;
		public override int BarCost => 8;

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.08f;
		}
	}
}
