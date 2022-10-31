namespace Meanscript {

public abstract class MSInputStream : MC {


public MSInputStream ()
{
}

public abstract int  getByteCount ();
public abstract int  readByte () ;
public abstract bool  end ();
public abstract void  close ()  ;

public int readByteWithCheck () 
{
	int i = readByte();
	MS.assertion(i != -1, EC_DATA, "input error (readByteWithCheck)");
	return i;
}

public int  readInt () 
{
	// bytes:	b[0] b[1] b[2] b[3] b[4] b[5] b[6] b[7]   ...
	// ints:	_________i[0]______|_________i[1]______|_ ...
	
	int i = 0;
	i |= (int)((readByte() << 24)		& 0xff000000);
	i |= (int)((readByte() << 16)		& 0x00ff0000);
	i |= (int)((readByte() << 8)		& 0x0000ff00);
	i |= (int)((readByte())				& 0x000000ff);
	return i;
}

public void  readArray (IntArray trg, int numInts) 
{
	MS.assertion(numInts <=  (getByteCount() * 4) + 1, EC_DATA, "readArray: buffer overflow");
	for (int i=0; i < numInts; i++)
	{
		trg[i] = readInt();
	}
	MS.assertion(end(),MC.EC_INTERNAL, "all bytes not read");
}
}
}
