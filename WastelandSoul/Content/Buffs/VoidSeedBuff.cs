using Terraria;
using Terraria.ModLoader;
using WastelandSoul.Content.Projectiles.Rift;

namespace WastelandSoul.Content.Buffs
{
	/// <summary>虚空种还在的时候，这盏星核就不会灭。</summary>
	public class VoidSeedBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<VoidSeedMinion>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
