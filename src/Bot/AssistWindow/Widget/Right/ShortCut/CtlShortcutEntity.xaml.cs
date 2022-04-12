using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bot.Common;
using Bot.Common.ImageHelper;
using BotLib;
using BotLib.Misc;
using BotLib.Wpf.Controls;
using BotLib.Wpf.Extensions;
using DbEntity;

namespace Bot.AssistWindow.Widget.Right.ShortCut
{
    public partial class CtlShortcutEntity : UserControl
    {
        private string _partShortcutImageFn;
        private Action<string, Action<BitmapImage>> _actUseImage;
        private WndAssist _wndDontUse;
        private bool _isMouseInsideImageControl;
        private WndAssist Wnd
        {
            get
            {
                if (_wndDontUse == null)
                {
                    _wndDontUse = this.xFindAncestor<WndAssist>();
                }
                return _wndDontUse;
            }
        }
        public CtlShortcutEntity()
		{
			_isMouseInsideImageControl = false;
			InitializeComponent();
		}

        public CtlShortcutEntity(ShortcutEntity shortcut ,string[] highlightKeys, SolidColorBrush backgroundBrush = null)
        {
            _isMouseInsideImageControl = false;
            InitializeComponent();
            tbkHeader.Text = shortcut.GetShowTitle();
			tbkHeader.HighlightKeys = highlightKeys;
            if (string.IsNullOrEmpty(shortcut.Code))
			{
				tbkKeyText.Visibility = Visibility.Collapsed;
			}
			else
			{
                tbkKeyText.Text = shortcut.Code;
				tbkKeyText.HighlightKeys = highlightKeys;
			}
			if (backgroundBrush != null)
			{
				tbkKeyText.Background = backgroundBrush;
			}
            if (!string.IsNullOrEmpty(shortcut.ImageName))
			{
                _partShortcutImageFn = shortcut.ImageName;
                _actUseImage = ShortcutImageHelper.UseImage;
				imgMain.Visibility = Visibility.Visible;
			}
            tblkContent.Text = shortcut.Text;
			tblkContent.HighlightKeys = highlightKeys;
            if (!string.IsNullOrEmpty(shortcut.ImageName))
			{
                _actUseImage(_partShortcutImageFn, (bitmapImage) =>
                {
                    if (bitmapImage != null)
                    {
                        try
                        {
                            imgContent.Source = bitmapImage;
                            imgContent.Visibility = Visibility.Visible;
                        }
                        catch (Exception e)
                        {
                            Log.Exception(e);
                        }
                    }
                });
			}
		}
        private void imgMain_MouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseInsideImageControl = true;
            if (!string.IsNullOrEmpty(_partShortcutImageFn) && Wnd != null)
            {
                DelayCaller.CallAfterDelayInUIThread(() => {
                    if (_isMouseInsideImageControl)
                    {
                        _actUseImage(_partShortcutImageFn, (bitmapImage) =>
                        {
                            if (bitmapImage != null)
                            {
                                try
                                {
                                    Wnd.BringTop();
                                    Rect rect = ZoomRect(bitmapImage);
                                    Wnd.imgBig.SetValue(Canvas.LeftProperty, rect.X);
                                    Wnd.imgBig.SetValue(Canvas.TopProperty, rect.Y);
                                    Wnd.imgBig.Width = rect.Width;
                                    Wnd.imgBig.Height = rect.Height;
                                    Wnd.imgBig.Source = bitmapImage;
                                }
                                catch (Exception ex)
                                {
                                    Log.Exception(ex);
                                    MsgBox.ShowErrTip(ex.Message,null);
                                }
                            }
                        });
                    }
                }, 300);
            }
        }
        private Rect ZoomRect(BitmapImage img)
        {
            Point point = base.TranslatePoint(new Point(-5.0, 0.0), Wnd);
            double width = img.Width;
            double height = img.Height;
            double maxWidth = point.X - 5.0;
            double maxHeight = SystemParameters.WorkArea.Height - 10.0;
            if (width > maxWidth)
            {
                double scale = width / maxWidth;
                width = maxWidth;
                height /= scale;
            }
            if (height > maxHeight)
            {
                double scale = height / maxHeight;
                width /= scale;
                height = maxHeight;
            }
            point.X -= width;
            if (point.Y + height > SystemParameters.WorkArea.Height)
            {
                point.Y = SystemParameters.WorkArea.Height - height - 5.0;
            }
            if (width < 0.0)
            {
                width = 100.0;
            }
            if (height < 0.0)
            {
                height = 100.0;
            }
            return new Rect(point.X, point.Y, width, height);
        }

        private void imgMain_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseInsideImageControl = false;
            if (Wnd != null)
            {
                Wnd.imgBig.Source = null;
            }
        }
    }
}
