namespace Meanscript
{

	public class MSText : MC
	{
		// Text bytes in an integer array. first int is the number of bytes. Bytes go from right to left (to be convinient on C++).
		// Specification:

		//		{0x00000000}{0x00000000}				= empty text, ie. just the terminating '\0' character
		//		{0x00000002}{0x00006261}				= 2 chars ("ab") and '\0', from right to left
		//		{0x00000005}{0x64636261}{0x00000065}	= 5 chars ("abcde") and '\0'

		// Number of ints after the first int 'i' is '(int)i / 4 + 1' if 'i > 0', and 0 otherwise.
		// Can't be modified. TODO: C++ reference counter for smart memory handling.

		IntArray data;

		public MSText(string src)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(src);
			Init(bytes, 0, bytes.Length);
		}

		public MSText(byte[] src, int start, int length)
		{
			Init(src, start, length);
		}

		private void Init(byte[] src, int start, int length)
		{
			data = new IntArray((length / 4) + 2);
			data[0] = length;
			BytesToInts(src, start, data, 1, length);
		}

		public MSText(MSText src)
		{
			MakeCopy(src.data, 0);
		}

		public MSText(IntArray src)
		{
			MakeCopy(src, 0);
		}

		public MSText(IntArray src, int start)
		{
			MakeCopy(src, start);
		}


		public bool Match(MSText t)
		{
			return Compare(t) == 0;
		}

		public bool Match(string s)
		{
			return s.Equals(GetString());
		}

		public IntArray GetData()
		{
			return data;
		}

		public int NumBytes()
		{
			// count is without the ending character
			return data[0];
		}
		public int DataSize()
		{
			return data.Length;
		}
		public int ByteAt(int index)
		{
			MS.Assertion(index >= 0 && index <= data[0], MC.EC_INTERNAL, "index overflow");
			return ((data[(index / 4) + 1]) >> ((index % 4) * 8) & 0x000000ff);
		}
		public int HashCode()
		{
			int hash = 0;
			for (int i = 0; i < data.Length; i++) hash += data[i];
			return hash;
		}
		public int Write(IntArray trg, int start)
		{
			for (int i = 0; i < data.Length; i++)
			{
				trg[start + i] = data[i];
			}
			return start + data.Length;
		}
		public void MakeCopy(IntArray src, int start)
		{
			int numChars = src[start];
			int size32 = (numChars / 4) + 2;
			data = new IntArray(size32);
			for (int i = 0; i < size32; i++)
			{
				data[i] = src[i + start];
			}
		}
		public int Compare(MSText text)
		{
			// returns -1 (less), 1 (greater), or 0 (equal)

			if (data.Length != text.data.Length)
			{
				return data.Length > text.data.Length ? 1 : -1;
			}

			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] != text.data[i])
				{
					return data[i] > text.data[i] ? 1 : -1;
				}
			}
			return 0; // equals
		}
		public void Check()
		{
			int size32 = (data[0] / 4) + 2;
			MS.Assertion(data.Length == size32, MC.EC_INTERNAL, "corrupted MSText object (size don't match)");
			MS.Assertion(ByteAt(data[0]) == 0, MC.EC_INTERNAL, "corrupted MSText object (no zero byte at end)");
		}
		public string GetString()
		{
			Check();
			return System.Text.Encoding.UTF8.GetString(MS.IntsToBytes(data, 1, data[0]));
		}
		public override string ToString()
		{
			try
			{
				Check();
				return System.Text.Encoding.UTF8.GetString(MS.IntsToBytes(data, 1, data[0]));
			}
			catch (System.Exception)
			{
				return "?";
			}
		}

	}
}
