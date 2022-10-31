namespace Meanscript {

public class MArgs : MC {
internal ByteCode byteCode;
internal StructDef structDef;
internal int baseIndex; // stack base where struct data start from
internal bool valid; // become invalid when stack changes

public MArgs (ByteCode _byteCode, StructDef _structDef, int _base)
{
	byteCode = _byteCode;
	structDef = _structDef;
	baseIndex = _base;
	valid = true;
}

}
}
