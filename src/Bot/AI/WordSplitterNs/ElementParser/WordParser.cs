using System;
using System.Collections.Generic;
using Bot.AI.WordSplitterNs.WordNs;
using Bot.Robot.Rule.Parser;
using BotLib.Misc;

namespace Bot.AI.WordSplitterNs.ElementParser
{
	public abstract class WordParser
	{
        protected abstract List<IndexRange> GetIndexRanges(string s);

        protected abstract TextWord Create(string s);

        public TextWord Create(string s, IndexRange ir)
		{
            return this.Create(s.Substring(ir.Start, ir.Length));
		}

        public List<PlainTextSymbol> Parse(string s, List<IndexRange> ranges, out List<IndexRange> exp)
		{
			var ptslist = new List<PlainTextSymbol>();
			exp = new List<IndexRange>();
            foreach (var ir in ranges)
			{
				List<IndexRange> irs;
				ptslist.AddRange(Parse(s, ir, out irs));
				exp.AddRange(irs);
			}
			return ptslist;
		}

        private List<PlainTextSymbol> Parse(string s, IndexRange r, out List<IndexRange> exp)
		{
            var ptslist = new List<PlainTextSymbol>();
            var indexRanges = this.GetIndexRanges(s.Substring(r.Start, r.Length));
			var ranges = new List<IndexRange>();
            foreach (var ir in indexRanges)
			{
                var nir = new IndexRange(ir.Start + r.Start, ir.Length);
				ranges.Add(nir);
                PlainTextSymbol item = new PlainTextSymbol(this.Create(s, nir), nir.Start, null);
                ptslist.Add(item);
			}
            exp = IndexRange.GetExceptRanges(ranges, r.Start, r.NextStart);
            return ptslist;
		}

        public List<PlainTextSymbol> Parse(string s, out List<IndexRange> exp)
		{
			List<PlainTextSymbol> list = new List<PlainTextSymbol>();
            List<IndexRange> ranges = this.GetIndexRanges(s);
			foreach (IndexRange ir in ranges)
			{
				PlainTextSymbol item = new PlainTextSymbol(this.Create(s, ir), ir.Start, null);
				list.Add(item);
			}
			exp = IndexRange.GetExceptRanges(ranges, s);
			return list;
		}
	}
}
