using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Common.Players;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Armor
{
	// ====================================================================================
	// 三套新防具（装备扩充包）。
	//
	// 与已有的「精钢套装」区分开：
	//   · 精钢套装是**五职业各自一套**（战士/法师/射手/召唤/盗贼），走的是 Boss1 后的职业分化；
	//   · 这三套是**不分职业的三档过渡/补强装**，套装加成都是"生存 + 一个招牌机制"，
	//     任何 build 都能穿，不会和精钢的职业套抢位置。
	//
	//   清道夫之惠（Scavenger's Grace）  史莱姆王前后（Orange）   15 防 + 穷途加速 + 免摔伤
	//   灰烬之心（Ash Heart）            世纪之花后（Lime）       23 防 + 命中点燃 + 火中回血
	//   炉卫（Hearth Guard）             月亮领主前（Red）        34 防 + 受击泄冷火 + 重甲
	//
	// 穿身贴图（_Head/_Body/_Legs，各 40x1120 = 20 帧 x 56 高）由
	// tools/gen_gear_art.py 程序化生成，与图标同色系。
	// ====================================================================================

	// ====================================================================================
	// 一、清道夫之惠（Scavenger's Grace）—— 精钢之后的过渡套，主打"跑得快、摔不死、残血能跑"
	// ====================================================================================

	[AutoloadEquip(EquipType.Head)]
	public class ScavengerGraceHood : ScavengerGracePiece
	{
		protected override int Defense => 4;

		protected override int SteelCost => 5;

		protected override int FragmentCost => 2;

		protected override int ChipCost => 3;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Ranged) += 0.05f;
			player.GetDamage(DamageClass.Throwing) += 0.05f;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class ScavengerGraceJacket : ScavengerGracePiece
	{
		protected override int Defense => 6;

		protected override int SteelCost => 9;

		protected override int FragmentCost => 3;

		protected override int ChipCost => 5;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Generic) += 0.04f;
			player.moveSpeed += 0.06f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<ScavengerGraceHood>()
				&& legs.type == ModContent.ItemType<ScavengerGraceBoots>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.ScavengerGraceJacket.SetBonus");

			WastelandGearPlayer gear = player.GetModPlayer<WastelandGearPlayer>();
			gear.scavengerSet = true;

			// 招牌机制 A：摔不死
			player.noFallDmg = true;

			// 招牌机制 B：残血爆发式加速（血量 <= 35% 才生效，鼓励残血脱战而不是站桩）
			if (player.statLife <= player.statLifeMax2 * 0.35f) {
				player.moveSpeed += 0.22f;
				player.runAcceleration += 0.10f;
				player.pickSpeed -= 0.14f;

				if (!Main.dedServ && Main.rand.NextBool(6)) {
					Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Smoke);
					dust.velocity *= 0.3f;
					dust.noGravity = true;
					dust.scale = 0.7f;
					dust.color = new Color(196, 168, 72);
				}
			}
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class ScavengerGraceBoots : ScavengerGracePiece
	{
		protected override int Defense => 5;

		protected override int SteelCost => 7;

		protected override int FragmentCost => 2;

		protected override int ChipCost => 4;

		public override void UpdateEquip(Player player)
		{
			player.GetAttackSpeed(DamageClass.Generic) += 0.04f;
			player.moveSpeed += 0.06f;
		}
	}

	// ====================================================================================
	// 二、灰烬之心（Ash Heart）—— 世纪之花后，打人上火、火上回血
	// ====================================================================================

	[AutoloadEquip(EquipType.Head)]
	public class AshHeartMask : AshHeartPiece
	{
		protected override int Defense => 7;

		protected override int AlloyCost => 6;

		protected override int FragmentCost => 3;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Melee) += 0.07f;
			player.GetDamage(DamageClass.Ranged) += 0.07f;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class AshHeartPlate : AshHeartPiece
	{
		protected override int Defense => 9;

		protected override int AlloyCost => 10;

		protected override int FragmentCost => 5;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Generic) += 0.06f;
			player.GetModPlayer<WastelandGearPlayer>().gearEmberOnHit += 0.10f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<AshHeartMask>()
				&& legs.type == ModContent.ItemType<AshHeartGreaves>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.AshHeartPlate.SetBonus");

			WastelandGearPlayer gear = player.GetModPlayer<WastelandGearPlayer>();
			gear.ashHeartSet = true;
			gear.gearEmberOnHit += 0.18f;

			player.GetAttackSpeed(DamageClass.Melee) += 0.08f;

			// 招牌机制：身上带着火（原版灼烧类减益）时反向回血 —— 用火焰换命
			if (player.onFire || player.onFire2 || player.onFire3
				|| player.HasBuff(BuffID.Burning) || player.HasBuff(BuffID.CursedInferno)) {
				player.lifeRegen += 4;
			}
			else {
				player.lifeRegen += 1;
			}
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class AshHeartGreaves : AshHeartPiece
	{
		protected override int Defense => 7;

		protected override int AlloyCost => 8;

		protected override int FragmentCost => 4;

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Generic) += 0.04f;
			player.moveSpeed += 0.05f;
		}
	}

	// ====================================================================================
	// 三、炉卫（Hearth Guard）—— 月亮领主前，冷金属 + 护盾，最肉的一套
	// ====================================================================================

	[AutoloadEquip(EquipType.Head)]
	public class HearthGuardVisor : HearthGuardPiece
	{
		protected override int Defense => 11;

		protected override int AlloyCost => 8;

		protected override int FragmentCost => 4;

		public override void UpdateEquip(Player player)
		{
			player.endurance += 0.03f;
			player.resistCold = true;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class HearthGuardCuirass : HearthGuardPiece
	{
		protected override int Defense => 14;

		protected override int AlloyCost => 12;

		protected override int FragmentCost => 6;

		public override void UpdateEquip(Player player)
		{
			player.endurance += 0.04f;
			player.noKnockback = true;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return head.type == ModContent.ItemType<HearthGuardVisor>()
				&& legs.type == ModContent.ItemType<HearthGuardGreaves>();
		}

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.WastelandSoul.Items.HearthGuardCuirass.SetBonus");

			WastelandGearPlayer gear = player.GetModPlayer<WastelandGearPlayer>();
			gear.hearthSet = true;

			player.statDefense += 4;
			player.endurance += 0.05f;
			player.noKnockback = true;

			// 招牌机制：受击时从甲缝里泄出一圈冷火 —— 实际触发在 WastelandGearPlayer.PostHurt
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class HearthGuardGreaves : HearthGuardPiece
	{
		protected override int Defense => 9;

		protected override int AlloyCost => 10;

		protected override int FragmentCost => 5;

		public override void UpdateEquip(Player player)
		{
			player.endurance += 0.03f;
			player.moveSpeed -= 0.04f;      // 重甲：换来防御，走路稍沉
		}
	}

	// ====================================================================================
	// 公共基类：三套共用尺寸/价值/稀有度/配方模板，子类只给防御与配方数量
	// ====================================================================================

	/// <summary>
	/// 装备扩充包防具的公共基类。抽象类不会被自动加载，因此不需要贴图。
	/// <para/>物品图标统一 <b>22x22</b>；穿身贴图统一 <b>40x1120</b>（20 帧 x 56 高）。
	/// </summary>
	public abstract class WastelandArmorPiece : ModItem
	{
		/// <summary>防御力。</summary>
		protected abstract int Defense { get; }

		/// <summary>稀有度。</summary>
		protected virtual int Rarity => WastelandRarityTiers.Early;

		/// <summary>售价。</summary>
		protected virtual int SellPrice => Item.sellPrice(gold: 1);

		/// <summary>制作台。默认铁砧。</summary>
		protected virtual int CraftTile => TileID.Anvils;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.value = SellPrice;
			Item.rare = Rarity;
			Item.defense = Defense;
		}
	}

	/// <summary>清道夫之惠三件：精钢锭 + 清道夫碎片 + 芯片，铁砧。</summary>
	public abstract class ScavengerGracePiece : WastelandArmorPiece
	{
		protected override int SellPrice => Item.sellPrice(gold: 1, silver: 20);

		protected abstract int SteelCost { get; }

		protected abstract int FragmentCost { get; }

		protected abstract int ChipCost { get; }

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(SteelCost)
				.AddIngredient<ScavengerFragment>(FragmentCost)
				.AddIngredient<Chip>(ChipCost)
				.AddTile(CraftTile)
				.Register();
		}
	}

	/// <summary>灰烬之心三件：灰烬之心合金锭 + 灰烬碎片，秘银砧。</summary>
	public abstract class AshHeartPiece : WastelandArmorPiece
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 3);

		protected override int CraftTile => TileID.MythrilAnvil;

		protected abstract int AlloyCost { get; }

		protected abstract int FragmentCost { get; }

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshHeartAlloyBar>(AlloyCost)
				.AddIngredient<AshHeartFragment>(FragmentCost)
				.AddTile(CraftTile)
				.Register();
		}
	}

	/// <summary>炉卫三件：壁炉合金锭 + 壁炉碎片 + 狱石锭，远古操纵机。</summary>
	public abstract class HearthGuardPiece : WastelandArmorPiece
	{
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int SellPrice => Item.sellPrice(gold: 6);

		protected override int CraftTile => TileID.LunarCraftingStation;

		protected abstract int AlloyCost { get; }

		protected abstract int FragmentCost { get; }

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<FireplaceAlloyBar>(AlloyCost)
				.AddIngredient<FireplaceFragment>(FragmentCost)
				.AddIngredient(ItemID.HellstoneBar, 6)
				.AddTile(CraftTile)
				.Register();
		}
	}
}
