namespace Meanscript
{
	using Core;

	public abstract class MSInputStream
	{
		public MSInputStream()
		{
		}

		public abstract int GetByteCount();
		public abstract int ReadByte();
		public abstract bool End();
		public abstract void Close();

		public int ReadByteWithCheck()
		{
			int i = ReadByte();
			MS.Assertion(i != -1, MC.EC_DATA, "input error (readByteWithCheck)");
			return i;
		}

		public int ReadInt()
		{
			// bytes:	b[0] b[1] b[2] b[3] b[4] b[5] b[6] b[7]   ...
			// ints:	_________i[0]______|_________i[1]______|_ ...

			int i = 0;
			i |= (int)((ReadByte() << 24) & 0xff000000);
			i |= (int)((ReadByte() << 16) & 0x00ff0000);
			i |= (int)((ReadByte() << 8) & 0x0000ff00);
			i |= (int)((ReadByte()) & 0x000000ff);
			return i;
		}

		public void ReadArray(IntArray trg, int numInts)
		{
			MS.Assertion(numInts <= (GetByteCount() * 4) + 1, MC.EC_DATA, "readArray: buffer overflow");
			for (int i = 0; i < numInts; i++)
			{
				trg[i] = ReadInt();
			}
			MS.Assertion(End(), MC.EC_INTERNAL, "all bytes not read");
		}
	}
}
