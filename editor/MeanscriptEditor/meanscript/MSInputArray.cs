
namespace Meanscript
{
	using Core;
	using System;

	// input stream that is an int array

	public class MSInputArray : MSInput
	{
		int[] buffer;
		int size;
		int index;

		public MSInputArray(MSOutputArray output)
		{
			// copy output's expanding array

			buffer = output.GetCopyAndClear();
			size = buffer.Length * 4;
			index = 0;
		}
		//public MSInputArray(IntArray src, int numInts)
		//{
		//	// make the scr array to input stream to read
		//	buffer = MS.GetIntsToBytesBE(src, 0, numInts * 4);
		//	size = buffer.Length;
		//	index = 0;
		//}
		public MSInputArray(string s)
		{
			// get string bytes and change it to an int array

			var bytes = System.Text.Encoding.UTF8.GetBytes(s);
			size = bytes.Length;
			if (size == 0) return;
			buffer = new int[MC.ByteArraySizeToIntArraySize(size)];
			MS.BytesToInts(bytes, 0, buffer, 0, bytes.Length);
			index = 0;
		}

		override public int GetByteCount()
		{
			return size;
		}

		public int IntIndex()
		{
			return index / 4;
		}

		public override int ReadInt()
		{
			// read at byte index / 4

			MS.Assertion(index % 4 == 0);
			int i = buffer[index/4];
			index += 4;
			return i;
		}

		override public byte ReadByte()
		{
			// read at index / 4 ja bit shift
			// for example:
			
			// output.WriteInt(0x04030201);
			// -->
			// input.ReadByte() == 0x01);
			// input.ReadByte() == 0x02);
			// input.ReadByte() == 0x03);
			// input.ReadByte() == 0x04);

			int i = buffer[index/4];
			int shift = index % 4;
			byte b = (byte)((i >> (shift * 8)) & 0xff);
			index ++;
			return b;
		}

		override public bool End()
		{
			return index >= size;
		}

		override public void Close()
		{
		}

		internal int[] Data()
		{
			return buffer;
		}
	}
}
