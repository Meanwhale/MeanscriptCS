using System;

namespace Meanscript
{
	public class MException : System.Exception
	{
		private static string begin = "---------------- EXCEPTION ----------------\n";
		private static string end = "\n-------------------------------------------";
		public readonly MSError error;
		private string info = "Meanscript exception";
		public MException() { error = null; }
		public MException(MSError err, string s) { error = err; info = s; }
		new public string ToString() { return begin + info + end; }
	}
	public class Printer : MSOutputPrint
	{
		override public void writeByte(byte x) { System.Console.Write((char)x); }
		override public MSOutputPrint print(String x) { System.Console.Write(x); return this; }
		override public MSOutputPrint print(char x) { System.Console.Write(x); return this; }
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

		public static Printer printOut = new Printer();
		public static Printer errorOut = new Printer();
		public static Printer userOut = new Printer();
		public static MSGlobal globalConfig = new MSGlobal();

		public static System.Collections.Generic.IEqualityComparer<MSText> textComparer = new TextComparer();

		public class TextComparer : System.Collections.Generic.IEqualityComparer<MSText>
		{
			public bool Equals(MSText x, MSText y) {
				return x.compare(y) == 0;
			}
			public int GetHashCode(MSText x) {
				return x.hashCode();
			}
		}
		
		public static void print(string s)
		{
			printOut.print(s).endLine();
		}
		public static void printn(string s)
		{
			printOut.print(s);
		}
		public static void verbose(string s)
		{
			if(_verboseOn) printOut.print(s).endLine();
		}
		public static void verbosen(string s)
		{
			if(_verboseOn) printOut.print(s);
		}

		public static void assertion(bool b, string msg)
		{
			if (!b) { throw new MException(null, "assertion failed: " + msg); }
		}

		public static void assertion(bool b, MSError err, string msg)
		{
			if (!b) { throw new MException(err, "assertion failed: " + msg); }
		}

		internal static void syntaxAssertion(bool b, NodeIterator it, string s)
		{
			// TODO: printtaa node. Javasta mallia.
			assertion(b, s);
		}

		public static void nativeTest()
		{
			// TODO: testaa unicode

			string s = "Toimii!";
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(s);
			IntArray ia = bytesToInts(bytes);
			byte[] ba = intsToBytes(ia, 0, 7);
			string ns = System.Text.Encoding.UTF8.GetString(ba);

			printOut.print("Toimii? " + s.Equals(ns));
		}

		public static byte[] intsToBytes(IntArray ia, int iaOffset, int bytesLength)
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

		public static IntArray bytesToInts(byte[] ba)
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


		public static int floatToIntFormat(float f)
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
		}
		public static long float64ToInt64Format(double f)
		{
			return BitConverter.ToInt64(BitConverter.GetBytes(f), 0);
		}
		public static float intFormatToFloat(int i)
		{
			return BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
		}
		public static double int64FormatToFloat64(long i)
		{
			return BitConverter.ToDouble(BitConverter.GetBytes(i), 0);
		}
		public static int parseInt(string s)
		{
			return System.Int32.Parse(s);
		}
		public static long parseInt64(string s)
		{
			return System.Int64.Parse(s);
		}
		public static float parseFloat32(string s)
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
		public static double parseFloat64(string s)
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
