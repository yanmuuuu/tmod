using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Info
{
	// ====================================================================================
	// 探索 / 信息类物品（2 种）。
	//   1) 废土地图碎片：给一句随机提示，**不消耗**，可以当"指南针"反复用；
	//   2) 废料宝匣：一个真正的战利品盒子，打开给一组随机物品（不是空壳）。
	// ⚠️ 所有随机物品都用 ItemID 里**确定存在**的原版物品，避免编译期找不到成员名。
	// ====================================================================================

	/// <summary>
	/// 废土地图碎片：一张画到一半的地图，每次展开都会指出一条不同的"活路"。
	/// <para/>用途：给新手一点方向感（去哪个方向、注意什么、壁炉里有什么）。
	/// 提示内容全部走本地化键，方便以后再加。
	/// </summary>
	public class WastelandMap : ModItem
	{
		/// <summary>提示条数——加提示时同步加 <c>Messages.MapHint*</c> 键。</summary>
		private const int HintCount = 6;

		/// <summary>上一次查看提示的时刻（帧计数），用来给反复点击做冷却。</summary>
		private static uint lastHintTick;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.maxStack = 1;
			Item.value = Terraria.Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 24;
			Item.useTime = 24;
			Item.consumable = false;   // 不消耗：这是一张要一直看的地图
			Item.noMelee = true;
			Item.UseSound = SoundID.MenuTick;
		}

		public override bool CanUseItem(Player player)
		{
			// 只在本地玩家上生效；顺带做 3 秒冷却，免得按住鼠标把提示刷爆
			if (player.whoAmI != Main.myPlayer) {
				return false;
			}

			if (Main.GameUpdateCount - lastHintTick < 180u) {
				return false;
			}

			return true;
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer) {
				return null;
			}

			lastHintTick = Main.GameUpdateCount;

			int index = Main.rand.Next(1, HintCount + 1);
			string hint = Language.GetTextValue("Mods.WastelandSoul.Messages.MapHint" + index);

			Main.NewText(hint, new Color(226, 206, 150));
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Book, 3)
				.AddIngredient<CircuitBoard>(1)
				.AddIngredient(ItemID.Leather, 2)
				.AddTile(TileID.WorkBenches)
				.Register();
		}
	}

	/// <summary>
	/// 废料宝匣：废土上偶尔能挖到的密封箱子，撬开之后是**随机战利品**。
	/// <para/>掉落池分三档：常用材料 / 装备 / 稀有件，越靠后概率越低。
	/// 使用后消耗。
	/// </summary>
	public class ScrapCache : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 99;
			Item.value = Terraria.Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Green;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 27;
			Item.useTime = 27;
			Item.consumable = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item37;
		}

		public override bool CanUseItem(Player player)
		{
			// 生成物品必须在服务端权威执行（多人时由主机做）
			return Main.netMode != NetmodeID.MultiplayerClient;
		}

		public override bool? UseItem(Player player)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return null;
			}

			IEntitySource source = new EntitySource_Misc("WastelandScrapCache");
			Vector2 position = player.Center + new Vector2(Main.rand.NextFloat(-80f, 80f), -40f);

			// ---------- 必给：常用材料 2~3 种 ----------
			Drop(source, position, ModContent.ItemType<RustedGear>(), Main.rand.Next(4, 9));
			Drop(source, position, ModContent.ItemType<Coke>(), Main.rand.Next(3, 7));

			if (Main.rand.NextBool(2)) {
				Drop(source, position, ModContent.ItemType<CircuitBoard>(), Main.rand.Next(1, 3));
			}

			if (Main.rand.NextBool(3)) {
				Drop(source, position, ModContent.ItemType<Coolant>(), 1);
			}

			// ---------- 经常给：消耗品 ----------
			switch (Main.rand.Next(6)) {
				case 0:
					Drop(source, position, ModContent.ItemType<Consumables.GasFilterPotion>(), 1);
					break;
				case 1:
					Drop(source, position, ModContent.ItemType<Consumables.AshenRation>(), 1);
					break;
				case 2:
					Drop(source, position, ItemID.HealingPotion, Main.rand.Next(3, 6));
					break;
				case 3:
					Drop(source, position, ItemID.ManaPotion, Main.rand.Next(3, 6));
					break;
				case 4:
					Drop(source, position, ItemID.RecallPotion, 2);
					break;
				default:
					Drop(source, position, ItemID.Torch, Main.rand.Next(15, 31));
					break;
			}

			// ---------- 偶尔给：装备 ----------
			switch (Main.rand.Next(6)) {
				case 0:
					Drop(source, position, ItemID.IronBroadsword, 1);
					break;
				case 1:
					Drop(source, position, ItemID.SilverBow, 1);
					break;
				case 2:
					Drop(source, position, ItemID.BandofRegeneration, 1);
					break;
				case 3:
					Drop(source, position, ItemID.Aglet, 1);
					break;
				default:
					Drop(source, position, ItemID.Shuriken, Main.rand.Next(20, 41));
					break;
			}

			// ---------- 稀有：8% 概率一件"像样的东西" ----------
			if (Main.rand.NextBool(12)) {
				int[] rare = new int[] {
					ItemID.Flare,
					ItemID.EnchantedBoomerang,
					ItemID.MagicMirror,
					ItemID.CloudinaBottle,
					ItemID.HermesBoots
				};

				Drop(source, position, rare[Main.rand.Next(rare.Length)], 1);
			}

			if (!Main.dedServ) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.CacheOpened"), new Color(210, 190, 130));
			}

			return true;
		}

		/// <summary>在指定位置生成一堆物品（带一点点随机速度，看起来像"倒出来"）。</summary>
		private static void Drop(IEntitySource source, Vector2 position, int itemType, int stack)
		{
			if (itemType <= 0 || stack <= 0) {
				return;
			}

			Vector2 velocity = new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-3.4f, -1.2f));
			Item.NewItem(source, position, velocity, itemType, stack);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Chest, 1)
				.AddIngredient<RustedGear>(6)
				.AddIngredient<SalvagedSteelChunk>(4)
				.AddIngredient(ItemID.IronBar, 4)
				.AddTile(TileID.WorkBenches)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.Chest, 1)
				.AddIngredient<RustedGear>(6)
				.AddIngredient<SalvagedSteelChunk>(4)
				.AddIngredient(ItemID.LeadBar, 4)
				.AddTile(TileID.WorkBenches)
				.Register();
		}
	}
}
