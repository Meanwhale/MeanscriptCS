using Meanscript;
using Meanscript.Core;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MeanscriptEditor
{

	public partial class MainWindow : Window
	{
		private WinOutputPrinter winOutput;
		private bool textModified = false;
		
		private MSCode? code; // currently loaded code

		public MainWindow()
		{
			InitializeComponent();
			winOutput = new WinOutputPrinter(TextBoxOutput, TextBoxOutputScrollViewer);
			Meanscript.MS.printOut = winOutput;
			Meanscript.MS.errorOut = winOutput;
			Meanscript.MS.userOut = winOutput;
		
			KeyDown += new KeyEventHandler(MainWindow_KeyDown);

			//TextBoxCode.Text = "bool a: true";
			//TextBoxCode.Text = "int a: 3";
			//TextBoxCode.Text = "array [int,5] a\nint b : 5\na[3]: 456\nprint a[3]";
			//TextBoxCode.Text = "text t: \"A쎄\"";
			TextBoxCode.Text = MCUnitTest.mapTestScript;
			
			//TextBoxCode.Text = "struct vec [int x, int y]\nvec v: 678 876\nint a: 11\nsum a v.x\nsum 7 8 9";
			//TextBoxCode.Text = "int a: 3\nint b : a\nobj[int] p\np: 5";

			//TextBoxCode.Text = "struct vec2 [int x, int y]\nstruct person [text name, obj [vec2] point]\n"
			//				  + "array [int, 9] arr\nint a: 5\nperson p: \"JANE\", (56,78)\np.name: \"JONE\"\np.point: (12,34)\nint b: 6\nprint a\nprint b";

			//TextBoxCode.Text = "struct vec2 [int x, int y]\nstruct person [int name, obj [vec2] point, obj[person] boss]\n" +
			//					"person p: (10, (2,3), null)\n" +
			//					"p.boss: (10, (4,5), null)\np.point: (6,7)";

			//TextBoxCode.Text = "struct vec2 [int x, int y]\nstruct person [text name, obj [vec2] point, obj[person] boss]\n"
			//				  + "int a: 5\narray [person, 3] arr: [(\"J\", (1,2), null),(\"J\", (1,2), null),(\"J\", (1,2), null)]\n"
			//				  + "arr[1].point: (23,45)\n"
			//				  + "arr[1].boss: (\"J\", (123,234), null)\n"
			//				  + "//arr[1].boss.point.x: 1010101\n"
			//				  + "int b: 6\nvec2 v : (777,888)\nprint a\nprint b\n//print arr[1].point.x";
			//TextBoxCode.Text = "struct vec2 [int x, int y]\nstruct person [text name, obj [vec2] point, obj[person] boss]\n"
			//				  + "array [person, 3] arr\n"
			//				  + "arr[1].boss: (\"j\", (123,234), null)\n";
			//TextBoxCode.Text = MeanscriptUnitTest.simpleArrayScript;
			//TextBoxCode.Text = "int a: 3\nint b : a\nobj[int] p: 5";

			// add tests
			
			foreach(var t in MCUnitTest.tests)
			{
				var item = new MenuItem();
				item.Header = t.TestName;
				item.Click += (object sender, RoutedEventArgs e) => { t.Run(); };
				TestListMenu.Items.Add(item);
			}
			textModified = false;
			BytecodeMenu.IsEnabled = false;
		}

		void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F5)
			{
				ComplileAndRun();
			}
			if (e.Key == Key.F8)
			{
				TextBoxCode.Text = MCUnitTest.simpleVariableScript;
				//MeanscriptUnitTest.SimpleVariable();
			}
			if (e.Key == Key.F9)
			{
				try
				{
					RunUnitTests();
				}
				catch (Exception x)
				{
					winOutput.Print(x.ToString());
				}
			}
		}
		
		public void Command_BytecodeData(object sender, RoutedEventArgs e)
		{
			code?.MM.PrintData();
		}
		public void Command_BytecodeInstructions(object sender, RoutedEventArgs e)
		{
			code?.MM.PrintCode();
		}
		
		public void Command_RunUnitTests(object sender, RoutedEventArgs e)
		{
			RunUnitTests();
		}
		private void RunUnitTests()
		{
			winOutput.Clear();
			MCUnitTest.RunAll();
			Status("MeanscriptUnitTest DONE!");
			winOutput.Print("\nTEST DONE!");
			winOutput.ScrollToEnd();
		}
		public void Command_ComplileAndRun(object sender, RoutedEventArgs e)
		{
			ComplileAndRun();//Implementation of run
		}
		private void ComplileAndRun()
		{
			winOutput.Clear();
			try
			{
				code = new MSCode(TextBoxCode.Text);
			}
			catch (MException e)
			{
				PrintError(e, code);
				code = null;
			}
			BytecodeMenu.IsEnabled = code != null;
		}

		private void PrintError(Exception e, MSCode? code)
		{
			try
			{
				winOutput.Print("\n\n" + e.ToString() + "\n\n");
				if (code != null)
				{
					code.MM.PrintStack();
					code.MM.Heap.Print();
					code = null;
				}
			}
			catch (Exception)
			{
				winOutput.Print("\n\nerror in PrintError");
			}
		}

		private void Verbose_Checked(object sender, RoutedEventArgs e)
		{
			Meanscript.MS.IsVerbose = true;
			Status("Verbose: ON");
		}

		private void Verbose_Unchecked(object sender, RoutedEventArgs e)
		{
			Meanscript.MS.IsVerbose = false;
			Status("Verbose: OFF");
		}
		public void Status(string s)
		{
			TextBoxStatus.Text = s;
		}
		private const string SCRIPT_EXTENSION = ".ms";
		private const string SCRIPT_FILTER = "Meanscript (.ms)|*.ms" ;
		private const string BYTECODE_EXTENSION = ".mb";
		private const string BYTECODE_FILTER = "Meanbits (.mb)|*.mb" ;

		private void textChangedEventHandler(object sender, TextChangedEventArgs args)
		{
			if (args.UndoAction == UndoAction.Create // true if text is changed by the user
				&& !textModified)
			{
				textModified = true;
				UpdateTitle();
			}
		}
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			// save modifications?
			if (textModified)
			{
				var result = MessageBox.Show("Save modifications?", "Save file", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				switch (result)
				{
					case MessageBoxResult.Yes:
						OpenSaveFileDialog(SaveScriptAction, MSCode.StreamType.SCRIPT);
						break;
					case MessageBoxResult.No:
						break;
					default:
						e.Cancel = true;
						break;
				}
			}
		}
		public void QuitCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}
		public void Command_TryQuit(object sender, RoutedEventArgs e)
		{
			Close();
		}
		
		// save bytecode

		public void Command_SaveBytecode(object sender, RoutedEventArgs e)
		{
			if (code != null)
			{
				OpenSaveFileDialog(ExportBytecode, MSCode.StreamType.BYTECODE);
			}
		}

		private void ExportBytecode(string fileName)
		{
			try
			{
				var sw = new MSBytecodeFileOutput(fileName);
				code?.MM.GenerateDataCode(sw);
				Status("bytecode exported: " + fileName);
			}
			catch (Exception e)
			{
				Status("Can't save " + fileName);
				PrintError(e, code);
			}
		}

		// run bytecode

		public void Command_RunBytecodeFile(object sender, RoutedEventArgs e)
		{
			Command_Open(RunBytecodeFile, BYTECODE_EXTENSION, BYTECODE_FILTER);
		}

		private void RunBytecodeFile(string filename)
		{
			try
			{
				var input = new MSBytecodeFileInput(filename);
				winOutput.PrintLine("run bytecode: " + filename);
				code = new MSCode(input);
			}
			catch (Exception e)
			{
				PrintError(e, code);
				code = null;
			}
			BytecodeMenu.IsEnabled = code != null;
		}
		
		// run script (CURRENTLY NOT IN USE)
		
		public void Command_RunScriptFile(object sender, RoutedEventArgs e)
		{
			Command_Open(RunScriptFile, SCRIPT_EXTENSION, SCRIPT_FILTER);
		}

		private void RunScriptFile(string filename)
		{
			try
			{
				var input = new MSScriptFileInput(filename);
				winOutput.PrintLine("run script file: " + filename);
				code = new MSCode(input);
			}
			catch (Exception e)
			{
				PrintError(e, code);
				code = null;
			}
		}

		// open file

		public void Command_Open(object sender, RoutedEventArgs e)
		{
			Command_Open(OpenFile, SCRIPT_EXTENSION, SCRIPT_FILTER);
		}
		private void Command_Open(Action<string> openFileAction, string extension, string filter)
		{
			Status("Open file");

			// Configure open file dialog box
			var dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.DefaultExt = extension; // Default file extension
			dialog.Filter = filter; // Filter files by extension
			dialog.InitialDirectory = currentDirectory;

			// Show open file dialog box
			bool? result = dialog.ShowDialog();

			// Process open file dialog box results
			if (result == true)
			{
				openFileAction(dialog.FileName);
			}
		}
		public void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Command_Open(OpenFile, SCRIPT_EXTENSION, SCRIPT_FILTER);
		}
		public async void OpenFile(string filename)
		{
			try
			{
				using (var sr = new StreamReader(filename))
				{
					TextBoxCode.Text = await sr.ReadToEndAsync();
					textModified = false;
					SetCurrentScript(filename);
				}
			}
			catch (FileNotFoundException)
			{
				// error
				Status("File not found: " + filename);
			}
		}

		// save

		private void Command_Save(object sender, RoutedEventArgs e)
		{
			Command_Save();
		}
		private void Command_SaveAs(object sender, RoutedEventArgs e)
		{
			OpenSaveFileDialog(SaveScriptAction, MSCode.StreamType.SCRIPT);
		}
		public void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Command_Save();
		}
		private void Command_Save()
		{
			if (string.IsNullOrEmpty(currentScript))
			{
				OpenSaveFileDialog(SaveScriptAction, MSCode.StreamType.SCRIPT);
				Status("save");
			}
			else if (textModified)
			{
				SaveScriptAction(currentScript);
				Status("save " + currentScript);
			}
		}
		private void OpenSaveFileDialog(Action<string> saveFileAction, MSCode.StreamType type)
		{
			Status("Save file");
			// Configure open file dialog box
			var dialog = new Microsoft.Win32.SaveFileDialog();
			
			if (type == MSCode.StreamType.BYTECODE)
			{

				dialog.DefaultExt = BYTECODE_EXTENSION; // Default file extension
				dialog.Filter = BYTECODE_FILTER; // Filter files by extension
			}
			else
			{
				dialog.DefaultExt = SCRIPT_EXTENSION; // Default file extension
				dialog.Filter = SCRIPT_FILTER; // Filter files by extension
			}
			dialog.InitialDirectory = currentDirectory;

			// Show open file dialog box
			bool? result = dialog.ShowDialog();

			// Process open file dialog box results
			if (result == true)
			{
				saveFileAction(dialog.FileName);
			}
		}

		private async void SaveScriptAction(string fileName)
		{
			try
			{
				using(var sw = new StreamWriter(fileName))
				{
					await sw.WriteAsync(TextBoxCode.Text);
					textModified = false;
					UpdateTitle();
				}
			}
			catch
			{
				Status("Can't save " + fileName);
			}
		}

		// status

		string currentScript = "";
		private string currentDirectory = "C:\\Projects\\MeanscriptCS\\tmp";

		private void SetCurrentScript(string filename)
		{
			currentScript = filename;
			UpdateTitle();
		}

		private void UpdateTitle()
		{
			string s = "Meanscript";
			if (!string.IsNullOrEmpty(currentScript))
			{
				s += " " + currentScript;
			}
			if (textModified) s += " [x]";
			Title = s;
		}

	}
}
