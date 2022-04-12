using Bot.Automation.ChatDeskNs;
using System;
using System.Collections.Concurrent;
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
using BotLib;
using BotLib.Extensions;
using BotLib.Wpf.Extensions;
using Bot.Options;
using Bot.ChatRecord;
using Bot.Common.TreeviewHelper;
using DbEntity;

namespace Bot.AssistWindow.Widget.Robot
{
    /// <summary>
    /// CtlRobotQa.xaml 的交互逻辑
    /// </summary>
    public partial class CtlRobotQA : UserControl
    {
        private RightPanel _rightPanel;
        private WndAssist _wnd;
        private static ConcurrentBag<string> _adjustedBag;
        private QaItemCreator _qaItemCreator;
        private bool _isInit;
        private TabItem _parentTab;
        private TabControl _tabControl;
        private ChatDesk _desk;
        private WndAssist _wndDontUse;
        public string Seller { get; private set; }
        public ContextMenu MenuQuestion { get; private set; }
        public RuleEditor RuleEditor { get; private set; }

        static CtlRobotQA()
        {
            _adjustedBag = new ConcurrentBag<string>();
        }
        public CtlRobotQA(WndAssist wnd)
        {
            _isInit = false;
            _wnd = wnd;
            InitializeComponent();
            tvMain.xSetRightClickSelectTreeviewItem();
            Init();
            Loaded += CtlRobotQA_Loaded;
            SizeChanged += CtlRobotQA_Loaded;
        }

        private void Init()
        {
            _desk = _wnd.Desk;
            Seller = _desk.Seller;
            MenuQuestion = (ContextMenu)FindResource("menuQuestion");
            RuleEditor = new RuleEditor(Seller, _wnd);
            _qaItemCreator = new QaItemCreator(_desk, this);
        }

        private void CtlRobotQA_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isInit)
            {
                _isInit = true;
                _parentTab = this.xFindAncestorFromMe<TabItem>();
                _tabControl = this.xFindAncestorFromMe<TabControl>();
                tvMain.SelectedItemChanged += tvMain_SelectedItemChanged;
                var seller = _desk.Seller;
                var opmode = Params.Robot.GetOperation(seller);
                if (opmode == Params.Robot.OperationEnum.Auto && Params.Robot.CancelAutoOnReset && !_adjustedBag.Contains(seller))
                {
                    opmode = Params.Robot.OperationEnum.None;
                    Params.Robot.SetOperation(seller, Params.Robot.OperationEnum.None);
                }
                if (!_adjustedBag.Contains(seller))
                {
                    _adjustedBag.Add(seller);
                }
                if (opmode == Params.Robot.OperationEnum.Quote)
                {
                    SetCheckboxStatus(cboxQuote, true);
                }
                else if (opmode == Params.Robot.OperationEnum.Send)
                {
                    SetCheckboxStatus(cboxSend, true);
                }
                else if (opmode == Params.Robot.OperationEnum.Auto)
                {
                    SetCheckboxStatus(cboxAuto, true);
                }
            }
            if (_parentTab.IsSelected)
            {
                WakeUp();
            }
        }

        public void Sleep()
        {
            _desk.EvRecieveNewMessage -= _desk_EvRecieveNewMessage;
            _desk.EvBuyerChanged -= _desk_EvBuyerChanged;
        }

        public void WakeUp()
        {
            _desk.EvRecieveNewMessage -= _desk_EvRecieveNewMessage;
            _desk.EvRecieveNewMessage += _desk_EvRecieveNewMessage;
            _desk.EvBuyerChanged -= _desk_EvBuyerChanged;
            _desk.EvBuyerChanged += _desk_EvBuyerChanged;
        }

        private void _desk_EvBuyerChanged(object sender, BuyerChangedEventArgs e)
        {
            CreateQaItem();
        }

        private void CreateQaItem()
        {
            var newestMsgs = _desk.GetNewestMessages(_desk.Buyer);
            DispatcherEx.xInvoke(() =>
            {
                if (newestMsgs != null)
                {
                    var startQsno = tvMain.Items.Count;
                    for (var i = 0; i < newestMsgs.Count; i++)
                    {
                        AddQaItem(newestMsgs[i].originalData.text, (i + 1) + startQsno);
                    }
                }
            });
        }

        private void _desk_EvRecieveNewMessage(object sender, ChromeNs.RecieveNewMessageEventArgs e)
        {
            CreateQaItem();
        }

        private void AddQaItem(string msg, int no)
        {
            tvMain.Items.Add(_qaItemCreator.Create(msg, no));
        }

        public void SetCheckboxStatus(CheckBox cbox, bool isChecked)
        {
            if (isChecked)
            {
                cbox.IsChecked = true;
                cbox.Foreground = Brushes.Red;
            }
            else
            {
                cbox.IsChecked = false;
                cbox.Foreground = Brushes.White;
            }
        }

        private void tvMain_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var it = e.NewValue as TreeViewItem;
            if (it == null) return;
            var txtBox = it.Header as TextBlock;
            if (txtBox == null) return;

            int indent;
            if (txtBox.Tag == null)
            {
                indent = it.xGetIndent() + 15;
                txtBox.Tag = indent;
            }
            else
            {
                indent = (int)txtBox.Tag;
            }
            int width = (int)tvMain.ActualWidth - indent;
            txtBox.xSetWidth(width);
            foreach (var inline in txtBox.Inlines)
            {
                if (inline.Background != null)
                {
                    inline.Tag = inline.Background;
                    inline.Background = null;
                }
            }
            it = (e.OldValue as TreeViewItem);
            if (it == null) return;

            txtBox = (it.Header as TextBlock);
            if (txtBox == null) return;

            foreach (var inline in txtBox.Inlines)
            {
                if (inline.Tag != null && inline.Tag is Brush)
                {
                    inline.Background = (inline.Tag as Brush);
                    inline.Tag = null;
                }
            }
        }

        private void btOption_Click(object sender, RoutedEventArgs e)
        {
            WndOption.MyShow(_desk.Seller, _wnd, OptionEnum.Robot);
        }

        private void cboxAuto_Click(object sender, RoutedEventArgs e)
        {
            var opmode = Params.Robot.OperationEnum.None;
            if (e.OriginalSource == cboxAuto)
            {
                SetCheckboxStatus(cboxQuote, false);
                SetCheckboxStatus(cboxSend, false);
                var isChecked = cboxAuto.IsChecked ?? false;
                SetCheckboxStatus(cboxAuto, isChecked);
                if (isChecked)
                {
                    opmode = Params.Robot.OperationEnum.Auto;
                }
            }
            else if (e.OriginalSource == cboxQuote)
            {
                SetCheckboxStatus(cboxAuto, false);
                SetCheckboxStatus(cboxSend, false);
                var isChecked = cboxQuote.IsChecked ?? false;
                SetCheckboxStatus(cboxQuote, isChecked);
                if (isChecked)
                {
                    opmode = Params.Robot.OperationEnum.Quote;
                }
            }
            else if (e.OriginalSource == cboxSend)
            {
                SetCheckboxStatus(cboxAuto, false);
                SetCheckboxStatus(cboxQuote, false);
                var isChecked = cboxSend.IsChecked ?? false;
                SetCheckboxStatus(cboxSend, isChecked);
                if (isChecked)
                {
                    opmode = Params.Robot.OperationEnum.Send;
                }
            }
            Params.Robot.SetOperation(_desk.Seller, opmode);
        }

        private void mNewRule_Click(object sender, RoutedEventArgs e)
        {
            var anstxt = GetQuestionText();
            RuleEditor.CreateRule(anstxt, ()=> {
                
            });
        }

        private void mAppendRule_Click(object sender, RoutedEventArgs e)
        {
            var anstxt = GetQuestionText();
            RuleEditor.CreateRule(anstxt, () => {

            });
        }

        private void mOpenRuleManager_Click(object sender, RoutedEventArgs e)
        {
            RuleEditor.OpenRobotRuleManager();
        }

        private void mSend_Click(object sender, RoutedEventArgs e)
        {
            var ans = GetAnswer();
            if (ans == null) return;
            _desk.Editor.SendAnswerAsync(ans,null);
        }

        private void mQuote_Click(object sender, RoutedEventArgs e)
        {
            var ans = GetAnswer();
            if (ans == null) return;
            _desk.Editor.SetAnswerAsync(ans);
        }

        private string GetQuestionText()
        {
            var it = (TreeViewItem)tvMain.SelectedItem;
            while (it != null && it.Parent != tvMain)
            {
                it = (TreeViewItem)it.Parent;
            }
            if (it == null) return String.Empty;
            return it.Tag.ToString();
        }

        private AnswerWithImage GetAnswer()
        {
            var qaIt = (TreeViewItem)tvMain.SelectedItem;
            while (qaIt != null && qaIt.Parent != tvMain)
            {
                qaIt = (TreeViewItem)qaIt.Parent;
            }
            qaIt = qaIt.Items[0] as TreeViewItem;
            AnswerWithImage awi = null;
            if (qaIt.Header is CtlLeaf)
            {
                var qa = qaIt.Header as CtlLeaf;
                awi = qa.Tag as AnswerWithImage;
            }
            return awi;
        }

        private void mCopy_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mRefresh_Click(object sender, RoutedEventArgs e)
        {

        }
        private void mHelp_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
