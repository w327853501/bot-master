using System;
using System.Collections.Generic;
using System.Linq;
using Bot.AI.WordSplitterNs.WordNs;
using Newtonsoft.Json;

namespace Bot.Robot.Rule.Parser
{
	public class PlainTextSymbol : Symbol
	{
		public TextWord Word { get; private set; }

		[JsonConstructor]
        public PlainTextSymbol(TextWord textWord, int startIndex)
            : base(textWord.Text, startIndex, null)
		{
			Word = textWord;
		}

        public PlainTextSymbol(TextWord textWord, int startIndex, Symbol preSymbol)
            : base(textWord.Text, startIndex, preSymbol)
		{
			
			Word = textWord;
		}

        public PlainTextSymbol(string text, int startIndex, Symbol preSymbol)
            : base(text, startIndex, preSymbol)
		{
			Word = new TextWord(text);
		}

        protected override string[] GetSemantics()
		{
			return Word.Semantics;
		}

        public override bool AsPatternSymbolSemanticMatchWith(IEnumerable<string> semantics)
		{
            return semantics.Contains(Word.Text);
		}
	}
}
