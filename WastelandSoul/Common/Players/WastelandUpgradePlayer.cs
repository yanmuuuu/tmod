using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Buffs;

namespace WastelandSoul.Common.Players
{
	/// <summary>
	/// **升级衍生树**（护甲进阶套 + 滤芯面罩进阶）专用的玩家状态。
	///
	/// <para/>刻意不写进 <c>WastelandPlayer.cs</c> / <c>WastelandGearPlayer.cs</c>：
	/// 那两个文件属于别的包，而这里的字段只服务于本包新增的进阶装备。
	///
	/// <para/>字段分两组：
	/// <list type="bullet">
	/// <item>**面罩进阶**（<c>*FilterEquipped</c>）——由饰品每帧置位，散掉自然失效；</item>
	/// <item>**护甲进阶套**（<c>ashAlloySet</c>）——由防具的 <c>UpdateArmorSet</c> 每帧置位，
	/// 只用来在 <see cref="ModifyHurt"/> 里追加一条抗污染乘区（伤害/速度那部分直接写在套装里）。</item>
	/// </list>
	/// </summary>
	public class WastelandUpgradePlayer : ModPlayer
	{
		/// <summary>强化滤芯面罩是否装备。</summary>
		public bool reinforcedFilterEquipped;

		/// <summary>灰烬之心净界面罩是否装备。</summary>
		public bool purifierMaskEquipped;

		/// <summary>灰烬合金战士套装是否完整（换抗污染的一档）。</summary>
		public bool ashAlloySet;

		public override void ResetEffects()
		{
			reinforcedFilterEquipped = false;
			purifierMaskEquipped = false;
			ashAlloySet = false;
		}

		/// <summary>
		/// 面罩进阶的**污染 / 有害气体**减免。
		///
		/// <para/>与 <c>WastelandGearPlayer.ModifyHurt</c> 里净化过滤面罩那一条是**各自独立的乘区**：
		/// 两个 ModPlayer 的 <c>ModifyHurt</c> 会先后作用在同一个
		/// <see cref="Player.HurtModifiers"/> 上，所以同时戴着是叠乘而不是互相顶掉。
		/// </summary>
		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			bool polluted = Player.HasBuff(ModContent.BuffType<Pollution>())
				|| Player.HasBuff(ModContent.BuffType<GasPoison>());

			if (!polluted) {
				return;
			}

			if (purifierMaskEquipped) {
				modifiers.IncomingDamageMultiplier *= 0.30f;
			}
			else if (reinforcedFilterEquipped) {
				modifiers.IncomingDamageMultiplier *= 0.55f;
			}

			// 灰烬合金战士套：进阶甲密封性更好，污染伤害再打八五折
			if (ashAlloySet) {
				modifiers.IncomingDamageMultiplier *= 0.85f;
			}
		}

		/// <summary>面罩进阶的常驻免疫与再生（每帧重设 <c>buffImmune</c>，脱下即失效）。</summary>
		public override void PostUpdateEquips()
		{
			if (purifierMaskEquipped) {
				Player.buffImmune[BuffID.Poisoned] = true;
				Player.buffImmune[BuffID.Venom] = true;
				Player.lifeRegen += 2;
			}
			else if (reinforcedFilterEquipped) {
				Player.buffImmune[BuffID.Poisoned] = true;
			}
		}
	}
}
