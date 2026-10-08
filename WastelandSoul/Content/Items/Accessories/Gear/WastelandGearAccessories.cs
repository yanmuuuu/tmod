using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Common.Players;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Accessories.Gear
{
	// ====================================================================================
	// 装备扩充包：12 件饰品。
	//
	// 设计原则：
	//   1. **每件一个明确差异**，不做"伤害 +3%"这种互相替代的凑数件；
	//   2. 都走 WastelandAccessory 基类，效果写在 UpdateWastelandAccessory；
	//   3. 需要跨帧/跨事件的状态一律放 WastelandGearPlayer，不改共享的 WastelandPlayer.cs；
	//   4. 需要"玩家侧状态"的（护盾、反弹、致命保护）都在 GearPlayer 里，饰品本体只置位标记。
	//
	// 档位（按稀有度颜色与原版同期对齐）：
	//   Orange    Early      拾荒者背带 / 净化过滤面罩
	//   LightRed  EarlyLate  废土指针 / 深渊呼吸器
	//   Lime      MidLate    煤渣踏靴 / 余烬玻璃透镜 / 祝圣之核 / 灰烬回响护符
	//   Red       Late       炉火信标 / 炉火护盾 / 炉火镜面 / 明日之匣
	// ====================================================================================

	// ====================================================================================
	// 前期：拾荒者背带 —— 免击退 + 免摔伤 + 跑得快（"摔不死的搬砖工"）
	// ====================================================================================

	/// <summary>拾荒者背带：免疫击退与坠落伤害，移动速度 +10%。</summary>
	public class ScavengerHarness : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int SellPrice => Item.sellPrice(gold: 1);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.noKnockback = true;
			player.noFallDmg = true;
			player.moveSpeed += 0.10f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Rope, 12)
				.AddIngredient(ItemID.Leather, 5)
				.AddIngredient(ItemID.Silk, 4)
				.AddIngredient<SalvagedSteelBar>(3)
				.AddTile(TileID.Loom)
				.Register();
		}
	}

	// ====================================================================================
	// 前期：净化过滤面罩 —— 站进污染与有毒大气里也不掉那么快（独立于已有的防毒面具）
	// ====================================================================================

	/// <summary>
	/// 净化过滤面罩：污染 / 有毒大气造成的伤害 **-65%**，另给 +2 防御。
	/// <para/>与已有的 GasMask 不冲突：防毒面具是"直接免疫壁炉大气"，
	/// 这件是"任何来源的污染伤害都打折"，两件可以一起戴。
	/// </summary>
	public class FilteredRebreather : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int SellPrice => Item.sellPrice(gold: 1, silver: 50);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.statDefense += 2;
			player.GetModPlayer<WastelandGearPlayer>().pollutionFilterEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Glass, 5)
				.AddIngredient(ItemID.Silk, 3)
				.AddIngredient<SalvagedSteelBar>(4)
				.AddIngredient<Chip>(2)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	// ====================================================================================
	// 前期后段：废土指针 —— 掉落率与幸运（"捡垃圾的人运气不会差"）
	// ====================================================================================

	/// <summary>
	/// 废土指针：幸运 +0.4、弹药 15% 概率不消耗，并降低仇恨（不容易被围）。
	/// <para/>幸运影响怪物掉落与部分随机判定（原版幸运药水走的就是这条通道）；
	/// 顺手给一点弹药节省，让它不至于"只有数字没有手感"。
	/// </summary>
	public class WastelandCompass : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int SellPrice => Item.sellPrice(gold: 2);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.luck += 0.4f;
			player.aggro -= 200;

			WastelandGearPlayer gear = player.GetModPlayer<WastelandGearPlayer>();
			gear.wastelandCompassEquipped = true;
			gear.gearAmmoSave += 0.15f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Compass, 1)
				.AddIngredient(ItemID.GoldBar, 5)
				.AddIngredient<Chip>(4)
				.AddIngredient(ItemID.Torch, 3)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	// ====================================================================================
	// 前期后段：深渊呼吸器 —— 下水不再怕（水下呼吸 + 免疫潮湿）
	// ====================================================================================

	/// <summary>深渊呼吸器：水下呼吸上限 +200、可游泳、陆上沾水时移速补偿，免疫潮湿减益。</summary>
	public class AbyssBreather : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int SellPrice => Item.sellPrice(gold: 2);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.breathMax += 200;
			player.ignoreWater = true;
			player.accFlipper = true;
			player.buffImmune[BuffID.Wet] = true;

			if (player.wet) {
				player.moveSpeed += 0.08f;
			}

			player.GetModPlayer<WastelandGearPlayer>().abyssBreatherEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.DivingHelmet, 1)
				.AddIngredient(ItemID.Flipper, 1)
				.AddIngredient(ItemID.Glass, 6)
				.AddIngredient(ItemID.Coral, 5)
				.AddIngredient<SalvagedSteelBar>(4)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	// ====================================================================================
	// 中期后段：煤渣踏靴 —— 冲刺（下 + 方向键，手感对齐克苏鲁之盾）
	// ====================================================================================

	/// <summary>
	/// 煤渣踏靴：**冲刺** —— 按【下 + 左/右】朝该方向冲出去，冲刺期间无敌并拖着火星。
	/// <para/>手感对齐原版克苏鲁之盾（<c>Player.dashType = 1</c>），但无敌帧压在 22 帧、
	/// 冷却 40 帧，避免变成"无限闪避"。
	/// </summary>
	public class CinderstepBoots : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 4);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.moveSpeed += 0.08f;
			player.accRunSpeed = System.Math.Max(player.accRunSpeed, 6.4f);
			player.GetModPlayer<WastelandGearPlayer>().cinderstepEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.HermesBoots, 1)
				.AddIngredient<AshHeartAlloyBar>(6)
				.AddIngredient<AshHeartFragment>(4)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	// ====================================================================================
	// 中期后段：余烬玻璃透镜 —— 命中叠灼烧（远程向）
	// ====================================================================================

	/// <summary>余烬玻璃透镜：远程伤害 +8%、远程暴击 +5%，命中 25% 概率挂"着火了！"。</summary>
	public class EmberglassLens : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 4);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Ranged) += 0.08f;
			player.GetCritChance(DamageClass.Ranged) += 5f;

			WastelandGearPlayer gear = player.GetModPlayer<WastelandGearPlayer>();
			gear.emberLensEquipped = true;
			gear.gearEmberOnHit += 0.25f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Obsidian, 10)
				.AddIngredient<AshHeartFragment>(6)
				.AddIngredient<AshHeartAlloyBar>(4)
				.AddIngredient(ItemID.Torch, 20)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	// ====================================================================================
	// 中期后段：祝圣之核 —— 召唤向（多一个仆从 + 鞭子更长）
	// ====================================================================================

	/// <summary>祝圣之核：召唤伤害 +12%、+1 仆从位、鞭子距离 +20%、仆从击退 +1。</summary>
	public class ConsecratedCore : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 4);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Summon) += 0.12f;
			player.maxMinions += 1;
			player.whipRangeMultiplier += 0.20f;
			player.GetKnockback(DamageClass.Summon).Base += 1f;
			player.GetModPlayer<WastelandGearPlayer>().consecratedCoreEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.PygmyNecklace, 1)
				.AddIngredient<ArchivistFragment>(8)
				.AddIngredient(ItemID.SoulofNight, 10)
				.AddIngredient<AshHeartAlloyBar>(4)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	// ====================================================================================
	// 中期后段：灰烬回响护符 —— 近战向，命中点燃
	// ====================================================================================

	/// <summary>灰烬回响护符：近战伤害 +9%、近战攻速 +8%，命中 20% 概率挂"着火了！"。</summary>
	public class AshEchoCharm : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 4);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Melee) += 0.09f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.08f;

			WastelandGearPlayer gear = player.GetModPlayer<WastelandGearPlayer>();
			gear.ashEchoEquipped = true;
			gear.gearEmberOnHit += 0.20f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.MagmaStone, 1)
				.AddIngredient<AshHeartAlloyBar>(6)
				.AddIngredient<AshHeartFragment>(5)
				.AddIngredient(ItemID.Fireblossom, 3)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	// ====================================================================================
	// 后期：炉火信标 —— 生命再生 + 拾取范围（团队向的"篝火")
	// ====================================================================================

	/// <summary>炉火信标：生命再生 +3、自动吸取附近心之碎片（生命磁铁）。</summary>
	public class HearthBeacon : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int SellPrice => Item.sellPrice(gold: 6);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.lifeRegen += 3;
			player.lifeMagnet = true;
			player.GetModPlayer<WastelandGearPlayer>().hearthBeaconEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.HeartLantern, 1)
				.AddIngredient(ItemID.Campfire, 2)
				.AddIngredient<FireplaceAlloyBar>(6)
				.AddIngredient<FireplaceFragment>(5)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}

	// ====================================================================================
	// 后期：炉火护盾 —— 挨打前减伤，挨打后碎盾 8 秒
	// ====================================================================================

	/// <summary>
	/// 炉火护盾：护盾完好时受到伤害 **-14%**；一旦挨打护盾就碎，
	/// 8 秒内不再减伤，并泄出一圈冷火（减速附近敌人，纯表现 + 减益）。
	/// <para/>"会碎的减伤"比常驻减伤更适合长线战斗：能扛爆发，但不能一直白嫖。
	/// </summary>
	public class HearthAegis : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int SellPrice => Item.sellPrice(gold: 6);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.statDefense += 5;
			player.noKnockback = true;
			player.GetModPlayer<WastelandGearPlayer>().hearthAegisEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.FrozenShield, 1)
				.AddIngredient(ItemID.AnkhShield, 1)
				.AddIngredient<FireplaceAlloyBar>(8)
				.AddIngredient<FireplaceFragment>(6)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}

	// ====================================================================================
	// 后期：炉火镜面 —— 受伤时反弹一部分伤害给最近的敌人
	// ====================================================================================

	/// <summary>炉火镜面：受到伤害时，把其中 **35%** 化作余烬碎片弹向 460 像素内最近的敌人。</summary>
	public class HearthMirror : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int SellPrice => Item.sellPrice(gold: 6);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.statDefense += 3;
			player.GetModPlayer<WastelandGearPlayer>().hearthMirrorEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.PocketMirror, 1)
				.AddIngredient(ItemID.MagicMirror, 1)
				.AddIngredient(ItemID.HellstoneBar, 8)
				.AddIngredient<FireplaceAlloyBar>(6)
				.AddIngredient<FireplaceFragment>(4)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}

	// ====================================================================================
	// 后期：明日之匣 —— 一次致命伤害免疫（45 秒冷却）
	// ====================================================================================

	/// <summary>
	/// 明日之匣：**45 秒一次**，把原本会致死的一击直接取消（只对来自敌人的伤害生效）。
	/// <para/>给的是"再来一次"而不是常驻减伤，所以可以和任何 build 叠，也不会让 Boss 变简单。
	/// </summary>
	public class MorrowFragment : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int SellPrice => Item.sellPrice(gold: 8);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Generic) += 0.05f;
			player.GetModPlayer<WastelandGearPlayer>().morrowFragmentEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.CrossNecklace, 1)
				.AddIngredient(ItemID.LifeFruit, 2)
				.AddIngredient<FireplaceAlloyBar>(10)
				.AddIngredient<ArchivistFragment>(6)
				.AddIngredient<AshHeartFragment>(6)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
