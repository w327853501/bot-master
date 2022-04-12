using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Robot.Testor
{
	public class PatternMatchSingleResult
	{		
		public RobotRuleEntity Rule;

		public PatternMatchSingleResult(RobotRuleEntity rule)
		{
			Rule = rule;
		}
	}
}
