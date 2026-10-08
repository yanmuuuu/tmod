using Terraria;
using Terraria.ModLoader;

namespace WastelandSoul.Common.Players
{
	/// <summary>
	/// **升级衍生树（第二批）** 专用的玩家状态。
	///
	/// <para/>刻意独立成文件：<c>WastelandPlayer.cs</c> 与 <c>WastelandUpgradePlayer.cs</c>
	/// 都属于别的包，这里只放本批新增内容自己的状态。
	///
	/// <para/>目前只有一项：召唤树终阶「灰烬之心齿灵哨」的**齿轮护盾**。
	/// 护盾由 <c>AshHeartGearBuff</c> 每帧置位（Buff 掉了自然失效），
	/// 真正的减伤乘区放在这里的 <see cref="ModifyHurt"/> —— 这样"护盾"是一层独立的
	/// 伤害乘区，而不是又一条 +防御 的数字。
	/// </summary>
	public class WastelandTreePlayer : ModPlayer
	{
		/// <summary>齿轮护盾是否生效（由齿灵哨的维持 Buff 每帧置位）。</summary>
		public bool gearShield;

		public override void ResetEffects()
		{
			gearShield = false;
		}

		/// <summary>
		/// 护盾生效期间再吃 8% 减免。与其它 ModPlayer 的 <c>ModifyHurt</c> 是各自独立的乘区，
		/// 同时戴着别的减伤饰品时是叠乘而不是互相顶掉。
		/// </summary>
		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			if (gearShield) {
				modifiers.IncomingDamageMultiplier *= 0.92f;
			}
		}
	}
}
