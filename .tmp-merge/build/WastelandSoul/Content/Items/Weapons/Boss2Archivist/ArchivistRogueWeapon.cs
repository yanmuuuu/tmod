using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Bone Boomerang（归档者 · A 线 · 可合成）
	/// <para/>设计定位：投掷用骨质回旋镖（按投掷伤害处理）：去程穿透 1 个敌人、命中 2 次或到达最大距离后回旋，返回途中再造成 70% 伤害并自带微弱追踪，落地可拾回。定位是本时期『安全消耗』武器，单发上限高但需要走位接镖，连投手感类似荆棘轮。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ArchivistRogueWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 26;
		protected override int UseTime => 20;
		protected override float Knockback => 6.5f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistBoneBoomerang>();

		protected override float ShootSpeed => 11.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
		}

		public override void AddRecipes()
		{
			CreateRecipe(5)
				.AddIngredient<ArchivistFragment>(9)
				.AddIngredient(ItemID.MeteoriteBar, 6)
				.AddIngredient(ItemID.Bone, 12)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
