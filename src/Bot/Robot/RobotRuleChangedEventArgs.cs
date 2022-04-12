using Bot.Common;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Robot
{
    public class RobotRuleChangedEventArgs : DbRecordChangedEventArgs
    {
        public RobotRuleChangedEventArgs(DbRecordChangedEventArgs args)
            : base(args.Entity, args.PreState)
		{

		}
    }
}
