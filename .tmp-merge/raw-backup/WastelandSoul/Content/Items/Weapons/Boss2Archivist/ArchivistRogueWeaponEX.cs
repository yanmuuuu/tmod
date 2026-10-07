using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Bone Boomerang MK-II（归档者 · B 线 · 专属掉落）
	/// <para/>设计定位：专属掉落（仅掉落袋）。投掷『封存骨标』（按投掷伤害处理，不依赖任何模组）：飞出约 10 格后突然折返、可穿透 2 个敌人，回程带加速；到达玩家身边时若命中过敌人则分裂出 2 枚可拾回的小骨片。单发爆发高、上限吃命中率，是本时期盗贼主手级投掷物。
	/// </summary>
	public class ArchivistRogueWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 38;
		protected override int UseTime => 18;
		protected override float Knockback => 5.0f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistBoneBoomerangEX>();

		protected override float ShootSpeed => 12.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从归档者的掉落袋开出。
	}
}
