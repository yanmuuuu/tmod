using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WastelandSoul.Common.Effects;
using WastelandSoul.Content.Buffs;
using WastelandSoul.Content.Projectiles.Gear;

namespace WastelandSoul.Common.Players
{
	/// <summary>
	/// **装备扩充包**（护甲与饰品）专用的玩家状态。
	///
	/// <para/>刻意不写进 <see cref="WastelandPlayer"/>：那份文件有别的代理同时在改，
	/// 而且这些字段只服务于本包新增的护甲/饰品，单独一个 ModPlayer 更好维护。
	///
	/// <para/>字段分三组：
	/// <list type="bullet">
	/// <item>**防具套装状态**（<c>scavengerSet</c> 等）——由防具的 <c>UpdateArmorSet</c> 每帧置位，
	/// 套装散掉自然失效，不需要自己比对三个装备栏；</item>
	/// <item>**饰品状态**（<c>*Equipped</c>）——由饰品的 <c>UpdateAccessory</c> 每帧置位；</item>
	/// <item>**有冷却的能力**（炉卫护盾、明日之匣、废土指针）——计时器放在 <see cref="PostUpdate"/> 里递减，
	/// 需要跨存档保留的写进 <see cref="SaveData"/>。</item>
	/// </list>
	/// </summary>
	public class WastelandGearPlayer : ModPlayer
	{
		// ==================== 防具套装 ====================

		/// <summary>清道夫之惠套装是否完整。</summary>
		public bool scavengerSet;

		/// <summary>灰烬之心套装是否完整。</summary>
		public bool ashHeartSet;

		/// <summary>炉卫套装是否完整。</summary>
		public bool hearthSet;

		// ==================== 饰品 ====================

		/// <summary>净化过滤面罩：污染物伤害减免。</summary>
		public bool pollutionFilterEquipped;

		/// <summary>煤渣踏靴：可以冲刺。</summary>
		public bool cinderstepEquipped;

		/// <summary>余烬玻璃透镜：命中叠灼烧。</summary>
		public bool emberLensEquipped;

		/// <summary>祝圣之核：召唤物增益。</summary>
		public bool consecratedCoreEquipped;

		/// <summary>炉火护盾：受伤减免（破盾期间失效）。</summary>
		public bool hearthAegisEquipped;

		/// <summary>炉火镜面：受伤时向附近敌人反弹伤害。</summary>
		public bool hearthMirrorEquipped;

		/// <summary>废土指针：掉落率与运气。</summary>
		public bool wastelandCompassEquipped;

		/// <summary>拾荒者背包：自动挥砍 + 换取弹药节省。</summary>
		public bool scavengerKitEquipped;

		/// <summary>明日之匣：致命伤害免疫。</summary>
		public bool morrowFragmentEquipped;

		/// <summary>壁炉信标：击杀后短时增益。</summary>
		public bool hearthBeaconEquipped;

		/// <summary>深渊呼吸器：水下呼吸、免疫溺水与潮湿。</summary>
		public bool abyssBreatherEquipped;

		/// <summary>灰烬回响护符：命中点燃 + 近战增益。</summary>
		public bool ashEchoEquipped;

		/// <summary>本包饰品提供的弹药节省概率（每帧由饰品重写）。</summary>
		public float gearAmmoSave;

		/// <summary>本包饰品提供的命中点燃概率（每帧由饰品重写）。</summary>
		public float gearEmberOnHit;

		// ==================== 冲刺（煤渣踏靴） ====================

		private int dashTimer;

		/// <summary>冲刺冷却（帧）。<c>Player</c> 上没有可写的冲刺冷却字段，所以自己记。</summary>
		private int dashCooldownTimer;

		// ==================== 炉火护盾 ====================

		/// <summary>护盾是否已经碎了（等冷却）。</summary>
		public bool hearthShieldBroken;

		/// <summary>碎盾后还有多久修好（帧）。</summary>
		public int hearthShieldTimer;

		/// <summary>修好之后又积累了多少帧（供表现用）。</summary>
		public int hearthShieldCharge;

		// ==================== 明日之匣 ====================

		/// <summary>致命保护剩余冷却（帧）。</summary>
		public int morrowCooldown;

		/// <summary>这一帧刚刚用掉致命保护（给 <see cref="PostHurt"/> 播提示用）。</summary>
		private bool morrowJustSaved;

		/// <summary>致命保护是否已经就绪。</summary>
		public bool MorrowReady => morrowCooldown <= 0;

		public override void Initialize()
		{
			dashTimer = 0;
			hearthShieldBroken = false;
			hearthShieldTimer = 0;
			hearthShieldCharge = 0;
			morrowCooldown = 0;
			morrowJustSaved = false;
		}

		public override void ResetEffects()
		{
			scavengerSet = false;
			ashHeartSet = false;
			hearthSet = false;

			pollutionFilterEquipped = false;
			cinderstepEquipped = false;
			emberLensEquipped = false;
			consecratedCoreEquipped = false;
			hearthAegisEquipped = false;
			hearthMirrorEquipped = false;
			wastelandCompassEquipped = false;
			scavengerKitEquipped = false;
			morrowFragmentEquipped = false;
			hearthBeaconEquipped = false;
			abyssBreatherEquipped = false;
			ashEchoEquipped = false;

			gearAmmoSave = 0f;
			gearEmberOnHit = 0f;
		}

		public override void PostUpdateEquips()
		{
			// 炉火护盾：碎了之后进入 8 秒冷却，冷却完自动修好（修好慢、碎得快，不给永久减伤）
			if (hearthShieldBroken) {
				hearthShieldCharge = 0;

				if (--hearthShieldTimer <= 0) {
					hearthShieldBroken = false;
					hearthShieldTimer = 0;
					hearthShieldCharge = 0;

					if (!Main.dedServ) {
						WastelandFxSystem.Ring(Player.Center, new Color(170, 222, 255), 8f, 40f, 24);
					}
				}
			}
			else if (hearthShieldCharge < 600) {
				hearthShieldCharge++;
			}
		}

		/// <summary>
		/// 命中叠灼烧：本包饰品/护甲的点燃概率与 <c>WastelandPlayer.emberOnHit</c> 叠加。
		/// 不改那份共享文件，只在这里追加自己的一份判定。
		/// </summary>
		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			TryGearIgnite(target);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			TryGearIgnite(target);
		}

		private void TryGearIgnite(NPC target)
		{
			if (target.friendly || !target.active) {
				return;
			}

			if (gearEmberOnHit > 0f && Main.rand.NextFloat() < gearEmberOnHit) {
				target.AddBuff(BuffID.OnFire3, 240);
			}
		}

		/// <summary>本包饰品的弹药节省（废土指针那条线）。</summary>
		public override bool CanConsumeAmmo(Item weapon, Item ammo)
		{
			if (gearAmmoSave > 0f && Main.rand.NextFloat() < gearAmmoSave) {
				return false;
			}

			return base.CanConsumeAmmo(weapon, ammo);
		}

		/// <summary>壁炉信标：击杀后短时增益，冷却由原版增益时长自己管。</summary>
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hearthBeaconEquipped && target.life <= 0 && !Player.HasBuff(BuffID.Heartreach)) {
				Player.AddBuff(BuffID.Heartreach, 300);
			}
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			int incoming = (int)modifiers.SourceDamage.ApplyTo(0f);

			// 净化过滤面罩：污染/有毒大气造成的伤害大幅削减
			if (pollutionFilterEquipped
				&& incoming > 0
				&& (Player.HasBuff(ModContent.BuffType<Pollution>()) || Player.HasBuff(ModContent.BuffType<GasPoison>()))) {
				modifiers.IncomingDamageMultiplier *= 0.35f;
			}

			// 炉火护盾：没碎的时候减伤
			if (hearthAegisEquipped && !hearthShieldBroken && incoming > 0) {
				modifiers.IncomingDamageMultiplier *= 0.86f;
			}

			// 明日之匣：一次致命伤害免疫。判定用"伤害来源是不是某个敌怪"，
			// 比拆 EntitySource 更稳（原版 PlayerDeathReason.SourceNPCIndex 就是这条信息）。
			// 注意 SourceDamage 是**减防之前**的原始伤害，所以这个判定偏保守
			// （偶尔会替一次本来打不死你的攻击挡刀），宁可多挡也不漏挡。
			int sourceNpc = modifiers.DamageSource.SourceNPCIndex;
			bool fromEnemy = sourceNpc >= 0 && sourceNpc < Main.maxNPCs
				&& Main.npc[sourceNpc].active && !Main.npc[sourceNpc].friendly;

			if (morrowFragmentEquipped && MorrowReady && !modifiers.PvP
				&& incoming > 0 && incoming >= Player.statLife && fromEnemy) {
				modifiers.Cancel();
				morrowJustSaved = true;
			}
		}

		public override void PostHurt(Player.HurtInfo info)
		{
			if (morrowJustSaved) {
				morrowJustSaved = false;
				morrowCooldown = 60 * 45;      // 45 秒
				Player.immune = true;
				Player.immuneTime = Math.Max(Player.immuneTime, 90);
				Player.immuneNoBlink = true;

				if (Main.netMode != NetmodeID.Server) {
					WastelandFxSystem.Ring(Player.Center, new Color(210, 240, 255), 10f, 90f, 30);
					WastelandFxSystem.Motes(Player.Center, 26f, 10, new Color(255, 214, 140));
				}

				if (Player.whoAmI == Main.myPlayer) {
					Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.GearMorrowSaved"), 210, 240, 255);
				}
			}

			// 炉火护盾：挨打即碎，并放出一圈冷火冲击
			if (hearthAegisEquipped && !hearthShieldBroken) {
				hearthShieldBroken = true;
				hearthShieldTimer = 60 * 8;    // 8 秒
				hearthShieldCharge = 0;

				if (Player.whoAmI == Main.myPlayer) {
					Projectile.NewProjectile(
						Player.GetSource_Misc("WastelandSoulHearthAegis"),
						Player.Center,
						Vector2.Zero,
						ModContent.ProjectileType<GearColdPulse>(),
						0,
						0f,
						Player.whoAmI,
						info.Damage);
				}
			}

			// 炉火镜面：把一部分伤害以余烬碎片的形式弹回最近的敌人
			if (hearthMirrorEquipped && info.Damage > 0) {
				int reflect = Utils.Clamp((int)(info.Damage * 0.35f), 8, 400);
				ReflectAtNearestEnemy(info, reflect);
			}
		}

		/// <summary>朝最近的敌人弹出一枚余烬碎片；找不到目标就只放个火花。</summary>
		private void ReflectAtNearestEnemy(Player.HurtInfo info, int reflectDamage)
		{
			NPC nearest = null;
			float best = 460f * 460f;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.immortal) {
					continue;
				}

				float distance = Vector2.DistanceSquared(npc.Center, Player.Center);

				if (distance >= best || !Collision.CanHitLine(Player.Center, 1, 1, npc.Center, 1, 1)) {
					continue;
				}

				best = distance;
				nearest = npc;
			}

			if (nearest == null) {
				if (Main.netMode != NetmodeID.Server) {
					WastelandFxSystem.Burst(Player.Center, 5, new Color(226, 120, 90), 3f);
				}

				return;
			}

			if (Player.whoAmI != Main.myPlayer) {
				return;
			}

			Vector2 direction = (nearest.Center - Player.Center).SafeNormalize(Vector2.UnitX);
			float speed = MathHelper.Clamp(9f + (float)Math.Sqrt(best) * 0.01f, 9f, 13f);

			Projectile.NewProjectile(
				Player.GetSource_Misc("WastelandSoulHearthMirror"),
				Player.Center,
				direction * speed,
				ModContent.ProjectileType<GearReboundShard>(),
				reflectDamage,
				3f,
				Player.whoAmI,
				1f);

			// 原版挨打后会给目标一小段无敌帧（NPC.immuneTime 是**静态 int**，改不了单个目标），
			// 所以反弹这一下有时会被吃掉。补一个不受无敌帧影响的击退，保证手感上"确实弹到了"。
			nearest.velocity += direction * (4.5f * MathHelper.Clamp(nearest.knockBackResist, 0f, 1f));
		}

		public override void PostUpdate()
		{
			if (morrowCooldown > 0) {
				morrowCooldown--;
			}

			if (dashCooldownTimer > 0) {
				dashCooldownTimer--;
			}

			if (!cinderstepEquipped) {
				return;
			}

			Vector2 direction = Vector2.Zero;

			// 同克苏鲁之盾的冲刺：下+方向键触发，冲刺期间给无敌帧。
			// 冷却用自己记的 dashCooldownTimer（Player 上没有可写的冲刺冷却字段，
			// 原版盾牌是靠它自己的 GlobalItem 推 dashDelay，这里同样只推 dashDelay）。
			if (Player.dashDelay == 0 && dashCooldownTimer <= 0) {
				if (Player.controlDown && Player.releaseDown) {
					if (Player.controlLeft && Player.releaseLeft) {
						direction = -Vector2.UnitX;
					}
					else if (Player.controlRight && Player.releaseRight) {
						direction = Vector2.UnitX;
					}
				}
			}

			if (direction == Vector2.Zero) {
				return;
			}

			Player.dashType = 1;
			Player.dash = 0;
			Player.dashDelay = 2;
			Player.velocity.X = direction.X * 12.5f;
			Player.immune = true;
			Player.immuneTime = Math.Max(Player.immuneTime, 22);
			Player.immuneNoBlink = true;
			dashCooldownTimer = 40;

			dashTimer = 13;

			if (Main.netMode != NetmodeID.Server) {
				WastelandFxSystem.Embers(Player.Center, 7, new Color(255, 150, 70));
			}
		}

		public override void PostUpdateRunSpeeds()
		{
			if (dashTimer <= 0) {
				return;
			}

			dashTimer--;

			// 冲刺尾段：保持横向速度、关掉奔跑加速，落回地面时结束
			Player.runAcceleration = 0f;
			Player.maxRunSpeed = 0f;
			Player.accRunSpeed = 0f;
			Player.velocity.X = MathHelper.Lerp(Player.velocity.X, 0f, 0.22f);
			Player.velocity.Y *= 0.94f;

			if (Player.velocity.Y == 0f) {
				dashTimer = 0;
				Player.dash = 0;
				Player.dashDelay = 22;
				return;
			}

			if (dashTimer <= 0) {
				Player.dash = 0;
				Player.dashDelay = 22;
			}
		}

		public override void OnRespawn()
		{
			// 死了重置：护盾和致命保护不该跨命保留
			hearthShieldBroken = false;
			hearthShieldTimer = 0;
			hearthShieldCharge = 0;
			dashTimer = 0;
		}

		public override void SaveData(TagCompound tag)
		{
			if (morrowCooldown > 0) {
				tag["morrowCooldown"] = morrowCooldown;
			}
		}

		public override void LoadData(TagCompound tag)
		{
			morrowCooldown = tag.GetInt("morrowCooldown");
		}
	}
}
