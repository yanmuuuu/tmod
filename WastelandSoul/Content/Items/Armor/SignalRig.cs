using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Armor
{
	/// <summary>
	/// 信号甲：穿在身上的是改色后的清道夫披挂（走路动画还在），
	/// 背包图标是另外画的锈铁青灯。全套时身边会飘一颗冷青光点。
	/// </summary>
	public abstract class SignalRigPiece : ModItem
	{
		public abstract int Defense { get; }

		public abstract int ChunkCost { get; }

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.value = Item.sellPrice(silver: 80);
			Item.rare = ItemRarityID.Orange;
			Item.defense = Defense;
			Item.vanity = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(ChunkCost)
				.AddIngredient(ItemID.Gel, 4)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	[AutoloadEquip(EquipType.Head)]
	public class SignalRigHelm : SignalRigPiece
	{
		public override int Defense => 2;
		public override int ChunkCost => 6;
	}

	[AutoloadEquip(EquipType.Body)]
	public class SignalRigPlate : SignalRigPiece
	{
		public override int Defense => 3;
		public override int ChunkCost => 10;

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<SignalRigHelm>()
				&& legs.type == ModContent.ItemType<SignalRigGreaves>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.SignalRigPlate.SetBonus");
			player.moveSpeed += 0.06f;
			Pulse(player);
		}

		public override bool IsVanitySet(int head, int body, int legs)
		{
			return head == EquipLoader.GetEquipSlot(Mod, nameof(SignalRigHelm), EquipType.Head)
				&& body == EquipLoader.GetEquipSlot(Mod, nameof(SignalRigPlate), EquipType.Body)
				&& legs == EquipLoader.GetEquipSlot(Mod, nameof(SignalRigGreaves), EquipType.Legs);
		}

		public override void UpdateVanitySet(Player player)
		{
			Pulse(player);
		}

		private static void Pulse(Player player)
		{
			if (Main.dedServ || Main.GameUpdateCount % 16u != 0u) {
				return;
			}

			WastelandFxSystem.Motes(player.Center, 26f, 1, new Color(90, 220, 230));
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class SignalRigGreaves : SignalRigPiece
	{
		public override int Defense => 2;
		public override int ChunkCost => 8;

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.04f;
		}
	}
}
