using Terraria;
using Terraria.GameContent.UI;
using Terraria.ModLoader;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Common.Systems
{
	/// <summary>芯片是智械人商店的货币。注册放在内容都加载完之后。</summary>
	public class ChipCurrencySystem : ModSystem
	{
		public static int CurrencyId { get; private set; }

		public override void PostSetupContent()
		{
			CurrencyId = CustomCurrencyManager.RegisterCurrency(new ChipCoin(ModContent.ItemType<Chip>(), 9999L));
		}

		private class ChipCoin : CustomCurrencySingleCoin
		{
			public ChipCoin(int coinItemID, long currencyCap) : base(coinItemID, currencyCap)
			{
			}
		}
	}
}
