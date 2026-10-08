using Terraria;
using Terraria.ID;

namespace WastelandSoul.Content.Items.Weapons
{
	/// <summary>
	/// 40 把 Boss 武器的共用「手感」参数：形态、声音、弹药、召唤增益。
	/// <para/>数值（伤害 / 使用时间 / 击退 / 稀有度 / 弹幕）一律由各武器类自己 override，
	/// 这里只负责把不同职业的通用形态写在一处，避免 40 个类里重复同一段样板。
	/// <para/>注意：本类不是 ModItem，不会被 tModLoader 自动加载，因此不需要贴图。
	/// </summary>
	public static class WastelandWeaponKit
	{
		/// <summary>
		/// 近战：保持**标准挥砍形态**（Swing + 有挥砍判定 + 显示手持贴图）。
		/// <para/>这样玩家装有 ArmamentDisplay / CoolerItemVisualEffect 时会自动获得刀光效果。
		/// </summary>
		public static void Melee(Item item)
		{
			item.useStyle = ItemUseStyleID.Swing;
			item.noMelee = false;
			item.noUseGraphic = false;
			item.autoReuse = true;
			item.UseSound = SoundID.Item1;
		}

		/// <summary>法师：法杖 / 法典，朝鼠标方向施法并消耗魔力。</summary>
		public static void Magic(Item item, int mana)
		{
			item.useStyle = ItemUseStyleID.Shoot;
			item.noMelee = true;
			item.noUseGraphic = false;
			item.autoReuse = true;
			item.mana = mana;
			item.UseSound = SoundID.Item20;
		}

		/// <summary>射手：枪械，消耗火枪子弹。</summary>
		public static void Ranged(Item item)
		{
			item.useStyle = ItemUseStyleID.Shoot;
			item.noMelee = true;
			item.noUseGraphic = false;
			item.autoReuse = true;
			item.useAmmo = AmmoID.Bullet;
			item.UseSound = SoundID.Item11;
		}

		/// <summary>召唤师：举起召唤杖，把仆从增益挂到玩家身上。</summary>
		public static void Summon(Item item, int buffType, int mana)
		{
			item.useStyle = ItemUseStyleID.Swing;
			item.noMelee = true;
			item.noUseGraphic = false;
			item.autoReuse = true;
			item.mana = mana;
			item.buffType = buffType;
			item.buffTime = 3600;
			item.UseSound = SoundID.Item44;
		}
	}
}
