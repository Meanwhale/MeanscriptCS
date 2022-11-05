namespace Meanscript
{

	public class TokenTree : MC
	{
		internal MNode root = null;
		private System.Collections.Generic.Dictionary<MSText, int> texts = new System.Collections.Generic.Dictionary<MSText, int>(MS.textComparer);
		internal int textCount;
		public TokenTree()
		{
			textCount = 1; // textID 0 means empty ("")
		}

		public int AddText(MSText data)
		{
			// text is already there
			if ((texts.ContainsKey(data))) return texts[data];

			// add a new text
			int id = textCount;
			texts[new MSText(data)] = textCount++;
			return id;
		}

		public int GetTextID(MSText data)
		{
			if ((texts.ContainsKey(data))) return texts[data];
			return -1;
		}

		public MSText GetTextByID(int id)
		{
			int numTexts = texts.Count;
			if (id < 0 || id > numTexts) return null;
			foreach (var entry in texts)
			{
				if (id == entry.Value) return entry.Key;
			}
			return null;
		}

		//
	}
}
