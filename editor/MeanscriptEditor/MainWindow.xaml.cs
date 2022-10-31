using System;
using System.IO;
using System.Windows;

namespace MeanscriptEditor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			try
			{
				Meanscript.MeanscriptUnitTest.runAll();
			}
			catch (Exception e)
			{
				Status("unit test failed");
			}
		}
		public void Status(string s)
		{
			TextBoxStatus.Text = s;
		}
		private void menu_Open(object sender, RoutedEventArgs e)
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

		private async void OpenFile(string filename)
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
