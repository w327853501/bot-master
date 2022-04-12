using Bot.Common;
using Bot.Common.TreeviewHelper;
using Bot.Common.Windows;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BotLib.Extensions;

namespace Bot.Robot.RuleEditor
{
	public class RuleTreeViewController : TreeViewController
	{
		public RuleTreeViewController(string dbAccount, string seller)
			: base(typeof(RobotRuleCatalogEntity), typeof(RobotRuleEntity), dbAccount, seller, new TreeDbAccessor(typeof(RobotRuleCatalogEntity), typeof(RobotRuleEntity), dbAccount))
		{
		}


		public override string ReadCataNodeName(TreeNode cn)
		{
			return (cn as RobotRuleCatalogEntity).Name;
		}

		public override string ReadNodeTitle(TreeNode tn)
		{
			return (tn as RobotRuleEntity).Intention;
		}


		public override string ControllerName()
		{
			return "规则";
		}

		private void ShowWndInput(string parentId, Window owner, string old, Action<string> callback)
		{
			WndInput.MyShow("请输入“规则分组”的名称：", "新建：规则分组", callback, old, null, name =>
			{
				if (name.xIsNullOrEmptyOrSpace())
				{
					return "必填";
				}
				else if (name == old || DbAccessor.ExistCatalogName(parentId, name))
				{
					return "分组名字已存在，请换一个名字。";
				}
				return string.Empty;
			}, owner);
		}

		public override void EditCata(TreeNode tn, Window owner, Action<TreeNode> callback)
		{
			if (QnHelper.Auth.CanEditRobotRule(Seller))
			{
				var cat = (tn as RobotRuleCatalogEntity);
				ShowWndInput(tn.ParentId, owner, cat.Name, name =>
				{
					if (name.xIsNullOrEmptyOrSpace())
					{
						callback(cat);
						return;
					}
					cat.Name = name;
					callback(cat);
				});
			}
			else
			{
				callback(null);
			}
		}

		public override void Edit(TreeNode tn, Window owner, Action<TreeNode> onClosed)
		{
			WndRuleEditor.Edit(tn as RobotRuleEntity, Seller, DbAccount, owner, onClosed);
		}

		public override void CreateCata(TreeNode parent, Window owner, Action<TreeNode> callback, object obj = null)
		{
			if (QnHelper.Auth.CanEditRobotRule(Seller))
			{
				ShowWndInput(parent.EntityId, owner, null, name =>
				{
					if (name.xIsNullOrEmptyOrSpace())
					{
						callback(null);
						return;
					}
					var ent = EntityHelper.Create<RobotRuleCatalogEntity>(DbAccount);
					ent.ParentId = parent.EntityId;
					ent.Name = name;
					callback(ent);
				});
			}
			else
			{
				callback(null);
			}
		}

		public override void Create(TreeNode node, Window owner, Action<TreeNode> callback, object obj = null)
		{
			WndRuleEditor.CreateNew(node.EntityId, DbAccount, Seller, owner, callback);
		}

        public override string ReadNodeCode(TreeNode n)
        {
			return null;
        }

        public override string ReadNodeImageName(TreeNode n)
        {
			return null;
        }
    }
}
