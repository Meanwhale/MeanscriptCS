
using System;

namespace Meanscript.Core
{
	public abstract class IDynamicObject
	{
		// base class for dynamic data objects, like data array, map,
		// and in the future list, etc.

		public readonly int tag;

		protected IDynamicObject(int tag)
		{
			this.tag = tag;
		}
		public int HeapID()
		{
			return MCHeap.TagHeapIndex(tag);
		}
		public int DataTypeID()
		{
			return MCHeap.TagType(tag);
		}
		public abstract void Print(MSOutputPrint o);
		public abstract void Write(MSOutput output);

	}
	public class MCStore : IDynamicObject
	{
		// Dynamically allocated Data

		public enum Role
		{
			GLOBAL,
			CONTEXT,
			OBJECT
		}

		// dynamic data
		public Role role;
		public IntArray data;
		
		public MCStore(Role role, int tag, IntArray data) : base(tag)
		{
			this.role = role;
			this.data = data;
		}
		
		public MCStore(Role role, int tag, int[] src, int startIndex, int dataLength) :
			this(role, tag, new IntArray(dataLength))
		{
			IntArray.Copy(src, startIndex, data.Data(), 0, dataLength);
		}
		public MCStore(Role role, int tag, MSInput input, int dataLength) :
			this(role, tag, new IntArray(dataLength))
		{
			IntArray.Read(input, data.Data(), dataLength);
		}
		override public void Write(MSOutput output)
		{
			// [ OP > HeapID > ... data ... ] size = data size + 1 (heapID)
					
			output.WriteOp(MC.OP_WRITE_HEAP_OBJECT, data.Length + 1, DataTypeID());
			output.WriteInt(HeapID());
			output.Write(data, 0, data.Length);
		}
		override public void Print(MSOutputPrint o)
		{
			o.PrintHex(tag).Print(" ");
			data.Print(o);
		}
		public bool InRange(int offset)
		{
			return offset >= 0 && offset < data.Length;
		}
	}

	public class MCMap : IDynamicObject
	{
		public MSMap map;

		public MCMap(MSMap map, int tag) : base(tag)
		{
			this.map = map;
		}
		public MCMap(int tag, CodeTypes types, MCHeap heap) : base(tag)
		{
			map = new MSMap(types, heap, tag);
		}
		public override void Print(MSOutputPrint o)
		{
			o.PrintHex(tag).Print(" map");
			if(map.dict.Count <= 0) return;
			o.EndLine();
			foreach(var kv in map.dict)
			{
				o.Print("    ").Print(kv.Key).Print(": ");
				kv.Value.Print(o);
				o.EndLine();
			}
		}

		public override void Write(MSOutput output)
		{
			output.WriteOpWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_VOID, tag);
			output.WriteOp(MC.OP_BEGIN_MAP, 0, 0);

			// NOTE: no need to save child maps separately as/if tags are right
			// write key-value pairs
			foreach(var kv in map.dict)
			{
				// write key
				var key = new MSText(kv.Key);
				output.WriteOp(MC.OP_MAP_KEY, key.DataSize(), 0);
				key.Write(output);

				// write value that contains an executable operation with data
				MS.Assertion((kv.Value[0] & MC.OPERATION_MASK) == MC.OP_SET_MAP_VALUE);
				kv.Value.Write(output);
			}
			output.WriteOp(MC.OP_END_MAP, 0, 0);
		}
	}

	public class MCHeap
	{
		internal int capacity = 16;
		internal IDynamicObject [] array;
		
		//private DData Target; // { private set; private get; }

		public MCHeap ()
		{
			array = new IDynamicObject[capacity];
		}
		//public void SetTarget(int tag)
		//{
		//	MS.Assertion(tag != 0, MC.EC_CODE, "null pointer exception");
		//	int index = TagIndex(tag);
		//	Target = array[index];
		//	MS.Assertion(tag == Target.tag, MC.EC_CODE, "pointer mismatch");
		//	MS.Verbose("HEAP set target index: " + index);
		//}
		//public void ClearTarget()
		//{
		//	Target = null;
		//}
		internal MCStore AllocGlobal(int size)
		{
			MS.Assertion(array[1] == null);
			var dd = new MCStore(MCStore.Role.GLOBAL, MakeTag(MC.GLOBALS_TYPE_ID, 1, 0), new IntArray(size));
			array[1] = dd;
			return dd;
		}
		private int FindFreeSlot()
		{
			// 0 = null, 1 = global
			for (int i=2; i<capacity; i++)
				if (array[i] == null) return i;
			throw new MException(MC.EC_INTERNAL, "heap full");
		}
		public MCStore AllocContext(int size)
		{
			int index = FindFreeSlot();
			int tag = MakeTag(0, index, 0);
			var dd = new MCStore(MCStore.Role.CONTEXT, tag, new IntArray(size));
			array[index] = dd;
			return dd;
		}
		public int AllocStoreObject(int type, IntArray data)
		{
			// create a new object and return tag
			int index = FindFreeSlot();
			return SetStoreObject(index, type, data);
		}
		public MCMap AllocMap(CodeTypes types)
		{
			int index = FindFreeSlot();
			int tag = MakeTag(MC.BASIC_TYPE_MAP,index,1);
			var map = new MCMap(tag, types, this);
			array[index] = map;
			return map;
		}
		public MCMap CreateMap(int index, CodeTypes types)
		{
			MS.Assertion(array[index] == null);
			int tag = MakeTag(MC.BASIC_TYPE_MAP,index,1);
			var map = new MCMap(tag, types, this);
			array[index] = map;
			return map;
		}
		public void SetMapObject(int index, MSMap map)
		{
			MS.Assertion(array[index] == null);
			int tag = MakeTag(MC.BASIC_TYPE_MAP,index,1);
			array[index] = new MCMap(map, tag);
		}
		public int SetStoreObject(int index, int type, IntArray data)
		{
			int tag = MakeTag(type,index,1);
			array[index] = new MCStore(MCStore.Role.OBJECT, tag, data);

			if (MS._verboseOn)
			{
				MS.printOut.Print("HEAP alloc [").Print(index).Print("] tag: ").PrintHex(tag).Print(" data:");
				data.Print(MS.printOut); MS.printOut.EndLine();
			}

			return tag;
		}
		internal void WriteHeap(MSOutput output)
		{
			// write heap data
			for(int i=0; i < capacity; i++)
			{
				if (array[i] != null)
				{
					MS.Assertion(array[i].HeapID() == i);
					array[i].Write(output);
				}
			}
		}
		internal void ReadFromInput(MCStore.Role role, int heapID, int typeID, MSInput input, int dataLength)
		{	
			int tag = MakeTag(typeID,heapID,1);
			
			if (array[heapID] != null) MS.Verbose("HEAP overwrite ID " + heapID);

			array[heapID] = new MCStore(role, tag, input, dataLength);

			if (MS._verboseOn)
			{
				MS.printOut.Print("HEAP WriteObject [").Print(heapID).Print("] tag: ").PrintHex(tag).Print(" data:");
				array[heapID].Print(MS.printOut); MS.printOut.EndLine();
			}
		}

		internal void ReadFromArray(MCStore.Role role, int heapID, int typeID, int[] src, int dataLength)
		{	
			int tag = MakeTag(typeID,heapID,1);
			
			if (array[heapID] != null) MS.Verbose("HEAP overwrite ID " + heapID);

			array[heapID] = new MCStore(role, tag, src, 0, dataLength);

			if (MS._verboseOn)
			{
				MS.printOut.Print("HEAP WriteObject [").Print(heapID).Print("] tag: ").PrintHex(tag).Print(" data:");
				array[heapID].Print(MS.printOut); MS.printOut.EndLine();
			}
		}

		public void Free(int tag, int datatype)
		{
			if (tag == 0) return;

			int type = TagType(tag);
			int index = TagHeapIndex(tag);

			MS.Assertion(datatype < 0 || type == datatype);
			if (array[index] == null)
			{
				MS.Verbose("heap free: was empty");
			}
			MS.Assertion(array[index].tag == tag);
			array[index] = null;
			MS.Verbose("heap free @ " + index);
		}
		internal void FreeContext(MCStore context)
		{
			int index = context.HeapID();
			array[index] = null;
			MS.Verbose("heap free context @ " + index);
		}
		internal IntArray GetStoreData(int heapID)
		{
			return GetStoreByIndex(heapID).data;
		}
		internal int GetAt(int heapID, int offset)
		{
			var dd = GetStoreByIndex(heapID);
			MS.Assertion(dd.InRange(offset), MC.EC_CODE, "object data index out of bounds: " + offset);
			return dd.data[offset];
		}		
		internal bool HasObject(int heapID)
		{
			if (InRange(heapID)) return array[heapID] != null;
			return false;
		}
		public bool InRange(int heapIndex)
		{
			return heapIndex >= 0 && heapIndex < array.Length;
		}
		public static int TagType(int tag)
		{
			return (tag >> DDATA_TYPE_SHIFT) & 0xfff;
		}
		public static int TagHeapIndex(int tag)
		{
			return (tag >> DDATA_INDEX_SHIFT) & 0xfff;
		}
		public static int TagSignature(int tag)
		{
			return (tag >> DDATA_SIGNATURE_SHIFT) & 0xff;
		}

		const uint
			DDATA_TYPE_MASK =	0xfff00000,
			DDATA_INDEX_MASK =	0x000fff00,
			DDATA_SIGNATURE_MASK =	0x000000ff;
		const int
			DDATA_TYPE_SHIFT =	20,
			DDATA_INDEX_SHIFT =	8,
			DDATA_SIGNATURE_SHIFT =	0;

		public static int MakeTag(int type, int index, int sign)
		{
			// fits pointer information in one int

			// hex:  7   6   5   4   3   2   1  0
			//
			//      |---type---|---index---|-sign-|

			return (int)(
				((type << DDATA_TYPE_SHIFT) & DDATA_TYPE_MASK) |
				((index << DDATA_INDEX_SHIFT) & DDATA_INDEX_MASK) |
				((sign << DDATA_SIGNATURE_SHIFT) & DDATA_SIGNATURE_MASK));
		}

		public MCStore GetStoreByIndex(int heapID)
		{
			MS.Assertion(HasObject(heapID), MC.EC_CODE, "no heap object by ID: " + heapID);
			if (array[heapID] is MCStore dd)
			{
				return dd;
			}
			MS.Assertion(false, MC.EC_DATA, "no a store, ID: " + heapID);
			return null;
		}

		internal IDynamicObject GetDynamicObjectAt(int heapID)
		{
			MS.Assertion(HasObject(heapID), MC.EC_CODE, "no heap object by ID: " + heapID);
			return array[heapID];
		}

		internal MCMap GetMapAt(int heapID)
		{
			MS.Assertion(HasObject(heapID), MC.EC_CODE, "no heap object by ID: " + heapID);
			if (array[heapID] is MCMap dd)
			{
				return dd;
			}
			MS.Assertion(false, MC.EC_DATA, "no a map, ID: " + heapID);
			return null;
		}

		public void Print()
		{
			MS.printOut.Print("HEAP. capacity: ").Print(capacity).Print(", objects:").EndLine();
			for(int i=0; i<capacity; i++)
			{
				if (array[i] == null) continue;
				MS.printOut.Print("[").Print(i).Print("] ");
				array[i].Print(MS.printOut);
				MS.printOut.EndLine();
			}
		}

	}
}
