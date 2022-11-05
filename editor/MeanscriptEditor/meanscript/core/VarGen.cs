namespace Meanscript
{

	public class VarGen : MC
	{
		internal int size;
		internal int type;
		internal int address;
		internal int arraySize;
		internal int charCount;
		internal bool isReference;

		public VarGen(int _size, int _type, int _address, int _arraySize, int _charCount, bool _isReference)
		{
			MS.Assertion(_type > 0 && _type < MAX_TYPES, MC.EC_INTERNAL, "invalid type: " + _type);

			size = _size;
			type = _type; // if array, then array item type
			address = _address;
			arraySize = _arraySize; // it's array if > 0
			charCount = _charCount;
			isReference = _isReference;
		}

		public bool IsArray()
		{
			return arraySize > 0;
		}

	}
}
