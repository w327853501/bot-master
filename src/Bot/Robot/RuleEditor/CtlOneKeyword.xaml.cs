using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using DbEntity;
using BotLib;
using Xceed.Wpf.Toolkit;

namespace Bot.Robot.RuleEditor
{
	public partial class CtlOneKeyword : UserControl
    {
        private readonly double _lineHeight;
        public static RoutedEvent EvRemoveEvent { get; set; }
        public static RoutedEvent EvAddEvent { get; set; }
        static CtlOneKeyword()
        {
            EvAddEvent = EventManager.RegisterRoutedEvent("EvAdd", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOneKeyword));
            EvRemoveEvent = EventManager.RegisterRoutedEvent("EvRemove", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOneKeyword));
        }

		public CtlOneKeyword(RuleKeyword kw)
		{
			InitializeComponent();
			Keyword = kw;
			btnAdd.ToolTip = "新增 关键字(在下方)";
			btnRemove.ToolTip = "删除 本关键字";
		}

		public CtlOneKeyword()
		{
			InitializeComponent();
			Height = _lineHeight = 25.0; ;
		}

		public void SetTitle(string title, int no)
		{
			tbkTitle.Text = string.Format("{0}{1}：", title, no);
		}

		public void FocusKeyword()
		{
			tboxKey.Focus();
		}

		public RuleKeyword Keyword
		{
			get
			{
				return new RuleKeyword
				{
					Keyword = tboxKey.Text.Trim(),
					SynonymCsv = tboxSynonym.Text.Trim()
				};
			}
			set
			{
				tboxKey.Text = (value ==null || string.IsNullOrEmpty(value.Keyword)) ? string.Empty : value.Keyword.Trim();
				tboxSynonym.Text = (value == null || string.IsNullOrEmpty(value.SynonymCsv)) ? string.Empty : value.SynonymCsv.Trim();
			}
		}

		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			RaiseAddEvent();
		}

		private void btnRemove_Click(object sender, RoutedEventArgs e)
		{
			RaiseRemoveEvent();
		}

		public event RoutedEventHandler EvAdd
		{
			add
			{
				AddHandler(CtlOneKeyword.EvAddEvent, value);
			}
			remove
			{
				RemoveHandler(CtlOneKeyword.EvAddEvent, value);
			}
		}

		protected RoutedEventArgs RaiseAddEvent()
		{
			var args = new RoutedEventArgs();
			args.RoutedEvent = CtlOneKeyword.EvAddEvent;
			args.Source = this;
			RaiseEvent(args);
			return args;
		}

		public event RoutedEventHandler EvRemove
		{
			add
			{
				AddHandler(CtlOneKeyword.EvRemoveEvent, value);
			}
			remove
			{
				RemoveHandler(CtlOneKeyword.EvRemoveEvent, value);
			}
		}

		protected RoutedEventArgs RaiseRemoveEvent()
		{
			var args = new RoutedEventArgs();
			args.RoutedEvent = CtlOneKeyword.EvRemoveEvent;
			RaiseEvent(args);
			return args;
		}

		private void tboxKey_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.OemPlus || e.Key == Key.OemComma)
			{
				Util.Beep();
				e.Handled = true;
			}
		}

    }
}
