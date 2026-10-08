using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Rust
{
	// ====================================================================================
	// 「锈蚀」线 · 召唤武器（前期 / 铁砧）
	// 召唤类必须「物品 + 仆从弹幕 + 专属 Buff」三件套成对出现，否则仆从会秒消。
	//   · RustDroneStaff     撞击型仆从（不射击、直接撞上去）
	//   · RustSpitterStaff   射击型仆从（每 70 帧一发酸弹）
	//   · RustSpitterStaffEX 强化射击仆从（每 55 帧两发扇形）
	// ====================================================================================

	/// <summary>
	/// Rust Drone Staff（废料无人机召唤杖）
	/// <para/>定位：前期「贴身肉盾型」仆从。召唤 1 只 11 伤害的废料无人机（占 1 个仆从位），
	/// 不发射弹幕，但会主动冲向 620 像素内的敌人撞击（接触伤害）。伤害高于 Slime Staff(8)、低于 Hornet Staff(11~12) 的持续输出，
	/// 好处是「不用管瞄准」，坏处是必须把主人也带进危险距离。
	/// </summary>
	public class RustDroneStaff : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 11;
		protected override int UseTime => 30;
		protected override float Knockback => 2f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(gold: 1);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustDrone>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.Rust.RustDroneBuff>(), 10);
			Item.width = 36;
			Item.height = 36;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.IronBar, 10)
				.AddIngredient(ItemID.Wood, 12)
				.AddIngredient(ItemID.Gel, 12)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.LeadBar, 10)
				.AddIngredient(ItemID.Wood, 12)
				.AddIngredient(ItemID.Gel, 12)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Rust Spitter Staff（锈蚀喷吐者召唤杖）
	/// <para/>定位：前期「远程消耗型」仆从。召唤 1 只 9 伤害的喷吐者（占 1 个仆从位），
	/// 悬浮在主人身侧，每 1.17 秒朝 620 像素内最近敌人吐一发酸弹（4 伤害、附中毒）。贴脸伤害只有无人机的一半，
	/// 但能安全吃线；和 Hornet Staff(11) 相比更脆、更依赖玩家拉住仇恨。
	/// </summary>
	public class RustSpitterStaff : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 9;
		protected override int UseTime => 28;
		protected override float Knockback => 2f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(gold: 1);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustSpitter>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.Rust.RustSpitterBuff>(), 10);
			Item.width = 34;
			Item.height = 34;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Wood, 14)
				.AddIngredient(ItemID.IronBar, 8)
				.AddIngredient(ItemID.Gel, 10)
				.AddIngredient<SalvagedSteelChunk>(6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.Wood, 14)
				.AddIngredient(ItemID.LeadBar, 8)
				.AddIngredient(ItemID.Gel, 10)
				.AddIngredient<SalvagedSteelChunk>(6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Rust Spitter Staff MK-II（锈蚀喷吐者召唤杖 · 强化型）
	/// <para/>定位：喷吐者召唤杖的强化版，**继承 <see cref="RustSpitterStaff"/>**。
	/// 仆从伤害 13、射击间隔 55 帧、一次吐**两发扇形**酸弹（各 6 伤害、穿透 2）；
	/// 蓝耗从 10 提到 12，属于「一只仆从当两只用」的前期顶级召唤，但仍低于 Imp Staff(17) 的单发。
	/// </summary>
	public class RustSpitterStaffEX : RustSpitterStaff
	{
		protected override int Damage => 13;
		protected override int UseTime => 26;
		protected override float Knockback => 2.5f;
		protected override int SellPrice => Item.sellPrice(gold: 2, silver: 20);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustSpitterEX>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 38;
			Item.height = 38;
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.Rust.RustSpitterBuffEX>(), 12);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RustSpitterStaff>()
				.AddIngredient<SalvagedSteelBar>(6)
				.AddIngredient(ItemID.Gel, 14)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
