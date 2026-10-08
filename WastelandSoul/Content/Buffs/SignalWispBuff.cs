using Terraria;
using Terraria.ModLoader;
using WastelandSoul.Content.Projectiles.Signal;

namespace WastelandSoul.Content.Buffs
{
	/// <summary>信号灯芯还亮着的时候，小灯就不会灭。</summary>
	public class SignalWispBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<SignalWispMinion>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
