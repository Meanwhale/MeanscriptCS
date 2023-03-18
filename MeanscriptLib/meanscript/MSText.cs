namespace Meanscript
{
	using Core;

	public class MSText
	{
		// Text bytes in an integer array. first int is the number of bytes. Bytes go from right to left (to be convinient on C++).
		// Specification:

		//		{0x00000000}{0x00000000}				= empty text, ie. just the terminating '\0' character
		//		{0x00000002}{0x00006261}				= 2 chars ("ab") and '\0', from right to left
		//		{0x00000005}{0x64636261}{0x00000065}	= 5 chars ("abcde") and '\0'

		// Number of ints after the first int 'i' is '(int)i / 4 + 1' if 'i > 0', and 0 otherwise.
		// Can't be modified. TODO: C++ reference counter for smart memory handling.

		public const int MAX_NUM_BYTES = 0x0000ffff;

		private IntArray data;

		private static readonly MSText _empty = new MSText("");
		public static MSText Empty() {return _empty;}

		public MSText(string src)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(src);
			Init(bytes, 0, bytes.Length);
		}

		public MSText(byte[] src, int start, int length)
		{
			Init(src, start, length);
		}
		public MSText(MSInput input)
		{
			int numBytes = input.ReadInt();
			MS.Assertion(numBytes <= MAX_NUM_BYTES);
			data = new IntArray((numBytes / 4) + 2);
			data[0] = numBytes;
			for (int i = 1; i < data.Length; i++)
			{
				data[i] = input.ReadInt();
			}
		}
		public void Write(MSOutput output)
		{
			data.Write(output);
		}
		private void Init(byte[] src, int start, int length)
		{
			data = new IntArray((length / 4) + 2);
			data[0] = length;
			MS.BytesToInts(src, start, data.Data(), 1, length);
		}

		public MSText(MSText src)
		{
			Copy(src.data);
		}

		public MSText(IntArray src)
		{
			Copy(src);
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
		public void Copy(IntArray src)
		{
			data = new IntArray(src.Length);
			IntArray.Copy(src, data, src.Length);
		}
		public int Compare(MSText text)
		{
			return MC.CompareIntStringsWithSizeEquals(data, text.data);
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
			return System.Text.Encoding.UTF8.GetString(MS.GetIntsToBytesLE(data, 1, data[0]));
		}
		
		// 'object' overrides:

		public override int GetHashCode()
		{
			int hash = 0;
			for (int i = 0; i < data.Length; i++) hash += data[i];
			return hash;
		}
		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			if (obj is MSText t) return Compare(t) == 0;
			if (obj is string s) return System.Text.Encoding.UTF8.GetString(MS.GetIntsToBytesLE(data, 1, data[0])).Equals(s);
			return false;
		}
		public override string ToString()
		{
			try
			{
				Check();
				return System.Text.Encoding.UTF8.GetString(MS.GetIntsToBytesLE(data, 1, data[0]));
			}
			catch (System.Exception)
			{
				return "?";
			}
		}
	}
}
