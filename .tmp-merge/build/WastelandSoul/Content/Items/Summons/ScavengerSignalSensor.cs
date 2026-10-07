using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.NPCs.Bosses.Scavenger;

namespace WastelandSoul.Content.Items.Summons
{
	/// <summary>
	/// 清道夫信号传感器：在铁砧用 5 个精钢制作。
	/// <para/>向废土广播一段伪造的执行指令，把清道夫引到使用者身边。
	/// <para/>**使用后不消耗**；同一时间只允许存在一只清道夫。
	/// </summary>
	public class ScavengerSignalSensor : ModItem
	{
		/// <summary>「已有一只清道夫」提示的节流，避免按住鼠标刷屏。</summary>
		private static uint lastBlockedMessage;

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

		public override bool CanUseItem(Player player)
		{
			if (!Scavenger.AnyAlive()) {
				return true;
			}

			// 已经有清道夫在执行清扫协议：给一次提示，但节流
			if (player.whoAmI == Main.myPlayer && Main.GameUpdateCount - lastBlockedMessage > 180u) {
				lastBlockedMessage = Main.GameUpdateCount;
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.ScavengerAlreadyActive"), 220, 120, 90);
			}

			return false;
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer) {
				return null;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// 联机同步方案待定（见文档「联机暂时搁置」）
				return true;
			}

			SpawnNear(player);
			return true;
		}

		private static void SpawnNear(Player player)
		{
			float side = Main.rand.NextBool() ? 1f : -1f;
			Vector2 position = player.Center + new Vector2(side * Main.rand.NextFloat(520f, 760f), -380f);

			int index = NPC.NewNPC(new EntitySource_SpawnNPC(), (int)position.X, (int)position.Y, ModContent.NPCType<Scavenger>());

			if (index >= 0 && index < Main.maxNPCs) {
				Main.npc[index].netUpdate = true;
			}

			if (Main.dedServ) {
				return;
			}

			Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.ScavengerSummoned"), 226, 90, 70);
		}

		public override void AddRecipes()
		{
			// 召唤物一律在恶魔祭坛 / 猩红祭坛上合成（原版 Boss 召唤物的传统），不用铁砧
			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(5)
				.AddTile(Common.ItemBases.WastelandCraftingStations.SummonAltar)
				.Register();
		}
	}
}
