namespace Meanscript {

public class MSOutputPrintArray : MSOutputPrint {
internal byte []  buffer;
internal int maxSize;
internal int index;


public MSOutputPrintArray ()
{
	maxSize = MS.globalConfig.outputArraySize;
	{ buffer = new byte[maxSize];  };
	index = 0;
}

override
public void close () 
{
	writeByte((byte)0);
}

override
public void  writeByte (byte b) 
{
	MS.assertion(index != -1, EC_DATA, "output closed");
	MS.assertion(index < maxSize, EC_DATA, "output: buffer overflow");
	buffer[index++] = b;
}

override
public MSOutputPrint  print (char x) 
{
	writeByte((byte)x);
	return this;
}

override
public MSOutputPrint  print (string x) 
{
	byte [] buffer;
	buffer = System.Text.Encoding.UTF8.GetBytes(x);
	for (int i = 0; i < buffer.Length; i++)
	{
		writeByte(buffer[i]);
	}
	return this;
}

public string getString()
{
	return System.Text.Encoding.UTF8.GetString(buffer,0,index-1);
}

public void  print () 
{
	MS.print("[[[MSOutputPrint.print: TODO]]]");
}

}
}
