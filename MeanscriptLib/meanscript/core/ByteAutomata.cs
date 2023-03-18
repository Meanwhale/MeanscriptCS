namespace Meanscript.Core
{

	public class ByteAutomata
	{
		internal bool ok;
		internal byte[] tr;
		internal int currentInput;
		internal byte currentState;
		internal System.Collections.Generic.Dictionary<int, string> stateNames = new System.Collections.Generic.Dictionary<int, string>();
		internal MS.MAction[] actions = new MS.MAction[128];
		internal byte stateCounter;
		internal byte actionCounter; // 0 = end

		// running:
		internal int inputByte = 0;
		internal int index = 0;
		internal int lineNumber = 0;
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
			tr = new byte[MAX_STATES * 256];
			for (int i = 0; i < MAX_STATES * 256; i++) tr[i] = (byte)0xff;
			for (int i = 0; i < 128; i++) actions[i] = null;

			inputByte = -1;
			index = 0;
			lineNumber = 0;
			stayNextStep = false;
			running = false;
			buffer = new byte[BUFFER_SIZE];
			tmp = new byte[BUFFER_SIZE];
		}

		//

		public void Print()
		{
			for (int i = 0; i <= stateCounter; i++)
			{
				MS.Print("state: " + i);

				for (int n = 0; n < 256; n++)
				{
					byte foo = tr[(i * 256) + n];
					if (foo == 0xff) MS.Printn(".");
					else MS.printOut.Print(foo);
				}
				MS.Print("");
			}
		}

		public byte AddState(string stateName)
		{
			stateCounter++;
			stateNames[stateCounter] = stateName;
			return stateCounter;
		}

		public void Transition(byte state, string input, MS.MAction action)
		{
			byte actionIndex = 0;
			if (action != null)
			{
				actionIndex = AddAction(action);
			}

			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);

			int i = 0;
			while (i < input.Length)
			{
				tr[(state * 256) + bytes[i]] = actionIndex;
				i++;
			}
		}

		public void FillTransition(byte state, MS.MAction action)
		{
			byte actionIndex = 0;
			if (action != null) actionIndex = AddAction(action);

			for (int i = 0; i < 256; i++)
			{
				tr[(state * 256) + i] = actionIndex;
			}			//{if (MS.debug) {MS.verbose("New Transition filled: id " + actionIndex);}};
		}

		public byte AddAction(MS.MAction action)
		{
			actionCounter++;
			actions[actionCounter] = action;
			return actionCounter;
		}


		public void Next(byte nextState)
		{
			currentState = nextState;

			{ if (MS._debug) { MS.Verbose("next state: " + stateNames[(int)currentState]); } };
		}

		// NOTE: don't use exceptions. On error, use error print and set ok = false

		public bool Step(int input)
		{
			currentInput = input;
			int index = (currentState * 256) + input;
			byte actionIndex = tr[index];

			if (actionIndex == 0) return true; // stay on same state and do nothing else
			if (actionIndex == 0xff || actionIndex < 0)
			{
				MS.errorOut.Print("unexpected char: ").PrintCharSymbol(input).Print("").Print(" code = ").Print(input).EndLine();

				ok = false;
				return false; // end
			}

			MS.MAction act = actions[actionIndex];

			if (act == null)
			{
				MS.Assertion(false, MC.EC_INTERNAL, "invalid action index");
			}
			act();
			return true;
		}

		public int GetIndex()
		{
			return index;
		}

		public int GetInputByte()
		{
			return inputByte;
		}

		public void Stay()
		{
			// same input byte on next step
			MS.Assertion(!stayNextStep, MC.EC_INTERNAL, "'stay' is called twice");
			stayNextStep = true;
		}

		public string GetString(int start, int length)
		{
			MS.Assertion(length < MS.globalConfig.maxNameLength, null, "name is too long");

			int i = 0;
			for (; i < length; i++)
			{
				tmp[i] = buffer[start++ % BUFFER_SIZE];
			}

			return System.Text.Encoding.UTF8.GetString(tmp, 0, length);
		}

		public void Run(MSInput input)
		{
			inputByte = -1;
			index = 0;
			lineNumber = 1;
			stayNextStep = false;
			running = true;

			while ((!input.End() || stayNextStep) && running && ok)
			{
				if (!stayNextStep)
				{
					index++;
					inputByte = input.ReadByte();
					buffer[index % BUFFER_SIZE] = (byte)inputByte;
					if (inputByte == 10) lineNumber++; // line break
				}
				else
				{
					stayNextStep = false;
				}
				running = Step(inputByte);
			}

			if (!stayNextStep) index++;
		}

		public void PrintError()
		{
			MS.errorOut.Print("ERROR: parser state [" + stateNames[(int)currentState] + "]");
			MS.errorOut.Print("line " + lineNumber + ": \"");

			// print nearby code
			int start = index - 1;
			while (start > 0 && index - start < BUFFER_SIZE && (char)buffer[start % BUFFER_SIZE] != '\n')
			{
				start--;
			}
			while (++start < index)
			{
				MS.errorOut.Print((char)(buffer[start % BUFFER_SIZE]));
			}
			MS.errorOut.Print("\"");
		}

	}
}
