using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;

namespace Bot.Robot.Testor
{
	public class PatternMatchResult
	{
		public string Pattern { get; private set; }

		public List<PatternMatchSingleResult> Matches;

		public RobotRuleEntity Rule
		{
			get
			{
				RobotRuleEntity result;
				if (Matches.xNotEmpty())
				{
					result = Matches[0].Rule;
				}
				else
				{
					result = null;
				}
				return result;
			}
		}


		public PatternMatchResult(string pattern)
		{
			Matches = new List<PatternMatchSingleResult>();
			Pattern = pattern;
		}

	}
}
