using BotLib;
using BotLib.Extensions;
using BotLib.Wpf.Extensions;
using Bot.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bot.AssistWindow.Widget;
using Bot.AssistWindow;
using DbEntity;
using Bot.Common.ImageHelper;

namespace Bot.Automation.ChatDeskNs
{
    public class DeskEditor
    {
        private ChatDeskEventArgs _evtArgs;
        private string _cachedText;
        private DateTime _cacheTextTime;
        private DateTime _preCachedTime;
        private ChatDesk _desk;
        private DateTime _preSendPlainTextTime;
        private DateTime _ShowEditorErrorTipTime;
        private object _setPlainTextSynObj;
        private string _preSendText;
        private BitmapImage _preImg;
        private DateTime _preSetImgTime;
        private bool _isSetPlainTextAndImageBusy;
        private DateTime _preSendPlainTextAndImageTime;
        private string _preSendPlainTextAndImageText;
        private BitmapImage _preSendPlainTextAndImageImage;
        private object _sendPtaiSynobj;
        public DateTime LatestSetTextTime;
        public event EventHandler<ChatDeskEventArgs> EvEditorTextChanged;
        public string PlainTextCached
        {
            get
            {
                return GetPlainTextUseCached(true);
            }
        }
        public string LastSetPlainText
        {
            get;
            private set;
        }
        public DeskEditor(int editorHwnd, ChatDesk chatDesk)
        {
            _cacheTextTime = DateTime.MinValue;
            _preSendPlainTextTime = DateTime.MinValue;
            _preSendText = null;
            _preSendPlainTextAndImageTime = DateTime.Now;
            _sendPtaiSynobj = new object();
            _setPlainTextSynObj = new object();
            _desk = chatDesk;
            _evtArgs = new ChatDeskEventArgs
            {
                Desk = chatDesk
            };
        }
        private void EditorTextChanged()
        {
            if (_desk.AssistWindow != null)
            {
                if (EvEditorTextChanged != null)
                {
                    EvEditorTextChanged(_desk, _evtArgs);
                }
            }
        }

        public string GetPlainTextUnCached()
        {
            return GetPlainTextUseCached(false);
        }

        private string GetPlainTextUseCached(bool useCache)
        {
            if (!useCache || _cachedText == null)
            {
                var cachedText = _cachedText;
                _cachedText = GetPlainTextInner();
                _cacheTextTime = DateTime.Now;
                if (cachedText != _cachedText)
                {
                    EditorTextChanged();
                }
            }
            return _cachedText ?? "";
        }
        public void ClearCachedText()
        {
            _cachedText = null;
        }
        private string GetPlainTextInner()
        {
            string result = "";
            HwndInfo hwndInfo = GetActivedEditorHwnd();
            if (hwndInfo.Handle != 0 && !WinApi.Editor.GetText(hwndInfo, out result))
            {
                Log.Error("无法从编辑器中获取文本！,hwnd=" + hwndInfo);
                _desk.CheckAlive();
                result = "";
            }
            return result;
        }
        public void SetPlainTextAsync(string text, bool moveCaretToEnd = true, bool focusEditor = true, Action cb = null)
        {
            Task.Factory.StartNew(() =>
            {
                if (_desk.IsChatRecordChromeOk && !string.IsNullOrEmpty(_desk.Buyer))
                {
                    _desk.InsertText2Inputbox(_desk.Buyer,text);
                }
                else
                {
                    SetPlainText(text, moveCaretToEnd, focusEditor);
                }
                if (cb != null) cb();
            });
        }
        public void SetPlainText()
        {
            for (int i = 0; i < 5; i++)
            {
                if (SetPlainText("", true))
                {
                    string value = GetPlainTextUnCached();
                    if (string.IsNullOrEmpty(value))
                    {
                        break;
                    }
                    Util.SleepWithDoEvent(50);
                }
                else
                {
                    Util.SleepWithDoEvent(50);
                }
            }
        }

        public bool SetPlainText(string txt, bool moveCaretToEnd = true, bool focusEditor = true)
        {
            if (string.IsNullOrEmpty(txt)) txt = string.Empty;
            var rt = false;
            lock (_setPlainTextSynObj)
            {
                HwndInfo hwndInfo = GetActivedEditorHwnd();
                if (hwndInfo.Handle > 0)
                {
                    if (!WinApi.Editor.SetText(hwndInfo, txt, moveCaretToEnd))
                    {
                        Log.Error("无法设置文本到编辑器");
                        _desk.CheckAlive();
                    }
                    else
                    {
                        LastSetPlainText = txt;
                        GetPlainTextUnCached();
                        rt = true;

                        EnsureShowEmoji(txt, hwndInfo.Handle);
                        if (focusEditor)
                        {
                            FocusEditor(focusEditor);
                        }
                    }
                }
            }
            return rt;
        }

        public void FocusEditor()
        {
            _desk.BringTopForMs(1000);
            var hwndInfo = GetActivedEditorHwnd();
            if (hwndInfo.Handle != 0)
            {
                WinApi.FocusWnd(hwndInfo);
            }
        }
        private void EnsureShowEmoji(string text, int hwnd)
        {
            if (text.Contains("/:"))
            {
                WinApi.Editor.MoveCaretToEnding(new HwndInfo(hwnd, "EnsureShowEmoji"));
                FocusEditor();
                WinApi.PressDot();
                if (PlainTextEndWithDot(1000))
                {
                    WinApi.PressBackSpace();
                }
            }
        }
        private bool PlainTextEndWithDot(int ms)
        {
            DateTime now = DateTime.Now;
            bool rt = false;
            while (!rt && (DateTime.Now - now).TotalMilliseconds < (double)ms)
            {
                rt = GetPlainTextUseCached(false).EndsWith(".");
                Thread.Sleep(20);
            }
            return rt;
        }

        public bool InsertRichTextToEditor(string rtf, string buyer)
        {
            var rt = false;
            try
            {
                HwndInfo hwndInfo = GetActivedEditorHwnd();
                if (hwndInfo.Handle > 0)
                {
                    WinApi.Editor.PasteRichText(hwndInfo, rtf);
                    rt = true;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
                rt = false;
            }
            return rt;
        }
        public void SendPlainTextAsync(string text, Action cb = null)
        {
            Task.Factory.StartNew(() =>
            {
                if (_desk.IsChatRecordChromeOk && !string.IsNullOrEmpty(_desk.Buyer))
                {
                    _desk.SendMsg(_desk.Buyer, text);
                    ClearPlainText();
                }
                else
                {
                    SendPlainText(text);
                }
                if (cb != null) cb();
            });
        }

        public void SetAnswerAsync(AnswerWithImage awi)
        {
            Task.Factory.StartNew(() => {
                SetAnswer(awi);
            }, TaskCreationOptions.LongRunning);
        }

        public void SetAnswer(AnswerWithImage awi)
        {
            var image = GetImage(awi);
            if (image == null)
            {
                SetPlainText(awi.Answer);
            }
            else
            {
                SetPlainTextAndImage(awi.Answer, image);
            }
        }

        private BitmapImage GetImage(AnswerWithImage awi)
        {
            BitmapImage image = null;
            if (!string.IsNullOrEmpty(awi.ImageName))
            {
                image = RuleAnswerImageHelper.UseImage(awi.ImageName);
            }
            return image;
        }

        public void SendAnswerAsync(AnswerWithImage awi,Action callback)
        {
            Task.Factory.StartNew(()=> {
                SendAnswer(awi);
                if (callback != null) callback();
            }, TaskCreationOptions.LongRunning);
        }

        public void SendAnswer(AnswerWithImage awi)
        {
            var image = GetImage(awi);
            if (image == null)
            {
                SendPlainTextAsync(awi.Answer);
            }
            else
            {
                SendPlainTextAndImage(awi.Answer, image);
            }
        }

        public void SendPlainText(string text)
        {
            try
            {
                if ((DateTime.Now - _preSendPlainTextTime).TotalSeconds >= 1.0 || !(text == _preSendText))
                {
                    _preSendPlainTextTime = DateTime.Now;
                    _preSendText = text;
                    if (SetPlainText(text, false))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            ClickSendButton();
                            Util.SleepWithDoEvent(50);
                            if (string.IsNullOrEmpty(GetPlainTextUnCached()))
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
        public void ClickSendButton()
        {
            bool? isGroupChat = _desk.IsGroupChat;
            if (isGroupChat.HasValue)
            {
                if (isGroupChat.Value)
                {
                    _desk.Automator.ClickGroupChatSendMessageButton();
                }
                else
                {
                    _desk.Automator.ClickSingleChatSendMessageButton();
                }
            }
        }
        public void SetOrSendPlainText(string text, bool isSend)
        {
            if (isSend)
            {
                SendPlainText(text);
            }
            else
            {
                SetPlainTextAsync(text, true);
            }
        }
        private bool IsNewImage(BitmapImage image)
        {
            return image != null && image == _preImg && _preSetImgTime.xElapse().TotalSeconds < 1.0;
        }

        public bool SetPlainTextAndImage(string text, BitmapImage image, bool focusEditor = true)
        {
            bool rt = false;
            if (!_isSetPlainTextAndImageBusy)
            {
                _isSetPlainTextAndImageBusy = true;
                try
                {
                    if (!text.EndsWith("\r\n"))
                    {
                        text += "\r\n";
                    }
                    if (SetPlainText(text, false, focusEditor) && image != null
                        && !IsNewImage(image) && (rt = SetPlainImage(image)))
                    {
                        _preImg = image;
                        _preSetImgTime = DateTime.Now;
                    }
                }
                catch (Exception e)
                {
			        Log.Exception(e);
                }

                _isSetPlainTextAndImageBusy = false;
            }
            return rt;
        }
        public bool SendPlainTextAndImage(string text, BitmapImage image)
        {
            bool rt = false;
            lock (_sendPtaiSynobj)
            {
                if ((DateTime.Now - _preSendPlainTextAndImageTime).TotalSeconds < 1.1 
                    && _preSendPlainTextAndImageText == text 
                    && _preSendPlainTextAndImageImage == image)
                {
                    rt = false;
                }
                else
                {
                    _preSendPlainTextAndImageTime = DateTime.Now;
                    _preSendPlainTextAndImageText = text;
                    _preSendPlainTextAndImageImage = image;
                    if (SetPlainTextAndImage(text, image))
                    {
                        ClickSendButton();
                        rt = true;
                    }
                    else
                    {
                        rt = false;
                    }
                }
            }
            return rt;
        }
        private bool SetPlainImage(BitmapImage img)
        {
            bool isok = false;
            DispatcherEx.xInvoke(() =>
            {
                HwndInfo hwndInfo = GetActivedEditorHwnd();
                if (hwndInfo.Handle > 0)
                {
                    FocusEditor(true);
                    WinApi.Editor.MoveCaretToEnding(hwndInfo);
                    var dict = ClipboardEx.Backup();
                    Clipboard.Clear();
                    Clipboard.SetImage(img);
                    var cacheTxt = GetPlainCachedText();
                    WinApi.PressCtrlV();
                    DateTime now = DateTime.Now;
                    
                    do {
                        string newTxt = GetPlainCachedText();
                        if (newTxt != cacheTxt)
                        {
                            isok = true;
                            break;
                        }
                        DispatcherEx.DoEvents();
                    } while ((DateTime.Now - now).TotalSeconds < 2.0) ;
                    Util.WriteTimeElapsed(now, "等待时间");
                    ClipboardEx.Restore(dict);
                    return;
                }
            });
            return isok;
        }
        public bool FocusEditor(bool focusEditor)
        {
            bool isok = false;
            DispatcherEx.xInvoke(() => 
            {
                if (focusEditor)
                {
                    _desk.BringTop();
                }
                HwndInfo hwndInfo = GetActivedEditorHwnd();
                if (hwndInfo.Handle > 0)
                {
                    isok = WinApi.SetFocus(hwndInfo);
                }
            });
            return isok;
        }

        public void SendTextToSomebody(string buyer, string text)
        {
            if (!string.IsNullOrEmpty(buyer) && buyer != _desk.Seller)
            {
                if (_desk.IsChatRecordChromeOk)
                {
                    _desk.SendMsg(buyer,text);
                }
                else
                {
                    OpenAndSendTextToSomebody(buyer, text);
                }
            }
        }
        public void SendTextToSomebodyAsync(string buyer, string text)
        {
            Task.Factory.StartNew(() =>
            {
                SendTextToSomebody(buyer, text);
            }, TaskCreationOptions.LongRunning);
        }
        private bool OpenAndSendTextToSomebody(string buyer, string text)
        {
            bool sendResult = false;
            string arguments = string.Format("aliim:sendmsg?uid=cntaobao{0}&touid=cntaobao{1}", _desk.Seller, buyer);
            string wwcmdPath = QnHelper.WwcmdPath;
            ProcessStartInfo startInfo = new ProcessStartInfo(wwcmdPath, arguments);
            Process.Start(startInfo);
            if (!_desk.IsVisible)
            {
                _desk.Show();
                Util.WaitFor(new Func<bool>(() => _desk.IsVisible), 3000, 10, false);
            }
            Thread.Sleep(500);
            Util.WaitFor(() => _desk.Buyer == buyer || _desk.HasBuyerButCantGetName(), 5000, 10, false);
            if (_desk.Buyer == buyer)
            {
                SendPlainText(text);
                sendResult = true;
            }
            return sendResult;
        }
        public HwndInfo GetActivedEditorHwnd()
        {
            int groupChatEditorHwnd = 0;
            if (_desk.IsGroupChat.HasValue && _desk.IsGroupChat.Value)
            {
                groupChatEditorHwnd = _desk.Automator.GroupChatEditorHwnd;
            }
            else
            {
                groupChatEditorHwnd = _desk.Automator.SingleChatEditorHwnd;
            }
            return new HwndInfo(groupChatEditorHwnd, "DeskEditor");
        }
        public void GetPlainText()
        {
            if ((DateTime.Now - _cacheTextTime).TotalMilliseconds > 500.0)
            {
                GetPlainTextUnCached();
            }
        }

        public HwndInfo GetChatEditorHwndInfo()
        {
            int chatEditorHwnd;
            if (_desk.IsGroupChat.GetValueOrDefault() & _desk.IsGroupChat.HasValue)
            {
                chatEditorHwnd = _desk.Automator.GroupChatEditorHwnd;
            }
            else
            {
                chatEditorHwnd = _desk.Automator.SingleChatEditorHwnd;
            }
            return new HwndInfo(chatEditorHwnd, "DeskEditor");
        }
        public bool IsEmptyForPlainCachedText(bool force = false)
        {
            string text = GetPlainCachedText(force);
            return string.IsNullOrEmpty((text != null) ? text.Trim() : null);
        }
        public string GetPlainCachedText(bool force = false)
        {
            string text;
            if (((_cachedText == null | force) || _preCachedTime.xIsTimeElapseMoreThanMs(50)) && GetPlainTextInner(out text))
            {
                if (text != _cachedText)
                {
                    _cachedText = text;
                    _preCachedTime = DateTime.Now;
                    EditorTextChanged();
                }
                else
                {
                    _preCachedTime = DateTime.Now;
                }
            }
            return _cachedText ?? string.Empty;
        }
        private void HideEditorErrorTip()
        {
            if (_ShowEditorErrorTipTime != DateTime.MinValue)
            {
                _ShowEditorErrorTipTime = DateTime.MinValue;
                if (_desk.AssistWindow != null)
                {
                    if (_desk.AssistWindow.ctlBottomPanel.TheTipper != null)
                    {
                        _desk.AssistWindow.ctlBottomPanel.TheTipper.ShowTip(null);
                    }
                }
            }
        }
        private bool GetPlainTextInner(out string txt)
        {
            bool rt = false;
            txt = "";
            try
            {
                HwndInfo hwndInfo = GetChatEditorHwndInfo();
                if (hwndInfo.Handle != 0)
                {
                    string err;
                    if (WinApi.Editor.GetText(hwndInfo, out txt, out err, true))
                    {
                        rt = true;
                        HideEditorErrorTip();
                    }
                    else
                    {
                        txt = "";
                        Log.Error(string.Format("无法从编辑器中获取文本！,hwnd={0},err={1}", hwndInfo.Handle, err));
                        if (_desk.HasSingleOrGroupChat)
                        {
                            ShowEditorErrorTipIfNeed("无法从千牛【回复框】中读取内容。请【重启】软件。");
                        }
                        else
                        {
                            HideEditorErrorTip();
                        }
                    }
                }
                else
                {
                    Log.Info("千牛回复框句柄为0,seller=" + _desk.Seller);
                    ShowEditorErrorTipIfNeed("无法【检测到】千牛【回复框】。请【重启】软件。");
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            return rt;
        }
        private void ShowEditorErrorTipIfNeed(string tip)
        {
            if (_ShowEditorErrorTipTime.xIsTimeElapseMoreThanSecond(10))
            {
                try
                {
                    _ShowEditorErrorTipTime = DateTime.Now;
                    WndAssist assistWindow = _desk.AssistWindow;
                    if (assistWindow != null)
                    {
                        BottomPanel.Tipper theTipper = assistWindow.ctlBottomPanel.TheTipper;
                        if (theTipper != null)
                        {
                            theTipper.ShowTip(tip);
                        }
                    }
                    Log.Info(string.Format("ShowEditorError:IsGroupChat={0},IsSingleChat={1}", _desk.IsGroupChat, _desk.IsSingleChat));
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }
        public bool HasEmoji()
        {
            string text = GetPlainCachedText(false);
            return !string.IsNullOrEmpty(text) && text.Contains("/:");
        }
        public void ClearPlainText(int tryCnt = 3)
        {
            var n = 0;
            while (n < tryCnt && (!SetPlainText("", true, true) || HasText()))
            {
                Thread.Sleep(50);
                n++;
            }
        }
        private bool HasText()
        {
            var txt = GetPlainCachedText(true);
            return !string.IsNullOrEmpty(txt);
        }
    }

}
