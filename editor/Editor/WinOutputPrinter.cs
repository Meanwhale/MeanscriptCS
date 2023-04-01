using Meanscript;
using System;
using System.Text;
using System.Windows.Controls;

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
}
