using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Common.Players;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Accessories
{
	/// <summary>掉落袋送出的两件，以及可以用芯片买到的另外三件。</summary>
	public static class WastelandAccessoryCatalog
	{
		public static int[] BagDrops(int bossIndex)
		{
			switch (bossIndex) {
				case 1:
					return new[] {
						ModContent.ItemType<ScavengerWarriorCharm>(),
						ModContent.ItemType<ScavengerMageCharm>()
					};
				case 2:
					return new[] {
						ModContent.ItemType<ArchivistRangerCharm>(),
						ModContent.ItemType<ArchivistSummonerCharm>()
					};
				case 3:
					return new[] {
						ModContent.ItemType<AshHeartRogueCharm>(),
						ModContent.ItemType<AshHeartWarriorCharm>()
					};
				case 4:
					return new[] {
						ModContent.ItemType<FireplaceMageCharm>(),
						ModContent.ItemType<FireplaceRangerCharm>()
					};
				default:
					return new int[0];
			}
		}
	}

	public class ScavengerWarriorCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Melee) += 0.06f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.04f;
		}
	}

	public class ScavengerMageCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.statManaMax2 += 20;
			player.manaCost -= 0.06f;
		}
	}

	public class ScavengerRangerCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Ranged) += 0.06f;
			player.GetModPlayer<WastelandPlayer>().ammoSave += 0.10f;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<SalvagedSteelBar>(8).AddTile(WastelandCraftingStations.EarlyAnvil).Register();
		}
	}

	public class ScavengerSummonerCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.maxMinions += 1;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<SalvagedSteelBar>(8).AddTile(WastelandCraftingStations.EarlyAnvil).Register();
		}
	}

	public class ScavengerRogueCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Throwing) += 0.06f;
			player.moveSpeed += 0.06f;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<SalvagedSteelBar>(8).AddTile(WastelandCraftingStations.EarlyAnvil).Register();
		}
	}

	public class ArchivistWarriorCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Melee) += 0.08f;
			player.GetCritChance(DamageClass.Melee) += 4f;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<ArchivistFragment>(8).AddIngredient(ItemID.Bone, 12).AddTile(WastelandCraftingStations.EarlyAnvil).Register();
		}
	}

	public class ArchivistMageCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Magic) += 0.08f;
			player.manaCost -= 0.08f;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<ArchivistFragment>(8).AddIngredient(ItemID.Bone, 12).AddTile(WastelandCraftingStations.EarlyAnvil).Register();
		}
	}

	public class ArchivistRangerCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Ranged) += 0.08f;
			player.GetCritChance(DamageClass.Ranged) += 5f;
		}
	}

	public class ArchivistSummonerCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Summon) += 0.12f;
		}
	}

	public class ArchivistRogueCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Throwing) += 0.08f;
			player.GetCritChance(DamageClass.Throwing) += 4f;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<ArchivistFragment>(8).AddIngredient(ItemID.Bone, 12).AddTile(WastelandCraftingStations.EarlyAnvil).Register();
		}
	}

	public class AshHeartWarriorCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Melee) += 0.10f;
			player.lifeRegen += 2;
		}
	}

	public class AshHeartMageCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Magic) += 0.10f;
			player.manaRegenBonus += 2;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<AshHeartAlloyBar>(8).AddTile(WastelandCraftingStations.HardmodeAnvil).Register();
		}
	}

	public class AshHeartRangerCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Ranged) += 0.10f;
			player.GetModPlayer<WastelandPlayer>().ammoSave += 0.15f;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<AshHeartAlloyBar>(8).AddTile(WastelandCraftingStations.HardmodeAnvil).Register();
		}
	}

	public class AshHeartSummonerCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Summon) += 0.12f;
			player.maxMinions += 1;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<AshHeartAlloyBar>(8).AddTile(WastelandCraftingStations.HardmodeAnvil).Register();
		}
	}

	public class AshHeartRogueCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Throwing) += 0.10f;
			player.GetModPlayer<WastelandPlayer>().emberOnHit += 0.12f;
		}
	}

	public class FireplaceWarriorCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Melee) += 0.12f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.08f;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<FireplaceAlloyBar>(8).AddTile(WastelandCraftingStations.HardmodeAnvil).Register();
		}
	}

	public class FireplaceMageCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Magic) += 0.12f;
			player.manaCost -= 0.12f;
		}
	}

	public class FireplaceRangerCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Ranged) += 0.12f;
			player.GetCritChance(DamageClass.Ranged) += 8f;
		}
	}

	public class FireplaceSummonerCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Summon) += 0.14f;
			player.maxMinions += 1;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<FireplaceAlloyBar>(8).AddTile(WastelandCraftingStations.HardmodeAnvil).Register();
		}
	}

	public class FireplaceRogueCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Throwing) += 0.12f;
			player.moveSpeed += 0.10f;
		}

		public override void AddRecipes()
		{
			CreateRecipe().AddIngredient<FireplaceAlloyBar>(8).AddTile(WastelandCraftingStations.HardmodeAnvil).Register();
		}
	}
}
