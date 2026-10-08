using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace WastelandSoul.Common.Effects
{
	// ====================================================================================
	// 【重要历史】这个文件以前还有三样东西，全部**因为玩家反馈"影响视线"而删除**，
	// **以后合并别的分支时绝对不要再加回来**（`tools/check_no_auto_trails.py` 会拦）：
	//
	//   1. `WastelandTrailGlobalProjectile`：给**每一个**本模组弹幕自动加拖尾
	//      （12 个历史点 + 1x1 白点拉成 16px 宽的带子，无职业伤害的弹幕还会落到近白兜底色）；
	//   2. `WastelandFxProjectile`：本模组弹幕消失时爆一串**拉长的条状粒子**（Spark）；
	//   3. `WastelandFxGlobal`：Boss 常驻氛围（每 2 帧一颗 2.8 倍光斑 + 条状火花）；
	//   4. `WastelandSwingTracker` + `WastelandSwingDrawSystem`：近战挥砍时用白点拉出的**挥砍弧光**
	//      （玩家原话："武器的也是"）。
	//
	// 结论：**不做任何"自动挂上去"的线状/条状特效**。要加特效就挂在具体招式自己的
	// PreDraw/PostDraw 或 AI 里，用明确的颜色、短时长、别横跨屏幕。
	// ====================================================================================

	/// <summary>五职业配色（按 DamageClass 区分），供具体招式取用。</summary>
	public static class WastelandEffectColors
	{
		public static Color For(DamageClass damageClass)
		{
			if (damageClass == DamageClass.Melee) {
				return new Color(232, 120, 72);      // 战士：橙红
			}
			if (damageClass == DamageClass.Magic) {
				return new Color(140, 120, 240);     // 法师：蓝紫
			}
			if (damageClass == DamageClass.Ranged) {
				return new Color(238, 206, 110);     // 射手：暖黄
			}
			if (damageClass == DamageClass.Summon) {
				return new Color(120, 220, 190);     // 召唤师：青绿
			}
			if (damageClass == DamageClass.Throwing) {
				return new Color(206, 110, 180);     // 盗贼：紫红
			}

			return new Color(200, 200, 200);
		}
	}
}
