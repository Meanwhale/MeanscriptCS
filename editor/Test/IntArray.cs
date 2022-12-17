using System.Runtime.InteropServices;
using System.Runtime.CompilerServices; 

namespace Meanscript
{
//	public unsafe class IntArray
//	{
//		private unsafe int * data;
//		public readonly int Size;
//		//public IntArray()
//		//{
//		//	data = null;
//		//}
//		public IntArray(int size)
//		{
//			//data = new int[size];
//			Size = size;
//			data = (int*)Marshal.AllocHGlobal(size * sizeof(int));
//		}
//		~IntArray()
//		{
//			Marshal.FreeHGlobal((IntPtr)data);
//		}

//		//public int this[int key]
//		//{
//		//	get => data[key];
//		//	set => data[key] = value;
//		//}
//		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
//		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
//		public unsafe int Get(int key)
//		{
//			return *(data+key);
//		}
//		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
//		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
//		public unsafe int Set(int key, int value)
//		{
//			return data[key] = value;
//		}
//		//public int this[uint key]
//		//{
//		//	get => data[key];
//		//	set => data[key] = value;
//		//}
//		public int Length { get { return Size; } }
//		//public void Copy(IntArray a, int ai, IntArray b, int bi, int length)
//		//{
//		//	Array.Copy(a.data, ai, b.data, bi, length);
//		//}
//		//public void Copy(IntArray a, IntArray b, int length)
//		//{
//		//	Array.Copy(a.data, b.data, length);
//		//}
//	}
}
