using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using Bot.AssistWindow.NotifyIcon.MenuCreator;
using Bot.Common;
using BotLib;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf.Extensions;
using Bot.Options.HotKey;
using Bot.Common.Windows;

namespace Bot.AssistWindow.NotifyIcon
{
    public partial class WndNotifyIcon : Window
    {
        private const int WM_HOTKEY = 786;
        private static WndNotifyIcon _inst;
        public static WndNotifyIcon Inst
        {
            get
            {
                if (_inst == null)
                {
                    _inst = new WndNotifyIcon();
                }
                return _inst;
            }
        }

        private WndNotifyIcon()
        {
            InitializeComponent();
            Loaded += WndNotifyIcon_Loaded;
        }


        private void WndNotifyIcon_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= WndNotifyIcon_Loaded;
            this.xMoveToWorkAreaCenter();
            this.xShowFirstTime();
            DelayCaller.CallAfterDelay(() =>
            {
                Visibility = Visibility.Collapsed;
            }, 5000, true);
            notifyIcon.Text = string.Format("{0}({1})", Params.AppName, Params.VersionStr);
            CreateHelpMenu();
            notifyIcon.StartBlink(base.FindResource("iconGray") as ImageSource);

            BootStrap.Init();
            var hotMsg = HotKeyHelper.Init();
            if (!string.IsNullOrEmpty(hotMsg))
            {
                MsgBox.ShowErrTip(hotMsg,null);
            }
            notifyIcon.StopBlink();
            Visibility = Visibility.Collapsed;
        }

        private void CreateHelpMenu()
        {
            HelpMenuCreator.Create(notifyIcon);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            try
            {
                base.OnSourceInitialized(e);
                var hWndSrc = PresentationSource.FromVisual(this) as HwndSource;
                if (hWndSrc != null)
                {
                    hWndSrc.AddHook(HwndSourceHookDelegate);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private IntPtr HwndSourceHookDelegate(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 786)
            {
                HotKeyHelper.Dispatch(wParam);
                handled = true;
            }
            return IntPtr.Zero;
        }


        private async void btnExit_Click(object sender, EventArgs e)
        {
            DelayCaller.CallAfterDelay(() =>
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Shutdown();
                }
            }, 5000, false);
            Visibility = Visibility.Visible;
            tbkClose.Visibility = Visibility.Visible;
            this.xMoveToWorkAreaCenter();
            notifyIcon.StartBlink(base.FindResource("iconGray") as ImageSource);
            await Task.Run(() =>
            {
                AppCloseEnder.EndApp();
            });
            notifyIcon.StopBlink();
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        public void AddSellerMenuItem(List<string> nickDatas)
		{
            DispatcherEx.xInvoke(() =>
            {
                if (nickDatas != null && nickDatas.Count > 0)
                {
                    for (int i = 0; i < nickDatas.Count; i++)
                    {
                        var nick = nickDatas[i];
                        try
                        {
                            SellerMenuCreator.Create(notifyIcon, nick);
                        }
                        catch (Exception e)
                        {
                            Log.Exception(e);
                        }
                    }
                }
            });
		}

        public void RemoveSellerMenuItem(HashSet<string> removeNicks)
        {
            if (!removeNicks.xIsNullOrEmpty())
            {
                foreach (var nick in removeNicks)
                {
                    SellerMenuCreator.RemoveMenuItem(notifyIcon, nick);
                }
            }
        }


    }
}
