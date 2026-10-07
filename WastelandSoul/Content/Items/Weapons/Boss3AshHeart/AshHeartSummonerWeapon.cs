using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Cinder Staff（灰烬之心 · A 线 · 可合成）
	/// <para/>设计定位：召唤『灰烬残影』：近战扑咬型仆从，攻击后留下指甲盖大的余烬（存在 2 秒，小额接触伤害）。单只 44 低于乌鸦法杖(55)、高于矮人法杖(40)，一次最多 3 只；靠数量与地面余烬打持续压制，不抢召唤师的主 C 位。
	/// <para/>制作站点：秘银砧（血肉墙之后装备站点）
	/// </summary>
	public class AshHeartSummonerWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 44;
		protected override int UseTime => 26;
		protected override float Knockback => 3.0f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.LateBosses.AshHeartEmberMinion>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.LateBosses.AshHeartEmberBuff>(), 14);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshHeartAlloyBar>(10)
				.AddIngredient(ItemID.ChlorophyteBar, 10)
				.AddIngredient(ItemID.Ectoplasm, 8)
				.AddIngredient(ItemID.HallowedBar, 6)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
