
namespace Meanscript
{
	public class MSBytecodeFileInput : MSInput
	{
		private BinaryReader reader;

		public MSBytecodeFileInput(string filename)
		{
			var stream = File.Open(filename, FileMode.Open);
			reader = new BinaryReader(stream, System.Text.Encoding.UTF8, false);
		}
		public override void Close()
		{
			reader.Close();
		}
		public override bool End()
		{
			return reader.BaseStream.Position != reader.BaseStream.Length;
		}
		public override long GetByteCount()
		{
			return reader.BaseStream.Length;
		}
		public override byte ReadByte()
		{
			return reader.ReadByte();
		}
		public override int ReadInt()
		{
			return reader.ReadInt32();
		}
	}
}
