namespace Meanscript
{

	public class MSDataArray : MC
	{
		readonly MeanMachine mm;
		readonly int itemType,
			itemCount,
			itemDataSize,
			dataIndex;  // address of the data

		public MSDataArray(MeanMachine _mm, int _itemType, int _itemCount, int _itemDataSize, int _dataIndex)
		{
			MS.Assertion(_itemType > 0 && _itemType < MAX_TYPES, MC.EC_INTERNAL, "invalid type ID: " + _itemType);
			MS.Assertion(_itemCount > 0, MC.EC_INTERNAL, "invalid item count: " + _itemCount);
			MS.Assertion(_itemDataSize > 0, MC.EC_INTERNAL, "invalid item data size: " + _itemDataSize);

			mm = _mm;
			itemType = _itemType;
			itemCount = _itemCount;
			itemDataSize = _itemDataSize;
			dataIndex = _dataIndex;
		}
		public int GetItemType()
		{
			return itemType;
		}

		public int GetArrayItemCount()
		{
			return itemCount;
		}

		public int GetArrayDataSize()
		{
			return itemDataSize;
		}

		// TODO: getIntAt(i), getTextAt(i), etc.

		public MSData GetAt(int i)
		{
			MS.Assertion(i >= 0 && i < itemCount, EC_DATA, "index out of bounds");
			int itemAddress = dataIndex + (i * itemDataSize);
			return new MSData(mm, itemType, itemAddress);
		}

		//;

	}
}
