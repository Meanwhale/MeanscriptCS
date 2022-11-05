namespace Meanscript
{

	public class MSInputArray : MSInputStream
	{
		byte[] buffer;
		int size;
		int index;

		public MSInputArray(MSOutputArray output)
		{
			// copy array
			size = output.index;
			{ buffer = new byte[size]; };
			for (int i = 0; i < size; i++) buffer[i] = output.buffer[i];
			index = 0;
		}

		public MSInputArray(string s)
		{
			buffer = System.Text.Encoding.UTF8.GetBytes(s);
			size = buffer.Length;
			index = 0;
		}

		override public int GetByteCount()
		{
			return size;
		}

		override public int ReadByte()
		{
			//MS.assertion(!end(), EC_DATA, "readInt: buffer overflow");
			if (End()) return -1;
			return (((int)buffer[index++]) & 0xff);
		}

		override public bool End()
		{
			return index >= size;
		}

		override public void Close()
		{
		}

	}
}
