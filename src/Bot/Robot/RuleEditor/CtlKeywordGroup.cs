using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DbEntity;
using BotLib.Extensions;

namespace Bot.Robot.RuleEditor
{
	public class CtlKeywordGroup : ScrollViewer
    {
        private const string Title = "关键字";
        private StackPanel _spanel;
        private double _rowHeight;
        private int _visibleRows;

		public CtlKeywordGroup()
		{
			_spanel = new StackPanel();
			_visibleRows = -1;
			if (!DesignerProperties.GetIsInDesignMode(this))
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				AddChild(_spanel);
				InitUI();
				Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				_rowHeight = DesiredSize.Height;
				VisibleRows = 5;
			}
		}

		public int VisibleRows
		{
			get
			{
				return _visibleRows;
			}
			set
			{
				if (value != _visibleRows)
				{
					_visibleRows = value;
					MaxHeight = _rowHeight * (double)value;
				}
			}
		}

		public void Init(List<RuleKeyword> slist)
		{
			_spanel.Children.Clear();
			if (slist == null)
			{
				InitUI();
			}
			else
			{
				foreach (var kw in slist)
				{
					InitUI(kw);
				}
			}
		}

		public new bool HasContent
		{
			get
			{
				if (Keywords == null || Keywords.Count == 0) return false;
				var rt = false;
				foreach (var kw in Keywords)
				{
					if (!kw.Keyword.xIsNullOrEmptyOrSpace())
					{
						rt = true;
					}
				}
				return rt;
			}
		}

		public List<RuleKeyword> Keywords
		{
			get
			{
				var kws = new List<RuleKeyword>();
				for (int i = 0; i < _spanel.Children.Count; i++)
				{
					var ctlOneKeyword = _spanel.Children[i] as CtlOneKeyword;
					if (ctlOneKeyword != null && !ctlOneKeyword.Keyword.Keyword.xIsNullOrEmptyOrSpace())
					{
						kws.Add(ctlOneKeyword.Keyword);
					}
				}
				return kws;
			}
			set
			{
				_spanel.Children.Clear();
				if (value.xIsNullOrEmpty())
				{
					InitUI();
				}
				else
				{
					foreach (var kw in value)
					{
						InitUI(kw);
					}
				}
			}
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

		private CtlOneKeyword InitUI(RuleKeyword keyword = null, object prevCtl = null)
		{
			var ctlOneKeyword = new CtlOneKeyword(keyword);
			ctlOneKeyword.EvAdd += ctlOneKeyword_EvAdd;
            ctlOneKeyword.EvRemove += ctlOneKeyword_EvRemove;
			ctlOneKeyword.Margin = new Thickness(5.0);
			if (prevCtl == null)
			{
				_spanel.Children.Add(ctlOneKeyword);
			}
			else
			{
				int index = _spanel.Children.IndexOf(prevCtl as UIElement) + 1;
				_spanel.Children.Insert(index, ctlOneKeyword);
			}
			RefreshUI();
			return ctlOneKeyword;
		}

		private void RefreshUI()
		{
			for (int i = 0; i < _spanel.Children.Count; i++)
			{
				var ctlOneKeyword = _spanel.Children[i] as CtlOneKeyword;
				if (ctlOneKeyword != null)
				{
					ctlOneKeyword.SetTitle("关键字", i + 1);
				}
			}
		}

        private void ctlOneKeyword_EvRemove(object sender, RoutedEventArgs e)
		{
			RemoveOneKeyword(sender);
		}

		private void RemoveOneKeyword(object ctlOneKeyword)
		{
			if (ctlOneKeyword != null)
			{
				_spanel.Children.Remove(ctlOneKeyword as UIElement);
				if (_spanel.Children.Count == 0)
				{
					InitUI();
				}
				RefreshUI();
			}
		}

        private void ctlOneKeyword_EvAdd(object sender, RoutedEventArgs e)
		{
			InitUI(null, sender);
		}

		public void FocusFirstKeyword()
		{
			if (_spanel.Children.Count > 0)
			{
				var ctlOneKeyword = _spanel.Children[0] as CtlOneKeyword;
				ctlOneKeyword.FocusKeyword();
			}
		}

	}
}
