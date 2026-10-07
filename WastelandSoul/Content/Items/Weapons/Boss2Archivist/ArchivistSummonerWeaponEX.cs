using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Familiar Staff MK-II（归档者 · B 线 · 专属掉落）
	/// <para/>设计定位：专属掉落（仅掉落袋）。召唤『归档哨塔』（哨兵类，占哨兵位）：静止悬浮的浮空书架，每 0.8 秒射出一枚追踪书页，命中附加 3 秒困惑。正面 DPS 略低于蜘蛛/弩车，但带软控且可独立布置在战场另一侧，攻守兼备。
	/// </summary>
	public class ArchivistSummonerWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 24;
		protected override int UseTime => 30;
		protected override float Knockback => 4.0f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistFamiliarMinionEX>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.Archivist.ArchivistFamiliarBuffEX>(), 14);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从归档者的掉落袋开出。
	}
}
