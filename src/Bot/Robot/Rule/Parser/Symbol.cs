using System;
using System.Collections.Generic;
using BotLib.Extensions;
using Newtonsoft.Json;

namespace Bot.Robot.Rule.Parser
{
	public abstract class Symbol
    {
        public string MatchText;

        private int _nextIndex;

        private string[] _sementic;

        private List<Symbol> _list;

		public string TypeName
		{
			get
			{
				return base.GetType().Name;
			}
		}

		[JsonIgnore]
		public bool IsSolid
		{
			get
			{
				return !(this is LengthWildcardSymbol);
			}
		}

		public int StartIndex { get; set; }

		[JsonIgnore]
		public int NextIndex
		{
			get
			{
				if (_nextIndex == 0)
				{
					_nextIndex = StartIndex + MatchText.Length;
				}
				return _nextIndex;
			}
		}

		[JsonIgnore]
		public string[] Semantics
		{
			get
			{
				if (_sementic == null)
				{
                    _sementic = GetSemantics();
				}
				return _sementic;
			}
		}

        protected abstract string[] GetSemantics();

		[JsonIgnore]
		public int Length
		{
			get
			{
				return MatchText.Length;
			}
		}

		[JsonIgnore]
		public Symbol PreSymbol { get; set; }

        public Symbol(string matchText, int startIndex, Symbol preSymbol)
		{
			_list = null;
            MatchText = matchText;
			StartIndex = startIndex;
			PreSymbol = preSymbol;
		}

        public List<Symbol> SplitToList()
		{
			var syms = new List<Symbol>();
			for (Symbol symbol = this; symbol != null; symbol = symbol.PreSymbol)
			{
				Symbol syb = symbol.Clone();
				syb.PreSymbol = null;
				syms.Add(syb);
			}
			syms.Reverse();
			return syms;
		}

		[JsonIgnore]
		public List<Symbol> List
		{
			get
			{
				if (_list == null)
				{
                    ReConstructList();
				}
				return _list;
			}
		}

        public void ReConstructList()
		{
            _list = SplitToList();
		}

		public override string ToString()
		{
            string text = string.Empty;
			if (PreSymbol != null)
			{
				text = PreSymbol.ToString() + text;
			}
			return text;
		}


        public Symbol Clone()
		{
			return base.MemberwiseClone() as Symbol;
		}

        public bool IsEqual(Symbol symbol)
		{
			return GetType() == symbol.GetType() && StartIndex == symbol.StartIndex && NextIndex == symbol.NextIndex;
		}

        public void UpdateStartIndex(int startIndexShift)
		{
			StartIndex += startIndexShift;
			_nextIndex = StartIndex + MatchText.Length;
		}

        public abstract bool AsPatternSymbolSemanticMatchWith(IEnumerable<string> semantics);

	}
}
