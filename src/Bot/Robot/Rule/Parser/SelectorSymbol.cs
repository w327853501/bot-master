using System;
using System.Collections.Generic;
using System.Linq;
using BotLib.Extensions;

namespace Bot.Robot.Rule.Parser
{
	public abstract class SelectorSymbol : Symbol
	{
        public SelectorSymbol(string matchText, int startIndex, Symbol preSymbol, List<Symbol> compoudSymbols)
            : base(matchText, startIndex, preSymbol)
		{
			CompoudSymbols = compoudSymbols;
		}

        public override bool AsPatternSymbolSemanticMatchWith(IEnumerable<string> semantics)
		{
			foreach (var value in Semantics)
			{
                if (semantics.Contains(value))
				{
					return true;
				}
			}
			return false;
		}

		public List<Symbol> CompoudSymbols { get; private set; }

        protected override string[] GetSemantics()
		{
			var semantics = new HashSet<string>();
			foreach (Symbol symbol in CompoudSymbols)
			{
				semantics.xAddRange(symbol.Semantics);
			}
			return semantics.ToArray();
		}
	}
}
