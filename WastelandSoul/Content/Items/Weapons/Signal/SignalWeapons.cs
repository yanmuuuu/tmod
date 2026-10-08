using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Buffs;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Projectiles.Signal;

namespace WastelandSoul.Content.Items.Weapons.Signal
{
	/// <summary>
	/// 信号组：同一套锈铁 + 冷青焊点的自制武器。数值停在清道夫之后、骷髅王前后，铁砧就能做。
	/// </summary>
	public class SignalCleaver : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 22;
		protected override int UseTime => 24;
		protected override float Knockback => 6.2f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<SignalRustShard>();
		protected override float ShootSpeed => 9f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
		}

		public override void AddRecipes()
		{
			Register(ItemID.IronBar);
			Register(ItemID.LeadBar);
		}

		private void Register(int bar)
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(10)
				.AddIngredient(bar, 8)
				.AddIngredient(ItemID.Gel, 8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>法师：提灯法典，打出一颗会拐弯的冷青灯火。</summary>
	public class SignalCodex : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 18;
		protected override int UseTime => 28;
		protected override float Knockback => 3.2f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<SignalLanternMote>();
		protected override float ShootSpeed => 8.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 8);
		}

		public override void AddRecipes()
		{
			Register(ItemID.IronBar);
			Register(ItemID.LeadBar);
		}

		private void Register(int bar)
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(8)
				.AddIngredient(bar, 6)
				.AddIngredient(ItemID.FallenStar, 3)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>射手：线圈钉枪，钉子走得直，命中时迸一截短电弧。</summary>
	public class SignalNailgun : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 15;
		protected override int UseTime => 16;
		protected override float Knockback => 2.4f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<SignalCoilNail>();
		protected override float ShootSpeed => 13f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
		}

		public override void AddRecipes()
		{
			Register(ItemID.IronBar);
			Register(ItemID.LeadBar);
		}

		private void Register(int bar)
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(12)
				.AddIngredient(bar, 8)
				.AddIngredient(ItemID.Wire, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>盗贼：余烬扇，一次甩出三片。</summary>
	public class SignalEmberFan : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 12;
		protected override int UseTime => 22;
		protected override float Knockback => 3.5f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<SignalEmberFanProj>();
		protected override float ShootSpeed => 11f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			for (int i = -1; i <= 1; i++) {
				Vector2 shot = velocity.RotatedBy(i * 0.2f);
				Projectile.NewProjectile(source, position, shot, type, damage, knockback, player.whoAmI);
			}

			return false;
		}

		public override void AddRecipes()
		{
			Register(ItemID.IronBar);
			Register(ItemID.LeadBar);
		}

		private void Register(int bar)
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(8)
				.AddIngredient(bar, 6)
				.AddIngredient(ItemID.Gel, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>召唤：信号灯芯，召出一盏会自己找人砸的小灯。</summary>
	public class SignalWispStaff : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 9;
		protected override int UseTime => 30;
		protected override float Knockback => 2f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<SignalWispMinion>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<SignalWispBuff>(), 10);
		}

		public override void AddRecipes()
		{
			Register(ItemID.IronBar);
			Register(ItemID.LeadBar);
		}

		private void Register(int bar)
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(12)
				.AddIngredient(bar, 6)
				.AddIngredient(ItemID.FallenStar, 2)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
