using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DbEntity;
using BotLib.Extensions;

namespace Bot.Robot.RuleEditor
{
	public class CtlPatternGroup : ScrollViewer
    {
        private const string Title = "模板";
        private StackPanel _spanel;
        private double _rowHeight;
        private int _visibleRows;
        private string _sellerMain;

		public CtlPatternGroup()
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
				VisibleRows = 10;
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

		public void Init(QuestionPattern[] pts, string sellerMain)
		{
			_sellerMain = sellerMain;
			_spanel.Children.Clear();
			if (pts == null)
			{
				InitUI();
				return;
			}
			foreach (var pattern in pts)
			{
				InitUI(pattern);
			}
			
		}

		public QuestionPattern[] Patterns
		{
			get
			{
				var patterns = new List<QuestionPattern>();
				for (int i = 0; i < _spanel.Children.Count; i++)
				{
					var ctlOnePattern = _spanel.Children[i] as CtlOnePattern;
					if (ctlOnePattern != null)
					{
						patterns.Add(ctlOnePattern.Pattern);
					}
				}
				return patterns.ToArray();
			}
			set
			{
				_spanel.Children.Clear();
				foreach (var pt in value.xSafeForEach())
				{
					InitUI(pt);
				}
			}
		}

		public event RoutedEventHandler EvRemove
		{
			add
			{
				AddHandler(CtlOnePattern.EvRemoveEvent, value);
			}
			remove
			{
				RemoveHandler(CtlOnePattern.EvRemoveEvent, value);
			}
		}

		public event RoutedEventHandler EvAdd
		{
			add
			{
				AddHandler(CtlOnePattern.EvAddEvent, value);
			}
			remove
			{
				RemoveHandler(CtlOnePattern.EvAddEvent, value);
			}
		}

		private CtlOnePattern InitUI(QuestionPattern pattern = null, object prevCtl = null)
		{
			var ctlOnePattern = new CtlOnePattern(pattern, _sellerMain);
			ctlOnePattern.EvAdd += ctlOnePattern_EvAdd;
			ctlOnePattern.EvRemove += ctlOnePattern_EvRemove;
			ctlOnePattern.Margin = new Thickness(5.0);
			if (prevCtl == null)
			{
				_spanel.Children.Add(ctlOnePattern);
			}
			else
			{
				var idx = _spanel.Children.IndexOf(prevCtl as UIElement) + 1;
				_spanel.Children.Insert(idx, ctlOnePattern);
			}
			RefreshUI();
			return ctlOnePattern;
		}

		private void RefreshUI()
		{
			for (var i = 0; i < _spanel.Children.Count; i++)
			{
				var ctlOnePattern = _spanel.Children[i] as CtlOnePattern;
				if (ctlOnePattern != null)
				{
					ctlOnePattern.SetTitle("模板", i + 1);
				}
			}
		}

		private void ctlOnePattern_EvRemove(object sender, RoutedEventArgs e)
		{
			RemoveOnePattern(sender);
		}

		private void RemoveOnePattern(object ctlOnePattern)
		{
			if (ctlOnePattern != null)
			{
				_spanel.Children.Remove(ctlOnePattern as UIElement);
				if (_spanel.Children.Count == 0)
				{
					InitUI();
				}
				RefreshUI();
			}
		}

		private void ctlOnePattern_EvAdd(object sender, RoutedEventArgs e)
		{
			InitUI(null, sender);
		}

	}
}
