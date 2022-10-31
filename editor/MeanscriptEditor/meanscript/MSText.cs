namespace Meanscript {

public class MSText : MC {
// Text bytes in an integer array. first int is the number of bytes. Bytes go from right to left (to be convinient on C++).
// Specification:

//		{0x00000000}{0x00000000}				= empty text, ie. just the terminating '\0' character
//		{0x00000002}{0x00006261}				= 2 chars ("ab") and '\0', from right to left
//		{0x00000005}{0x64636261}{0x00000065}	= 5 chars ("abcde") and '\0'

// Number of ints after the first int 'i' is '(int)i / 4 + 1' if 'i > 0', and 0 otherwise.
// Can't be modified. TODO: C++ reference counter for smart memory handling.


IntArray data;

public MSText (string src) 
{
	byte[] bytes = System.Text.Encoding.UTF8.GetBytes(src);
	_init(bytes, 0, bytes.Length);
}

public MSText (byte[]src, int start, int length) 
{
	_init(src, start, length);
}

public void _init(byte[]src, int start, int length)
{
	data = new IntArray((length / 4) + 2);
	data[0] = length;
	bytesToInts(src,start,data,1,length);
}

public MSText (MSText src) 
{
	makeCopy(src.data,0);
}

public MSText (IntArray src) 
{
	makeCopy(src,0);
}

public MSText (IntArray src, int start) 
{
	makeCopy(src,start);
}


public bool match(MSText t)
{
	return compare(t) == 0;
}

public bool match(string s) 
{
	return (s.Equals(getString()));
}

public IntArray getData()
{
	return data;
}

public int numBytes() 
{
	// count is without the ending character
	return data[0];
}
public int dataSize() 
{
	return data.Length;
}
public int byteAt(int index)  
{
	MS.assertion(index >= 0 && index <= data[0],MC.EC_INTERNAL,"index overflow");
	return ((data[(index / 4) + 1]) >> ((index % 4) * 8) & 0x000000ff);
}
public int hashCode() 
{
	int hash = 0;
	for (int i=0; i<data.Length; i++) hash += data[i];
	return hash;
}
public int write(IntArray trg, int start)  
{
	for (int i=0; i<data.Length; i++)
	{
		trg[start + i] = data[i];
	}
	return 	start + data.Length;
}
public void makeCopy(  IntArray src, int start)
{
	int numChars = src[start];
	int size32 = (numChars/4) + 2;
	data = new IntArray(size32);
	for (int i=0; i<size32; i++)
	{
		data[i] = src[i+start];
	}
}
public int compare (MSText text) 
{
	// returns -1 (less), 1 (greater), or 0 (equal)
	
	if (data.Length != text.data.Length)
	{
		return data.Length > text.data.Length ? 1 : -1;
	}
	
	for (int i=0; i<data.Length; i++)
	{
		if (data[i] != text.data[i])
		{
			return data[i] > text.data[i] ? 1 : -1;
		}
	}
	return 0; // equals
}
public void check() 
{
	int size32 = (data[0]/4) + 2;
	MS.assertion(data.Length == size32,MC.EC_INTERNAL,"corrupted MSText object (size don't match)");
	MS.assertion(byteAt(data[0]) == 0,MC.EC_INTERNAL, "corrupted MSText object (no zero byte at end)");
}
public string getString() 
{
	check();
	return System.Text.Encoding.UTF8.GetString(MS.intsToBytes(data,1,data[0]));
}
public override string ToString()
{
	try {
		check();
		return System.Text.Encoding.UTF8.GetString(MS.intsToBytes(data,1,data[0]));
	} catch (System.Exception) {
		return "?";
	}
}

}
}
