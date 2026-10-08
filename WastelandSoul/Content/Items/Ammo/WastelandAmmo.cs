using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Projectiles.Ranged;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Ammo
{
	// ====================================================================================
	// 弹药扩充（4 种）。共同点收在 WastelandAmmo 里，子类只写"伤害 / 弹幕 / 稀有度 / 售价"。
	//
	// 设计取向：三种实弹（钉 / 冷光 / 余烬）走原版子弹槽，一种法术弹药（残页）走箭矢槽，
	//          让"用废土材料自己搓子弹"成为前期就能成立的循环。
	// ⚠️ 物品介绍里不许写制作方法。
	// ====================================================================================

	/// <summary>
	/// 弹药基类：设置 <c>Item.ammo</c> / <c>Item.shoot</c> / <c>Item.damage</c> 与远程职业参数。
	/// <para/>注意：<c>Item.DamageType</c> 用 <see cref="DamageClass.Ranged"/> 时，
	/// 弹药伤害会按原版规则乘到武器伤害上；法术弹药请传 <see cref="DamageClass.Magic"/>。
	/// </summary>
	public abstract class WastelandAmmo : ModItem
	{
		/// <summary>对应的弹幕类型。</summary>
		protected abstract int ShotType { get; }

		/// <summary>弹药分类（决定进哪个弹药槽）。</summary>
		protected abstract int AmmoType { get; }

		/// <summary>弹药自身伤害。</summary>
		protected abstract int Damage { get; }

		/// <summary>伤害类型（默认远程）。</summary>
		protected virtual DamageClass Class => DamageClass.Ranged;

		/// <summary>击退。</summary>
		protected virtual float Knockback => 2f;

		/// <summary>图标边长。</summary>
		protected virtual int IconSize => 14;

		/// <summary>稀有度。</summary>
		protected virtual int Rarity => ItemRarityID.Blue;

		/// <summary>售价（铜币）。</summary>
		protected virtual int SellPrice => Terraria.Item.sellPrice(copper: 14);

		public override void SetDefaults()
		{
			Item.width = IconSize;
			Item.height = IconSize;
			Item.maxStack = 9999;
			Item.damage = Damage;
			Item.DamageType = Class;
			Item.knockBack = Knockback;
			Item.ammo = AmmoType;
			Item.shoot = ShotType;
			Item.shootSpeed = 9.5f;
			Item.value = SellPrice;
			Item.rare = Rarity;
			Item.consumable = true;
		}
	}

	/// <summary>
	/// 废料钉：把废铁片剪成钉，用旧弹壳的底火打出去。前期最容易量产的弹药。
	/// <para/>特点：伤害中规中矩、速度快、几乎不吃材料——废土上"有得打"比"打得疼"重要。
	/// </summary>
	public class ScrapNail : WastelandAmmo
	{
		protected override int ShotType => ModContent.ProjectileType<ScrapNailProjectile>();

		protected override int AmmoType => AmmoID.Bullet;

		protected override int Damage => 7;

		protected override float Knockback => 1.8f;

		protected override int SellPrice => Terraria.Item.sellPrice(copper: 6);

		public override void AddRecipes()
		{
			CreateRecipe(45)
				.AddIngredient<RustedGear>(1)
				.AddIngredient<SalvagedSteelChunk>(1)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	/// <summary>
	/// 冷光弹：弹头里塞了一小罐冷却液，会自己发光并冻伤命中的目标。
	/// <para/>特点：伤害略高、穿透 1 个敌人、命中附加**霜冻**；夜里自带照明。
	/// </summary>
	public class ColdlightRound : WastelandAmmo
	{
		protected override int ShotType => ModContent.ProjectileType<ColdlightProjectile>();

		protected override int AmmoType => AmmoID.Bullet;

		protected override int Damage => 9;

		protected override float Knockback => 2.4f;

		protected override int Rarity => ItemRarityID.Green;

		protected override int SellPrice => Terraria.Item.sellPrice(copper: 22);

		public override void AddRecipes()
		{
			CreateRecipe(35)
				.AddIngredient(ItemID.SilverBullet, 35)
				.AddIngredient<Coolant>(1)
				.AddIngredient<CircuitBoard>(1)
				.AddTile(TileID.Anvils)
				.Register();

			// 钨弹（腐化世界出钨，不用银）走同一条配方
			CreateRecipe(35)
				.AddIngredient(ItemID.TungstenBullet, 35)
				.AddIngredient<Coolant>(1)
				.AddIngredient<CircuitBoard>(1)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	/// <summary>
	/// 余烬弹：弹壳里压了焦炭粉，命中之后还在烧。
	/// <para/>特点：伤害最高、穿透 2 个敌人，命中附加**着火了！**（4 秒）。
	/// </summary>
	public class EmberShell : WastelandAmmo
	{
		protected override int ShotType => ModContent.ProjectileType<EmberShellProjectile>();

		protected override int AmmoType => AmmoID.Bullet;

		protected override int Damage => 12;

		protected override float Knockback => 3.2f;

		protected override int Rarity => ItemRarityID.Orange;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 1);

		public override void AddRecipes()
		{
			CreateRecipe(25)
				.AddIngredient(ItemID.MusketBall, 25)
				.AddIngredient<Coke>(2)
				.AddIngredient<AshCrystal>(1)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	/// <summary>
	/// 残页：归档者散落的索引页，边缘还留着没写完的判词——把它塞进任何远程武器都能打出去。
	/// <para/>特点：**法术伤害**（吃魔法加成而不吃远程加成）、穿透 4 个敌人、飞行中会翻滚。
	/// 之所以做成法术弹药：废土上的纸不是用来挡子弹的，是用来"引用"目标的。
	/// </summary>
	public class TornPage : WastelandAmmo
	{
		protected override int ShotType => ModContent.ProjectileType<TornPageProjectile>();

		protected override int AmmoType => AmmoID.Arrow;

		protected override int Damage => 15;

		protected override DamageClass Class => DamageClass.Magic;

		protected override float Knockback => 1.5f;

		protected override int IconSize => 16;

		protected override int Rarity => ItemRarityID.LightRed;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 2);

		public override void AddRecipes()
		{
			CreateRecipe(20)
				.AddIngredient<ArchivistFragment>(1)
				.AddIngredient(ItemID.Book, 1)
				.AddIngredient(ItemID.SoulofNight, 2)
				.AddTile(TileID.Bookcases)
				.Register();
		}
	}
}
