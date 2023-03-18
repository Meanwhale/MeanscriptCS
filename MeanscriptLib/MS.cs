using System;

namespace Meanscript
{
	using Core;

	public class MException : System.Exception
	{
		public readonly MSError error;
		public readonly string info;
		public MException() {
			error = null; info = "Meanscript exception";
		}
		public MException(MSError err, string s) {
			error = err; info = s;
		}
		new public string ToString() {
			return "\n---------------- EXCEPTION ----------------\n"
			     + info
				 + "\n-------------------------------------------\n"; }
	}
	public class Printer : MSOutputPrint
	{
		override public void WriteByte(byte x) { System.Console.Write((char)x); }
		override public MSOutputPrint Print(string x) { System.Console.Write(x); return this; }
		override public MSOutputPrint Print(char x) { System.Console.Write(x); return this; }
	}

	public class MSError
	{
		public readonly MSError type;
		public readonly string title;

		public MSError(MSError type, string title)
		{
			this.type = type;
			this.title = title;
		}
	}

	public class MS
	{
		internal static bool _debug = true;
		public static bool _verboseOn = true;

		public delegate void MAction();
		public delegate void MCallbackAction(MeanMachine mm, MArgs args);

		public static MSOutputPrint printOut = new Printer();
		public static MSOutputPrint errorOut = new Printer();
		public static MSOutputPrint userOut = new Printer();
		public static MSConfig globalConfig = new MSConfig();

		public static System.Collections.Generic.IEqualityComparer<MSText> textComparer = new TextComparer();

		public class TextComparer : System.Collections.Generic.IEqualityComparer<MSText>
		{
			public bool Equals(MSText x, MSText y)
			{
				return x.Compare(y) == 0;
			}
			public int GetHashCode(MSText x)
			{
				return x.GetHashCode();
			}
		}
		public static string Title(string s)
		{
			return "-------------------------------------------\n     "
			     + s
				 + "\n-------------------------------------------";
		}
		public static void Print(string s)
		{
			printOut.Print(s).EndLine();
		}
		public static void Printn(string s)
		{
			printOut.Print(s);
		}
		public static void Verbose(string s)
		{
			if (_verboseOn) printOut.Print(s).EndLine();
		}
		public static void Verbosen(string s)
		{
			if (_verboseOn) printOut.Print(s);
		}

		public static void Assertion(bool b)
		{
			Assertion(b, null, "");
		}

		public static void Assertion(bool b, MSError err, string msg)
		{
			if (!b)
			{
				throw new MException(err, "assertion failed: " + msg);
			}
		}
		
		public static void Assertion(bool b, MSError err, Func<string> f)
		{
			if (b) return;
			Assertion(false, err, f()); // assertion that has some code to produce error message
		}
		internal static void SyntaxAssertion(bool b, NodeIterator it, Func<string> f)
		{
			if (b) return;
			SyntaxAssertion(false, it, f()); 
		}
		internal static void SyntaxAssertion(bool b, NodeIterator it, string s)
		{
			// printtaa node. Javasta mallia.
			s += "\nLine: " + it.Line() + " Character: " + it.node.characterNumber;
			Assertion(b, MC.EC_SYNTAX, s);
		}

		/*public static byte[] IntsToBytesLE(IntArray ia, int iaOffset, int bytesLength)
		{

			byte[] bytes = new byte[bytesLength];

			int shift = 0;
			for (int i = 0; i < bytesLength;)
			{
				//ints[i/4] += (ba[i] & 0x000000FF) << shift;
				bytes[i] = (byte)((ia[iaOffset + (i / 4)] >> shift) & 0x000000FF);

				i++;
				if (i % 4 == 0) shift = 0;
				else shift += 8;
			}
			return bytes;
		}

		public static IntArray BytesToInts(byte[] ba)
		{
			int bytesLength = ba.Length;
			int intsLength = (bytesLength / 4) + 1;
			var ints = new IntArray(intsLength);

			int shift = 0;
			for (int i = 0; i < bytesLength;)
			{
				ints[i / 4] += (ba[i] & 0x000000FF) << shift;

				i++;
				if (i % 4 == 0) shift = 0;
				else shift += 8;
			}
			return ints;
		}*/
		public static byte[] GetIntsToBytesLE(IntArray ia, int iaOffset, int bytesLength)
		{
			byte[] bytes = new byte[bytesLength];
			IntsToBytesLE(ia, iaOffset, bytes, 0, bytesLength);
			return bytes;
		}
		public static void IntsToBytesLE(IntArray ints, int intsOffset, byte[] bytes, int bytesOffset, int bytesLength)
		{
			// little endian, for strings

			// For example 0x00ccbbaa
			// --> byte[]
			// [0]	0xaa	byte
			// [1]	0xbb	byte
			// [2]	0xcc	byte
			
			Assertion(bytesLength >= 0);
			if (bytesLength == 0) return;
			Assertion(bytes.Length >= bytesOffset + bytesLength);
			int intsLength = (bytesLength / 4) + 1;
			int bytesLast = bytesOffset + bytesLength;

			for (int i = intsOffset; i < intsOffset + intsLength; i++)
			{
				int data = ints[i];
				if (bytesOffset < bytesLast) bytes[bytesOffset] = (byte)(data       & 0x000000ff); else break; bytesOffset++;
				if (bytesOffset < bytesLast) bytes[bytesOffset] = (byte)(data >> 8  & 0x000000ff); else break; bytesOffset++;
				if (bytesOffset < bytesLast) bytes[bytesOffset] = (byte)(data >> 16 & 0x000000ff); else break; bytesOffset++;
				if (bytesOffset < bytesLast) bytes[bytesOffset] = (byte)(data >> 24 & 0x000000ff); else break; bytesOffset++;
			}
		}

		/*public static byte[] GetIntsToBytesBE(IntArray ia, int iaOffset, int bytesLength)
		{
			byte[] bytes = new byte[bytesLength];
			IntsToBytesBE(ia, iaOffset, bytes, 0, bytesLength);
			return bytes;
		}
		public static void IntsToBytesBE(IntArray ints, int intsOffset, byte[] bytes, int bytesOffset, int bytesLength)
		{
			// big endian, for strings

			// For example 0x00ccbbaa
			// --> byte[]
			// [0]	0x00	byte
			// [1]	0xcc	byte
			// [2]	0xbb	byte
			// [2]	0xaa	byte
			
			Assertion(bytesLength > 0);
			Assertion(bytes.Length >= bytesOffset + bytesLength);
			int intsLength = (bytesLength / 4) + 1;
			int bytesLast = bytesOffset + bytesLength;

			for (int i = intsOffset; i < intsOffset + intsLength; i++)
			{
				int data = ints[i];
				if (bytesOffset < bytesLast) bytes[bytesOffset] = (byte)(data >> 24 & 0x000000ff); else break; bytesOffset++;
				if (bytesOffset < bytesLast) bytes[bytesOffset] = (byte)(data >> 16 & 0x000000ff); else break; bytesOffset++;
				if (bytesOffset < bytesLast) bytes[bytesOffset] = (byte)(data >> 8  & 0x000000ff); else break; bytesOffset++;
				if (bytesOffset < bytesLast) bytes[bytesOffset] = (byte)(data       & 0x000000ff); else break; bytesOffset++;
			}
		}*/

		public static void BytesToInts(byte[] bytes, int bytesOffset, int[] ints, int intsOffset, int bytesLength)
		{
			// TODO: tarvitaanko bytesOffset?

			// order: 0x04030201

			// bytes:	b[3] b[2] b[1] b[0] b[7] b[6] b[5] b[4]...
			// ints:	_________i[0]______|_________i[1]______...

			int shift = 0;
			ints[intsOffset] = 0;
			for (int i = 0; i < bytesLength;)
			{
				ints[(i / 4) + intsOffset] += (bytes[i] & 0x000000FF) << shift;

				i++;
				if (i % 4 == 0)
				{
					shift = 0;
					if (i < bytesLength)
					{
						ints[(i / 4) + intsOffset] = 0;
					}
				}
				else shift += 8;
			}
		}

		public static int FloatToIntFormat(float f)
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
		}
		public static long Float64ToInt64Format(double f)
		{
			return BitConverter.ToInt64(BitConverter.GetBytes(f), 0);
		}
		public static float IntFormatToFloat(int i)
		{
			return BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
		}
		public static double Int64FormatToFloat64(long i)
		{
			return BitConverter.ToDouble(BitConverter.GetBytes(i), 0);
		}
		public static int ParseInt(string s)
		{
			return System.Int32.Parse(s);
		}
		public static long ParseInt64(string s)
		{
			return System.Int64.Parse(s);
		}
		public static float ParseFloat32(string s)
		{
			try
			{
				return float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				throw new MException(MC.EC_PARSE, "float parsing failed: " + s);
			}
		}
		public static double ParseFloat64(string s)
		{
			try
			{
				return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				throw new MException(MC.EC_PARSE, "float64 parsing failed: " + s);
			}
		}
	}
}
