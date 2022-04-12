using Bot.Common.Account;
using Bot.Robot;
using Bot.Robot.RuleEditor;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Bot.AssistWindow.Widget.Robot
{
    public class RuleEditor
    {
        private string _seller;
        private Window _owner;
        public RuleEditor(string seller, Window owner)
        {
	        _seller = seller;
	        _owner = owner;
        }
        public void CreateRule(string intention, Action onClosed = null)
        {
            WndRuleEditor.Create(intention, _seller, _owner, () =>
            {
                if (onClosed != null)
                {
                    onClosed.Invoke();
                }
                else
                {
                    AccountHelper.GetPubDbAccount(_seller);
                    WndRobotRuleManager.UpdateRobotManagerUI(_seller);
                }
            });
        }

        public void EditRule(RobotRuleEntity robotRule, Action onClosed)
        {
            WndRuleEditor.Edit(robotRule, _seller, _owner,()=> {
            
            });
        }

        public void OpenRobotRuleManager()
        {
            WndRobotRuleManager.MyShow(_seller, null);
        }
    }
}
