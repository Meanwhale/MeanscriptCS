namespace Meanscript
{
	using Core;

	public class MSOutputArray : MSOutputStream
	{
		internal byte[] buffer;
		internal int maxSize;
		internal int index;

		public MSOutputArray()
		{
			maxSize = MS.globalConfig.outputArraySize;
			{ buffer = new byte[maxSize]; };
			index = 0;
		}

		override public void Close()
		{
			index = -1;
		}
		
		override public void WriteByte(byte b)
		{
			MS.Assertion(index != -1, MC.EC_DATA, "output closed");
			MS.Assertion(index < maxSize, MC.EC_DATA, "output: buffer overflow");
			buffer[index++] = b;
		}
	}
}
