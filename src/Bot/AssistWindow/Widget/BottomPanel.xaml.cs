using BotLib;
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
using BotLib.Extensions;
using BotLib.Wpf.Extensions;
using BotLib.Misc;
using System.ComponentModel;
using Bot.Options.HotKey;
using Bot.Automation;
using Bot.Options;
using Bot.Automation.ChatDeskNs;
using Bot.ChatRecord;

namespace Bot.AssistWindow.Widget
{
    public partial class BottomPanel : UserControl, IWakable
    {
        public Tipper TheTipper;
        private WndAssist _wndDontUse;
        private string _buyerBeforeDetach;
        private string _preBuyer;
        private bool _isHighden;
        public static Queue<string> BuyerAutoReplyTasks;
        public class Tipper
        {
            private BottomPanel _bottomPanel;
            public Tipper(BottomPanel bottomPanel)
            {
                _bottomPanel = bottomPanel;
            }
            public void ShowTip(string msg)
            {
                ShowTip(msg, -1);
            }
            public void ShowTip(string msg, int showSeconds = 5)
            {
                if (_bottomPanel.tblkTip.Visibility == Visibility.Collapsed)
                {
                    _bottomPanel.tblkTip.Visibility = Visibility.Visible;
                }
                _bottomPanel.tblkTip.Text = msg;
                DelayCaller.CallAfterDelayInUIThread(() =>
                {
                    _bottomPanel.tblkTip.Visibility = Visibility.Collapsed;
                }, showSeconds * 1000);
            }
        }

        private WndAssist Wnd
        {
            get
            {
                if (_wndDontUse == null)
                {
                    _wndDontUse = (this.xFindParentWindow() as WndAssist);
                    Init(_wndDontUse);
                    Util.Assert(_wndDontUse != null);
                }
                return _wndDontUse;
            }
            set
            {
                _wndDontUse = value;
            }
        }

        public BottomPanel()
        {
            _buyerBeforeDetach = null;
            _isHighden = false;
            InitializeComponent();
            gridMain.ColumnDefinitions[0].Width = WpfUtil.ConvertPixelWidthToGridLength(275);
            gridMain.ColumnDefinitions[2].Width = WpfUtil.ConvertPixelWidthToGridLength(150);
            TheTipper = new Tipper(this);
            BuyerAutoReplyTasks = new Queue<string>();
            SizeChanged += BottomPanel_SizeChanged;
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Loaded += BottomPanel_Loaded;
            }
        }

        private void BottomPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= BottomPanel_Loaded;
            btnOption.ContextMenu = (base.FindResource("menuSuper") as ContextMenu);
        }

        private void BottomPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double actualHeight = row1.ActualHeight;
            if (actualHeight > 0.0)
            {
                column2.Width = new GridLength(row1.ActualHeight, GridUnitType.Pixel);
            }
        }

        public void Init(WndAssist wnd)
        {
            if (_wndDontUse == null)
            {
                Wnd = wnd;
                ctlAnswer.Init(wnd);
                InitUI();
            }
        }

        private void InitUI()
        {

        }

        public void WakeUp()
        {
            Wnd.Desk.EvChromeConnected -= Desk_EvChromeConnected;
            Wnd.Desk.EvChromeDetached -= Desk_EvChromeDetached;
            Wnd.Desk.EvChromeConnected += Desk_EvChromeConnected;
            Wnd.Desk.EvChromeDetached += Desk_EvChromeDetached;
            Wnd.Desk.EvBuyerChanged -= Desk_EvBuyerChanged;
            Wnd.Desk.EvBuyerChanged += Desk_EvBuyerChanged;
            Wnd.Desk.EvRecieveNewMessage -= Desk_EvRecieveNewMessage;
            Wnd.Desk.EvRecieveNewMessage += Desk_EvRecieveNewMessage;
            Wnd.Desk.EvShopRobotReceriveNewMessage -= Desk_EvShopRobotReceriveNewMessage;
            Wnd.Desk.EvShopRobotReceriveNewMessage += Desk_EvShopRobotReceriveNewMessage;
            Wnd.Desk.EvAddNewItem -= Desk_EvAddNewItem;
            Wnd.Desk.EvAddNewItem += Desk_EvAddNewItem;
            if (Wnd.Desk.IsChatRecordChromeOk)
            {
                DispatcherEx.xInvoke(() =>
                {
                    ctlBuyer.ShowBuyer(Wnd.Desk.Buyer);
                });
            }

            if (_buyerBeforeDetach != Wnd.Desk.Buyer)
            {
                DispatcherEx.xInvoke(() =>
                {
                    ctlBuyer.ShowBuyer(Wnd.Desk.Buyer);
                    ctlMemo.LoadBuyerNote(Wnd.Desk.BuyerMainNick, Wnd.Desk.Seller);
                    _preBuyer = Wnd.Desk.Buyer;
                });
            }
        }

        private void Desk_EvShopRobotReceriveNewMessage(object sender, ChromeNs.ShopRobotReceriveNewMessageEventArgs e)
        {
            var opmode = Params.Robot.GetOperation(Wnd.Desk.Seller);
            if (opmode == Params.Robot.OperationEnum.Auto)
            {
                //全自动模式下才生效
                //加入待回复的队列，在 CtlAnswer 中，清除回复队列
                BuyerAutoReplyTasks.Enqueue(e.Buyer);
                ctlAnswer.UpdateAnswerIfNeed();
            }
        }

        private void Desk_EvAddNewItem(object sender, NewItemEventArgs e)
        {
            
        }

        private void Desk_EvRecieveNewMessage(object sender, ChromeNs.RecieveNewMessageEventArgs e)
        {
            ctlAnswer.UpdateAnswerIfNeed();
        }

        private void Desk_EvChromeConnected(object sender, ChatDeskEventArgs e)
        {
        }

        private void Desk_EvChromeDetached(object sender, ChatDeskEventArgs e)
        {
        }

        public void Sleep()
        {

        }

        void Desk_EvBuyerChanged(object sender, Automation.ChatDeskNs.BuyerChangedEventArgs e)
        {
            DispatcherEx.xInvoke(() =>
            {
                ctlBuyer.ShowBuyer(Wnd.Desk.Buyer);
                ctlMemo.LoadBuyerNote(Wnd.Desk.BuyerMainNick, Wnd.Desk.Seller);
                _preBuyer = Wnd.Desk.Buyer;
            });
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            Wnd.Desk.Editor.ClearPlainText();
        }

        public void MonitorHotKey(HotKeyHelper.HotOp op)
        {
            switch (op)
            {
                case HotKeyHelper.HotOp.QinKong:
                    btnClear.xPerformClick();
                    break;
                    break;
                case HotKeyHelper.HotOp.ArrowDown:
                    FocusAnswerItem(false);
                    break;
                case HotKeyHelper.HotOp.ArrowDown2:
                    FocusAnswerItem(true);
                    break;
            }
        }

        private bool FocusAnswerIfNeed(int hWnd, bool forceFocusAnswer)
        {
            bool rt;
            if (forceFocusAnswer)
            {
                rt = (Params.InputSuggestion.IsUseDownKey && ctlAnswer.HasItems && Wnd.Desk.Editor.GetChatEditorHwndInfo().Handle == hWnd);
            }
            else
            {
                rt = (Params.InputSuggestion.IsUseDownKey && ctlAnswer.HasItems && Wnd.Desk.Editor.GetChatEditorHwndInfo().Handle == hWnd && !Wnd.Desk.Editor.IsEmptyForPlainCachedText(false) && !Wnd.Desk.Editor.HasEmoji());
            }
            return rt;
        }

        private void FocusAnswerItem(bool focusAnswer)
        {
            int hWnd = WinApi.GetFocusHwnd();
            if (FocusAnswerIfNeed(hWnd, focusAnswer))
            {
                ctlAnswer.FocusItem();
                string text = Wnd.Desk.Editor.GetPlainCachedText(false).Trim();
                if (!text.xIsNullOrEmptyOrSpace())
                {
                    ctlAnswer.AppendInpuTextForEndItem(text);
                }
            }
            else
            {
                WinApi.FocusWnd(hWnd);
            }
        }

        private void btnShowPanelRight_Click(object sender, RoutedEventArgs e)
        {
            btnShowPanelRight.Visibility = Visibility.Collapsed;
            Wnd.ShowPanelRight();
        }

        private void rectHighden_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isHighden)
            {
                if (e.LeftButton == MouseButtonState.Released)
                {
                    _isHighden = false;
                    var captured = System.Windows.Input.Mouse.Captured;
                    if (captured != null)
                    {
                        captured.ReleaseMouseCapture();
                    }
                }
                else
                {
                    var rectangle = sender as Rectangle;
                    rectangle.CaptureMouse();
                    int height = (int)e.GetPosition(this).Y + 5;
                    SetBottomPanelHeight(height, 5);
                }
            }
        }

        private void SetBottomPanelHeight(int height, int minVal = 5)
        {
            int miniVal = 100;
            if (height < miniVal)
            {
                height = miniVal;
            }
            if (Math.Abs(ActualHeight - (double)height) > (double)minVal)
            {
                this.xSetHeight((double)height);
                Wnd.ctlRightPanel.xSetHeight((double)height + Wnd.ToLogicalHeight() + 6.0);
                WndAssist.WaParams.SetBottomPanelHeight(Wnd.Desk.Seller, height);
            }
        }

        private void rectHighden_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isHighden = false;
            Rectangle rectangle = sender as Rectangle;
            rectangle.ReleaseMouseCapture();
            int height = (int)e.GetPosition(this).Y + 5;
            SetBottomPanelHeight(height, 0);
        }

        private void rectHighden_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isHighden = true;
        }

        private void btnOption_Click(object sender, RoutedEventArgs e)
        {
            WndOption.MyShow(Wnd.Desk.Seller, Wnd, OptionEnum.Unknown);
        }

        private void btnKnowledge_Click(object sender, RoutedEventArgs e)
        {
            Wnd.ctlRightPanel.SelectGoodsTabItem();
        }

        private void btnChatlogViewer_Click(object sender, RoutedEventArgs e)
        {
            WndChatlogViewer.MyShow(Wnd.Desk.Seller);
        }
    }
}
