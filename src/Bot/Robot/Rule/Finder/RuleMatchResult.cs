using Bot.Robot.Testor;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;
using BotLib;

namespace Bot.Robot.Rule.Finder
{
	public class RuleMatchResult
	{
		private RobotRuleEntity _rule;
		public List<PatternMatchResult> MatchInfo { get; private set; }
		public RobotRuleEntity Rule
		{
			get
			{
				if (_rule == null && MatchInfo.xNotEmpty())
				{
					_rule = MatchInfo[0].Rule;
				}
				return _rule;
			}
		}
	}
}

