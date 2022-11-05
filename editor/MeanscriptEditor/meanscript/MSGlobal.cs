namespace Meanscript
{

	public class MSGlobal : MC
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
		public readonly int outputArraySize = 65536;
		public readonly int maxCallbacks = 256;

		// bool verbose = true; // --> native

		// stream types

		public readonly int STREAM_TYPE_FIRST = 100001;
		public readonly int STREAM_BYTECODE = 100001;
		public readonly int STREAM_SCRIPT = 100002;
		public readonly int STREAM_BYTECODE_READ_ONLY = 100003;
		public readonly int STREAM_TYPE_LAST = 100003;

		// public void setVerbose (bool b)
		// {
		// verbose = b;
		// }
		// #ifndef CPP
		// public bool verboseOn ()
		// {
		// return verbose;
		// }
		// #endif

	}
}
