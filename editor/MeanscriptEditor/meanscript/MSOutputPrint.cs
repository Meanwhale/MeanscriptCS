namespace Meanscript
{

	public abstract class MSOutputPrint : MSOutputStream
	{

		// write byte as an ASCII char, e.g. writeByte(64) writes "@", and not "64"
		//public abstract void  writeByte (byte x) ;
		public abstract MSOutputPrint Print(char x);
		public abstract MSOutputPrint Print(string x);

		public MSOutputPrint PrintLine(string x)
		{
			return Print(x).EndLine();
		}

		public override void Close()
		{
		}

		public MSOutputPrint Print(MSText text)
		{
			if (text == null) return Print("null");
			return PrintIntsToChars(text.GetData(), 1, text.NumBytes(), false);
		}

		public MSOutputPrint Print(int x)
		{
			Print((long)x);
			return this;
		}
		public MSOutputPrint Print(long x)
		{
			// TODO: make iterative instead of recursive
			if (x < 0)
			{
				Print('-');
				x = -x;
			}
			if (x / 10 > 0) Print(x / 10);
			Print((char)('0' + (x % 10)));
			return this;
		}

		// Floating-point number printing uses native string for now.
		// For a 'proper' solution:
		//		- https://www.cs.tufts.edu/~nr/cs257/archive/florian-loitsch/printf.pdf
		//		- https://github.com/romange/Grisu

		public MSOutputPrint Print(float x)
		{
			Print(x.ToString(System.Globalization.CultureInfo.InvariantCulture));
			return this;
		}
		public MSOutputPrint Print(double x)
		{
			Print(x.ToString(System.Globalization.CultureInfo.InvariantCulture));
			return this;
		}
		public MSOutputPrint Print(bool x)
		{
			Print(x ? "true" : "false");
			return this;
		}

		public static readonly char[] hexs = new char[]
		{
			'0','1','2','3',
			'4','5','6','7',
			'8','9','a','b',
			'c','d','e','f'
		};

		// Character codes 0-255: ISO/IEC 8859-1
		//     0-31    Code symbols
		//     32-127  ASCII characters
		//     128-159 Not defined for ISO/IEC 8859-1
		//     160-255 Character descriptions (as their printed character can vary according to environment)
		// source: https://en.wikipedia.org/wiki/ISO/IEC_8859-1

		public static readonly string[] ascii = new string[]
		{
			"[NUL]",       // null
			"[SOH]",       // start of heading
			"[STX]",       // start of text
			"[ETX]",       // end of text
			"[EOT]",       // end of transmission
			"[ENQ]",       // enquiry
			"[ACK]",       // acknowledge
			"[BEL]",       // bell
			"[BS]",        // backpace
			"[HT]",        // horizontal tab
			"[LF]",        // line feed, new line
			"[VT]",        // vertical tab
			"[FF]",        // form feed, new page
			"[CR]",        // carriage return
			"[SO]",        // shift out
			"[SI]",        // shift in
			"[DLE]",       // data link escape
			"[DC1]",       // device control 1
			"[DC2]",       // device control 2
			"[DC3]",       // device control 3
			"[DC4]",       // device control 4
			"[NAK]",       // negative acknowledge
			"[SYN]",       // synchonous idle
			"[ETB]",       // end of transmission block
			"[CAN]",       // cancel
			"[EM]",        // end of medium
			"[SUB]",       // substitute
			"[ESC]",       // escape
			"[FS]",        // file separator
			"[GS]",        // group separator
			"[RS]",        // record separator
			"[US]"         // unit separator
		};


		public MSOutputPrint PrintHex(int h)
		{
			for (int i = 28; i >= 0; i -= 4)
			{
				int index = (h >> i);
				index &= 0x0000000f;
				Print(hexs[index]);
			}
			return this;
		}

		public MSOutputPrint PrintCharSymbol(int i)
		{
			// print an ASCII character, symbol, or description for it.
			if (i > 127) // 127 = [DEL]
			{
				Print("[#");
				Print(i);
				Print("]");
			}
			else if (i < 32) Print(ascii[i]);
			else if (i == 127) Print("[DEL]");
			else Print((char)i);
			return this;
		}

		public MSOutputPrint PrintIntsToChars(IntArray ints, int start, int numChars, bool quote)
		{

			int shift = 0;
			for (int i = 0; i < numChars;)
			{
				int b = ((ints[start + (i / 4)] >> shift) & 0x000000FF);

				if (quote)
				{
					if (b < 32 || b >= 127)
					{
						Print("\\x");
						int index = (b >> 4) & 0x0000000f;
						Print(hexs[index]);
						index = b & 0x0000000f;
						Print(hexs[index]);
					}
					else Print((char)b);
				}
				else if (b >= 32 && b < 128)
				{
					Print((char)b); //print(ascii[b]);
				}
				else
				{
					Print("[#");
					Print(b);
					Print("]");
				}

				i++;
				if (i % 4 == 0) shift = 0;
				else shift += 8;
			}
			return this;
		}

		public MSOutputPrint EndLine()
		{
			Print('\n');
			return this;
		}
	}
}
