using System;

namespace Meanscript
{
	public class MException : System.Exception
	{
		public readonly MSError error;
		private readonly string info;
		public MException() { error = null; info = "Meanscript exception"; }
		public MException(MSError err, string s) { error = err; info = s; }
		new public string ToString() {
			return "---------------- EXCEPTION ----------------\n"
			     + info
				 + "\n-------------------------------------------"; }
	}
	public class Printer : MSOutputPrint
	{
		override public void WriteByte(byte x) { System.Console.Write((char)x); }
		override public MSOutputPrint Print(String x) { System.Console.Write(x); return this; }
		override public MSOutputPrint Print(char x) { System.Console.Write(x); return this; }
	}

	/*public class Printer
	{
		// override to implement own printer
		public virtual Printer print(object o)
		{
			Console.Write(o);
			return this;
		}
		public Printer print(int i)
		{
			print("" + i);
			return this;
		}
		public Printer print(char c)
		{
			print("" + c);
			return this;
		}
		public static string[] hexs = new string[] {
			"0","1","2","3",
			"4","5","6","7",
			"8","9","a","b",
			"c","d","e","f",
			};
		public Printer printHex(int h)
		{
			print("0x");
			for (int i = 28; i >= 0; i -= 4)
			{
				int index = (h >> i); // TODO: zero fill?
				index &= 0x0000000f;
				print(hexs[index]);
			}
			return this;
		}
	}

	public class NullPrinter : Printer
	{
		override public Printer print(object o)
		{
			return this;
		}
	}*/

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
		internal static bool _verboseOn = true;

		public delegate void MAction();
		public delegate void MCallbackAction(MeanMachine mm, MArgs args);

		public static MSOutputPrint printOut = new Printer();
		public static MSOutputPrint errorOut = new Printer();
		public static MSOutputPrint userOut = new Printer();
		public static MSGlobal globalConfig = new MSGlobal();

		public static System.Collections.Generic.IEqualityComparer<MSText> textComparer = new TextComparer();

		public class TextComparer : System.Collections.Generic.IEqualityComparer<MSText>
		{
			public bool Equals(MSText x, MSText y)
			{
				return x.Compare(y) == 0;
			}
			public int GetHashCode(MSText x)
			{
				return x.HashCode();
			}
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

		internal static void SyntaxAssertion(bool b, NodeIterator it, string s)
		{
			// TODO: printtaa node. Javasta mallia.
			Assertion(b, null, s);
		}

		public static void NativeTest()
		{
			// TODO: testaa unicode

			string s = "Toimii!";
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(s);
			IntArray ia = BytesToInts(bytes);
			byte[] ba = IntsToBytes(ia, 0, 7);
			string ns = System.Text.Encoding.UTF8.GetString(ba);

			printOut.Print("Toimii? " + s.Equals(ns));
		}

		public static byte[] IntsToBytes(IntArray ia, int iaOffset, int bytesLength)
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
