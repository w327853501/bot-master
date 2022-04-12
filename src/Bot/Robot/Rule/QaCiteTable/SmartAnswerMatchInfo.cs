using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;

namespace Bot.Robot.Rule.QaCiteTable
{
	public class SmartAnswerMatchInfo
	{
		public AnswerWithImage AnswerWithImg;
		public double QuestionMatchScore;
		public double GoodsMatchScore;
		public bool IsRuleFullMatched;
		public bool IsQuestionFullMatched;
		public double OtherMatchScore;
		public RobotRuleEntity Rule;
		public SmartAnswerMatchInfo()
		{
			this.IsRuleFullMatched = false;
			this.IsQuestionFullMatched = false;
		}
	}
}
