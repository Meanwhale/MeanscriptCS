
namespace Meanscript
{
	using Core;
	public class MSScriptFileInput : MSInput
	{
		private readonly StreamReader reader;

		public MSScriptFileInput(string fileName)
		{
			try
			{
				reader = new StreamReader(fileName);
			}
			catch
			{
				throw new MException(MC.EC_NATIVE, "can't open file: " + fileName);
			}
		}

		public override void Close()
		{
			reader.Close();
		}

		public override bool End()
		{
			return reader.EndOfStream;
		}

		public override long GetByteCount()
		{
			MS.Assertion(false, MC.EC_NATIVE, "MSFileInput: size is unknown");
			return 0;
		}

		public override byte ReadByte()
		{
			return (byte)reader.Read();
		}
	}
}
