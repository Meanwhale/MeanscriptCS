namespace Meanscript
{
	public class MSConfig
	{
		// configuration parameters

		public readonly int ipStackSize = 1024;
		public readonly int maxFunctions = 256;
		public readonly int registerSize = 256;
		public readonly int maxNameLength = 128; // TODO: not a configurable, but according to language spex
		public readonly int stackSize = 65536;
	}
}
