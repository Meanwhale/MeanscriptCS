namespace Meanscript
{
	using Core;
	public abstract class MSOutput
	{

		public MSOutput()
		{
		}
		public abstract void WriteByte(byte b);
		public abstract void Close();
		public abstract bool Closed();

		public virtual void WriteInt(int i)
		{
			WriteByte((byte)((i >> 24) & 0xff));
			WriteByte((byte)((i >> 16) & 0xff));
			WriteByte((byte)((i >> 8) & 0xff));
			WriteByte((byte)(i & 0xff));
		}

		public void Write(IntArray code, int firstIndex, int length)
		{
			for (int i = firstIndex; i < firstIndex + length; i++)
			{
				WriteInt(code[i]);
			}
		}
		public void WriteOpWithData(int operation, int size, int valueType, int data)
		{
			int instruction = MC.MakeInstruction(operation, size, valueType);
			WriteInt(instruction);
			WriteInt(data);
			MS.Verbose("add instruction with data: [" + MC.GetOpName(instruction) + "] [" + data + "]");
		}

		public void WriteOp(int operation, int size, int valueType)
		{
			int instruction = MC.MakeInstruction(operation, size, valueType);
			WriteInt(instruction);
			MS.Verbose("add instruction: [" + MC.GetOpName(instruction) + "]");
		}
	}
}
