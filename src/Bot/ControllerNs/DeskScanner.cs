using BotLib.BaseClass;
using BotLib.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;
using BotLib;
using Bot.AssistWindow.NotifyIcon;
using Bot.Automation.ChatDeskNs.Automators;
using Bot.Common;
using Bot.AssistWindow.NotifyIcon.WorkMode;
using Bot.Robot.Rule.QaCiteTable;
using Bot.Options.HotKey;
using Bot.Automation;
using Bot.AssistWindow;
using Bot.Net.Api;
using Bot.Version;
using Bot.Common.Db;
using DbEntity;
using Bot.Automation.ChatDeskNs;

namespace Bot.ControllerNs
{
    public class DeskScanner : Disposable
    {
        private const int ScanIntervalMs = 1000;
        private static NoReEnterTimer _timer;
        private static int _notUseLoopCount;
        private static bool _hadDetectSellerEver;
        private static bool _hadTipNoSellerEver;

        static DeskScanner()
        {
            _notUseLoopCount = 0;
            _hadDetectSellerEver = false;
            _hadTipNoSellerEver = false;
        }

        public static void LoopScan()
        {
            _timer = new NoReEnterTimer(Loop, 1000, 0);
        }

        private async static void Loop()
        {
            try
            {
                RegisterHotKey();
                HashSet<string> closed;
                var selers = GetOpenedSellers(out closed);
                if (!selers.xIsNullOrEmpty())
                {
                    var nicks = selers.Keys.ToList();
                    var loginInfo = await BotApi.Login(nicks);
                    if (loginInfo == null)
                    {
                        Log.Error("Login返回null登录失败");
                        DelayCaller.CallAfterDelay(()=> {                         
                            ClientUpdater.Reboot();
                        },5000,false);
                    }
                    else
                    {
                        DealLoginReturn(loginInfo, nicks);
                    }
                }

                if (!closed.xIsNullOrEmpty())
                {
                    Log.Info("DeskScanner.Loop,关闭desk,sellers=" + closed.xToString(",", true));
                    var closeSet = HashSetEx.Create<string>(closed);
                    WndNotifyIcon.Inst.RemoveSellerMenuItem(closeSet);
                    foreach (var seller in closeSet)
                    {
                        Log.Info("DeskScanner.Loop,关闭desk,seller=" + seller);
                        var desk = ChatDesk.GetDeskFromCache(seller);
                        if (desk != null)
                        {
                            desk.Dispose();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private static void DealLoginReturn(LoginDownloadEntity loginInfo, List<string> nicks)
        {
            var nickDatas = loginInfo.NickDatas;
            for (int k = 0; k < nickDatas.Count; k++)
            {
                var nick = nickDatas[k];
                Util.Assert(!string.IsNullOrEmpty(nick));
                var workMode = WorkModeHelper.GetWorkMode(nick);
                if (workMode == WorkModeEnum.Assist)
                {
                    ReadDataFromDb(nick); //init data
                }
            }
            //从服务器同步数据
            DbSyner.Syn(); 

            WndNotifyIcon.Inst.AddSellerMenuItem(loginInfo.NickDatas); // create icon menu

            if (loginInfo.UpdateEntity != null)
            {
                ClientUpdater.UpdateForTip(loginInfo.UpdateEntity);
            }
        }

        private static void RegisterHotKey()
        {
            if (Params.InputSuggestion.IsUseDownKey)
            {
                int hWnd = WinApi.Api.GetForegroundWindow();
                if (hWnd != 0 && !WndAssist.AssistBag.xSafeForEach().Any(k => k.Handle == hWnd)
                    && !WndAssist.AssistBag.xSafeForEach().Any(k => k.Desk.Hwnd.Handle == hWnd))
                {
                    _notUseLoopCount++;
                }
                else
                {
                    _notUseLoopCount = 0;
                    if (!HotKeyHelper.IsDownKeyRegistered)
                    {
                        HotKeyHelper.TryRegisterDownKey();
                    }
                }
                if (_notUseLoopCount > 2 && HotKeyHelper.IsDownKeyRegistered)
                {
                    HotKeyHelper.TryUnRegisterDownKey();
                }
            }
        }

        public static void ReadDataFromDb(string sellerName)
        {
            CiteTableManager.InitCiteTables(TbNickHelper.ConvertNickToPubDbAccount(sellerName));
            CiteTableManager.InitCiteTables(TbNickHelper.ConvertNickToPrvDbAccount(sellerName));
        }

        private static Dictionary<string, LoginedSeller> GetOpenedSellers(out HashSet<string> closed)
        {
            var sellers = QnAccountFinderFactory.Finder.GetLoginedSellers();
            DetectQianniu(sellers);
            var newSellers = QnHelper.Detected.Update(sellers, out closed);
            return newSellers;
        }

        private static void DetectQianniu(Dictionary<string, LoginedSeller> sellers)
        {
            if (sellers.xIsNullOrEmpty())
            {
                if (!_hadDetectSellerEver && !_hadTipNoSellerEver)
                {
                    var msg = string.Empty;
                    if (QnHelper.HasQnRunning)
                    {
                        msg = string.Format("需要打开千牛【聊天窗口】,{0}才能起作用", "提示");
                    }
                    else
                    {
                        msg = string.Format("需要运行千牛，并打开聊天窗口，{0}才能起作用!!", "提示");
                    }
                    _hadTipNoSellerEver = true;
                    MsgBox.ShowTrayTip(msg, "没有检测到【千牛聊天窗口】", 30);
                }
            }
            else
            {
                _hadDetectSellerEver = true;
            }
        }

        protected override void CleanUp_Managed_Resources()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }

}
