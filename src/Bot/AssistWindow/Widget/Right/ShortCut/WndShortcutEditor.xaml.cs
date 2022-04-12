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
using Bot.Asset;
using Bot.Common;
using Bot.Common.Windows;
using BotLib;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf;
using BotLib.Wpf.Extensions;
using Xceed.Wpf.Toolkit;
using Bot.Common.EmojiInputer;
using Bot.AI.WordSplitterNs.ElementParser;
using DbEntity;
using Bot.Common.ImageHelper;

namespace Bot.AssistWindow.Widget.Right.ShortCut
{
	public partial class WndShortcutEditor : EtWindow
	{
        private VdWndShortcutEditor _data;
        private bool _isSubmit;
        private List<IndexRange> _emojiRanges;
        private EmojiParser _eParser;

        public WndShortcutEditor(string title = null, string content = null, string code = null, string imageName = null)
		{
			_isSubmit = false;
			InitializeComponent();
            _eParser = new EmojiParser();
            _data = new VdWndShortcutEditor(content, code, this);
            ShowImage(imageName);
            tboxQuestion.Text = (title ?? "");
			Loaded += WndShortcutEditor_Loaded;
		}

		private void WndShortcutEditor_Loaded(object sender, RoutedEventArgs e)
		{
			tboxContent.Focus();
			tboxContent.xMoveCaretToTail();
		}


        private void tboxCode_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

		public static void CreateNew(string parentId, string dbAccount, string seller, Window ownnerWnd, Action<TreeNode> callback)
		{
            var wnd = new WndShortcutEditor(null, null, null);
            wnd.FirstShow(seller, ownnerWnd, () =>
            {
                if (wnd._isSubmit)
                {
                    var et = EntityHelper.Create<ShortcutEntity>(dbAccount);
                    et.ParentId = parentId;
                    et.Title = wnd.tboxQuestion.Text.Trim();
                    et.Text = wnd._data.Content;
                    et.Code = wnd._data.Code;
                    et.ImageName = wnd.imgMain.Tag == null ? "" : wnd.imgMain.Tag.ToString();
                    callback(et);
                }
            }, false);

		}

		public static void Edit(ShortcutEntity pre, string seller, string dbAccount, Window ownnerWnd, Action<TreeNode> callback)
		{
            var wnd = new WndShortcutEditor(pre.Title,pre.Text, pre.Code, pre.ImageName);
            wnd.FirstShow(seller, ownnerWnd, () =>
            {
                if (wnd._isSubmit)
                {
                    var et = pre.Clone<ShortcutEntity>(false);
                    et.Title = wnd.tboxQuestion.Text.Trim();
                    et.Text = wnd._data.Content;
                    et.Code = wnd._data.Code;
                    et.ImageName =  wnd.imgMain.Tag==null ? "" : wnd.imgMain.Tag.ToString();

                    EntityHelper.SetModifyTick(et);
                    callback(et);
                }
            }, false);
		}

		private void imgEmoji_MouseDown(object sender, MouseButtonEventArgs e)
		{
            WndEmojiInputer.MyShow(this, (emojisText) => {
                if (!string.IsNullOrEmpty(emojisText))
                {
                    tboxContent.xInsertOrAppend(emojisText);
                }
            });
		}

        private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
        private void btnOk_Click(object sender, RoutedEventArgs e)
		{
            if (Validate())
			{
				_isSubmit = true;
				Close();
			}
		}

        private bool Validate()
		{
			bool rt = false;
			if ((rt = _data.Submitable()) && imgMain.Tag != null)
			{
				var code = tboxCode.Text;
				if (string.IsNullOrEmpty((code != null) ? code.Trim() : null))
				{
                    rt = !string.IsNullOrEmpty((tboxContent.Text != null) ? tboxContent.Text.Trim() : null);
				}
				else
				{
                    rt = true;
				}
				if (!rt)
				{
                    MsgBox.ShowErrTip("选中图片时，至少还要设置【快捷编码】或者【分类短语】", null);
				}
			}
			return rt;
		}

		private void ShowImage(string imageName)
		{
			if (string.IsNullOrEmpty(imageName))
			{
				tbkImage.Visibility = Visibility.Collapsed;
				imgMain.Visibility = Visibility.Collapsed;
				imgMain.Tag = null;
				btnAddImage.Visibility = Visibility.Visible;
				btnAddImage.IsEnabled = true;
				btnUpdateImage.Visibility = Visibility.Collapsed;
			}
			else
			{
				tbkImage.Visibility = Visibility.Visible;
				imgMain.Visibility = Visibility.Visible;
				imgMain.Tag = imageName;
				btnAddImage.Visibility = Visibility.Collapsed;
				btnUpdateImage.Visibility = Visibility.Visible;
				btnUpdateImage.IsEnabled = false;
                ShortcutImageHelper.UseImage(imageName, (imgSrc) => {
                    imgMain.Source = imgSrc;
                    btnUpdateImage.IsEnabled = true;
                });
			}
		}

		private void btnAddImage_Click(object sender, RoutedEventArgs e)
		{
			btnAddImage.IsEnabled = false;
            var imageName = SelectImageFile();
            ShowImage(imageName);
		}

		private string SelectImageFile(string parnFnOld = null)
		{
            string imageName = null;
			try
			{
				imageName = OpenFileDialogEx.GetOpenFileName("选择图片", "图片文件|*.gif;*.jpeg;*.jpg;*.png;*.tif;*.tiff", null);
                if (!string.IsNullOrEmpty(imageName))
				{
                    if (FileEx.IsFileLengthMoreKB(imageName, 1024))
					{
						throw new Exception("图片大小不能超过1MB");
					}
					BitmapImage bitmapImage;
                    if (!BitmapImageEx.TryCreateFromFile(imageName, out bitmapImage))
					{
						throw new Exception("无法解析图片，请选择正常的图片");
					}
                    imageName = ShortcutImageHelper.AddNewImage(imageName, parnFnOld);
				}
			}
			catch (Exception ex)
			{
                Log.Exception(ex);
                MsgBox.ShowErrTip(ex.Message, null);
			}
            return imageName;
		}

        private void btnHelp_Click(object sender, RoutedEventArgs e)
		{
		}

		private void btnUpdateImage_Click(object sender, RoutedEventArgs e)
		{
			btnUpdateImage.IsEnabled = false;
			string text = SelectImageFile(imgMain.Tag as string);
			if (!string.IsNullOrEmpty(text))
			{
				ShowImage(text);
			}
			else
			{
				btnUpdateImage.IsEnabled = true;
			}
		}

		private void btnDeleteImage_Click(object sender, RoutedEventArgs e)
		{
            MsgBox.ShowNotTipAgain("确定要删除图片？", "操作确认", "WndShortcutEditor_DeleteImage", (b1, okClicked) => {
                if (!b1 || okClicked)
                {
                    string text = imgMain.Tag as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        ShortcutImageHelper.DeleteImage(text);
                    }
                    ShowImage(null);
                }
            });
		}

		private void tboxContent_SelectionChanged(object sender, RoutedEventArgs e)
		{
			int selectionStart = tboxContent.SelectionStart;
			bool hasShow = false;
			if (selectionStart >= 0 && !_emojiRanges.xIsNullOrEmpty())
			{
				foreach (var ir in _emojiRanges)
				{
					if (selectionStart >= ir.Start && selectionStart < ir.NextStart)
					{
                        var rect = EmojiHelper.FindEmojisRect(tboxContent.Text.Substring(ir.Start, ir.Length));
                        ShowEmojiImage(rect);
                        hasShow = true;
                        break;
					}
				}
			}
			if (!hasShow)
			{
				imgEmojiSelected.Visibility = Visibility.Collapsed;
			}
		}

		private void ShowEmojiImage(Int32Rect imgRect)
		{
			if (imgRect == null)
			{
				imgEmojiSelected.Visibility = Visibility.Collapsed;
			}
			else
			{
				CroppedBitmap source = new CroppedBitmap(AssetImageHelper.GetImageFromWpfCache(AssetImageEnum.imgEmojiAll), imgRect);
				imgEmojiSelected.Source = source;
				imgEmojiSelected.Visibility = Visibility.Visible;
			}
		}

		private void tboxContent_TextChanged(object sender, TextChangedEventArgs e)
		{
            _emojiRanges = _eParser.GetExceptRanges(tboxContent.Text);
		}

		private void imgMain_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (imgMain.Tag != null && e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
			{
				e.Handled = true;
                Process.Start(ShortcutImageHelper.GetFullPath(imgMain.Tag as string));
			}
		}




		private class VdWndShortcutEditor : ViewData
		{
			public string Content { get; set; }
            private string _oriCode;
            private string _code;
			private WndShortcutEditor _wnd;

			public string Code
			{
				get
				{
					return _code;
				}
				set
				{
                    _oriCode = value;
					_code = value.xToBanJiao().ToLower();
				}
			}

			public VdWndShortcutEditor(string content, string code, WndShortcutEditor wnd) :base(wnd)
			{
				Content = (content ?? "");
				Code = (code ?? "");
				_wnd = wnd;
                SetBinding("Content", wnd.tboxContent, () => { 
                    return (!string.IsNullOrEmpty(Content) || _wnd.imgMain.Tag != null) ? null : "必填!";
                });
                SetBinding("Code", wnd.tboxCode, () => {
                    string err = null;
                    var c = _oriCode.Trim();
                    if (!string.IsNullOrEmpty(c))
                    {
                        if (char.IsDigit(c[0]))
                        {
                            err = "快捷编码的“第一个字”不能为数字";
                        }
                        else if (char.IsDigit(c.ToCharArray()[c.Length - 1]))
                        {
                            err = "快捷编码的“最后一个字”不能为数字";
                        }
                    }
                    return err;
                });
			}
		}

	}
}
