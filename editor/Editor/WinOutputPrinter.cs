using Meanscript;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Windows.Controls;

namespace MeanscriptEditor
{
	public class WinOutputPrinter : MSOutputPrint
	{
		private RichTextBox tb;
		StringBuilder sb = new StringBuilder();
		private bool textChanged = false;
		private Stopwatch stopWatch;
		const int MAX_SIZE = 40000;
		const int MIN_SIZE = 20000;

		public WinOutputPrinter(RichTextBox textBoxOutput)
		{
			tb = textBoxOutput;
			tb.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
			tb.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
			tb.IsReadOnly = true;

			tb.SpellCheck.IsEnabled = false;

			var th = new Thread(TextUpdater);
			th.IsBackground = true;
			th.Start();

			stopWatch = new Stopwatch();
		}

		public void Clear()
		{
			sb.Clear();
			tb.Document.Blocks.Clear();
			Console.Write("output cleared");
		}

		private void TextUpdater()
		{
			while(true)
			{
				// check if text is changed and some time has passed 
				if (textChanged && stopWatch.ElapsedMilliseconds > 100) try
				{
					tb.Dispatcher.Invoke(() =>
					{
						tb.Document.Blocks.Clear();
						tb.AppendText(sb.ToString());
						ScrollToEnd();
					});
					textChanged = false;
				}
				catch
				{
					return;
				}

				Thread.Sleep(100);
			}
		}

		public override MSOutputPrint Print(char x)
		{
			// print both console (for debug) and window
			Console.Write(x);
			PrintTB(x.ToString());
			return this;
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

		private void PrintTB(string x)
		{
			sb.Append(x);
			if (sb.Length > MAX_SIZE)
			{
				// estä tekstiä kasvamasta liian isoksi: kopioi vanhasta loppupuoli uuden alkuun
				var newSB = new StringBuilder(sb.ToString(), MIN_SIZE, sb.Length-MIN_SIZE, 2*MAX_SIZE);
				sb = newSB;
			}
			textChanged = true;
			stopWatch.Restart();
		}

		public void ScrollToEnd()
		{
			tb.ScrollToEnd();
		}
	}
}
