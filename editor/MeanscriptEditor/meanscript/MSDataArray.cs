namespace Meanscript {

public class MSDataArray : MC {
MeanMachine mm;
int itemType,
	itemCount,
	itemDataSize,
	dataIndex;  // address of the data
IntArray structCode;	// where struct info is
IntArray dataCode;	// where actual data is

public MSDataArray (MeanMachine _mm, int _itemType, int _itemCount, int _itemDataSize, int _dataIndex) 
{
	MS.assertion(_itemType > 0 && _itemType < MAX_TYPES,MC.EC_INTERNAL, "invalid type ID: " + _itemType);
	MS.assertion(_itemCount > 0,MC.EC_INTERNAL, "invalid item count: " + _itemCount);
	MS.assertion(_itemDataSize > 0,MC.EC_INTERNAL, "invalid item data size: " + _itemDataSize);
	
	mm = _mm;
	
	structCode = mm.getStructCode();
	dataCode = mm.getDataCode();

	itemType = _itemType;
	itemCount = _itemCount;
	itemDataSize = _itemDataSize;
	dataIndex = _dataIndex;
}
public int getItemType ()
{
	return itemType;
}

public int getArrayItemCount () 
{
	return itemCount;
}

public int getArrayDataSize () 
{
	return itemDataSize;
}

// TODO: getIntAt(i), getTextAt(i), etc.

public MSData getAt (int i) 
{
	MS.assertion(i >=0 && i < itemCount, EC_DATA, "index out of bounds");
	int itemAddress = dataIndex + (i * itemDataSize);
	return new MSData (mm, itemType, itemAddress);
}

//;

}
}
