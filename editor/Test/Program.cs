
// TIIVISTELMÄ: kaikki näyttää olevan yhtä nopeita.
// erona periaatteessa boundary check mutta siitä ei tule ajankulua.
// indexin "huono ennustettavuus" hidastaa: yksinkertainen vs. ennustamaton ~10% suoraviivaisessa luupissa.
// TODO: vertaa samaan, alustariippumattomaan koodiin C++:lla

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe class IntArray
{
	public unsafe int * data;
	public readonly int Size;
	public IntArray(int size)
	{
		Size = size;
		data = (int*)Marshal.AllocHGlobal(size * sizeof(int));
	}
	~IntArray()
	{
		Marshal.FreeHGlobal((IntPtr)data);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe int Get(int key)
	{
		return *(data+key);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe int Set(int key, int value)
	{
		return data[key] = value;
	}
};
class IntArrayTest
{
    static unsafe void Main(string[] args)
    {
		const int magnitude = 5000, repeatTimes = 10;
		var safeArray = new int[magnitude];
		IntArray unsafeArray = new IntArray(magnitude);
		var spanArray = new Span<int>(new int[magnitude]);

		long safeTotal = 0, unsafeTotal = 0, spanTotal = 0, ptrTotal = 0, fixedTotal = 0;
		var watch = new System.Diagnostics.Stopwatch();
		watch.Start();
		for (int repeat = 0; repeat <= repeatTimes; repeat++)
		{
			watch.Restart();
			for (int i=0; i<magnitude; i++)
			{
				safeArray[0] = 1;
				safeArray[1] = 1;
				for (int j=2; j<magnitude; j++)
				{
					safeArray[j] = safeArray[safeArray[j - 1] % j] + (int)watch.ElapsedMilliseconds;
				}
			}
			if (repeat != 0) safeTotal += watch.ElapsedMilliseconds;
			/*
			watch.Restart();
			for (int i=0; i<magnitude; i++)
			{
				unsafeArray.Set(0, 1);
				unsafeArray.Set(1, 1);
				for (int j=2; j<magnitude; j++)
				{
					unsafeArray.Set(j, unsafeArray.Get(j-1) + unsafeArray.Get(j-2) + (int)watch.ElapsedMilliseconds);
				}
			}
			if (repeat != 0) unsafeTotal += watch.ElapsedMilliseconds;

			watch.Restart();
			for (int i=0; i<magnitude; i++)
			{
				spanArray[0] = 1;
				spanArray[1] = 1;
				for (int j=2; j<magnitude; j++)
				{
					spanArray[j] = spanArray[j-1] + spanArray[j-2] + (int)watch.ElapsedMilliseconds;
				}
			}
			if (repeat != 0) spanTotal += watch.ElapsedMilliseconds;
			
			
			watch.Restart();
			fixed (int* fixedPtr = &MemoryMarshal.GetReference(spanArray))
			{
				for (int i=0; i<magnitude; i++)
				{
					fixedPtr[0] = 1;
					fixedPtr[1] = 1;
					for (int j = 2; j < magnitude; j++)
					{
						fixedPtr[j] = fixedPtr[j - 1] + fixedPtr[j - 2] + (int)watch.ElapsedMilliseconds;
					}
				}
			}
			if (repeat != 0) fixedTotal += watch.ElapsedMilliseconds;
			*/
			watch.Restart();
			var unsafeData = unsafeArray.data;
			for (int i = 0; i < magnitude; i++)
			{
				unsafeData[0] = 1;
				unsafeData[1] = 1;
				for (int j = 2; j < magnitude; j++)
				{
					// 683..687
					//unsafeData[j] = unsafeData[unsafeData[j - 1] % j] + (int)watch.ElapsedMilliseconds;
					
					// 607..610
					unsafeData[j] = unsafeData[j - 1] + unsafeData[j - 2] % j + (int)watch.ElapsedMilliseconds;
				}
			}
			if (repeat != 0) ptrTotal += watch.ElapsedMilliseconds;
		}
		Console.WriteLine("safe:   " + (safeTotal / repeatTimes));
		Console.WriteLine("unsafe: " + (unsafeTotal / repeatTimes));
		Console.WriteLine("span:   " + (spanTotal / repeatTimes));
		Console.WriteLine("ptr:    " + (ptrTotal / repeatTimes));
		Console.WriteLine("fixed:  " + (fixedTotal / repeatTimes));
		Console.ReadLine();
	}
}