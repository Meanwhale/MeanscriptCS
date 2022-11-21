namespace Meanscript
{
	public class IntArray
	{
		private int [] data;
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
		public int Get(int key)
		{
			unsafe {
			fixed (int* p = &data[0]){
				return *(p+key);
			}}
		}
		public int this[uint key]
		{
			get => data[key];
			set => data[key] = value;
		}
		public int Length { get { return data.Length; } }
		public void Copy(IntArray a, int ai, IntArray b, int bi, int length)
		{
			Array.Copy(a.data, ai, b.data, bi, length);
		}
		public void Copy(IntArray a, IntArray b, int length)
		{
			Array.Copy(a.data, b.data, length);
		}
	}
}
