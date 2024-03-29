﻿using System;

namespace Meanscript.Core
{
	public class Texts
	{
		internal System.Collections.Generic.Dictionary<MSText, int> texts = new System.Collections.Generic.Dictionary<MSText, int>(MS.textComparer);
		
		private int textCounter = 0;

		public int AddText(string s)
		{
			return AddText(new MSText(s));
		}
		public int TextCount ()
		{
			return textCounter;
		}

		internal bool HasTextID(int nameID)
		{
			return GetTextByID(nameID) != null;
		}

		public int AddText(MSText data)
		{
			// text is already there
			if (texts.ContainsKey(data)) return texts[data];

			// add a new text
			int id = ++textCounter;
			texts[new MSText(data)] = textCounter;
			return id;
		}

		public void AddText(int id, string data)
		{
			texts[new MSText(data)] = id;
		}

		public void AddText(int id, MSText data)
		{
			texts[data] = id;
		}
		public MSText GetText(int id)
		{
			return GetTextByID(id);
		}
		public string FindTextStringByID(int id, bool nullIfNotFound = false)
		{
			var t = GetTextByID(id);
			if (t != null) return t.ToString();
			if (nullIfNotFound) return null;
			return "?";
		}
		public int GetTextID(MSText data)
		{
			if (data != null && texts.ContainsKey(data)) return texts[data];
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
	}
}
