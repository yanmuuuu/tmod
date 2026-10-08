using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Buffs
{
	/// <summary>
	/// 废铁护盾（增益）：用「废料收割者之刃」命中后，一层拼起来的废铁临时挂在身上。
	/// <para/>效果很轻 —— **+4 防御**、持续 4 秒 —— 定位是"迷你 Boss 武器的附加小甜头"，
	/// 不参与任何伤害减免的乘算，所以不会和别的防御饰品叠出问题。
	/// <para/>图标（32×32）与「煤渣修补者」的护盾视觉共用同一张 <c>ScrapShield.png</c>。
	/// </summary>
	public class ScrapShield : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = false;
			Main.debuff[Type] = false;
			BuffID.Sets.NurseCannotRemoveDebuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.statDefense += 4;
		}
	}
}
