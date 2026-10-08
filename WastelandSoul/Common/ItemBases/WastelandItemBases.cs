using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace WastelandSoul.Common.ItemBases
{
	// ====================================================================================
	// 全 Boss 共用的物品基类。
	// 目的：后面 4 个 Boss 的召唤物 / 材料 / 各职业武器 / 饰品都只写"数值与差异"，
	//       公共行为（不消耗、同时仅一只、默认尺寸、配方模板）都收在这里。
	// 说明：抽象类不会被 tModLoader 自动加载，所以这些基类不需要贴图。
	// ====================================================================================

	/// <summary>
	/// 材料基类：所有 Boss 掉落材料（含精钢碎块这类）都从这里派生。
	/// </summary>
	public abstract class WastelandMaterial : ModItem
	{
		/// <summary>物品图标边长（默认 20）。</summary>
		protected virtual int IconSize => 20;

		/// <summary>稀有度。</summary>
		protected virtual int Rarity => ItemRarityID.Blue;

		/// <summary>售价（铜币）。</summary>
		protected virtual int SellPrice => Item.sellPrice(silver: 3);

		public override void SetDefaults()
		{
			Item.width = IconSize;
			Item.height = IconSize;
			Item.maxStack = 9999;
			Item.value = SellPrice;
			Item.rare = Rarity;
		}
	}

	/// <summary>
	/// Boss 召唤物基类：**不消耗**、**同一时间只允许一只该 Boss**，使用时把 Boss 引到玩家附近。
	/// <para/>子类只需给出 <see cref="BossType"/> 与配方。
	/// </summary>
	public abstract class WastelandSummonItem : ModItem
	{
		/// <summary>要召唤的 Boss 的 NPC 类型。</summary>
		protected abstract int BossType { get; }

		/// <summary>生成距离（水平）。</summary>
		protected virtual float SpawnDistanceMin => 520f;

		protected virtual float SpawnDistanceMax => 760f;

		/// <summary>生成高度（相对玩家，负值在上方）。</summary>
		protected virtual float SpawnHeight => -380f;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = 1;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.LightRed;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.UseSound = SoundID.Roar;
			Item.consumable = false;   // 不消耗
			Item.noMelee = true;
		}

		/// <summary>场上是否已经存在这只 Boss。</summary>
		public static bool BossActive(int npcType)
		{
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (npc.active && npc.type == npcType) {
					return true;
				}
			}

			return false;
		}

		public override bool CanUseItem(Player player)
		{
			// 对应的 Boss 还没实现（BossType 为 0）：给出提示而不是静默什么都不做
			if (BossType <= 0) {
				ShowThrottledMessage(player, Language.GetTextValue("Mods.WastelandSoul.Messages.BossNotImplemented"), new Color(240, 200, 120));
				return false;
			}

			if (!BossActive(BossType)) {
				return true;
			}

			ShowThrottledMessage(player, Language.GetTextValue("Mods.WastelandSoul.Messages.BossAlreadyActive"), new Color(220, 120, 90));
			return false;
		}

		/// <summary>
		/// 按帧节流地显示一条提示，避免按住鼠标刷屏。
		/// <para/>颜色传 <see cref="Color"/>：<c>Main.NewText</c> 有 <c>(string, byte, byte, byte)</c>
		/// 这种重载，若实参是三个 int 会被重载解析选中而编译不过。
		/// </summary>
		private static void ShowThrottledMessage(Player player, string message, Color color)
		{
			if (player.whoAmI != Main.myPlayer || Main.GameUpdateCount % 180u >= 3u) {
				return;
			}

			Main.NewText(message, color);
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer) {
				return null;
			}

			if (BossType <= 0 || Main.netMode == NetmodeID.MultiplayerClient) {
				return true;   // 未实现 / 联机同步方案待定
			}

			float side = Main.rand.NextBool() ? 1f : -1f;
			Vector2 position = player.Center + new Vector2(side * Main.rand.NextFloat(SpawnDistanceMin, SpawnDistanceMax), SpawnHeight);

			int index = NPC.NewNPC(new EntitySource_SpawnNPC(), (int)position.X, (int)position.Y, BossType);

			if (index >= 0 && index < Main.maxNPCs) {
				Main.npc[index].netUpdate = true;
			}

			if (!Main.dedServ) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.BossSummoned"), 226, 90, 70);
			}

			return true;
		}
	}

	/// <summary>
	/// 各职业武器基类：子类给出职业、伤害与弹幕，其余使用手感参数有默认值。
	/// </summary>
	public abstract class WastelandClassWeapon : ModItem
	{
		/// <summary>职业伤害类型（DamageClass.Melee / Magic / Ranged / Summon）。</summary>
		protected abstract DamageClass Class { get; }

		/// <summary>基础伤害。</summary>
		protected abstract int Damage { get; }

		/// <summary>使用时间（越小越快）。</summary>
		protected virtual int UseTime => 24;

		/// <summary>击退。</summary>
		protected virtual float Knockback => 4f;

		/// <summary>稀有度。</summary>
		protected virtual int Rarity => ItemRarityID.Orange;

		/// <summary>售价。</summary>
		protected virtual int SellPrice => Item.sellPrice(gold: 3);

		/// <summary>弹幕类型，0 表示自己实现 Shoot。</summary>
		protected virtual int ShootType => 0;

		protected virtual float ShootSpeed => 9f;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = Damage;
			Item.DamageType = Class;
			Item.knockBack = Knockback;
			Item.useTime = UseTime;
			Item.useAnimation = UseTime;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = Class != DamageClass.Melee;
			Item.value = SellPrice;
			Item.rare = Rarity;
			Item.UseSound = SoundID.Item1;

			if (ShootType > 0) {
				Item.shoot = ShootType;
				Item.shootSpeed = ShootSpeed;
			}
		}
	}

	/// <summary>饰品基类：默认只有 1 个饰品槽、不消耗，子类重写 UpdateAccessory 给效果。</summary>
	public abstract class WastelandAccessory : ModItem
	{
		protected virtual int Rarity => ItemRarityID.LightRed;

		protected virtual int SellPrice => Item.sellPrice(gold: 2);

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.accessory = true;
			Item.value = SellPrice;
			Item.rare = Rarity;
		}

		/// <summary>把所有 Boss 饰品都注册进同一套"已装备"提示逻辑，方便以后扩展。</summary>
		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			UpdateWastelandAccessory(player, hideVisual);
		}

		protected abstract void UpdateWastelandAccessory(Player player, bool hideVisual);
	}

	/// <summary>
	/// 稀有度分档：**按原版对应时期的颜色**来标装备与武器（与文档里的 Boss 定位一致）。
	/// </summary>
	public static class WastelandRarityTiers
	{
		/// <summary>前期·史莱姆王前后（清道夫）：橙色。</summary>
		public const int Early = ItemRarityID.Orange;

		/// <summary>前期后段·骷髅王之后（归档者）：浅红。</summary>
		public const int EarlyLate = ItemRarityID.LightRed;

		/// <summary>中期后段·世纪之花之后（灰烬之心）：黄绿。</summary>
		public const int MidLate = ItemRarityID.Lime;

		/// <summary>后期·月亮领主之前（壁炉守卫）：红色。</summary>
		public const int Late = ItemRarityID.Red;
	}

	/// <summary>
	/// Boss 灵魂碎片：**任务道具**，智械人恢复记忆用的。
	/// <list type="bullet">
	/// <item><b>每个世界只给一次</b>：掉落袋按 <c>WastelandMemorySystem.ShouldGrantSoulFragment</c> 判断
	/// （这段记忆还没恢复、且背包/银行里没有这枚碎片才发），所以不会反复打同一个 Boss 刷出一堆碎片；</item>
	/// <item><b>交给她会被消耗</b>：<c>WastelandMemorySystem.TryHandIn</c> 把它从背包/银行里扣掉，
	/// 然后由她自己读取、更新记忆；</item>
	/// <item><b>玩家自己用不了</b>：没有使用方式，也不响应右键 —— 免得随手乱点把剧情跳过去。</item>
	/// </list>
	/// </summary>
	public abstract class SoulFragment : WastelandMaterial
	{
		/// <summary>属于第几个 Boss（1 起）。</summary>
		public abstract int BossIndex { get; }

		protected override int IconSize => 24;

		/// <summary>任务道具的原版稀有度（与「智械核心」一致）。</summary>
		protected override int Rarity => ItemRarityID.Quest;

		protected override int SellPrice => 0;

		public override void SetDefaults()
		{
			base.SetDefaults();

			Item.maxStack = 1;                     // 任务道具：不叠放，每个世界就这一枚
			Item.consumable = true;                // 交给智械人时被消耗掉
			Item.useStyle = ItemUseStyleID.None;   // 但不是「拿来用」的东西
			Item.noMelee = true;
		}

		/// <summary>玩家自己用不了它：只有智械人那边的交付流程会消耗碎片。</summary>
		public override bool CanUseItem(Player player)
		{
			return false;
		}

		public override void SetStaticDefaults()
		{
			// 剧情物品：不参与合成，只用于推进智械人的记忆
			ItemID.Sets.ItemNoGravity[Type] = false;
		}
	}
}
