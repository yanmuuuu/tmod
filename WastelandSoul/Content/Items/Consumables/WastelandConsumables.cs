using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Buffs;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Consumables
{
	// ====================================================================================
	// 药水 / 食物（6 种）。全部是「物品 + ModBuff + 32×32 图标」成对出现。
	// 共同手感参数收在 WastelandPotion 里，子类只写 buff、时长、稀有度与配方。
	//
	// ⚠️ 物品介绍（Tooltip）里**不许写制作方法**，只写效果与味道。
	// ====================================================================================

	/// <summary>
	/// 药水/食物基类：一口喝掉 / 吃掉，给一个自定义增益。
	/// <para/>默认尺寸 18×18（和原版药水一致）；食物类子类可以改大一点。
	/// </summary>
	public abstract class WastelandPotion : ModItem
	{
		/// <summary>要挂上的增益类型。</summary>
		protected abstract int BuffType { get; }

		/// <summary>增益持续时间（帧，60 帧 = 1 秒）。</summary>
		protected abstract int BuffDuration { get; }

		/// <summary>图标边长。</summary>
		protected virtual int IconSize => 18;

		/// <summary>稀有度。</summary>
		protected virtual int Rarity => ItemRarityID.Blue;

		/// <summary>售价（铜币）。</summary>
		protected virtual int SellPrice => Terraria.Item.sellPrice(silver: 2);

		/// <summary>使用音效。</summary>
		protected virtual SoundStyle? UseSound => SoundID.Item3;

		public override void SetDefaults()
		{
			Item.width = IconSize;
			Item.height = IconSize;
			Item.maxStack = 999;
			Item.value = SellPrice;
			Item.rare = Rarity;
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.useTurn = true;
			Item.consumable = true;
			Item.buffType = BuffType;
			Item.buffTime = BuffDuration;

			if (UseSound.HasValue) {
				Item.UseSound = UseSound.Value;
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BottledWater)
				.AddIngredient(ItemID.Daybloom)
				.AddIngredient(ItemID.Cactus, 2)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}

	/// <summary>
	/// 滤芯药剂：12 分钟免疫有害气体与污染。
	/// <para/>壁炉世界离不开它——没有防毒面具的玩家靠这一瓶换 12 分钟的活动时间。
	/// </summary>
	public class GasFilterPotion : WastelandPotion
	{
		protected override int BuffType => ModContent.BuffType<GasFilterBuff>();

		protected override int BuffDuration => 60 * 60 * 12;

		protected override int Rarity => ItemRarityID.Green;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 8);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BottledWater)
				.AddIngredient(ItemID.Daybloom)
				.AddIngredient<Coke>(2)
				.AddIngredient<Coolant>(1)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}

	/// <summary>掘进液：8 分钟大幅提升挖掘与建造速度，并多挖一格。</summary>
	public class MinersSolution : WastelandPotion
	{
		protected override int BuffType => ModContent.BuffType<MinersSolutionBuff>();

		protected override int BuffDuration => 60 * 60 * 8;

		protected override int Rarity => ItemRarityID.Blue;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 4);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BottledWater)
				.AddIngredient(ItemID.Blinkroot)
				.AddIngredient(ItemID.ShinePotion)     // 原版微光药水做引子
				.AddIngredient<RustedGear>(2)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}

	/// <summary>冷光补剂：10 分钟夜视 + 危险感知 + 探宝，让玩家敢往废土深处走。</summary>
	public class ColdSpotlight : WastelandPotion
	{
		protected override int BuffType => ModContent.BuffType<ColdSpotlightBuff>();

		protected override int BuffDuration => 60 * 60 * 10;

		protected override int Rarity => ItemRarityID.Green;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 6);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BottledWater)
				.AddIngredient(ItemID.Moonglow)
				.AddIngredient(ItemID.NightOwlPotion)
				.AddIngredient<AshCrystal>(2)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}

	/// <summary>纳米修复膏：6 分钟持续回血 + 加速自然恢复。打着打着就回满了。</summary>
	public class NaniteSalve : WastelandPotion
	{
		protected override int BuffType => ModContent.BuffType<NaniteSalveBuff>();

		protected override int BuffDuration => 60 * 60 * 6;

		protected override int Rarity => ItemRarityID.LightRed;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 12);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BottledWater)
				.AddIngredient(ItemID.PixieDust, 3)
				.AddIngredient<CircuitBoard>(1)
				.AddIngredient<Coolant>(2)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}

	/// <summary>兽哨余响：7 分钟召唤伤害 +15%、鞭速与鞭长提升。召唤师的进攻药。</summary>
	public class BeastWhistle : WastelandPotion
	{
		protected override int BuffType => ModContent.BuffType<BeastWhistleBuff>();

		protected override int BuffDuration => 60 * 60 * 7;

		protected override int Rarity => ItemRarityID.Orange;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 10);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BottledWater)
				.AddIngredient(ItemID.Bone, 4)
				.AddIngredient(ItemID.SummoningPotion)
				.AddIngredient<AshCrystal>(3)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}

	/// <summary>
	/// 灰烬口粮：压缩过的废土口粮。食物类，尺寸稍大（20×20），给饱食 + 全伤害 + 恢复。
	/// </summary>
	public class AshenRation : WastelandPotion
	{
		protected override int BuffType => ModContent.BuffType<AshenRationBuff>();

		protected override int BuffDuration => 60 * 60 * 8;

		protected override int IconSize => 20;

		protected override int Rarity => ItemRarityID.Green;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 5);

		protected override SoundStyle? UseSound => SoundID.Item2;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.EatFood;
			Item.useAnimation = 20;
			Item.useTime = 20;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Bowl)
				.AddIngredient<Coke>(2)
				.AddIngredient(ItemID.Mushroom, 2)
				.AddIngredient(ItemID.Hay, 5)
				.AddTile(TileID.CookingPots)
				.Register();
		}
	}
}
