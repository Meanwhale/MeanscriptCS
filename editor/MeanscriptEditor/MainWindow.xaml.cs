using Meanscript;
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
		
		const int MAX_SIZE = 10000;
		const int MIN_SIZE = 5000;

		// TODO: test more (printtaa vähän kerrallaan ja testaa toimiiko oikein)

		public WinOutputPrinter(TextBlock textBoxOutput, ScrollViewer textBoxOutputScrollViewer)
		{
			tb = textBoxOutput;
			sw = textBoxOutputScrollViewer;
		}

		public override MSOutputPrint Print(char x)
		{
			return Print(x.ToString());
		}

		public override MSOutputPrint Print(string x)
		{
			sb.Append(x);
			if (sb.Length > MAX_SIZE)
			{
				// estä tekstiä kasvamasta liian isoksi: kopioi vanhasta loppupuoli uuden alkuun
				var newSB = new StringBuilder(sb.ToString(), MIN_SIZE, sb.Length-MIN_SIZE, 2*MAX_SIZE);
				sb = newSB;
			}
			tb.Text = sb.ToString();

			//tb.AppendText(x);
			return this;
		}

		public override void WriteByte(byte b)
		{
			Print(b.ToString());
		}
		public void ScrollToEnd()
		{
			sw.ScrollToBottom();
			//tb.CaretIndex = tb.Text.Length;
			//tb.ScrollToEnd();
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
		try
		{
			Meanscript.MeanscriptUnitTest.RunAll();
			Status("MeanscriptUnitTest DONE!");
			winOutput.ScrollToEnd();
		}
		catch (Exception e)
		{
			Status("unit test failed");
			TextBoxOutput.Text = e.ToString();
		}
		
		KeyDown += new KeyEventHandler(MainWindow_KeyDown);

	}

	void MainWindow_KeyDown(object sender, KeyEventArgs e)
	{
		Status("You pressed a keyboard key: " + e.Key);
		if (e.Key == Key.F5)
		{
			Command_CompileAndRun();
		}
	}
	public void Status(string s)
	{
		TextBoxStatus.Text = s;
	}
	public void menu_Open(object sender, RoutedEventArgs e)
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
	public void Command_CompileAndRun()
	{
	}
	public void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		Command_Open();//Implementation of open file
	}
	public void CompileAndRunExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		Command_CompileAndRun();//Implementation of run
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
