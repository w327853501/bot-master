using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Bot.AI.WordSplitterNs.ElementParser;
using Bot.Asset;
using Bot.Common.EmojiInputer;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf.Controls;
using BotLib.Wpf.Extensions;

namespace Bot.Robot.RuleEditor
{
	public partial class CtlGroupItem : UserControl
	{       
		private string _title;
		private DelayCaller _delayValidater;
		private string _preTextChangedText;
		private readonly double _lineHeight;
		private readonly int _showLinesAtFocus;
		private CtlRichTextBox.SyntaxErrorException _errDesc;
		public static readonly DependencyProperty TitleProperty;
		private bool _isAddButtonEnabled;
		private bool _isRemoveButtonEnabled;
		private string _preText;
		private bool _isEditMode;
		private EmojiParser _eParser;
		private List<IndexRange> _emojiRanges;
		public delegate void RoutedValidateEventHandler(object sender, RoutedValidateEventArgs e);
        public static RoutedEvent EvRemoveEvent { get; set; }
        public static RoutedEvent EvAddEvent { get; set; }
        public static RoutedEvent EvIntoEditModeEvent { get; set; }
        public static RoutedEvent EvExitEditModeEvent { get; set; }
        public static RoutedEvent EvSubmitEvent { get; set; }
        public static RoutedEvent EvValidateEvent { get; set; }

        static CtlGroupItem()
        {
            EvAddEvent = EventManager.RegisterRoutedEvent("EvAdd", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlGroupItem));
            EvRemoveEvent = EventManager.RegisterRoutedEvent("EvRemove", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlGroupItem));
            EvIntoEditModeEvent = EventManager.RegisterRoutedEvent("EvIntoEditMode", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlGroupItem));
            EvExitEditModeEvent = EventManager.RegisterRoutedEvent("EvExitEditMode", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlGroupItem));
            EvValidateEvent = EventManager.RegisterRoutedEvent("EvValidate", RoutingStrategy.Bubble, typeof(RoutedValidateEventHandler), typeof(CtlGroupItem));
            EvSubmitEvent = EventManager.RegisterRoutedEvent("EvSubmit", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlGroupItem));
            TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(CtlGroupItem), new FrameworkPropertyMetadata("", new PropertyChangedCallback((d,e)=> {
				var ctlGroupItem = d.xFindAncestorFromMe<CtlGroupItem>();
				if (ctlGroupItem != null)
				{
					ctlGroupItem.btnAdd.ToolTip = "新增 " + ctlGroupItem._title;
					ctlGroupItem.btnRemove.ToolTip = "删除本 " + ctlGroupItem._title;
				}
			})));
        }

		public CtlGroupItem(string title = "")
		{
			_lineHeight = 20.0;
			_showLinesAtFocus = 5;
			_errDesc = null;
			_isAddButtonEnabled = true;
			_isRemoveButtonEnabled = true;
			_preText = "";
			_isEditMode = false;
			_eParser = new EmojiParser();
			InitializeComponent();
			_title = title;
			Height = _lineHeight;
			tboxContent.TextChanged += tboxContent_TextChanged;
			_delayValidater = new DelayCaller(()=> {
				string text = tboxContent.Text;
				if (text != _preTextChangedText)
				{
					_preTextChangedText = text;
					RaiseValidateEvent();
				}
			}, 500, true);
			btnAdd.ToolTip = string.Format("新增 {0} (在下方)", title);
			btnRemove.ToolTip = string.Format("删除{0} {1}", string.IsNullOrEmpty(title) ? "" : "本", title);
		}

		public string Text
		{
			get
			{
				return tboxContent.Text;
			}
			set
			{
				tboxContent.Text = value;
			}
		}

		public CtlRichTextBox.SyntaxErrorException SyntaxError
		{
			get
			{
				return _errDesc;
			}
			set
			{
				_errDesc = value;
				if (value == null)
				{
					tbkError.Visibility = Visibility.Collapsed;
				}
				else
				{
					tbkError.Text = value.Error;
					tbkError.Visibility = Visibility.Visible;
				}
			}
		}

		public bool HasSyntaxError()
		{
			return SyntaxError != null;
		}

		public event RoutedEventHandler EvAdd
		{
			add
			{
				AddHandler(EvAddEvent, value);
			}
			remove
			{
				RemoveHandler(EvAddEvent, value);
			}
		}

		protected RoutedEventArgs RaiseAddEvent()
		{
			var args = new RoutedEventArgs();
			args.RoutedEvent = EvAddEvent;
			args.Source = this;
			RaiseEvent(args);
			return args;
		}

		public event RoutedEventHandler EvRemove
		{
			add
			{
				AddHandler(EvRemoveEvent, value);
			}
			remove
			{
				RemoveHandler(EvRemoveEvent, value);
			}
		}

		protected RoutedEventArgs RaiseRemoveEvent()
		{
			var args = new RoutedEventArgs();
			args.RoutedEvent = EvRemoveEvent;
			RaiseEvent(args);
			return args;
		}

		public event RoutedEventHandler EvIntoEditMode
		{
			add
			{
				AddHandler(EvIntoEditModeEvent, value);
			}
			remove
			{
				RemoveHandler(EvIntoEditModeEvent, value);
			}
		}

		protected RoutedEventArgs RaiseIntoEditModeEvent()
		{
			var args = new RoutedEventArgs();
			args.RoutedEvent = EvIntoEditModeEvent;
			RaiseEvent(args);
			return args;
		}

		public event RoutedEventHandler EvExitEditMode
		{
			add
			{
				AddHandler(EvExitEditModeEvent, value);
			}
			remove
			{
				RemoveHandler(EvExitEditModeEvent, value);
			}
		}

		protected RoutedEventArgs RaiseExitEditModeEvent()
		{
			var args = new RoutedEventArgs();
			args.RoutedEvent = EvExitEditModeEvent;
			RaiseEvent(args);
			return args;
		}

		public event RoutedValidateEventHandler EvValidate
		{
			add
			{
				AddHandler(EvValidateEvent, value);
			}
			remove
			{
				RemoveHandler(EvValidateEvent, value);
			}
		}

		protected RoutedValidateEventArgs RaiseValidateEvent()
		{
			var args = new RoutedValidateEventArgs();
			args.RoutedEvent = EvValidateEvent;
			RaiseEvent(args);
			SyntaxError = args.SyntaxErrorExp;
			return args;
		}

		public void FocusContent()
		{
			tboxContent.Focus();
		}

		public event RoutedEventHandler EvSubmit
		{
			add
			{
				AddHandler(EvSubmitEvent, value);
			}
			remove
			{
				RemoveHandler(EvSubmitEvent, value);
			}
		}

		protected RoutedEventArgs RaiseSubmitEvent()
		{
			var args = new RoutedEventArgs();
			args.RoutedEvent = EvSubmitEvent;
			RaiseEvent(args);
			return args;
		}


		public void SetTitle(int no, string title)
		{
			_title = title;
			tbkTitle.Text = _title + no + "：";
		}

		public bool IsAddButtonEnabled
		{
			get
			{
				return _isAddButtonEnabled;
			}
			set
			{
				if (value != _isAddButtonEnabled)
				{
					btnAdd.IsEnabled = value;
					_isAddButtonEnabled = value;
				}
			}
		}

		public bool IsRemoveButtonEnabled
		{
			get
			{
				return _isRemoveButtonEnabled;
			}
			set
			{
				if (_isRemoveButtonEnabled != value)
				{
					_isRemoveButtonEnabled = value;
					btnRemove.IsEnabled = value;
				}
			}
		}

		private void tboxContent_GotFocus(object sender, RoutedEventArgs e)
		{
			IntoEditMode();
		}

		private void IntoEditMode()
		{
			if (!_isEditMode)
			{
				_isEditMode = true;
				_preText = tboxContent.Text;
				Height = _lineHeight * (double)_showLinesAtFocus;
				spAddRemoveButtons.Visibility = Visibility.Collapsed;
				spRow1.Visibility = Visibility.Visible;
				tbkError.Visibility = Visibility.Collapsed;
				RaiseIntoEditModeEvent();
			}
		}

		private void ExitEditMode()
		{
			if (_isEditMode)
			{
				_isEditMode = false;
				Height = _lineHeight;
				spRow1.Visibility = Visibility.Collapsed;
				spAddRemoveButtons.Visibility = Visibility.Visible;
				RaiseExitEditModeEvent();
			}
		}

		private void tboxContent_LostFocus(object sender, RoutedEventArgs e)
		{
			IInputElement focusedElement = Keyboard.FocusedElement;
			if (focusedElement != btnOk && focusedElement != btnCancel && focusedElement != btnEmoji && _isEditMode)
			{
				Submit();
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

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			Submit();
		}

		private void Submit()
		{
			RaiseValidateEvent();
			if (SyntaxError == null)
			{
				ExitEditMode();
				RaiseSubmitEvent();
			}
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			tboxContent.Text = _preText;
			ExitEditMode();
		}

		private void tboxContent_SelectionChanged(object sender, RoutedEventArgs e)
		{
			int selectionStart = tboxContent.SelectionStart;
			var showEmoji = false;
			if (selectionStart >= 0 && !_emojiRanges.xIsNullOrEmpty<IndexRange>())
			{
				foreach (var rng in _emojiRanges)
				{
					if (selectionStart >= rng.Start && selectionStart < rng.NextStart)
					{
						var emojiRect = EmojiHelper.FindEmojisRect(tboxContent.Text.Substring(rng.Start, rng.Length));
						CroppedEmoji(emojiRect);
						showEmoji = true;
						break;
					}
				}
			}
			if (!showEmoji)
			{
				imgEmojiSelected.Visibility = Visibility.Collapsed;
			}
		}

		private void tboxContent_TextChanged(object sender, TextChangedEventArgs e)
		{
			_emojiRanges = _eParser.GetExceptRanges(tboxContent.Text);

			if (SyntaxError != null)
			{
				_delayValidater.CallAfterDelay();
			}
		}

		private void CroppedEmoji(Int32Rect? rect)
		{
			if (rect == null)
			{
				imgEmojiSelected.Visibility = Visibility.Collapsed;
			}
			else
			{
				var source = new CroppedBitmap(AssetImageHelper.GetImageFromWpfCache(AssetImageEnum.imgEmojiAll), rect.Value);
				imgEmojiSelected.Source = source;
				imgEmojiSelected.Visibility = Visibility.Visible;
			}
		}

		private void btnEmoji_Click(object sender, RoutedEventArgs e)
		{
            WndEmojiInputer.MyShow(this.xFindParentWindow(), txt => {
                if (!string.IsNullOrEmpty(txt))
                {
                    tboxContent.xInsertOrAppend(txt);
                }
            });
		}

		public class RoutedValidateEventArgs : RoutedEventArgs
		{
			public string ErrorDesc { get; set; }
			public CtlRichTextBox.SyntaxErrorException SyntaxErrorExp { get; set; }
		}
    }
}
