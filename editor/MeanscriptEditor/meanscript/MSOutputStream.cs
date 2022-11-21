namespace Meanscript
{

	public abstract class MSOutputStream : MC
	{

		public MSOutputStream()
		{
		}
		public abstract void WriteByte(byte b);
		public abstract void Close();

		public void WriteInt(int i)
		{
			WriteByte((byte)((i >> 24) & 0xff));
			WriteByte((byte)((i >> 16) & 0xff));
			WriteByte((byte)((i >> 8) & 0xff));
			WriteByte((byte)(i & 0xff));
		}
	}
}
