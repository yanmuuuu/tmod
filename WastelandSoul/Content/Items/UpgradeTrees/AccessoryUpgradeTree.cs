using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Common.Players;
using WastelandSoul.Content.Items.Accessories;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Subworlds;

namespace WastelandSoul.Content.Items.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树（三）· 饰品：防毒面具 → 强化滤芯面罩 → 灰烬之心净界面罩
	//
	// 三级差异（每级都是"新的机制"，不是同一条效果加数字）：
	//   防毒面具（已有）    免疫壁炉大气（有害气体），其余不管
	//   强化滤芯面罩        保留免疫大气 + **污染/有害气体伤害再 -45%** + 防御 +3 + 免疫中毒
	//   灰烬之心净界面罩    免疫大气 + **污染伤害再 -70%** + 防御 +5 + 免疫中毒与毒液 + 生命再生 +2
	//
	// 注意：这两件与「净化过滤面罩」（装备扩充包）**不冲突**——那件是独立的一个乘区
	// （<c>WastelandGearPlayer.pollutionFilterEquipped</c>），这里走
	// <see cref="WastelandUpgradePlayer"/>，同时戴是叠乘。
	// ====================================================================================

	/// <summary>
	/// Reinforced Filter Mask（强化滤芯面罩）
	/// <para/>防毒面具的第一档进阶：多一层可更换滤芯。
	/// 效果 = 免疫壁炉大气（沿用 <c>gasMaskEquipped</c>）+ 污染伤害 ×0.55 + 防御 +3 + 免疫中毒。
	/// </summary>
	public class ReinforcedFilterMask : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int SellPrice => Item.sellPrice(gold: 2, silver: 50);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			// 沿用原有的"进壁炉世界不被大气侵蚀"判定，保持与防毒面具一致的体验
			player.GetModPlayer<FireplaceAtmospherePlayer>().gasMaskEquipped = true;
			player.GetModPlayer<WastelandUpgradePlayer>().reinforcedFilterEquipped = true;
			player.statDefense += 3;
		}

		public override void AddRecipes()
		{
			// 进阶件：上一级面罩 + 换一套滤芯
			CreateRecipe()
				.AddIngredient<GasMask>()
				.AddIngredient<SalvagedSteelBar>(6)
				.AddIngredient(ItemID.Glass, 4)
				.AddIngredient<Chip>(4)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	/// <summary>
	/// Ash Heart Purifier Mask（灰烬之心净界面罩）
	/// <para/>终阶：滤芯换成灰烬之心的自净化滤网。
	/// 效果 = 免疫壁炉大气 + 污染伤害 ×0.30 + 防御 +5 + 免疫中毒/毒液 + 生命再生 +2。
	/// 与「净化过滤面罩」「强化滤芯面罩」同时佩戴时三条乘区各自生效（互不覆盖）。
	/// </summary>
	public class AshHeartPurifierMask : ReinforcedFilterMask
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 5);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			// 先拿进阶件的效果（免疫大气 + 污染乘区 + 防御 +3），再补终阶自己的部分
			base.UpdateWastelandAccessory(player, hideVisual);

			WastelandUpgradePlayer upgrade = player.GetModPlayer<WastelandUpgradePlayer>();
			upgrade.reinforcedFilterEquipped = false;   // 换成更强的净界乘区，避免一帧里叠两条
			upgrade.purifierMaskEquipped = true;

			player.statDefense += 2;
		}

		public override void AddRecipes()
		{
			// 终阶件：进阶面罩 + 灰烬之心合金锭 + 灰烬结晶做自净化滤网
			CreateRecipe()
				.AddIngredient<ReinforcedFilterMask>()
				.AddIngredient<AshHeartAlloyBar>(8)
				.AddIngredient<AshCrystal>(5)
				.AddIngredient<CircuitBoard>(3)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
