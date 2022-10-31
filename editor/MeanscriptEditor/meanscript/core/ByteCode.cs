namespace Meanscript {

public class ByteCode : MC {
public int codeTop;
public IntArray code;
internal Common common;



public ByteCode (Common _common)
{
	common = _common;
	code = new IntArray(MS.globalConfig.codeSize);
	codeTop = 0;
}

public ByteCode (Common _common, MSInputStream input) 
{
	common = _common;
	int byteCount = input.getByteCount();
	MS.assertion(byteCount % 4 == 0,MC.EC_INTERNAL, "bytecode file size not divisible by 4");
	int size = byteCount / 4;
	code = new IntArray(size);
	input.readArray(code, size);
	codeTop = size;
}

public ByteCode (ByteCode bc) 
{
	codeTop = bc.codeTop;
	common = bc.common;
	
	code = new IntArray(codeTop);
	
	// copy array
	
	for (int i=0; i<codeTop; i++)
	{
		code[i] = bc.code[i];
	}
}

//;


public void addInstructionWithData (int operation, int size, int valueType, int data) 
{
	addInstruction(operation,size,valueType);
	addWord(data);
}

public void addInstruction (int operation, int size, int valueType) 
{
	int instruction = makeInstruction(operation, size, valueType);
	MS.verbose("Add instruction: [" + getOpName(instruction) + "]");
	code[codeTop++] = instruction;
}

public void addWord(int data)
{
	//{if (MS.debug) {MS.print("data: " + data);}};
	code[codeTop++] = data;
}

public void  writeCode (MSOutputStream output) 
{
	for (int i=0; i < codeTop; i++)
	{
		output.writeInt(code[i]);
	}
}

public void  writeStructInit (MSOutputStream output) 
{
	int i=0;

	// change init tag
	
	int op = (int)((code[i]) & OPERATION_MASK);
	MS.assertion(op == OP_START_INIT,MC.EC_INTERNAL, "writeStructInit: bytecode error");
	output.writeInt(makeInstruction(OP_START_INIT, 1, BYTECODE_READ_ONLY));
	i++;
	output.writeInt(code[i]);
	i++;
	
	int tagIndex = i;
	
	// copy necessary tags
	
	for (; i < codeTop; i++)
	{
		if (i == tagIndex)
		{
			tagIndex += instrSize(code[i]) + 1;
			op = (int)((code[i]) & OPERATION_MASK);
			if (op == OP_FUNCTION)
			{
				// skip function inits
				i += instrSize(code[i]);
				continue;
			}
			else if (op == OP_END_INIT)
			{
				return;
			}
		}
		output.writeInt(code[i]);
	}
	MS.assertion(false,MC.EC_INTERNAL,"bytecode init end tag not found");
}

}
}
