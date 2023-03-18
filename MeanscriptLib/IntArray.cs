using System;

namespace Meanscript
{
	public class IntArray
	{
		private readonly int[] data;
		public IntArray()
		{
		}
		public IntArray(int [] _data)
		{
			data = _data;
		}
		public IntArray(int size)
		{
			data = new int[size];
		}
		public int this[int key]
		{
			get => data[key];
			set => data[key] = value;
		}
		public int this[uint key]
		{
			get => data[key];
			set => data[key] = value;
		}
		public int Length { get { return data.Length; } }
		public int[] Data() { return data; }
		public static bool Match(IntArray a, int aIndex, IntArray b, int bIndex, int length)
		{
			// check bounds
			if (length <= 0 || a == null || b == null) return false;
			if (aIndex + length > a.Length) return false;
			if (bIndex + length > b.Length) return false;
			for (int i = 0; i < length; i++)
			{
				if (a[aIndex + i] != b[bIndex + i]) return false;
			}
			return true;
		}

		public static void Read(MSInput input, int[] trg, int dataLength)
		{
			MS.Assertion(dataLength <= trg.Length);
			for(int i = 0; i < dataLength; i++) trg[i] = input.ReadInt();
		}

		public static void Copy(IntArray src, int srcIndex, IntArray trg, int trgIndex, int length)
		{
			System.Array.Copy(src.Data(), srcIndex, trg.Data(), trgIndex, length);
		}
		public static void Copy(int [] src, int srcIndex, int [] trg, int trgIndex, int length)
		{
			System.Array.Copy(src, srcIndex, trg, trgIndex, length);
		}

		internal void Write(MSOutput output)
		{
			Write(output, data);
		}
		public static void Write(MSOutput output, int [] data)
		{
			for (int i = 0; i < data.Length; i++)
			{
				output.WriteInt(data[i]);
			}
		}
		public static void Copy(IntArray src, IntArray trg, int length)
		{
			System.Array.Copy(src.Data(), trg.Data(), length);
		}
		public static void Copy(int [] src, int [] trg, int length)
		{
			System.Array.Copy(src, trg, length);
		}

		internal void Print(MSOutputPrint o)
		{
			foreach (int i in data) o.Print("/").PrintHex(i);
		}
	}
	public class DynamicArray
	{
		private int[] data;
		private int capacity;
		private int count;

		public DynamicArray(int _capacity = 256)
		{
			capacity = _capacity;
			data = new int[capacity];
			count = 0;
		}
		
		public void Expand(int size)
		{
			// TODO optimize
			MS.Assertion(size > 0);
			while (size-- > 0) Add(0);
		}

		public void Add(int item)
		{
			if (count == capacity)
			{
				// expand the array by doubling its capacity
				capacity *= 2;
				int[] newData = new int[capacity];
				System.Array.Copy(data, newData, count);
				data = newData;
			}

			data[count] = item;
			count++;
		}

		public int this[int index]
		{
			get
			{
				if (index < 0 || index >= count)
				{
					throw new System.IndexOutOfRangeException();
				}

				return data[index];
			}
			set
			{
				if (index < 0 || index >= count)
				{
					throw new System.IndexOutOfRangeException();
				}

				data[index] = value;
			}
		}

		public int Count
		{
			get { return count; }
		}

		internal int[] Data()
		{
			return data;
		}
	}
}
