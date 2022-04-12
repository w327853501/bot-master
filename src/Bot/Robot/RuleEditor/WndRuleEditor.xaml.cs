using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Bot.AssistWindow;
using Bot.Common;
using Bot.Common.Account;
using Bot.Common.Windows;
using Bot.Help;
using Bot.Robot.Rule.QaCiteTable;
using DbEntity;
using BotLib;
using BotLib.Db.Sqlite;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf;
using Xceed.Wpf.Toolkit;
using System.Media;
using Bot.Common.ImageHelper;

namespace Bot.Robot.RuleEditor
{
	public partial class WndRuleEditor : EtWindow
	{
		private bool _isCloseBySubmit;
		private string _dbAccount;
		private RobotRuleEntity _ent;
		private VdWndRuleEditor _viewData;
		public RobotRuleEntity Result { get; private set; }

		private const string _pattern = "\\(.*?\\)";

		private WndRuleEditor(string seller)
		{
			_isCloseBySubmit = false;
			InitializeComponent();
			Seller = seller;
			MaxHeight = SystemParameters.WorkArea.Height - 10.0;
			_viewData = new VdWndRuleEditor(this);
			groupQuestionPattern.Init(null, TbNickHelper.GetMainPart(seller));
		}

		private WndRuleEditor(RobotRuleEntity ent, string seller, string dbAccount, bool showRuleCat) : this(seller)
		{
			_ent = ent;
			SetContent(ent);
			ShowRuleCatalog(showRuleCat, (ent != null ? ent.ParentId : null));
		}


		private WndRuleEditor(string parentId, string dbAccount, string seller) : this(seller)
		{
			_ent = EntityHelper.Create<RobotRuleEntity>(string.Empty);
			_ent.ParentId = parentId;
			_ent.DbAccount = dbAccount;
			_dbAccount = dbAccount;
			grdRuleCat.Visibility = Visibility.Collapsed;
		}

		public WndRuleEditor(string intention, string seller) : this(seller)
		{
			_ent = EntityHelper.Create<RobotRuleEntity>(string.Empty);
			grdRuleCat.Visibility = Visibility.Visible;
			var entityId = RuleEditorParams.GetSelectCatalogLastTime(Seller);
			tboxRuleCatalog.Text = RobotRuleCatalogEntityEx.GetCataBreadcrumbBySeller(entityId, Seller, 100);
			var ruleCata = RobotRuleCatalogEntityEx.FindOne(entityId, null);
			SetDbAccount(ruleCata.DbAccount);
			if (!intention.xIsNullOrEmptyOrSpace())
			{
				intention = intention.Trim();
				_ent.Intention = intention;
				var intenlst = new List<string>
				{
					intention
				};
				_ent.SetQuestions(intenlst);
				tboxIntention.Text = intention;
				groupQuestions.Init(intenlst);
			}
		}

		private RobotRuleEntity CreateNew()
		{
			if (_ent.ParentId.xIsNullOrEmpty())
			{
				_ent.ParentId = RobotRuleCatalogEntityEx.GetCataRoot(_dbAccount).EntityId;
			}
			_ent.Intention = tboxIntention.Text.Trim();
			_ent.SetAnswersV3(groupAnswers.Answers);
			_ent.SetPatternsV3(groupQuestionPattern.Patterns);
			_ent.SetQuestions(groupQuestions.Texts);
			_ent.SetModifyTick();
			return _ent;
		}

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			CloseWnd(true);
		}
		private void CloseWnd(bool closeWnd)
		{
			Close(closeWnd);
			RuleAnswerImageHelper.DeleteImages(groupAnswers.GetAddNewImages());
		}

		private void Close(bool closeWnd)
		{
			Result = null;
			if (closeWnd)
			{
				Close();
			}
		}

		private void btnSubmit_Click(object sender, RoutedEventArgs e)
		{
			Submit();
			RuleAnswerImageHelper.DeleteImages(groupAnswers.GetDeleteImages());
			var wndAssist = WndAssist.FindWndAssist(Seller);
			if (wndAssist != null)
			{
				wndAssist.ctlRightPanel.UpdateRobotTab();
			}
		}

		private void Submit()
		{
			if (CanSubmit())
			{
				Result = CreateNew();
				CiteTableManager.AddOrUpdateRule(Result);
				DbHelper.SaveToDb(Result, true);
				_isCloseBySubmit = true;
				Close();
			}
			else
			{
				SystemSounds.Beep.Play();
			}
		}

		private bool CanSubmit()
		{
			return _viewData.Submitable() && HasContent();
		}

		private new bool HasContent()
		{
			return !groupAnswers.IsEmptyOrHasSyntaxError() && !groupQuestions.HasSyntaxError() && HasQuestion();
		}

		private bool HasQuestion()
		{
			if (!groupQuestions.HasContent && !groupQuestionPattern.HasContent)
			{
				ShowTip("【完全匹配、问题模式】不能同步为空");
				groupQuestions.FocusFirstItem();
				return false;
			}
			else if (!groupAnswers.HasContent)
			{
				ShowTip("答案不能为空！");
				groupAnswers.FocusOneAnswer();
				return false;
			}
			return true;
		}


		public static void Edit(RobotRuleEntity rule, string seller, Window owner, Action onClosed)
		{
			if (QnHelper.Auth.CanEditRobotRule(seller))
			{
				var copy = Util.Clone<RobotRuleEntity>(rule);
				var wnd = new WndRuleEditor(copy, seller, copy.DbAccount, true);
				wnd.FirstShow(seller, owner, ()=>{
                    if(wnd.Result!=null && NotEqual(rule,wnd.Result))
                    {
                        if (wnd.Result.ParentId == rule.ParentId)
                        {
							RobotRuleEntityEx.Save(wnd.Result);
                        }
                        else
                        {
							RobotRuleEntityEx.Delete(wnd.Result);
							RobotRuleEntityEx.Append(wnd.Result,rule.ParentId);
						}
						if(onClosed!=null) onClosed();
                    }
                
                });
			}
		}

		private void SetDbAccount(string dbAccount)
		{
			_ent.DbAccount = dbAccount;
			_dbAccount = dbAccount;
		}

		public static void Edit(RobotRuleEntity ent, string seller, string dbAccount, Window ownerWnd, Action<RobotRuleEntity> callback)
		{
			if (QnHelper.Auth.CanEditRobotRule(seller))
			{
				var copy = Util.Clone<RobotRuleEntity>(ent);
				var wnd = new WndRuleEditor(copy, seller, dbAccount, false);
				wnd.FirstShow(seller, ownerWnd, ()=> {
					if (NotEqual(copy, ent))
					{							
						EntityHelper.SetModifyTick(wnd.Result);
					}					
					callback(wnd.Result);
				});
			}
			else
			{
				callback(null);
			}
		}

		private static bool NotEqual(RobotRuleEntity ruleEt1, RobotRuleEntity ruleEt2)
		{
			return ruleEt1.DbAccount != ruleEt2.DbAccount || ruleEt1.EntityId != ruleEt2.EntityId || ruleEt1.IsDeleted != ruleEt2.IsDeleted || ruleEt1.AnswersJsonV3 != ruleEt2.AnswersJsonV3 || ruleEt1.GoodsCatIdsCsv != ruleEt2.GoodsCatIdsCsv || ruleEt1.GoodsNumiidsCsv != ruleEt2.GoodsNumiidsCsv || ruleEt1.Intention != ruleEt2.Intention || ruleEt1.PatternsJsonV3 != ruleEt2.PatternsJsonV3 || ruleEt1.QuestionsJson != ruleEt2.QuestionsJson;
		}
		private string ValidateIntention(string intention)
		{
			string errmsg = null;
			if (string.IsNullOrWhiteSpace(intention))
			{
				errmsg = "必填";
			}
			else if (HasSameIntention(_ent, intention))
			{
				errmsg = "含意已存在(不允许相同)，请修改";
			}
			if (errmsg != null)
			{
				tboxIntention.Focus();
			}
			return errmsg;
		}

		private bool HasSameIntention(RobotRuleEntity ent, string intention)
		{
			return DbHelper.FirstOrDefault<RobotRuleEntity>(ent.DbAccount,  (rule)=> rule.ParentId == ent.ParentId && rule.Intention == intention && rule.EntityId != ent.EntityId) != null;
		}

		public static void CreateNew(string parentId, string dbAccount, string seller, Window ownerWnd, Action<RobotRuleEntity> callback)
		{
			if (QnHelper.Auth.CanEditRobotRule(seller))
			{
				var wnd = new WndRuleEditor(parentId, dbAccount, seller);
				wnd.FirstShow(seller, ownerWnd,()=>{
                    if (wnd.Result != null)
                    {
                        wnd.Result.DbAccount = dbAccount;
                        EntityHelper.SetModifyTick(wnd.Result);                        
                        if (callback != null)
                            callback(wnd.Result);
                    }
                }, false);
			}
			else
			{
			    callback(null);
			}
		}

		private void ShowRuleCatalog(bool showRuleCat, string cid)
		{
			if (showRuleCat)
			{
				grdRuleCat.Visibility = Visibility.Visible;
				tboxRuleCatalog.Text = RobotRuleCatalogEntityEx.GetCataBreadcrumbBySeller(cid, Seller, 100);
			}
			else
			{
				grdRuleCat.Visibility = Visibility.Collapsed;
			}
		}

		private void SetContent(RobotRuleEntity ent)
		{
			tboxIntention.Text = ent.Intention;
			QuestionPattern[] patternsV = ent.GetPatternsV3();
			groupQuestionPattern.Init(patternsV, TbNickHelper.GetMainPart(Seller));
			var questions = ent.GetQuestions();
			groupQuestions.Init(questions);
			if (questions.xIsNullOrEmpty() && !patternsV.xIsNullOrEmpty())
			{
				tabcQuestion.SelectedIndex = 1;
			}
			groupAnswers.Init(ent.GetAnswersV3());
		}

		private void btnChangeRuleCatalog_Click(object sender, RoutedEventArgs e)
		{
			var cid = RuleEditorParams.GetSelectCatalogLastTime(Seller);
            WndRuleCatalogSelector.SelectedCatalog(cid, Seller, this, newcid =>
            {
				if (string.IsNullOrEmpty(newcid)) return;
				RuleEditorParams.SetSelectCatalogLastTime(Seller,newcid);
				tboxRuleCatalog.Text = RobotRuleCatalogEntityEx.GetCataBreadcrumbBySeller(newcid,Seller);
				var cataEnt = RobotRuleCatalogEntityEx.FindOne(cid);
				_ent.ParentId = newcid;
				SetDbAccount(cataEnt.DbAccount);
			});
        }

		private void btnTest_Click(object sender, RoutedEventArgs e)
		{
			if (CanSubmit())
			{
				var ent = CreateNew();
				//WndRuleTestorForEditor.smethod_3(ent, base.Seller, this);
			}
			else
			{
				MsgBox.ShowErrTip("需要输入完整的规则，然后才能测试", null);
			}
		}

		private void btnHelp_Click(object sender, RoutedEventArgs e)
		{
		}

		public static void Create(string intention, string seller, Window owner, Action onClosed)
		{
			if (QnHelper.Auth.CanEditRobotRule(seller))
			{
				var wnd = new WndRuleEditor(intention, seller);
				wnd.FirstShow(seller, owner, ()=> {
					
				}, false);
			}
		}

		private void btnGetItemIdHelp_Click(object sender, RoutedEventArgs e)
		{

		}


		private void EtWindow_Closing(object sender, CancelEventArgs e)
		{
			if (!_isCloseBySubmit)
			{
				Close(false);
			}
		}

		private class VdWndRuleEditor : ViewData
		{
			public string Intention { get; set; }
			public string GoodsCatalog { get; set; }
			public string GoodsId { get; set; }
			private WndRuleEditor _wnd;

			public VdWndRuleEditor(WndRuleEditor wnd) : base(wnd)
			{
				Intention = "";
				_wnd = wnd;
				SetBinding("Intention", wnd.tboxIntention, ()=>{
                    return _wnd.ValidateIntention(Intention);
                });
			}

		}

		public static class RuleEditorParams
		{
			public static string GetSelectCatalogLastTime(string seller)
			{
				string entityId = PersistentParams.GetParam2Key("WndRuleEditorV2.SelectCatalogLastTime", seller, null);
				if (entityId != null)
				{
					var cata = RobotRuleCatalogEntityEx.FindOne(entityId, null);
					if (cata == null || cata.IsDeleted)
					{
						entityId = null;
					}
				}
				return entityId ?? RobotRuleCatalogEntityEx.GetCataRoot(AccountHelper.GetPubDbAccount(seller)).EntityId;
			}

			public static void SetSelectCatalogLastTime(string seller, string entityId)
			{
				PersistentParams.TrySaveParam2Key("WndRuleEditor.SelectCatalogLastTime", seller, entityId);
			}
		}
	}
}
