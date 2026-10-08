using System;
using System.Linq;
using System.Reflection;

internal static class Program
{
	private static void Main()
	{
		AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
		{
			string simple = new AssemblyName(e.Name).Name;
			string[] roots = {
				@"e:\steam\steamapps\common\tModLoader\Libraries",
				@"e:\steam\steamapps\common\tModLoader",
				@"e:\steam\steamapps\common\tModLoader\dotnet\shared\Microsoft.NETCore.App"
			};

			foreach (string root in roots) {
				if (!System.IO.Directory.Exists(root)) {
					continue;
				}

				foreach (string file in System.IO.Directory.GetFiles(root, simple + ".dll", System.IO.SearchOption.AllDirectories)) {
					try {
						return Assembly.LoadFrom(file);
					}
					catch {
						// 试下一个
					}
				}
			}

			return null;
		};

		Assembly asm = typeof(Terraria.Player).Assembly;
		Console.WriteLine("Assembly: " + asm.FullName);
		Type t = typeof(Terraria.Player);

		string[] fields = {
			"dropRate", "luck", "whipRangeMultiplier", "immuneTime", "aggro", "noKnockback",
			"allCrit", "minionKB", "fireWalk", "shadowDodge", "longInvince", "hurtCooldown",
			"endurance", "noFallDmg", "dashType", "dash", "dashDelay", "maxMinions", "resistCold",
			"pickSpeed", "moveSpeed", "statDefense", "ammoCost75", "ammoCost80", "manaCost",
			"GetDropRate", "whipDamage", "minionDamage", "lifeRegen", "setBonus", "ownedProjectileCounts"
		};

		foreach (string name in fields) {
			FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
			PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
			Console.WriteLine($"{name,-24} field={(f != null ? f.FieldType.Name : "-"),-10} prop={(p != null ? p.PropertyType.Name : "-")}");
		}

		Console.WriteLine("===== methods with Immune =====");

		foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name.Contains("Immune"))) {
			Console.WriteLine(m.Name + "(" + string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name)) + ")");
		}

		Console.WriteLine("===== Player.Hurt =====");

		foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name == "Hurt")) {
			Console.WriteLine("Hurt(" + string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name)) + ")");
		}

		Console.WriteLine("===== NPC fields =====");
		Type n = typeof(Terraria.NPC);

		foreach (string name in new[] { "coldDamage", "buffImmune", "lifeRegenCount" }) {
			FieldInfo f = n.GetField(name, BindingFlags.Public | BindingFlags.Instance);
			Console.WriteLine($"{name,-20} {(f != null ? f.FieldType.Name : "-")}");
		}

		Console.WriteLine("===== Projectile.Kill / NewProjectile =====");

		foreach (MethodInfo m in typeof(Terraria.Projectile).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name == "Kill")) {
			Console.WriteLine("Kill(" + string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name)) + ")");
		}

		foreach (MethodInfo m in typeof(Terraria.Projectile).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "NewProjectile")) {
			Console.WriteLine("NewProjectile(" + string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name)) + ")");
		}

		Console.WriteLine("===== ModPlayer hooks =====");
		Type mp = typeof(Terraria.ModLoader.ModPlayer);

		foreach (string name in new[] { "ModifyHurt", "OnHurt", "PostHurt", "ResetEffects", "PostUpdateEquips", "PostUpdate", "PostUpdateMiscEffects", "OnHitNPCWithItem", "OnHitNPCWithProj", "ModifyHitNPCWithItem" }) {
			bool exists = mp.GetMethods(BindingFlags.Public | BindingFlags.Instance).Any(m => m.Name == name);
			Console.WriteLine($"{name,-24} {exists}");
		}

		Console.WriteLine("===== Player.HurtModifiers fields =====");
		Type hm = typeof(Terraria.Player.HurtModifiers);

		foreach (FieldInfo f in hm.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
			Console.WriteLine($"{f.Name,-32} {f.FieldType.Name}");
		}

		foreach (PropertyInfo p in hm.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
			Console.WriteLine($"prop {p.Name,-28} {p.PropertyType.Name}");
		}
	}
}
