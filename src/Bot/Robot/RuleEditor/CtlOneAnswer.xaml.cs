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
using Bot.Common;
using Bot.Common.EmojiInputer;
using DbEntity;
using BotLib;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf.Controls;
using BotLib.Wpf.Extensions;
using Xceed.Wpf.Toolkit;
using Bot.Common.ImageHelper;

namespace Bot.Robot.RuleEditor
{
	public partial class CtlOneAnswer : UserControl
	{
        private string _title;
        private DelayCaller _delayValidater;
        public List<string> AddNewImageNames;
        public List<string> DeleteImageNames;
        private string _preTextChangedText;
        private readonly double _lineHeight;
        private readonly int _showLinesAtFocus;
        private CtlRichTextBox.SyntaxErrorException _errDesc;
        public static readonly DependencyProperty TitleProperty;
        private bool _isAddButtonEnabled;
        private bool _isRemoveButtonEnabled;
        private string _preText;
        private string _prePartFn;
        private bool _isEditMode;
        private EmojiParser _eParser;
        private List<IndexRange> _emojiRanges;
        public static RoutedEvent EvAddEvent { get; set; }
        public static RoutedEvent EvRemoveEvent { get; set; }
        public static RoutedEvent EvExitEditModeEvent { get; set; }
        public static RoutedEvent EvIntoEditModeEvent { get; set; }
        public static RoutedEvent EvValidateEvent { get; set; }
        public static RoutedEvent EvSubmitEvent { get; set; }


        static CtlOneAnswer()
        {
            EvAddEvent = EventManager.RegisterRoutedEvent("EvAdd", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOneAnswer));
            EvRemoveEvent = EventManager.RegisterRoutedEvent("EvRemove", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOneAnswer));
            EvIntoEditModeEvent = EventManager.RegisterRoutedEvent("EvIntoEditMode", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOneAnswer));
            EvExitEditModeEvent = EventManager.RegisterRoutedEvent("EvExitEditMode", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOneAnswer));
            EvValidateEvent = EventManager.RegisterRoutedEvent("EvValidate", RoutingStrategy.Bubble, typeof(RoutedValidateEventHandler), typeof(CtlOneAnswer));
            EvSubmitEvent = EventManager.RegisterRoutedEvent("EvSubmit", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CtlOneAnswer));
            TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(CtlOneAnswer), new FrameworkPropertyMetadata("", new PropertyChangedCallback((d,e)=> {
				var ctlOneAnswer = d.xFindAncestorFromMe<CtlOneAnswer>();
				if (ctlOneAnswer != null)
				{
					ctlOneAnswer.btnAdd.ToolTip = "新增 " + ctlOneAnswer._title;
					ctlOneAnswer.btnRemove.ToolTip = "删除本 " + ctlOneAnswer._title;
				}
			})));
        }

		public CtlOneAnswer(AnswerWithImage answer)
		{
			_title = "答案";
			AddNewImageNames = new List<string>();
			DeleteImageNames = new List<string>();
			_lineHeight = 20.0;
			_showLinesAtFocus = 5;
			_errDesc = null;
			_isAddButtonEnabled = true;
			_isRemoveButtonEnabled = true;
			_preText = "";
			_isEditMode = false;
			_eParser = new EmojiParser();
			InitializeComponent();
			Height = _lineHeight;
			tboxContent.TextChanged += tboxContent_TextChanged;
			_delayValidater = new DelayCaller(()=> {
				var text = tboxContent.Text;
				if (text != _preTextChangedText)
				{
					_preTextChangedText = text;
					RaiseValidateEvent();
				}
			}, 500, true);
			btnAdd.ToolTip = string.Format("新增 {0} (在下方)", _title);
			btnRemove.ToolTip = string.Format("删除{0} {1}", string.IsNullOrEmpty(_title) ? "" : "本", _title);
			Answer = answer;
		}

		public AnswerWithImage Answer
		{
			get
			{
				return new AnswerWithImage
				{
					Answer = tboxContent.Text,
					ImageName = (imgAnswer.Tag as string)
				};
			}
			set
			{
				if (value != null)
				{
					tboxContent.Text = string.IsNullOrEmpty(value.Answer) ? string.Empty : value.Answer;
					ShowAnswerImage((value != null) ? value.ImageName : null);
				}
			}
		}

		private void tboxContent_TextChanged(object sender, TextChangedEventArgs e)
		{
			_emojiRanges = _eParser.GetExceptRanges(tboxContent.Text);
			ShowErrorTip();
			if (SyntaxError != null)
			{
				_delayValidater.CallAfterDelay();
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

		public bool IsEmptyOrHasSyntaxError()
		{
			return SyntaxError != null || ShowErrorTip();
		}

		private bool ShowErrorTip()
		{
			var rt = string.IsNullOrEmpty(tboxContent.Text.Trim()) && string.IsNullOrEmpty(imgAnswer.Tag as string);
			if (rt)
			{
				ShowTip("文字和图片不能同时为空");
			}
			else
			{
				ShowTip(null);
			}
			return rt;
		}

		private void ShowTip(string tip)
		{
			if (string.IsNullOrEmpty(tip))
			{
				tbkError.Visibility = Visibility.Collapsed;
			}
			else
			{
				tbkError.Visibility = Visibility.Visible;
				tbkError.Text = tip;
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

		public void SetTitle(string title, int no)
		{
			_title = title;
			tbkTitle.Text = _title + no + "：";
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

		protected RoutedEventArgs RaiseInfoEditModeEvent()
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
			InfoEditMode();
		}

		private void InfoEditMode()
		{
			if (!_isEditMode)
			{
				_isEditMode = true;
				_preText = tboxContent.Text;
				_prePartFn = (imgAnswer.Tag as string);
				Height = _lineHeight * (double)_showLinesAtFocus * 2.0;
				spAddRemoveButtons.Visibility = Visibility.Collapsed;
				spRow1.Visibility = Visibility.Visible;
				imgAnswer.Visibility = Visibility.Visible;
				ShowAnswerImage(imgAnswer.Tag as string);
				ShowErrorTip();
				RaiseInfoEditModeEvent();
			}
		}

		private void ExitEditMode()
		{
			if (_isEditMode)
			{
				_isEditMode = false;
				Height = _lineHeight;
				spRow1.Visibility = Visibility.Collapsed;
				imgAnswer.Visibility = Visibility.Collapsed;
				spAddRemoveButtons.Visibility = Visibility.Visible;
				RaiseExitEditModeEvent();
				imgTip.xIsVisible(imgAnswer.Tag != null);
			}
		}

		private void tboxContent_LostFocus(object sender, RoutedEventArgs e)
		{
			IInputElement focusedElement = Keyboard.FocusedElement;
			if (focusedElement != btnOk && focusedElement != btnCancel && focusedElement != btnEmoji && _isEditMode)
			{
				Submit(false);
			}
		}

		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			RaiseAddEvent();
		}

		private void btnRemove_Click(object sender, RoutedEventArgs e)
		{
			RuleAnswerImageHelper.DeleteImages(AddNewImageNames);
			RuleAnswerImageHelper.DeleteImages(DeleteImageNames);
			RaiseRemoveEvent();
		}

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			Submit(true);
		}

		private void Submit(bool isExitEditMode)
		{
			RaiseValidateEvent();
			if (!IsEmptyOrHasSyntaxError())
			{
				if (isExitEditMode)
				{
					ExitEditMode();
				}
				RaiseSubmitEvent();
			}
			else
			{
				Util.Beep();
			}
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			tboxContent.Text = _preText;
			string text = imgAnswer.Tag as string;
			imgAnswer.Tag = _prePartFn;
			if (!string.IsNullOrEmpty(text))
			{
				AddNewImageNames.Remove(text);
				DeleteImageNames.Remove(text);
			}
			if (!string.IsNullOrEmpty(_prePartFn))
			{
				DeleteImageNames.Remove(_prePartFn);
			}
			ExitEditMode();
		}

		private void tboxContent_SelectionChanged(object sender, RoutedEventArgs e)
		{
			int selectionStart = tboxContent.SelectionStart;
			bool showEmoji = false;
			if (selectionStart >= 0 && !_emojiRanges.xIsNullOrEmpty<IndexRange>())
			{
				foreach (IndexRange indexRange in _emojiRanges)
				{
					if (selectionStart >= indexRange.Start && selectionStart < indexRange.NextStart)
					{
						Int32Rect? nullable_ = EmojiHelper.FindEmojisRect(tboxContent.Text.Substring(indexRange.Start, indexRange.Length));
						CroppedEmoji(nullable_);
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
			WndEmojiInputer.MyShow(this.xFindParentWindow(), text => {
				if (!string.IsNullOrEmpty(text))
				{
					tboxContent.xInsertOrAppend(text);
				}
			});
		}

		private void ShowAnswerImage(string imageName)
		{
			if (string.IsNullOrEmpty(imageName) || DeleteImageNames.Contains(imageName))
			{
				imgTip.Visibility = Visibility.Collapsed;
				imgAnswer.Tag = null;
				btnAddImage.Visibility = Visibility.Visible;
				btnAddImage.IsEnabled = true;
				btnUpdateImage.Visibility = Visibility.Collapsed;
				imgAnswer.Source = null;
			}
			else
			{
				imgTip.Visibility = Visibility.Visible;
				imgAnswer.Tag = imageName;
				btnAddImage.Visibility = Visibility.Collapsed;
				btnUpdateImage.Visibility = Visibility.Visible;
				btnUpdateImage.IsEnabled = false;
				RuleAnswerImageHelper.UseImage(imageName, image=> {
					imgAnswer.Source = image;
					btnUpdateImage.IsEnabled = true;
				});
			}
			ShowErrorTip();
		}

		private void btnAddImage_Click(object sender, RoutedEventArgs e)
		{
			btnAddImage.IsEnabled = false;
			string string_ = SelectImageFile(null);
			btnAddImage.IsEnabled = true;
			ShowAnswerImage(string_);
		}

		private string SelectImageFile(string partFnOld = null)
		{
			string text = null;
			try
			{
				text = OpenFileDialogEx.GetOpenFileName("选择图片", "图片文件|*.gif;*.jpeg;*.jpg;*.png;*.tif;*.tiff", null);
				if (!string.IsNullOrEmpty(text))
				{
					if (FileEx.IsFileLengthMoreKB(text, 1024))
					{
						throw new Exception("图片大小不能超过1MB");
					}
					BitmapImage bitmapImage;
					if (!BitmapImageEx.TryCreateFromFile(text, out bitmapImage))
					{
						throw new Exception("无法解析图片，请选择正常的图片");
					}
					if (!string.IsNullOrEmpty(partFnOld))
					{
						DeleteImageNames.Add(partFnOld);
						AddNewImageNames.Remove(partFnOld);
					}
					string text2 = RuleAnswerImageHelper.AddNewImage(text);
					AddNewImageNames.Add(text2);
					text = text2;
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				MsgBox.ShowErrTip(ex.Message);
			}
			return text;
		}

		private void imgAnswer_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (imgAnswer.Tag != null && e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
			{
				e.Handled = true;
				Process.Start(RuleAnswerImageHelper.GetFullName(imgAnswer.Tag as string));
			}
		}

		private void btnUpdateImage_Click(object sender, RoutedEventArgs e)
		{
			btnUpdateImage.IsEnabled = false;
			string text = SelectImageFile(imgAnswer.Tag as string);
			if (!string.IsNullOrEmpty(text))
			{
				ShowAnswerImage(text);
			}
			btnUpdateImage.IsEnabled = true;
		}

		private void btnDeleteImage_Click(object sender, RoutedEventArgs e)
		{
            MsgBox.ShowNotTipAgain("确定要删除图片？", "操作确认", "WndShortcutEditor_DeleteImage",(b, isOkClick) => {
				if (isOkClick)
				{
					string item = imgAnswer.Tag as string;
					DeleteImageNames.Add(item);
					AddNewImageNames.Remove(item);
					ShowAnswerImage(null);
				}
			});
		}

		private void imgTip_MouseDown(object sender, MouseButtonEventArgs e)
		{
			InfoEditMode();
		}


		public delegate void RoutedValidateEventHandler(object sender, RoutedValidateEventArgs e);

		public class RoutedValidateEventArgs : RoutedEventArgs
		{
			public string ErrorDesc { get; set; }

			public CtlRichTextBox.SyntaxErrorException SyntaxErrorExp { get; set; }

		}

    }
}
