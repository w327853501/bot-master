using BotLib;
using BotLib.Wpf.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Automation.ChatDeskNs
{
    public class Workbench
    {
        private int _hwnd;
        private string _seller;
        private const int WaitChromeInitTimeMs = 10000;
        private int _chatDeskButtonHwnd;
        private List<WinApi.WindowClue> _chatDeskButtonClue;
        private int _addrTextBoxHwnd;
        private List<WinApi.WindowClue> _AddrTextBoxClue;

        public Workbench(int hwnd, string seller)
		{
			_chatDeskButtonHwnd = 0;
			_addrTextBoxHwnd = 0;
			_hwnd = hwnd;
			_seller = seller;
		}

        public void BringTop()
        {
            WinApi.TopMost(_hwnd);
            DispatcherEx.DoEvents();
            WinApi.CancelTopMost(_hwnd);
        }

        public bool IsAlreadyExist { get; set; }

        public bool IsAlive
        {
            get
            {
                return WinApi.IsHwndAlive(_hwnd);
            }
        }
        public bool Nav(string text, ChatDesk chatDesk)
        {
            bool result = false;
            try
            {
                chatDesk.Automator.OpenWorkbench();
                Util.WaitFor(() => false, 300, 300, false);
                HwndInfo hwndInfo = new HwndInfo(AddrTextBoxHwnd, "AddrTextBoxHwnd");
                WinApi.ClickPointBySendMessage(hwndInfo.Handle, 30, 5);
                Util.WaitFor(() => false, 100, 100, false);
                WinApi.Editor.SetText(hwndInfo, text, true);
                for (int i = 0; i < 2; i++)
                {
                    Util.WaitFor(() => false, 100, 100, false);
                    WinApi.ClickHwndBySendMessage(hwndInfo.Handle, 1);
                    WinApi.PressEnterKey();
                }
                result = true;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                result = false;
            }
            return result;
        }

        public void HideWorkbench()
        {
            WinApi.HideWindow(_hwnd);
        }

        public void OpenChatDesk()
        {
            WinApi.ClickHwndBySendMessage(ChatDeskButtonHwnd, 2);
        }

        private int ChatDeskButtonHwnd
        {
            get
            {
                if (_chatDeskButtonHwnd == 0)
                {
                    _chatDeskButtonHwnd = WinApi.FindDescendantHwnd(_hwnd, ChatDeskButtonClue, "ChatDeskButtonHwnd");
                }
                return _chatDeskButtonHwnd;
            }
        }

        private List<WinApi.WindowClue> ChatDeskButtonClue
        {
            get
            {
                if (_chatDeskButtonClue == null)
                {
                    _chatDeskButtonClue = GetChatDeskButtonClue();
                }
                return _chatDeskButtonClue;
            }
        }

        private List<WinApi.WindowClue> GetChatDeskButtonClue()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StackPanel, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StackPanel, null, 2),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardButton, null, 1)
			};
        }

        private int AddrTextBoxHwnd
        {
            get
            {
                if (_addrTextBoxHwnd == 0)
                {
                    _addrTextBoxHwnd = WinApi.FindDescendantHwnd(_hwnd, AddrTextBoxClue, "AddrTextBoxHwnd");
                }
                return _addrTextBoxHwnd;
            }
        }

        private List<WinApi.WindowClue> AddrTextBoxClue
        {
            get
            {
                if (_AddrTextBoxClue == null)
                {
                    _AddrTextBoxClue = GetAddrTextBoxClue();
                }
                return _AddrTextBoxClue;
            }
        }

        private List<WinApi.WindowClue> GetAddrTextBoxClue()
        {
            return new List<WinApi.WindowClue>
			{
				new WinApi.WindowClue(WinApi.ClsNameEnum.StackPanel, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StackPanel, null, 1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StackPanel, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.StandardWindow, null, -1),
				new WinApi.WindowClue(WinApi.ClsNameEnum.EditComponent, null, -1)
			};
        }

        public void CloseWorkbench()
        {
            WinApi.CloseWindow(_hwnd, 2000);
        }

    }
}
