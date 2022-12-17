using System;

namespace Meanscript
{
	public class DData
	{
		// dynamic data
		public int tag;
		public IntArray data;

		public DData(int tag, IntArray data)
		{
			this.tag = tag;
			this.data = data;
		}

		internal void Print(MSOutputPrint o)
		{
			o.PrintHex(tag).Print(" ");
			data.Print(o);
			o.Print("    ").Print(tag);
		}
	}
	public class MHeap
	{
		private int capacity = 16;
		private DData [] array;
		
		public DData Target { private set; get; }

		public MHeap ()
		{
			array = new DData[capacity];
		}
		public void SetTarget(int tag)
		{
			MS.Assertion(tag != 0, MC.EC_CODE, "null pointer exception");
			int index = TagIndex(tag);
			Target = array[index];
			MS.Assertion(tag == Target.tag, MC.EC_CODE, "pointer mismatch");
			MS.Verbose("HEAP set target index: " + index);
		}
		public void ClearTarget()
		{
			Target = null;
		}
		private int FindFreeSlot()
		{
			for (int i=0; i<capacity; i++)
				if (array[i] == null) return i;
			throw new MException(MC.EC_INTERNAL, "heap full");
		}
		public int Alloc(int type, IntArray data)
		{
			// create a new object and return tag
			int index = FindFreeSlot();
			int tag = MakeTag(type,index,1);
			array[index] = new DData(tag, data);

			if (MS._verboseOn) MS.printOut.Print("HEAP alloc [").Print(index).Print("] tag: ").PrintHex(tag).EndLine();

			return tag;
		}
		public void Free(int tag, int datatype)
		{
			if (tag == 0) return;

			int type = TagType(tag);
			int index = TagIndex(tag);

			MS.Assertion(type == datatype);
			if (array[index] == null)
			{
				MS.Verbose("Heap free: was empty");
			}
			MS.Assertion(array[index].tag == tag);
			array[index] = null;
			MS.Verbose("Heap free @ " + index);
		}
		
		private int TagType(int tag)
		{
			return (tag >> DDATA_TYPE_SHIFT) & 0xfff;
		}
		private int TagIndex(int tag)
		{
			return (tag >> DDATA_INDEX_SHIFT) & 0xfff;
		}
		private int TagSign(int tag)
		{
			return (tag >> DDATA_SIGN_SHIFT) & 0xff;
		}

		const uint
			DDATA_TYPE_MASK =	0xfff00000,
			DDATA_INDEX_MASK =	0x000fff00,
			DDATA_SIGN_MASK =	0x000000ff;
		const int
			DDATA_TYPE_SHIFT =	20,
			DDATA_INDEX_SHIFT =	8,
			DDATA_SIGN_SHIFT =	0;

		private int MakeTag(int type, int index, int sign)
		{
			// fits pointer information in one int

			// hex:  7   6   5   4   3   2   1  0
			//
			//      |---type---|---index---|-sign-|

			return (int)(
				((type << DDATA_TYPE_SHIFT) & DDATA_TYPE_MASK) |
				((index << DDATA_INDEX_SHIFT) & DDATA_INDEX_MASK) |
				((sign << DDATA_SIGN_SHIFT) & DDATA_SIGN_MASK));
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
