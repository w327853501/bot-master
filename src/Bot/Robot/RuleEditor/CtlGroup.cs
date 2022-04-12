using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BotLib.Extensions;

namespace Bot.Robot.RuleEditor
{
	public class CtlGroup : ScrollViewer
    {
        private StackPanel _spanel;
        private int _visibleRows;
        private double _rowHeight;
        private SolidColorBrush _editorBackground;
        public static readonly DependencyProperty TitleProperty;

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

		public SolidColorBrush EditorBackground
		{
			get
			{
				return _editorBackground;
			}
			set
			{
				_editorBackground = value;
				foreach (CtlGroupItem ctlGroupItem in _spanel.Children)
				{
					if (ctlGroupItem != null)
					{
						ctlGroupItem.tboxContent.Background = _editorBackground;
					}
				}
			}
		}

        static CtlGroup()
        {
            CtlGroup.TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(CtlGroup), new PropertyMetadata("", new PropertyChangedCallback((d,e)=> {
				var ctlGroup = d as CtlGroup;
				if (ctlGroup != null)
				{
					ctlGroup.RefreshUI();
				}
			})));
        }

		public CtlGroup()
		{
			_spanel = new StackPanel();
			_visibleRows = -1;
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			AddChild(_spanel);
			InitUI();
			Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			_rowHeight = DesiredSize.Height;
			VisibleRows = 10;
		}

		public void Init(IEnumerable<string> slist)
		{
			_spanel.Children.Clear();
			if (slist == null)
			{
				InitUI();
			}
			else
			{
				foreach (var txt in slist)
				{
					InitUI(txt);
				}
			}
		}

		public string Title
		{
			get
			{
				return (string)GetValue(CtlGroup.TitleProperty);
			}
			set
			{
				SetValue(CtlGroup.TitleProperty, value);
			}
		}

		public new bool HasContent
		{
			get
			{
				var rt = false;
				if (Texts == null || Texts.Count == 0) return rt;
				foreach (string s in Texts)
				{
					if (!s.xIsNullOrEmptyOrSpace())
					{
						rt = true;
						break;
					}
				}
				return rt;
			}
		}

		public List<string> Texts
		{
			get
			{
				var txts = new List<string>();
				for (var i = 0; i < _spanel.Children.Count; i++)
				{
					var ctlGroupItem = _spanel.Children[i] as CtlGroupItem;
					if (ctlGroupItem != null)
					{
						txts.Add(ctlGroupItem.Text.Trim());
					}
				}
				return txts;
			}
			set
			{
				_spanel.Children.Clear();
				foreach (string text in value)
				{
					var ctlGroupItem = InitUI();
					ctlGroupItem.Text = text;
				}
			}
		}

		public event RoutedEventHandler EvRemove
		{
			add
			{
				AddHandler(CtlGroupItem.EvRemoveEvent, value);
			}
			remove
			{
				RemoveHandler(CtlGroupItem.EvRemoveEvent, value);
			}
		}

		public event RoutedEventHandler EvAdd
		{
			add
			{
				AddHandler(CtlGroupItem.EvAddEvent, value);
			}
			remove
			{
				RemoveHandler(CtlGroupItem.EvAddEvent, value);
			}
		}

		public event RoutedEventHandler EvIntoEditMode
		{
			add
			{
				AddHandler(CtlGroupItem.EvIntoEditModeEvent, value);
			}
			remove
			{
				RemoveHandler(CtlGroupItem.EvIntoEditModeEvent, value);
			}
		}

		public event RoutedEventHandler EvExitEditMode
		{
			add
			{
				AddHandler(CtlGroupItem.EvExitEditModeEvent, value);
			}
			remove
			{
				RemoveHandler(CtlGroupItem.EvExitEditModeEvent, value);
			}
		}

		public event CtlGroupItem.RoutedValidateEventHandler EvValidate
		{
			add
			{
				AddHandler(CtlGroupItem.EvValidateEvent, value);
			}
			remove
			{
				RemoveHandler(CtlGroupItem.EvValidateEvent, value);
			}
		}

		public event RoutedEventHandler EvSubmit
		{
			add
			{
				AddHandler(CtlGroupItem.EvSubmitEvent, value);
			}
			remove
			{
				RemoveHandler(CtlGroupItem.EvSubmitEvent, value);
			}
		}

		public bool HasSyntaxError()
		{
			foreach (CtlGroupItem ctlGroupItem in _spanel.Children)
			{
				if (ctlGroupItem.HasSyntaxError())
				{
					return true;
				}
			}
			return false;
		}

		private CtlGroupItem InitUI(string content = "", object prevCtl = null)
		{
			var ctlGroupItem = new CtlGroupItem(Title);
			ctlGroupItem.EvAdd += ctlGroupItem_EvAdd;
			ctlGroupItem.EvRemove += ctlGroupItem_EvRemove;
			ctlGroupItem.Margin = new Thickness(5.0);
			ctlGroupItem.Text = content;
			if (EditorBackground != null)
			{
				ctlGroupItem.tboxContent.Background = EditorBackground;
			}
			if (prevCtl == null)
			{
				_spanel.Children.Add(ctlGroupItem);
			}
			else
			{
				int index = _spanel.Children.IndexOf(prevCtl as UIElement) + 1;
				_spanel.Children.Insert(index, ctlGroupItem);
			}
			RefreshUI();
			return ctlGroupItem;
		}

		private void RefreshUI()
		{
			for (int i = 0; i < _spanel.Children.Count; i++)
			{
				CtlGroupItem ctlGroupItem = _spanel.Children[i] as CtlGroupItem;
				if (ctlGroupItem != null)
				{
					ctlGroupItem.SetTitle(i + 1, Title);
				}
			}
		}

		private void ctlGroupItem_EvRemove(object sender, RoutedEventArgs e)
		{
			RemoveGroupItem(sender);
		}

		private void RemoveGroupItem(object ctlGroupItem)
		{
			if (ctlGroupItem != null)
			{
				_spanel.Children.Remove(ctlGroupItem as UIElement);
				if (_spanel.Children.Count == 0)
				{
					InitUI("", null);
				}
				RefreshUI();
			}
		}

		private void ctlGroupItem_EvAdd(object sender, RoutedEventArgs e)
		{
			InitUI("", sender);
		}

		public void FocusFirstItem()
		{
			if (_spanel.Children.Count > 0)
			{
				var ctlGroupItem = _spanel.Children[0] as CtlGroupItem;
				ctlGroupItem.FocusContent();
			}
		}

	}
}
