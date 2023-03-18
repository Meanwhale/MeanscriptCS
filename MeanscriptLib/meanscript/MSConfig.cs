namespace Meanscript
{
	public class MSConfig
	{

		// configuration parameters

		public readonly int maxStructDefSize = 4096;
		public readonly int maxStructMembers = 1024;
		public readonly int ipStackSize = 1024;
		public readonly int baseStackSize = 1024;
		public readonly int maxFunctions = 256;
		public readonly int registerSize = 256;
		public readonly int maxArraySize = 4096;
		public readonly int maxNameLength = 128; // TODO: not a configurable, but according to language spex
		public readonly int codeSize = 65536; // 2^16
		public readonly int stackSize = 65536;
		public readonly int builderValuesSize = 65536;
		public readonly int maxCallbacks = 256;
	}
}
