namespace Meanscript {

public class MSOutputArray : MSOutputStream {
internal byte []  buffer;
internal int maxSize;
internal int index;

public MSOutputArray ()
{
	maxSize = MS.globalConfig.outputArraySize;
	{ buffer = new byte[maxSize];  };
	index = 0;
}

override
public void close () 
{
	index = -1;
}

override
public void  writeByte (byte b) 
{
	MS.assertion(index != -1, EC_DATA, "output closed");
	MS.assertion(index < maxSize, EC_DATA, "output: buffer overflow");
	buffer[index++] = b;
}

public void  print () 
{
	MS.print("TODO");
}

}
}
