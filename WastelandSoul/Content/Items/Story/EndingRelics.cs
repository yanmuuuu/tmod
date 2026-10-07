using Terraria;
using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Story
{
	/// <summary>选择成为重置载体后留下的封印。世界仍然会走到尽头，但下一次黎明记着她的名字。</summary>
	public class DawnSeal : WastelandMaterial
	{
		protected override int IconSize => 26;

		protected override int Rarity => ItemRarityID.Cyan;

		protected override int SellPrice => 0;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.maxStack = 1;
		}
	}

	/// <summary>拒绝成为武器之后留下的名字。这具精灵躯体终于只属于她自己。</summary>
	public class UnburnedName : WastelandMaterial
	{
		protected override int IconSize => 26;

		protected override int Rarity => ItemRarityID.Pink;

		protected override int SellPrice => 0;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.maxStack = 1;
		}
	}
}
