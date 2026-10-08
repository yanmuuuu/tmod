using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;
using WastelandSoul.Content.Projectiles.UpgradeTrees;

namespace WastelandSoul.Content.Items.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树（七）· 盗贼 / 投掷：锈蚀齿轮镖 → 精钢锯齿环 → 灰烬之心余烬环
	//
	// ⚠️ 本模组的盗贼**单件约定**（见 CLineWeapons 的 CLineKind.Rogue 分支）：
	//    这类武器是"掉出来/做出来就这一把"的单件，**不消耗、不叠放（堆叠 1）**，
	//    丢出去靠**回旋回收**而不是丢一把少一把。
	//    所以三级都走 RogueTreeKit.Single()，而**不**调用 WastelandWeaponKit.Rogue()
	//    （那个是"可堆叠 999 + 投出即消耗"的消耗品形态，会跟单件约定打架）。
	//
	// 三级差异（不是纯数值 +10%）：
	//   锈蚀齿轮镖        单段回旋：飞出去 30 帧后回手，全程穿透，无附加效果
	//   精钢锯齿环        回旋 + **命中爆炸**：撞到敌人就炸开一圈锯齿碎刃（25 帧一次上限）
	//   灰烬之心余烬环    **三段相位**：去程 → 悬停原地切割 45 帧（点燃 + 削防）→ 回手，
	//                     并且每次命中都会溅出 1 枚追踪余烬（最多 5 枚）
	//
	// 每级各有自己的配方（在合成界面可见；物品介绍里**不写**制作方法）。
	// ====================================================================================

	/// <summary>盗贼单件武器的公共形态（不消耗 / 堆叠 1 / 隐藏手持贴图 / 可连投）。</summary>
	internal static class RogueTreeKit
	{
		/// <summary>
		/// 单件投掷形态：**不消耗**、堆叠 1、隐藏手上的贴图，伤害全在飞出去的那一发上。
		/// <para/>之所以不直接用 <see cref="WastelandWeaponKit.Rogue"/>：那套是"可堆叠 999 +
		/// 投出即消耗"的量产消耗品形态，与单件约定相反。
		/// </summary>
		public static void Single(Item item)
		{
			item.useStyle = ItemUseStyleID.Swing;
			item.noMelee = true;
			item.noUseGraphic = true;
			item.consumable = false;   // 单件：丢出去还能捡回来（回旋）
			item.maxStack = 1;
			item.autoReuse = true;
			item.UseSound = SoundID.Item19;
		}
	}

	/// <summary>
	/// Rusted Gear Shuriken（锈蚀齿轮镖）
	/// <para/>定位：这条树的树根。伤害 18、使用时间 16；丢出去的是**会回手**的齿轮镖：
	/// 去程 30 帧、全程穿透（本地无敌帧 14），回程还能再打一次同一个敌人。
	/// 因为不消耗，它的定位是"站位换输出"而不是"拿数量堆伤害"。
	/// </summary>
	public class RustedGearShuriken : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 18;
		protected override int UseTime => 16;
		protected override float Knockback => 3f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(gold: 1, silver: 50);
		protected override int ShootType => ModContent.ProjectileType<RustedGearBoomerang>();
		protected override float ShootSpeed => 13f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			RogueTreeKit.Single(Item);
			Item.width = 24;
			Item.height = 24;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RustedGear>(6)
				.AddIngredient<SalvagedSteelChunk>(8)
				.AddIngredient<Chip>(2)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Salvaged Steel Chakram（精钢锯齿环）
	/// <para/>定位：进阶件把"回旋"变成"回旋 + 炸"。伤害 26、使用时间 14、回程更快；
	/// 命中敌人时会在原地炸开一个 44x44 的锯齿爆裂（伤害取面板一半，25 帧内只炸一次，
	/// 不会伤到自己），所以它对**怪群**的收益远高于对单体的收益。
	/// </summary>
	public class SalvagedSteelChakram : RustedGearShuriken
	{
		protected override int Damage => 26;
		protected override int UseTime => 14;
		protected override float Knockback => 4f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 3);
		protected override int ShootType => ModContent.ProjectileType<SalvagedSteelChakramProj>();
		protected override float ShootSpeed => 14f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 26;
			Item.height = 26;
		}

		public override void AddRecipes()
		{
			// 进阶件：树根 + 精钢锭重新开齿 + 归档者碎片对齐回旋参数
			CreateRecipe()
				.AddIngredient<RustedGearShuriken>()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient<ArchivistFragment>(4)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Ash Heart Cinder Ring（灰烬之心余烬环）
	/// <para/>定位：终阶，飞行轨迹从"一去一回"改成**三段**：
	/// 去程 28 帧 → 在落点**悬停原地切割 45 帧**（这一段每次命中都点燃 4 秒并削防 3 秒，
	/// 命中频率压到 8 帧）→ 回手。并且每次命中溅出 1 枚**追踪余烬**（最多 5 枚）。
	/// 对站桩/贴脸的敌人是这条树最高的一段输出，代价是落点要丢准。
	/// </summary>
	public class AshHeartCinderRing : SalvagedSteelChakram
	{
		protected override int Damage => 36;
		protected override int UseTime => 13;
		protected override float Knockback => 4.5f;
		protected override int Rarity => WastelandRarityTiers.MidLate;
		protected override int SellPrice => Item.sellPrice(gold: 6);
		protected override int ShootType => ModContent.ProjectileType<AshHeartCinderRingProj>();
		protected override float ShootSpeed => 15f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 28;
			Item.height = 28;
			Item.UseSound = SoundID.Item73;
		}

		public override void AddRecipes()
		{
			// 终阶件：进阶环 + 灰烬之心合金锭 + 灰烬结晶做余烬核心 + 电路板重写回旋逻辑
			CreateRecipe()
				.AddIngredient<SalvagedSteelChakram>()
				.AddIngredient<AshHeartAlloyBar>(8)
				.AddIngredient<AshCrystal>(5)
				.AddIngredient<CircuitBoard>(4)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
