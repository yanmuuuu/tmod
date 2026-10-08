using System;
using System.IO;
using System.Reflection;

public static class Probe
{
	public static void Main()
	{
		Assembly asm = Assembly.LoadFrom(@"E:\steam\steamapps\common\tModLoader\tModLoader.dll");
		Type bio = asm.GetType("Terraria.ModLoader.IO.BinaryIO", throwOnError: true);

		foreach (MethodInfo mi in bio.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
			Console.WriteLine("M: " + mi);
		}

		MethodInfo safeRead = null;
		foreach (MethodInfo mi in bio.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
			if (mi.Name == "SafeRead") {
				safeRead = mi;
			}
		}

		Console.WriteLine("SafeRead = " + safeRead);

		// Case A: reader over a stream that has more bytes than "length" declares
		byte[] payload = new byte[] { 0x02, 0x00, 0x00, 0x00, 0xAA, 0xBB };
		using (MemoryStream ms = new MemoryStream(payload))
		using (BinaryReader br = new BinaryReader(ms)) {
			int total = br.ReadInt32();
			Console.WriteLine("total(len field)=" + total + " pos=" + br.BaseStream.Position);
			try {
				safeRead.Invoke(null, new object[] { br, total });
				Console.WriteLine("A ok, pos=" + br.BaseStream.Position);
			}
			catch (TargetInvocationException tie) {
				Console.WriteLine("A threw: " + tie.InnerException.GetType().Name + ": " + tie.InnerException.Message);
			}
		}

		// Case B: reader over a 6-byte stream, handler reads only 2 bytes
		byte[] payload2 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
		using (MemoryStream ms = new MemoryStream(payload2))
		using (BinaryReader br = new BinaryReader(ms)) {
			br.ReadByte();
			br.ReadByte();
			try {
				safeRead.Invoke(null, new object[] { br, 6 });
				Console.WriteLine("B ok, pos=" + br.BaseStream.Position);
			}
			catch (TargetInvocationException tie) {
				Console.WriteLine("B threw: " + tie.InnerException.GetType().Name + ": " + tie.InnerException.Message);
			}
		}

		// Case C: reader over a 9-byte stream, handler reads 1 byte
		byte[] payload3 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
		using (MemoryStream ms = new MemoryStream(payload3))
		using (BinaryReader br = new BinaryReader(ms)) {
			br.ReadByte();
			try {
				safeRead.Invoke(null, new object[] { br, 9 });
				Console.WriteLine("C ok, pos=" + br.BaseStream.Position);
			}
			catch (TargetInvocationException tie) {
				Console.WriteLine("C threw: " + tie.InnerException.GetType().Name + ": " + tie.InnerException.Message);
			}
		}
	}
}
