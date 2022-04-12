using System;

namespace Bot.Robot.Rule.Parser
{
	public class NotLessThanLengthSymbol : LengthWildcardSymbol
	{
		public const string SymbolText = "{*:m,}";
		public int MinLength { get; private set; }

        public NotLessThanLengthSymbol(string matchText, int startIndex, Symbol preSymbol, int minLength)
            : base(matchText, startIndex, preSymbol)
		{
			this.MinLength = minLength;
		}

        public override string GetSymbolText()
		{
			return "{*:m,}";
		}

        public override bool IsLengthMatch(int len)
		{
			return len >= this.MinLength;
		}

	}
}
