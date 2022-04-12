using BotLib.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Automation.ChatDeskNs.Automators
{
    public class DeskAutomator
    {
        private HwndInfo _deskHwnd;
        private string _seller;
        private int _firstControlHwnd;
        private int _singleChatEditorHwnd;
        private int _groupChatEditorHwnd;
        private int _groupChatCloseButtonHwnd;
        private int _groupChatSendButtonHwnd;
        private int _toolbarPlusHwnd;
        private int _recentContactButtonHwnd;
        private int _buyerPicHwnd;
        private List<WinApi.WindowClue> _buyerPicClue;
        private int _ChatRecordChromeHwnd;
        private DateTime _chatRecordChromeHwndCacheTime;
        private List<WinApi.WindowClue> _ChatRecordChromeHwndClue;

        private List<WinApi.WindowClue> _groupChatEditorHwndClue;
        private List<WinApi.WindowClue> _groupChatCloseButtonHwndClue;
        private List<WinApi.WindowClue> _groupChatSendButtonHwndClue;
        private List<WinApi.WindowClue> _editorHwndClue;
        private List<WinApi.WindowClue> _sendMessageButtonHwndClue;
        private List<WinApi.WindowClue> _toolbarPlusClueDontUse;
        private List<WinApi.WindowClue> _recentContactButtonClueDontUse;
        private List<WinApi.WindowClue> _closeBuyerButtonHwndClue;
        private List<WinApi.WindowClue> _openWorkbenchButtonHwndClue;
        protected int _sendMessageButtonHwnd;
        protected int _closeBuyerButtonHwnd;
        protected int _openWorkbenchButtonHwnd;
        public int SingleChatEditorHwnd
        {
            get
            {
                if (_singleChatEditorHwnd == 0)
                {
                    _singleChatEditorHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetEditorHwndClue(), "SingleChatEditorHwnd");
                }
                return _singleChatEditorHwnd;
            }
        }
        public int GroupChatEditorHwnd
        {
            get
            {
                if (_groupChatEditorHwnd == 0)
                {
                    _groupChatEditorHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetGroupChatEditorHwndClue(), "GroupChatEditorHwnd");
                }
                return _groupChatEditorHwnd;
            }
        }
        public int GroupChatCloseButtonHwnd
        {
            get
            {
                if (_groupChatCloseButtonHwnd == 0 )
                {
                    _groupChatCloseButtonHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetGroupChatCloseButtonHwndClue(), "GroupChatCloseButtonHwnd");
                }
                return _groupChatCloseButtonHwnd;
            }
        }
        public int GroupChatSendButtonHwnd
        {
            get
            {
                if (_groupChatSendButtonHwnd == 0)
                {
                    _groupChatSendButtonHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetGroupChatSendButtonHwndClue(), "GroupChatSendButtonHwnd");
                }
                return _groupChatSendButtonHwnd;
            }
        }
        public int ToolbarPlusHwnd
        {
            get
            {
                if (_toolbarPlusHwnd == 0)
                {
                    _toolbarPlusHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetToolbarPlusClueDontUse(), "ToolbarPlusHwnd");
                }
                return _toolbarPlusHwnd;
            }
        }
        public int RecentContactButtonHwnd
        {
            get
            {
                if (_recentContactButtonHwnd == 0)
                {
                    _recentContactButtonHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetRecentContactButtonClue(), "RecentContactButtonHwnd");
                }
                return _recentContactButtonHwnd;
            }
        }
        public int BuyerPicHwnd
        {
            get
            {
                if (_buyerPicHwnd == 0)
                {
                    _buyerPicHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetBuyerPicClue(), "BuyerPicHwnd");
                }
                return _buyerPicHwnd;
            }
        }
        protected int SingleChatSendMessageButtonHwnd
        {
            get
            {
                if (_sendMessageButtonHwnd == 0)
                {
                    _sendMessageButtonHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetSendMessageButtonHwndClue(), "SingleChatSendMessageButtonHwnd");
                }
                return _sendMessageButtonHwnd;
            }
        }
        protected int SingleChatCloseButtonHwnd
        {
            get
            {
                if (_closeBuyerButtonHwnd == 0)
                {
                    _closeBuyerButtonHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetCloseBuyerButtonHwndClue(), "SingleChatCloseButtonHwnd");
                }
                return _closeBuyerButtonHwnd;
            }
        }
        protected int OpenWorkbenchButtonHwnd
        {
            get
            {
                if (_openWorkbenchButtonHwnd == 0)
                {
                    _openWorkbenchButtonHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, GetOpenWorkbenchButtonHwnd(), "OpenWorkbenchButtonHwnd");
                }
                return _openWorkbenchButtonHwnd;
            }
        }
        protected virtual string WidgetWindowTitlePattern
        {
            get
            {
                return ".*(?= - 工作台)";
            }
        }
        protected virtual string QnWindowProcessName
        {
            get
            {
                return "AliWorkbench";
            }
        }
        public DeskAutomator(HwndInfo deskhwnd, string seller)
        {
            _firstControlHwnd = 0;
            _singleChatEditorHwnd = 0;
            _groupChatEditorHwnd = 0;
            _groupChatCloseButtonHwnd = 0;
            _groupChatSendButtonHwnd = 0;
            _toolbarPlusHwnd = 0;
            _recentContactButtonHwnd = 0;
            _buyerPicHwnd = 0;
            _sendMessageButtonHwnd = 0;
            _closeBuyerButtonHwnd = 0;
            _openWorkbenchButtonHwnd = 0;
            _deskHwnd = deskhwnd;
            _seller = seller;
        }

        public virtual bool GetIsDeskVisible()
        {
            return WinApi.IsVisible(_deskHwnd.Handle);
        }

        public bool IsSingleChatCloseButtonEnable()
        {
            return SingleChatCloseButtonHwnd != 0 && WinApi.IsWindowEnabled(SingleChatCloseButtonHwnd);
        }

        public bool? IsSingleChatEditorVisible
        {
            get
            {
                return WinApi.IsVisible(SingleChatEditorHwnd);
            }
        }
        public bool IsSingleChatCloseButtonVisible()
        {
            return SingleChatCloseButtonHwnd != 0 && WinApi.IsVisible(SingleChatCloseButtonHwnd);
        }
        public bool IsGroupChatCloseButtonEnable()
        {
            return GroupChatCloseButtonHwnd != 0 && WinApi.IsWindowEnabled(GroupChatCloseButtonHwnd);
        }
        public bool IsAlive(bool useCache = true)
        {
            bool rt;
            if (!WinApi.IsHwndAlive(_deskHwnd.Handle))
            {
                rt = false;
            }
            else
            {
                bool hasTitle;
                string seller = GetSellerOfDesk(_deskHwnd, out hasTitle);
                rt = (!hasTitle || seller == _seller);
            }
            return rt;
        }
        public bool IsControlVisible()
        {
            return WinApi.IsVisible(GetDeskFirstControlHwnd());
        }
        private int GetDeskFirstControlHwnd()
        {
            if (_firstControlHwnd == 0)
            {
                WinApi.WindowClue clue = new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1);
                _firstControlHwnd = WinApi.FindChildHwnd(_deskHwnd.Handle, clue);
            }
            return _firstControlHwnd;
        }
        public void ClickSingleChatSendMessageButton()
        {
            WinApi.ClickHwndBySendMessage(SingleChatSendMessageButtonHwnd, 1);
        }
        public void ClickGroupChatSendMessageButton()
        {
            WinApi.ClickHwndBySendMessage(GroupChatSendButtonHwnd, 1);
        }
        public void ClickSingleChatCloseBuyerButton()
        {
            WinApi.ClickHwndBySendMessage(SingleChatCloseButtonHwnd, 1);
        }
        public void ClickGroupChatCloseBuyerButton()
        {
            WinApi.ClickHwndBySendMessage(GroupChatCloseButtonHwnd, 1);
        }
        public void OpenWorkbench()
        {
            WinApi.ClickHwndBySendMessage(OpenWorkbenchButtonHwnd, 1);
        }
        public Rectangle GetBuyerNameRegion()
        {
            int buyerPicHwnd = BuyerPicHwnd;
            Rectangle windowRectangle = WinApi.GetWindowRectangle(buyerPicHwnd);
            return new Rectangle(windowRectangle.Right + 8, windowRectangle.Top + 10, 200, 3);
        }
        private string IsChatWnd(HwndInfo hwndInfo)
        {
            string isChatWindow = "";
            string txt;
            if (WinApi.GetText(hwndInfo, out txt))
            {
                isChatWindow = RegexEx.Match(txt, QnAccountFinderFactory.Finder.ChatWindowTitlePattern);
            }
            return isChatWindow;
        }
        public void ClickRecentContactButton()
        {
            WinApi.ClickHwndBySendMessage(RecentContactButtonHwnd, 1);
        }
        private string GetSellerOfDesk(HwndInfo deskHwnd, out bool hasTitle)
        {
            string matchChatWindowTitle = "";
            string title;
            hasTitle = WinApi.GetText(deskHwnd, out title);
            if (hasTitle)
            {
                matchChatWindowTitle = RegexEx.Match(title, QnAccountFinderFactory.Finder.ChatWindowTitlePattern);
            }
            return matchChatWindowTitle;
        }
        private List<WinApi.WindowClue> GetBuyerPicClue()
        {
            if (_buyerPicClue == null)
            {
                _buyerPicClue = GetBuyerPicClueUnCache();
            }
            return _buyerPicClue;
        }
        protected virtual List<WinApi.WindowClue> GetBuyerPicClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardButton, null, -1)
			};
        }
        private List<WinApi.WindowClue> GetGroupChatEditorHwndClue()
        {
            if (_groupChatEditorHwndClue == null)
            {
                _groupChatEditorHwndClue = GetGroupChatEditorHwndClueUnCache();
            }
            return _groupChatEditorHwndClue;
        }
        protected virtual List<WinApi.WindowClue> GetGroupChatEditorHwndClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 5),
				new WinApi.WindowClue(WinApi.ClsNameEnum.SplitterBar, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.RichEditComponent, null, -1)
			};
        }
        private List<WinApi.WindowClue> GetGroupChatCloseButtonHwndClue()
        {
            if (_groupChatCloseButtonHwndClue == null)
            {
                _groupChatCloseButtonHwndClue = GetGroupChatCloseButtonHwndClueUnCache();
            }
            return _groupChatCloseButtonHwndClue;
        }
        protected virtual List<WinApi.WindowClue> GetGroupChatCloseButtonHwndClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 5),
				new WinApi.WindowClue(WinApi.ClsNameEnum.SplitterBar, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardButton, "关闭", -1)
			};
        }
        private List<WinApi.WindowClue> GetGroupChatSendButtonHwndClue()
        {
            if (_groupChatSendButtonHwndClue == null)
            {
                _groupChatSendButtonHwndClue = GetGroupChatSendButtonHwndClueUnCache();
            }
            return _groupChatSendButtonHwndClue;
        }
        protected virtual List<WinApi.WindowClue> GetGroupChatSendButtonHwndClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 5),
				new WinApi.WindowClue(WinApi.ClsNameEnum.SplitterBar, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardButton, "发送", -1)
			};
        }
        private List<WinApi.WindowClue> GetEditorHwndClue()
        {
            if (_editorHwndClue == null)
            {
                _editorHwndClue = GetEditorHwndClueUnCache();
            }
            return _editorHwndClue;
        }
        protected virtual List<WinApi.WindowClue> GetEditorHwndClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.SplitterBar, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.RichEditComponent, null, -1)
			};
        }
        private List<WinApi.WindowClue> GetSendMessageButtonHwndClue()
        {
            if (_sendMessageButtonHwndClue == null)
            {
                _sendMessageButtonHwndClue = GetSendMessageButtonHwndClueUnCache();
            }
            return _sendMessageButtonHwndClue;
        }
        protected virtual List<WinApi.WindowClue> GetSendMessageButtonHwndClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 2),
				new WinApi.WindowClue(WinApi.ClsNameEnum.SplitterBar, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardButton, "发送", -1)
			};
        }
        private List<WinApi.WindowClue> GetToolbarPlusClueDontUse()
        {
            if (_toolbarPlusClueDontUse == null)
            {
                _toolbarPlusClueDontUse = GetToolbarPlusClueUnCache();
            }
            return _toolbarPlusClueDontUse;
        }
        protected virtual List<WinApi.WindowClue> GetToolbarPlusClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.ToolBarPlus, null, -1)
			};
        }
        private List<WinApi.WindowClue> GetRecentContactButtonClue()
        {
            if (_recentContactButtonClueDontUse == null)
            {
                _recentContactButtonClueDontUse = GetRecentContactButtonClueUnCache();
            }
            return _recentContactButtonClueDontUse;
        }
        protected virtual List<WinApi.WindowClue> GetRecentContactButtonClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, "ID_ESERVICE_CONTROLPAGE", -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardButton, null, 1)
			};
        }
        private List<WinApi.WindowClue> GetCloseBuyerButtonHwndClue()
        {
            if (_closeBuyerButtonHwndClue == null)
            {
                _closeBuyerButtonHwndClue = GetCloseBuyerButtonHwndClueUnCache();
            }
            return _closeBuyerButtonHwndClue;
        }
        private List<WinApi.WindowClue> GetOpenWorkbenchButtonHwnd()
        {
            if (_openWorkbenchButtonHwndClue == null)
            {
                _openWorkbenchButtonHwndClue = GetOpenWorkbenchButtonHwndClueUnCache();
            }
            return _openWorkbenchButtonHwndClue;
        }
        protected virtual List<WinApi.WindowClue> GetCloseBuyerButtonHwndClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 2),
				new WinApi.WindowClue(WinApi.ClsNameEnum.SplitterBar, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardButton, "关闭", -1)
			};
        }
        protected virtual List<WinApi.WindowClue> GetOpenWorkbenchButtonHwndClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, "ID_ESERVICE_CONTROLPAGE", -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardButton, null, 1)
			};
        }
        protected virtual List<string> GetUsersFromProcessMainWindowTitle()
        {
            List<string> titles = new List<string>();
            Process[] processesByName = Process.GetProcessesByName(QnWindowProcessName);
            if (processesByName != null)
            {
                titles = processesByName.Select(new Func<Process, string>(GetChatTitleFromProcessMainWindowTitle)).ToList<string>();
            }
            return titles;
        }
        private string GetChatTitle(string title)
        {
            string text = MatchChatTitle(title);
            if (string.IsNullOrEmpty(text))
            {
                text = MatchWidgetWindowTitle(title);
            }
            return text;
        }
        private string MatchChatTitle(string title)
        {
            return RegexEx.Match(title.Trim(), QnAccountFinderFactory.Finder.ChatWindowTitlePattern).Trim();
        }
        private string MatchWidgetWindowTitle(string title)
        {
            return RegexEx.Match(title.Trim(), WidgetWindowTitlePattern).Trim();
        }
        private string GetChatTitleFromProcessMainWindowTitle(Process ps)
        {
            return GetChatTitle(ps.MainWindowTitle);
        }

        public int ChatRecordChromeHwnd
        {
            get
            {
                if (_ChatRecordChromeHwnd == 0 || _chatRecordChromeHwndCacheTime.xElapse().TotalSeconds > 5.0)
                {
                    _ChatRecordChromeHwnd = WinApi.FindDescendantHwnd(_deskHwnd.Handle, ChatRecordChromeHwndClue, "ChatRecordChromeHwnd");
                    _chatRecordChromeHwndCacheTime = DateTime.Now;
                }
                return _ChatRecordChromeHwnd;
            }
        }

        private List<WinApi.WindowClue> ChatRecordChromeHwndClue
        {
            get
            {
                if (_ChatRecordChromeHwndClue == null)
                {
                    _ChatRecordChromeHwndClue = ChatRecordChromeHwndClueUnCache();
                }
                return _ChatRecordChromeHwndClue;
            }
        }

        protected virtual List<WinApi.WindowClue> ChatRecordChromeHwndClueUnCache()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, 2),
				new WinApi.WindowClue(WinApi.ClsNameEnum.SplitterBar, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.PrivateWebCtrl, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.Aef_WidgetWin_0, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.Aef_RenderWidgetHostHWND, null, -1)
			};
        }


        private bool? _isGroupEditorVisible;
        public bool? IsGroupEditorVisible
        {
            get
            {
                if (_isGroupEditorVisible == null)
                {
                    _isGroupEditorVisible = WinApi.IsVisible(GroupChatEditorHwnd);
                }
                return _isGroupEditorVisible;
            }
        }
    }
}
