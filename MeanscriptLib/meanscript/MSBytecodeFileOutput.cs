
namespace Meanscript
{
	using Core;
	public class MSBytecodeFileOutput : MSOutput
	{
		private BinaryWriter writer;

		public MSBytecodeFileOutput(string filename)
		{
			var stream = File.Open(filename, FileMode.Create);
			writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, false);
		}

		public override void Close()
		{
			writer.Flush();
			writer.Close();
		}

		public override bool Closed()
		{
			return writer.BaseStream == null;
		}

		public override void WriteByte(byte x)
		{
			writer.Write(x);
		}
		public override void WriteInt(int x)
		{
			writer.Write(x);
		}
	}
}
