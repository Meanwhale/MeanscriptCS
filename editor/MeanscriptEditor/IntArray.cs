namespace Meanscript
{
	public class IntArray
	{
		private int [] data;
		public IntArray()
		{
			data = null;
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
	}
}
