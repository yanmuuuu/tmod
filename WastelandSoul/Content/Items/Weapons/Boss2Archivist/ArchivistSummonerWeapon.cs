using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Familiar Staff（归档者 · A 线 · 可合成）
	/// <para/>设计定位：召唤『档案浮空使魔』（占 1 仆从位）：慢速漂浮于玩家上方，每 1 秒投下一枚坠落书页，命中造成小额伤害并附带 2 秒缓慢。主打『自动补刀+减速』，单体 DPS 低于小鬼法杖，但双使魔时对群控场非常舒服（腐化世界可把碎片换成 TissueSample x8）。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ArchivistSummonerWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 19;
		protected override int UseTime => 36;
		protected override float Knockback => 3.0f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistFamiliarMinion>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.Archivist.ArchivistFamiliarBuff>(), 12);
		}

		public override void AddRecipes()
		{
			// 分支 1：ArchivistFragment / Bone / ShadowScale
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(12)
				.AddIngredient(ItemID.Bone, 20)
				.AddIngredient(ItemID.ShadowScale, 8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			// 分支 2：ArchivistFragment / Bone / TissueSample
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(12)
				.AddIngredient(ItemID.Bone, 20)
				.AddIngredient(ItemID.TissueSample, 8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
