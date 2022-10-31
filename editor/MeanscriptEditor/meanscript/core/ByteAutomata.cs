namespace Meanscript {

public class ByteAutomata : MC {
internal bool ok;
internal byte[] tr;
internal int currentInput;
internal byte currentState;
internal System.Collections.Generic.Dictionary<int, string> stateNames = new System.Collections.Generic.Dictionary<int, string>();
internal MS.MAction [] actions=new MS.MAction [128];
internal byte stateCounter;
internal byte actionCounter; // 0 = end

// running:
internal int inputByte = 0;
internal int  index = 0;
internal int  lineNumber = 0;
internal bool stayNextStep = false;
internal bool running = false;

internal byte[] buffer;
internal byte[] tmp;


public const int MAX_STATES = 32;
public const int BUFFER_SIZE = 512;

public ByteAutomata()
{
	ok = true;
	currentInput = -1;
	currentState = 0;
	stateCounter = 0;
	actionCounter = 0;
	tr = new  byte[ MAX_STATES * 256];
	for (int i=0; i<MAX_STATES * 256; i++) tr[i] = (byte)0xff;
	for (int i=0; i<128; i++) actions[i] = null;

	inputByte = -1;
	index = 0;
	lineNumber = 0;	
	stayNextStep = false;
	running = false;	
	buffer = new  byte[ BUFFER_SIZE];
	tmp = new  byte[ BUFFER_SIZE];
}

//

public void print () 
{
	for (int i = 0; i <= stateCounter; i++)
	{
		MS.print("state: " + i);

		for (int n = 0; n < 256; n++)
		{
			byte foo = tr[(i * 256) + n];
			if (foo == 0xff) MS.printn(".");
			else MS.printOut.print(foo);
		}
		MS.print("");
	}
}

public byte  addState (string stateName)
{
	stateCounter++;
	stateNames[ (int)stateCounter] =  stateName;
	return stateCounter;
}

public void transition (byte state, string input, MS.MAction action)
{
	byte actionIndex = 0;
	if (action != null)
	{
		actionIndex = addAction(action);
	}

	byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);

	int i = 0;
	while (i<input.Length)
	{
		tr[(state * 256) + bytes[i]] = actionIndex;
		i++;
	}
	//{if (MS.debug) {MS.verbose("New Transition added: id " + actionIndex);}};
}

public void fillTransition (byte state, MS.MAction action)
{
	byte actionIndex = 0;
	if (action != null) actionIndex = addAction(action);

	for (int i=0; i<256; i++)
	{
		tr[(state * 256) + i] = actionIndex;
	}
	//{if (MS.debug) {MS.verbose("New Transition filled: id " + actionIndex);}};
}

public byte  addAction (MS.MAction action)
{
	actionCounter++;
	actions[actionCounter] = action;
	return actionCounter;
}


public void next (byte nextState) 
{
	currentState = nextState;

	{if (MS._debug) {MS.verbose("Next state: " + stateNames[ (int)currentState]);}};
}

// NOTE: don't use exceptions. On error, use error print and set ok = false

public bool step (int input) 
{
	currentInput = input;
	int index = (currentState * 256) + input;
	byte actionIndex = tr[index];

	if (actionIndex == 0) return true; // stay on same state and do nothing else
	if (actionIndex == 0xff||actionIndex < 0)
	{
		MS.errorOut.print("unexpected char: ").printCharSymbol(input).print("").print(" code = ").print(input).endLine();

		ok = false;
		return false; // end
	}

	MS.MAction act = actions[actionIndex];

	if (act == null)
	{
		MS.assertion(false,MC.EC_INTERNAL, "invalid action index");
	}
	act();
	return true;
}

public int getIndex ()
{
	return index;
}

public int getInputByte ()
{
	return inputByte;
}

public void stay () 
{
	// same input byte on next step
	MS.assertion(!stayNextStep,MC.EC_INTERNAL, "'stay' is called twice");
	stayNextStep = true;
}

public string getString (int start, int length) 
{
	MS.assertion(length < MS.globalConfig.maxNameLength, null, "name is too long");
	
	int i = 0;
	for (; i < length; i++)
	{
		tmp[i] = buffer[start++ % BUFFER_SIZE];
	}

	return System.Text.Encoding.UTF8.GetString(tmp,0,length);
}

public void run (MSInputStream input) 
{
	inputByte = -1;
	index = 0;
	lineNumber = 1;
	stayNextStep = false;
	running = true;

	while ((!input.end() || stayNextStep) && running && ok)
	{
		if (!stayNextStep)
		{
			index ++;
			inputByte = input.readByte();
			buffer[index % BUFFER_SIZE] = (byte)inputByte;
			if (inputByte == 10) lineNumber++; // line break
		}
		else
		{
			stayNextStep = false;
		}
		running = step(inputByte);
	}
	
	if (!stayNextStep) index++;
}

public void printError () 
{
	MS.errorOut.print("ERROR: parser state [" + stateNames[ (int)currentState] + "]");
	MS.errorOut.print("Line " + lineNumber + ": \"");
	
	// print nearby code
	int start = index-1;
	while (start > 0 && index - start < BUFFER_SIZE && (char)buffer[start % BUFFER_SIZE] != '\n')
	{
		start --;
	}
	while (++start < index)
	{
		MS.errorOut.print((char)(buffer[start % BUFFER_SIZE]));
	}
	MS.errorOut.print("\"");
}

}
}
