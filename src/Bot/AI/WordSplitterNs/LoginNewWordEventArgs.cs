using System;

namespace Bot.AI.WordSplitterNs
{
	public class LoginNewWordEventArgs : EventArgs
	{
		public string Word { get; private set; }

		public LoginNewWordEventArgs(string word)
		{
			Word = word;
		}

		public override string ToString()
		{
			return Word;
		}
	}
}
