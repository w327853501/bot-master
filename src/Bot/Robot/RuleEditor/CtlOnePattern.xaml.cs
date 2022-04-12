using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Bot.AI.WordSplitterNs.ElementParser;
using Bot.Asset;
using Bot.Common;
using Bot.Common.EmojiInputer;
using DbEntity;
using BotLib;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf.Extensions;
using Xceed.Wpf.Toolkit;

namespace Bot.Robot.RuleEditor
{
	public partial class CtlOnePattern : UserControl
    {
        private const double _lineHeight = 20.0;
        private const int _showLinesAtFocus = 10;
        private string _sellerMain;
        private bool _isEditMode;
        private List<RuleKeyword> _preKeys;
        private EmojiParser _eParser;
        private List<IndexRange> _emojiRanges;
        private WatermarkTextBox _tbox;
        private WatermarkTextBox _preLostFocusTextBox;
        public static RoutedEvent EvAddEvent { get; set; }
        public static RoutedEvent EvRemoveEvent { get; set; }

		static CtlOnePattern()
		{
			EvAddEvent = EventManager.RegisterRoutedEvent("EvAdd", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOnePattern));
			EvRemoveEvent = EventManager.RegisterRoutedEvent("EvRemove", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOnePattern));
		}
		public CtlOnePattern(QuestionPattern pattern, string sellerMain)
            : this(sellerMain)
		{
            Pattern = pattern; 
			btnAdd.ToolTip = "新增 模板 (在下方)";
			btnRemove.ToolTip = "删除 本模板";
		}

        public CtlOnePattern(string sellerMain)
		{
			_isEditMode = false;
			_eParser = new EmojiParser();
			InitializeComponent();
            _sellerMain = sellerMain;
			if (!string.IsNullOrEmpty(sellerMain))
			{
				expExclude.xIsVisible(Params.Robot.GetRuleIncludeExcept(sellerMain));
			}
			ExitEditMode(true);
		}

		public void SetTitle(string title, int no)
		{
			tbkTitle.Text = string.Format("{0}{1}：", title, no);
			gboxEditMode.Header = string.Format("{0}{1}", title, no);
		}

		public QuestionPattern Pattern
		{
			get
			{
				return new QuestionPattern
				{
					Keywords = ctlKeys.Keywords.ToArray(),
					Except = GetExceptKeywords(),
					QuestionMaxLength = GetQuestionMaxLength()
				};
			}
			set
			{
				ctlKeys.Keywords = ((value != null) ? value.Keywords.ToList() : null);
				ShowExclude((value != null) ? value.Except : null);
				ShowCharLimit((value != null) ? value.QuestionMaxLength : 0);
				tboxContent.Text = QuestionPattern.ConvertPatternToString(value);
			}
		}

		private void ShowExclude(RuleKeyword[] kws)
		{
			if (kws.xIsNullOrEmpty())
			{
				ctlExclude.Keywords = new List<RuleKeyword>();
				expExclude.IsExpanded = false;
			}
			else
			{
				ctlExclude.Keywords = kws.ToList();
				expExclude.IsExpanded = true;
			}
		}

		private void ShowCharLimit(int len)
		{
			if (len > 0)
			{
				tboxCharLimit.Text = len.ToString();
			}
			else
			{
				tboxCharLimit.Text = "";
			}
		}

		private int GetQuestionMaxLength()
		{
			int maxQstLen = 0;
			try
			{
				var charLimit = tboxCharLimit.Text.Trim();
				maxQstLen = (string.IsNullOrEmpty(charLimit) ? 0 : Convert.ToInt32(charLimit));
				if (maxQstLen < 0)
				{
					maxQstLen = SetCharLimit(0);
				}
				else if (maxQstLen > 1000)
				{
					maxQstLen = SetCharLimit(1000);
				}
			}
			catch
			{
				maxQstLen = SetCharLimit(0);
			}
			return maxQstLen;
		}

		private RuleKeyword[] GetExceptKeywords()
		{
			if (ctlExclude.Keywords == null || ctlExclude.Keywords.Count < 1) return null;
			return ctlExclude.Keywords.ToArray();
		}

		private void IntoEditMode()
		{
			if (!_isEditMode)
			{
				_preKeys = Util.Clone<List<RuleKeyword>>(ctlKeys.Keywords);
				_isEditMode = true;
				base.Height = 200;
                grdReadMode.Visibility = Visibility.Collapsed;
                gboxEditMode.Visibility = Visibility.Visible;
				ctlKeys.FocusFirstKeyword();
			}
		}


		private void ExitEditMode(bool init = false)
		{
			if (init || _isEditMode)
			{
				_isEditMode = false;
				Height = 20;
                grdReadMode.Visibility = Visibility.Visible;
                gboxEditMode.Visibility = Visibility.Collapsed;
				tboxContent.Text = QuestionPattern.ConvertPatternToString(Pattern);
			}
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

		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			RaiseAddEvent();
		}

		private void btnRemove_Click(object sender, RoutedEventArgs e)
		{
			RaiseRemoveEvent();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			ctlKeys.Keywords = _preKeys;
			ExitEditMode();
		}

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			ExitEditMode();
		}

		private void tboxContent_GotFocus(object sender, RoutedEventArgs e)
		{
			IntoEditMode();
		}

		private void gboxEditMode_LostFocus(object sender, RoutedEventArgs e)
		{
			IInputElement focusedElement = Keyboard.FocusedElement;
			if (!gboxEditMode.xContainsDescendant(focusedElement))
			{
				ExitEditMode();
			}
		}

		private void btnEmoji_Click(object sender, RoutedEventArgs e)
		{
			if (_preLostFocusTextBox == null)
			{
				MsgBox.ShowTip("请先点击一个文本框", null, string.Empty);
			}
			else
			{
				WndEmojiInputer.MyShow(this.xFindParentWindow(), emojiText=> {
					if (!string.IsNullOrEmpty(emojiText))
					{
						_preLostFocusTextBox.xInsertOrAppend(emojiText);
					}
				});
			}
		}

		private void ctlKeywordGroup_TextChanged(object sender, TextChangedEventArgs e)
		{
			ExtractEmojiRanges(e.OriginalSource as WatermarkTextBox);
		}

		private void ctlKeywordGroup_SelectionChanged(object sender, RoutedEventArgs e)
		{
			var tbox = e.OriginalSource as WatermarkTextBox;
			if (tbox != null)
			{
				if (_tbox != tbox)
				{
					ExtractEmojiRanges(tbox);
				}
				int selectionStart = tbox.SelectionStart;
				bool showEmoji = false;
				if (selectionStart >= 0 && !_emojiRanges.xIsNullOrEmpty<IndexRange>())
				{
					foreach (IndexRange indexRange in _emojiRanges)
					{
						if (selectionStart >= indexRange.Start && selectionStart < indexRange.NextStart)
						{
							var rect = EmojiHelper.FindEmojisRect(tbox.Text.Substring(indexRange.Start, indexRange.Length));
							CroppedEmoji(rect);
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

		private void ExtractEmojiRanges(WatermarkTextBox txtbox)
		{
			if (txtbox != null)
			{
				_tbox = txtbox;
				_emojiRanges = _eParser.GetExceptRanges(_tbox.Text);
			}
		}

		private void ctlKeys_LostFocus(object sender, RoutedEventArgs e)
		{
			var ctlKeys = e.OriginalSource as WatermarkTextBox;
			if (ctlKeys != null)
			{
				_preLostFocusTextBox = ctlKeys;
			}
		}

		private void ctlExclude_SelectionChanged(object sender, RoutedEventArgs e)
		{
		}

		private void ctlExclude_TextChanged(object sender, TextChangedEventArgs e)
		{
		}

		private void ctlExclude_LostFocus(object sender, RoutedEventArgs e)
		{
		}

		private void tboxCharLimit_LostFocus(object sender, RoutedEventArgs e)
		{
			GetQuestionMaxLength();
		}

		private int SetCharLimit(int len)
		{
			tboxCharLimit.Text = len.ToString();
			Util.Beep();
			return len;
		}


    }
}
