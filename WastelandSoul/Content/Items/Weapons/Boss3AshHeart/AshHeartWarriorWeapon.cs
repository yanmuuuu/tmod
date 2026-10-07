using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Greatblade（灰烬之心 · A 线 · 可合成）
	/// <para/>设计定位：定位=叶绿大剑(75/慢)与钥匙剑(70/快)之间的中速重剑：单次伤害略低、攻速略慢，但每挥一刀沿弧线甩出一道缓慢的余烬剑气（不穿透、命中后在地面留下 1.5 秒的余灰），对成排小怪比叶绿大剑舒服。手感偏『沉、烫、有拖尾』。专属材料链：AshHeartFragment ×2 →（精金熔炉）→ AshHeartAlloyBar。
	/// <para/>制作站点：秘银砧（血肉墙之后装备站点）
	/// </summary>
	public class AshHeartWarriorWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 72;
		protected override int UseTime => 24;
		protected override float Knockback => 6.5f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartWarriorProjectile>();

		protected override float ShootSpeed => 11.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshHeartAlloyBar>(10)
				.AddIngredient(ItemID.ChlorophyteBar, 12)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddIngredient(ItemID.Ectoplasm, 6)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
