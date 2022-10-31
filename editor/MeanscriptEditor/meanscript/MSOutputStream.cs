namespace Meanscript {

public abstract class MSOutputStream : MC {

public MSOutputStream ()
{
}
public abstract void  writeByte (byte b) ;
public abstract void  close () ;

public void  writeInt (int i) 
{
	writeByte((byte)((i>>24) & 0xff));
	writeByte((byte)((i>>16) & 0xff));
	writeByte((byte)((i>>8) & 0xff));
	writeByte((byte)(i & 0xff));
}


}
}
