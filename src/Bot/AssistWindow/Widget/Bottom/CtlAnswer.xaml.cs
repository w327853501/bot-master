using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Bot.AssistWindow.Widget.Right.ShortCut;
using Bot.Common.TreeviewHelper;
using Bot.Automation.ChatDeskNs;
using Bot.Automation.ChatDeskNs.InputSuggestion;
using BotLib;
using BotLib.Collection;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf.Extensions;
using System.Windows.Forms;
using Bot.AssistWindow.Widget.Right;
using Bot.Robot.Rule.QaCiteTable;
using Bot.Automation;
using System.Windows.Media;
using DbEntity;
using Bot.Robot.Rule.Finder;
using Bot.ChatRecord;
using Bot.Common.ImageHelper;

namespace Bot.AssistWindow.Widget.Bottom
{
    public partial class CtlAnswer : System.Windows.Controls.UserControl, IComponentConnector
    {
        private ChatDesk _desk;
        private BottomPanel.Tipper _tipper;
        private WndAssist _wnd;
        private static Thickness ItemMargin;
        private DateTime _preSetTagInfoTime;
        private object _preTag;
        private ShowListTypeEnum _showListType;
        private object _showListKey;
        private object _updateAnswerSynObj;
        private bool _isUpdateAnswerBusy;
        private int _updateAnswerUnDealCount;
        private bool _isDoubleClick;
        private DateTime _preTypingTime;
        private DateTime _doubleClickTime;
        public bool HasItems
        {
            get
            {
                return tvMain.HasItems;
            }
        }

        static CtlAnswer()
        {
            ItemMargin = new Thickness(-10.0, 0.0, 0.0, 5.0);
        }

        public CtlAnswer()
        {
            _preSetTagInfoTime = DateTime.MinValue;
            _showListType = ShowListTypeEnum.Unknown;
            _showListKey = null;
            _updateAnswerSynObj = new object();
            _isUpdateAnswerBusy = false;
            _preTypingTime = DateTime.MinValue;
            _updateAnswerUnDealCount = 0;
            _isDoubleClick = false;
            _doubleClickTime = DateTime.MinValue;
            InitializeComponent();
            tvMain.xSetRightClickSelectTreeviewItem();
        }

        public void Init(WndAssist wnd)
        {
            _desk = wnd.Desk;
            _tipper = wnd.ctlBottomPanel.TheTipper;
            _wnd = wnd;
            _desk.Editor.EvEditorTextChanged += Editor_EvEditorTextChanged;
        }

        private void Editor_EvEditorTextChanged(object sender, ChatDeskEventArgs e)
        {
            _preTypingTime = DateTime.Now;
        }

        private void tvMain_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _isDoubleClick = true;
            _doubleClickTime = DateTime.Now;
            SendTagInfo();
        }

        private void SendTagInfo()
        {
            try
            {
                var tag = GetItemTag();
                if (tag == null) return;
                if (tag is ShortcutEntity)
                {
                    var shortcut = tag as ShortcutEntity;
                    shortcut.SetOrSendShortcutAsync(_desk, true, false);
                }
                else if (tag is string)
                {
                    var txt = tag as string;
                    _desk.Editor.SendPlainTextAsync(txt);
                }

            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public object GetItemTag(int index)
        {
            object rt = null;
            if (index >= 0 && index < tvMain.Items.Count)
            {
                rt = GetItemTag(tvMain.Items[index] as TreeViewItem);
            }
            return rt;
        }

        private object GetItemTag()
        {
            return GetItemTag(tvMain.SelectedItem as TreeViewItem);
        }

        private object GetItemTag(TreeViewItem it)
        {
            object rt = null;
            try
            {
                if (it != null)
                {
                    DispatcherEx.xInvoke(() =>
                    {
                        rt = ((it != null) ? it.Tag : null);
                    });
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            return rt;
        }

        private void tvMain_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
        }


        private void SetTagInfoToEditor(bool focusEditor)
        {
            try
            {
                var tag = GetItemTag();
                if (tag != null && (tag != _preTag || (DateTime.Now - _preSetTagInfoTime).TotalSeconds >= 1.0))
                {
                    _preTag = tag;
                    _preSetTagInfoTime = DateTime.Now;
                    string txt = null;
                    if (tag is ShortcutEntity)
                    {
                        var shortcut = tag as ShortcutEntity;
                        shortcut.SetOrSendShortcutAsync(_desk, false, focusEditor);
                    }
                    if (tag is AnswerWithImage)
                    {
                        var awi = tag as AnswerWithImage;
                        _desk.Editor.SendAnswerAsync(awi,null);
                    }
                    else if (tag is string)
                    {
                        txt = (tag as string);
                        _desk.Editor.SetPlainTextAsync(txt);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public void UpdateAnswerIfNeed()
        {
            lock (_updateAnswerSynObj)
            {
                if (_isUpdateAnswerBusy || _desk == null)
                {
                    _updateAnswerUnDealCount++;
                    return;
                }
                _isUpdateAnswerBusy = true;
            }
            try
            {
                var opmode = Params.Robot.GetOperation(_desk.Seller);
                var seller = _desk.Seller;
                var buyer = _desk.Buyer;
                if (!_desk.Editor.IsEmptyForPlainCachedText(false)) return;
                if (string.IsNullOrEmpty(buyer)) {
                    Log.Info("buyer为空，跳过UpdateAnswerIfNeed");
                    return;
                }
                var qlist = _desk.GetUnReplyMessages(_desk.Buyer);
                if (!qlist.xIsNullOrEmpty())
                {
                    var answers = AnswerFinder.GetAnswersOfSeller(qlist, seller, Params.BottomPannelAnswerCount);
                    if (buyer == _desk.Buyer)
                    {
                        var needShowAnswer = true;
                        switch (opmode)
                        {
                            case Params.Robot.OperationEnum.Auto:
                                SendSmartAnswer(answers);
                                needShowAnswer = false;
                                break;
                            case Params.Robot.OperationEnum.Send:
                                SendSmartAnswer(answers);
                                needShowAnswer = false;
                                break;
                            case Params.Robot.OperationEnum.Quote:
                                QuoteSmartAnswer(answers);
                                needShowAnswer = true;
                                break;
                        }
                        if (needShowAnswer)
                        {
                            ShowAnswers(answers, qlist);
                        }
                    }
                }
                //等待手动回复完成
                Util.WaitFor(() =>
                {
                    return _preTypingTime.xIsTimeElapseMoreThanSecond(2);
                },10000);
                //自动切换到下一个需要回复的客户
                if (Params.Robot.OperationEnum.Auto == opmode)
                {
                    ClearTaskAndOpenNextBuyer(_desk.Buyer);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            finally
            {
                _isUpdateAnswerBusy = false;
                if (_updateAnswerUnDealCount > 0)
                {
                    _updateAnswerUnDealCount = 0;
                    UpdateAnswerIfNeed();
                }
            }
        }

        private void QuoteSmartAnswer(List<SmartAnswerMatchInfo> answers)
        {
            var smtAns = answers.LastOrDefault();
            if (smtAns == null) return;
            _desk.Editor.SetAnswerAsync(smtAns.AnswerWithImg);
            //answers.ForEach(async ans => {
            //    _desk.Editor.SetAnswerAsync(ans.AnswerWithImg);
            //    await Task.Delay(500);
            //});
        }

        private void ClearTaskAndOpenNextBuyer(string buyer)
        {
            var fisrtBuyer = BottomPanel.BuyerAutoReplyTasks.Peek();
            if (fisrtBuyer == buyer)
            {
                BottomPanel.BuyerAutoReplyTasks.Dequeue();
            }
            var nextBuyer = BottomPanel.BuyerAutoReplyTasks.Peek();
            _desk.OpenChat(nextBuyer);
        }

        private void SendSmartAnswer(List<SmartAnswerMatchInfo> answers)
        {
            var smtAns = answers.FirstOrDefault();
            var delaySec = Params.Robot.GetSendModeReplyDelaySec(_desk.Seller);
            DelayCaller.CallAfterDelayInUIThread(() =>
            {
                if (smtAns == null)
                {
                    SendNoAnswerTip();
                }
                else
                {
                    SendSmartAnswers(answers);
                }
            }, delaySec * 1000);
        }

        private void SendSmartAnswers(List<SmartAnswerMatchInfo> answers)
        {
            var smtAns = answers.LastOrDefault();
            if (smtAns == null) return;
            _desk.Editor.SendAnswerAsync(smtAns.AnswerWithImg, null);
            //answers.ForEach(async ans => {
            //    _desk.Editor.SendAnswerAsync(ans.AnswerWithImg, null);
            //    await Task.Delay(500);
            //});
        }

        private void SendNoAnswerTip()
        {
            var noAnswerTip = Params.Robot.GetAutoModeNoAnswerTip(_desk.Seller);
            if (!noAnswerTip.xIsNullOrEmptyOrSpace())
            {
                _desk.Editor.SendPlainTextAsync(noAnswerTip);
            }
        }


        private void ShowAnswers(List<SmartAnswerMatchInfo> smartAnswers,List<ChatRecord.ChatMessage> qlist)
        {
            var awis = smartAnswers.Select(k=>k.AnswerWithImg).ToList();
            var items = awis.ConvertAll(awi => new Item4Show(awi));
            ShowListItem(items);
            _showListKey = qlist;
            _showListType = ShowListTypeEnum.AutoAnswer;
        }

        private void tvMain_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Key key = e.Key;
            if (key != Key.Return)
            {
                switch (key)
                {
                    case Key.Left:
                    case Key.Right:
                        _wnd.Desk.Editor.FocusEditor(true);
                        break;
                    case Key.Up:
                        if (SelectItem())
                        {
                            _wnd.Desk.Editor.FocusEditor(true);
                        }
                        break;
                    case Key.Down:
                        if ((System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            int idx = GetItemIndex(tvMain.SelectedItem);
                            if (idx < 0)
                            {
                                idx = 0;
                            }
                            if (idx < tvMain.Items.Count - 1)
                            {
                                var it = tvMain.Items[idx + 1] as TreeViewItem;
                                if (it != null)
                                {
                                    it.IsSelected = true;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                SendTagInfo();
            }
        }

        private int GetItemIndex(object it)
        {
            var idx = -1;
            if (it != null)
            {
                for (int i = 0; i < tvMain.Items.Count; i++)
                {
                    if (it == tvMain.Items[i])
                    {
                        idx = i;
                        break;
                    }
                }
            }
            return idx;
        }

        private bool SelectItem()
        {
            var rt = false;
            if (tvMain.Items.Count > 0)
            {
                var it = tvMain.Items[0] as TreeViewItem;
                if (it != null)
                {
                    rt = it.IsSelected;
                }
            }
            return rt;
        }

        private void tvMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((DateTime.Now - _doubleClickTime).TotalMilliseconds > 500.0)
            {
                DelayCaller.CallAfterDelayInUIThread(() =>
                {
                    if (!_isDoubleClick)
                    {
                        SetTagInfoToEditor(true);
                    }
                    _isDoubleClick = false;
                }, SystemInformation.DoubleClickTime);
            }
        }

        public void FocusItem()
        {
            int idx = GetItemIndex(tvMain.SelectedItem);
            FocusItem(idx);
        }

        public void FocusItem(int idx)
        {
            _wnd.BringTop();
            if (idx >= tvMain.Items.Count)
            {
                idx = tvMain.Items.Count - 1;
            }
            if (idx < 0)
            {
                idx = 0;
            }
            WinApi.BringTopAndDoAction(_wnd.Handle, () =>
            {
                tvMain.Focus();
                if (idx < tvMain.Items.Count)
                {
                    var it = tvMain.Items[idx] as TreeViewItem;
                    if (it != null) it.IsSelected = true;
                }
            });
        }

        public void AppendInpuTextForEndItem(string txt)
        {
            var item4Show = new Item4Show(txt, txt);
            var ctlLeaf = new CtlLeaf(item4Show.Text.xRemoveLineBreak(" ").Trim(), item4Show.HighlightKey, null, item4Show.ImageName, item4Show.ActUseImage, null);
            var sp = new StackPanel();
            sp.Orientation = System.Windows.Controls.Orientation.Horizontal;
            var noTxt = new TextBlock
            {
                Text = string.Format("{0}. ", tvMain.Items.Count + 1)
            };
            sp.Children.Add(noTxt);
            sp.Children.Add(ctlLeaf);
            ctlLeaf.Padding = new Thickness(0.0);
            var it = tvMain.xAppend(sp, null);
            it.Tag = item4Show.Tag;
            it.Margin = ItemMargin;
            it.MouseDoubleClick += tvMain_MouseDoubleClick;
            it.MouseLeftButtonUp += tvMain_MouseLeftButtonUp;
        }

        public bool IsShowListItem(string txt)
        {
            return _showListType == ShowListTypeEnum.InputLegend && _showListKey as string == txt;
        }

        public void ShowListItem(List<Item4Show> items, string key)
        {
            items = (items ?? new List<Item4Show>());
            ShowListItem(items);
            _showListType = ShowListTypeEnum.InputLegend;
            _showListKey = key;
        }

        private void ShowListItem(List<Item4Show> items)
        {
            DispatcherEx.xInovkeLowestPriority(() =>
            {
                try
                {
                    tvMain.Items.Clear();
                    int no = 0;
                    foreach (var itShow in items.xSafeForEach())
                    {
                        no++;
                        var headerPnl = new StackPanel();
                        headerPnl.Orientation = System.Windows.Controls.Orientation.Horizontal;

                        var noTxt = new TextBlock();
                        noTxt.Text = string.Format("{0}.", no);
                        noTxt.Padding = new Thickness(0, 5, 0.0, 0);
                        var leaf = new CtlLeaf(itShow.Text.xRemoveLineBreak(" ").Trim(),
                                    itShow.HighlightKey,
                                    null, itShow.ImageName,
                                    itShow.ActUseImage,
                                    null);

                        headerPnl.Children.Add(noTxt);
                        headerPnl.Children.Add(leaf);

                        var it = tvMain.xAppend(headerPnl, null);
                        it.Tag = itShow.Tag;
                        it.Margin = ItemMargin;

                        it.MouseDoubleClick += tvMain_MouseDoubleClick;
                        it.MouseLeftButtonUp += tvMain_MouseLeftButtonUp;
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            });
        }

        public class Item4Show
        {
            public string Text;
            public string HighlightKey;
            public string ImageName;
            public object Tag;
            public Action<string, Action<BitmapImage>> ActUseImage;

            public Item4Show(AnswerWithImage awi)
            {
                ActUseImage = RuleAnswerImageHelper.UseImage;
                ImageName = awi.ImageName;
                Tag = awi;
                Text = (awi.Answer ?? "");
            }

            public Item4Show(ShortcutEntity shortcut)
            {
                ActUseImage = ShortcutImageHelper.UseImage;
                HighlightKey = shortcut.Code;
                ImageName = shortcut.ImageName;
                Tag = shortcut;
                Text = shortcut.Text;
            }

            public Item4Show(string text, object tag)
            {
                Text = text;
                Tag = tag;
            }
        }

        private enum ShowListTypeEnum
        {
            Unknown,
            Nothing,
            InputLegend,
            InputCommand,
            AutoAnswer,
            GoodsLink
        }

    }
}
