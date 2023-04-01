
namespace Meanscript
{
	using Core;

	public class MSScriptFileOutput : MSOutputPrint
	{
		private readonly StreamWriter writer;

		public MSScriptFileOutput(string fileName)
		{
			try
			{
				writer = new StreamWriter(fileName, System.Text.Encoding.UTF8, new FileStreamOptions());
			}
			catch (FileNotFoundException)
			{
				throw new MException(MC.EC_NATIVE, "file not found: " + fileName);
			}
		}
		public override void Close()
		{
			writer.Flush();
			writer.Close();
		}

		public override bool Closed()
		{
			return writer == null || writer.BaseStream == null;
		}

		public override MSOutputPrint Print(char x)
		{
			writer.Write(x);
			return this;
		}

		public override MSOutputPrint Print(string x)
		{
			writer.Write(x);
			return this;
		}

		public override void WriteByte(byte x)
		{
			writer.Write(x);
		}
	}
}
