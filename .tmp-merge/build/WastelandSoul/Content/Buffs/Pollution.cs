using Terraria;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Buffs
{
	/// <summary>
	/// 污染（减益）：由「污染区域」与清道夫的破损推进器施加。
	/// <para/>表现：持续掉血 + 防御下降，对应世界观里被污染的环境对生命体的腐蚀。
	/// </summary>
	public class Pollution : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// 标记为减益，这样能被护士治疗、也会显示为红色图标
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			// 持续伤害：lifeRegen 为负即持续掉血
			player.lifeRegen -= 4;

			// 污染腐蚀护甲
			player.statDefense -= 4;
		}
	}
}
