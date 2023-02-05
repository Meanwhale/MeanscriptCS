using System;

namespace Meanscript
{
	public class DData
	{
		public enum Role
		{
			GLOBAL,
			CONTEXT,
			OBJECT
		}

		// dynamic data
		public Role role;
		public int tag;
		public IntArray data;

		public DData(Role role, int tag, IntArray data)
		{
			this.role = role;
			this.tag = tag;
			this.data = data;
		}

		public DData(Role role, int tag, IntArray src, int startIndex, int dataLength) :
			this(role, tag, new IntArray(dataLength))
		{
			IntArray.Copy(src, startIndex, data, 0, dataLength);
		}

		public int HeapID()
		{
			return MHeap.TagIndex(tag);
		}
		public int DataTypeID()
		{
			return MHeap.TagType(tag);
		}
		internal void Print(MSOutputPrint o)
		{
			o.PrintHex(tag).Print(" ");
			data.Print(o);
		}

		internal bool InRange(int offset)
		{
			return offset >= 0 && offset < data.Length;
		}
	}
	public class MHeap
	{
		internal int capacity = 16;
		internal DData [] array;
		
		//private DData Target; // { private set; private get; }

		public MHeap ()
		{
			array = new DData[capacity];
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
		internal DData AllocGlobal(int size)
		{
			MS.Assertion(array[1] == null);
			array[1] = new DData(DData.Role.GLOBAL, MakeTag(0, 1, 0), new IntArray(size));
			return array[1];
		}
		private int FindFreeSlot()
		{
			// 0 = null, 1 = global
			for (int i=2; i<capacity; i++)
				if (array[i] == null) return i;
			throw new MException(MC.EC_INTERNAL, "heap full");
		}
		public DData AllocContext(int size)
		{
			int index = FindFreeSlot();
			int tag = MakeTag(0, index, 0);
			array[index] = new DData(DData.Role.CONTEXT, tag, new IntArray(size));
			return array[index];
		}
		public int AllocObject(int type, IntArray data)
		{
			// create a new object and return tag
			int index = FindFreeSlot();
			int tag = MakeTag(type,index,1);
			array[index] = new DData(DData.Role.OBJECT, tag, data);

			if (MS._verboseOn)
			{
				MS.printOut.Print("HEAP alloc [").Print(index).Print("] tag: ").PrintHex(tag).Print(" data:");
				data.Print(MS.printOut); MS.printOut.EndLine();
			}

			return tag;
		}
		public int Write(DData.Role role, int heapID, int type, IntArray data, int startIndex, int dataLength)
		{
			// create a new object and return tag
			
			int tag = MakeTag(type,heapID,1);
			
			if (array[heapID] == null)
			{
				array[heapID] = new DData(role, tag, data, startIndex, dataLength);
			}
			else
			{
				// when serializing, global data could have been created already. check it's size matches.
				MS.Assertion(heapID == 1 && array[1].data.Length == dataLength);
				IntArray.Copy(data, startIndex, array[1].data, 0, dataLength);
			}

			if (MS._verboseOn)
			{
				MS.printOut.Print("HEAP WriteObject [").Print(heapID).Print("] tag: ").PrintHex(tag).Print(" data:");
				array[heapID].Print(MS.printOut); MS.printOut.EndLine();
			}

			return tag;
		}
		public void Free(int tag, int datatype)
		{
			if (tag == 0) return;

			int type = TagType(tag);
			int index = TagIndex(tag);

			MS.Assertion(datatype < 0 || type == datatype);
			if (array[index] == null)
			{
				MS.Verbose("Heap free: was empty");
			}
			MS.Assertion(array[index].tag == tag);
			array[index] = null;
			MS.Verbose("Heap free @ " + index);
		}
		internal void FreeContext(DData context)
		{
			int index = context.HeapID();
			array[index] = null;
			MS.Verbose("Heap free context @ " + index);
		}
		internal IntArray GetDataArray(int heapID)
		{
			return array[heapID].data;
		}
		internal int GetAt(int heapID, int offset)
		{
			MS.Assertion(HasObject(heapID), MC.EC_CODE, "wrong heap ID: " + heapID);
			MS.Assertion(array[heapID].InRange(offset), MC.EC_CODE, "object data index out of bounds: " + offset);
			return array[heapID].data[offset];
		}		
		internal bool HasObject(int heapID)
		{
			return array[heapID] != null;
		}
		public static int TagType(int tag)
		{
			return (tag >> DDATA_TYPE_SHIFT) & 0xfff;
		}
		public static int TagIndex(int tag)
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

		public DData Get(int tag)
		{
			return null; // TODO
		}

		internal void Print()
		{
			MS.printOut.Print("HEAP!!! capacity: ").Print(capacity).EndLine();
			for(int i=0; i<capacity; i++)
			{
				if (array[i] == null) continue;
				MS.printOut.Print(i).Print(": ");
				array[i].Print(MS.printOut);
				MS.printOut.EndLine();
			}
		}
	}
}
