using System;

namespace Meanscript
{
	public class IntArray
	{
		// TODO: testaa olisiko tämä optimoituna nopeampi:
		// public int Get(int key)
		// {
		// 	unsafe {
		// 	fixed (int* p = &data[0]){
		// 		return *(p+key);
		// 	}}
		// }
		// HUOM! int* p ei voi olla member koska pitää olla fixedin sisällä.

		private readonly int [] data;
		public IntArray()
		{
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
		public static void Copy(IntArray src, int srcIndex, IntArray trg, int trgIndex, int length)
		{
			System.Array.Copy(src.data, srcIndex, trg.data, trgIndex, length);
		}
		public static void Copy(IntArray src, IntArray trg, int length)
		{
			System.Array.Copy(src.data, trg.data, length);
		}

		internal void Print(MSOutputPrint o)
		{
			foreach(int i in data) o.Print("/").PrintHex(i);
		}
	}
}
