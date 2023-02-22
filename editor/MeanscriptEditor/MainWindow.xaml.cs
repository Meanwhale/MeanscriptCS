using Meanscript;
using Meanscript.Core;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MeanscriptEditor
{
	public class WinOutputPrinter : MSOutputPrint
	{
		private TextBlock tb;
		private ScrollViewer sw;
		StringBuilder sb = new StringBuilder();
		
		const int MAX_SIZE = 40000;
		const int MIN_SIZE = 20000;

		// TODO: test more (printtaa vähän kerrallaan ja testaa toimiiko oikein)

		public WinOutputPrinter(TextBlock textBoxOutput, ScrollViewer textBoxOutputScrollViewer)
		{
			tb = textBoxOutput;
			sw = textBoxOutputScrollViewer;
		}

		public override MSOutputPrint Print(char x)
		{
			// print both console (for debug) and window
			Console.Write(x);
			PrintTB(x.ToString());
			return this;
		}

		private void PrintTB(string x)
		{
			sb.Append(x);
			if (sb.Length > MAX_SIZE)
			{
				// estä tekstiä kasvamasta liian isoksi: kopioi vanhasta loppupuoli uuden alkuun
				var newSB = new StringBuilder(sb.ToString(), MIN_SIZE, sb.Length-MIN_SIZE, 2*MAX_SIZE);
				sb = newSB;
			}
			tb.Text = sb.ToString();
			sw.ScrollToBottom();
		}

		public override MSOutputPrint Print(string x)
		{
			// print both console (for debug) and window
			Console.Write(x);
			PrintTB(x);
			return this;
		}

		public override void WriteByte(byte b)
		{
			Print(b.ToString());
		}
		public void ScrollToEnd()
		{
			sw.ScrollToBottom();
		}
		public void Clear()
		{
			sb.Clear();
			tb.Text = "";
		}
	}

	public partial class MainWindow : Window
	{
		private WinOutputPrinter winOutput;

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

			//TextBoxCode.Text = "struct vec [int x, int y]\nvec v: 678 876\nint a: 11\nsum a v.x\nsum 7 8 9";
			TextBoxCode.Text = "array [int,5] a\nint b : 5\na[3]: 456\nprint a[3]";
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
		}

		void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F5)
			{
				ComplileAndRun();
			}
			if (e.Key == Key.F8)
			{
				TextBoxCode.Text = MeanscriptUnitTest.simpleVariableScript;
				//MeanscriptUnitTest.SimpleVariable();
			}
			if (e.Key == Key.F9)
			{
				RunUnitTests();
			}
		}
		
		public void Command_RunUnitTests(object sender, RoutedEventArgs e)
		{
			RunUnitTests();//Implementation of run
		}
		private void RunUnitTests()
		{
			winOutput.Clear();
			//try
			{
				MeanscriptUnitTest.RunAll();
				Status("MeanscriptUnitTest DONE!");
				winOutput.Print("\nTEST DONE!");
				winOutput.ScrollToEnd();
			}
			//catch (Exception e)
			//{
			//	Status("unit test failed");
			//	TextBoxOutput.Text = e.ToString();
			//}
		}
		public void Command_ComplileAndRun(object sender, RoutedEventArgs e)
		{
			ComplileAndRun();//Implementation of run
		}
		private void ComplileAndRun()
		{
			winOutput.Clear();
			MeanMachine mm = null;
			try
			{
				MSCode code = new MSCode(TextBoxCode.Text);
			}
			catch (MException e)
			{
				winOutput.Print(e.ToString());
				if (mm != null)
				{
					mm.PrintStack();
					mm.Heap.Print();
				}
			}
			//catch (Exception e)
			//{
			//	winOutput.Print("\n" + e.ToString());
			//}
		}
		private void Verbose_Checked(object sender, RoutedEventArgs e)
		{
			Meanscript.MS._verboseOn = true;
			Status("Verbose: ON");
		}

		private void Verbose_Unchecked(object sender, RoutedEventArgs e)
		{
			Meanscript.MS._verboseOn = false;
			Status("Verbose: OFF");
		}
		public void Status(string s)
		{
			TextBoxStatus.Text = s;
		}
		public void Command_Open(object sender, RoutedEventArgs e)
		{
			Command_Open();
		}
		public void Command_Open()
		{
			Status("Open file");

			// Configure open file dialog box
			var dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.DefaultExt = ".ms"; // Default file extension
			dialog.Filter = "Meanscript (.ms)|*.ms"; // Filter files by extension
			dialog.InitialDirectory = "C:\\Projects\\Meanscript";

			// Show open file dialog box
			bool? result = dialog.ShowDialog();

			// Process open file dialog box results
			if (result == true)
			{
				OpenFile(dialog.FileName);
			}
		}
		public void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Command_Open();//Implementation of open file
		}
		public async void OpenFile(string filename)
		{
			try
			{
				using (var sr = new StreamReader(filename))
				{
					TextBoxCode.Text = await sr.ReadToEndAsync();
				}
			}
			catch (FileNotFoundException)
			{
				// error
				Status("File not found: " + filename);
			}
		}
		
	}
}
