using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Shard（灰烬之心 · A 线 · 可合成）
	/// <para/>设计定位：『灰烬裂片』投掷：按原版投掷手感实现（Ranged 类，不依赖 Thorium 等模组的 Throwing 伤害类）。抛出带轻微重力的旋转裂片，命中后在原地自旋 1.5 秒持续割伤同一目标，可回收复用。量级取影焰小刀(52，硬核前期)之上、附身飞斧(80，石巨人后)之下的中间值 60。
	/// <para/>制作站点：秘银砧（血肉墙之后装备站点）
	/// </summary>
	public class AshHeartRogueWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 60;
		protected override int UseTime => 15;
		protected override float Knockback => 4.5f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartRogueProjectile>();

		protected override float ShootSpeed => 13.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
		}

		public override void AddRecipes()
		{
			CreateRecipe(5)
				.AddIngredient<AshHeartAlloyBar>(10)
				.AddIngredient(ItemID.ChlorophyteBar, 10)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddIngredient(ItemID.Ectoplasm, 8)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
