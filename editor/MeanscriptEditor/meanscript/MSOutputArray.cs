namespace Meanscript
{
	public class MSOutputArray : MSOutput
	{
		internal DynamicArray buffer;
		public int Index { get; private set; } // byte index

		public MSOutputArray()
		{
			buffer = new DynamicArray(16384);
			Index = 0;
		}

		public override void WriteInt(int i)
		{
			buffer.Add(i);
			Index += 4;
		}

		override public void Close()
		{
			Index = -1;
		}
		public override bool Closed()
		{
			return Index < 0;
		}
		
		override public void WriteByte(byte b)
		{
			MS.Assertion(false);
			//MS.Assertion(Index != -1, MC.EC_DATA, "output closed");
			//MS.Assertion(Index < maxSize, MC.EC_DATA, "output: buffer overflow");
			//buffer[Index++] = b;
		}
		public int [] GetCopyAndClear()
		{
			var ints = new int[buffer.Count];
			IntArray.Copy(buffer.Data(), ints, buffer.Count);
			buffer = null;
			Close();
			return ints;
		}

	}
}
