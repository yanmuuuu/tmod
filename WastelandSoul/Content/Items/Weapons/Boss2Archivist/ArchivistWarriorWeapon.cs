using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Boneblade（归档者 · A 线 · 可合成）
	/// <para/>设计定位：骨刃长剑，定位=骷髅王后的稳定真近战。挥砍时向前抛出一小段短距骨片弧光（约 3 格射程、不穿透、伤害为面板的 60%），用来补足纯近战贴脸风险；击退偏高、前摇略慢，手感介于草薙与村正之间，强调『能砍能推』而非无脑连点。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ArchivistWarriorWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 25;
		protected override int UseTime => 22;
		protected override float Knockback => 5.5f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistBoneArc>();

		protected override float ShootSpeed => 10.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(8)
				.AddIngredient(ItemID.Bone, 15)
				.AddIngredient(ItemID.MeteoriteBar, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
