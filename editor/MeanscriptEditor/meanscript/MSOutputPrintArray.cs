namespace Meanscript
{
	using Core;

	public class MSOutputPrintArray : MSOutputPrint
	{
		internal byte[] buffer;
		internal int maxSize;
		internal int index;


		public MSOutputPrintArray()
		{
			maxSize = MS.globalConfig.outputArraySize;
			{ buffer = new byte[maxSize]; };
			index = 0;
		}

		override public void Close()
		{
			WriteByte(0);
		}

		override public void WriteByte(byte b)
		{
			MS.Assertion(index != -1, MC.EC_DATA, "output closed");
			MS.Assertion(index < maxSize, MC.EC_DATA, "output: buffer overflow");
			buffer[index++] = b;
		}

		override public MSOutputPrint Print(char x)
		{
			WriteByte((byte)x);
			return this;
		}

		override public MSOutputPrint Print(string x)
		{
			byte[] buffer;
			buffer = System.Text.Encoding.UTF8.GetBytes(x);
			for (int i = 0; i < buffer.Length; i++)
			{
				WriteByte(buffer[i]);
			}
			return this;
		}

		public string GetString()
		{
			return System.Text.Encoding.UTF8.GetString(buffer, 0, index - 1);
		}
	}
}
