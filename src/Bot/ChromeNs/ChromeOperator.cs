using BotLib;
using BotLib.BaseClass;
using BotLib.Extensions;
using MasterDevs.ChromeDevTools;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Console;
using MasterDevs.ChromeDevTools.Protocol.Chrome.DOM;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Inspector;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Network;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Runtime;
using Newtonsoft.Json;
using Bot.Automation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BotLib.Net;

namespace Bot.ChromeNs
{
    public class ChromeOperator : Disposable
    {
        public class TitleNotExistException : Exception
        {
            public TitleNotExistException(string title)
                : base(string.Format("找不到title={0}的session", title))
            {

            }
        }
        public class SessionOcupiedException : Exception
        {
            public SessionOcupiedException(string title)
                : base(string.Format("session ocupied,title={0}", title))
            {
            }
        }
        private class TcpConnectionInfo
        {
            public enum TCP_TABLE_CLASS : int
            {
                TCP_TABLE_BASIC_LISTENER,
                TCP_TABLE_BASIC_CONNECTIONS,
                TCP_TABLE_BASIC_ALL,
                TCP_TABLE_OWNER_PID_LISTENER,
                TCP_TABLE_OWNER_PID_CONNECTIONS,
                TCP_TABLE_OWNER_PID_ALL,
                TCP_TABLE_OWNER_MODULE_LISTENER,
                TCP_TABLE_OWNER_MODULE_CONNECTIONS,
                TCP_TABLE_OWNER_MODULE_ALL
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_TCPROW_OWNER_PID
            {
                public uint state;
                public uint localAddr;
                public byte localPort1;
                public byte localPort2;
                public byte localPort3;
                public byte localPort4;
                public uint remoteAddr;
                public byte remotePort1;
                public byte remotePort2;
                public byte remotePort3;
                public byte remotePort4;
                public int owningPid;

                public ushort LocalPort
                {
                    get
                    {
                        return BitConverter.ToUInt16(
                            new byte[2] { localPort2, localPort1 }, 0);
                    }
                }

                public ushort RemotePort
                {
                    get
                    {
                        return BitConverter.ToUInt16(
                            new byte[2] { remotePort2, remotePort1 }, 0);
                    }
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_TCPTABLE_OWNER_PID
            {
                public uint dwNumEntries;
                MIB_TCPROW_OWNER_PID table;
            }

            [DllImport("iphlpapi.dll", SetLastError = true)]
            static extern uint GetExtendedTcpTable(IntPtr pTcpTable,
                ref int dwOutBufLen,
                bool sort,
                int ipVersion,
                TCP_TABLE_CLASS tblClass,
                int reserved);

            public static MIB_TCPROW_OWNER_PID[] GetAllTcpConnections()
            {
                MIB_TCPROW_OWNER_PID[] tTable;
                int AF_INET = 2;    // IP_v4
                int buffSize = 0;

                // how much memory do we need?
                uint ret = GetExtendedTcpTable(IntPtr.Zero,
                    ref buffSize,
                    true,
                    AF_INET,
                    TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                    0);
                if (ret != 0 && ret != 122) // 122 insufficient buffer size
                    throw new Exception("bad ret on check " + ret);
                IntPtr buffTable = Marshal.AllocHGlobal(buffSize);

                try
                {
                    ret = GetExtendedTcpTable(buffTable,
                        ref buffSize,
                        true,
                        AF_INET,
                        TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                        0);
                    if (ret != 0)
                        throw new Exception("bad ret " + ret);

                    // get the number of entries in the table
                    MIB_TCPTABLE_OWNER_PID tab =
                        (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(
                            buffTable,
                            typeof(MIB_TCPTABLE_OWNER_PID));
                    IntPtr rowPtr = (IntPtr)((long)buffTable +
                        Marshal.SizeOf(tab.dwNumEntries));
                    tTable = new MIB_TCPROW_OWNER_PID[tab.dwNumEntries];

                    for (int i = 0; i < tab.dwNumEntries; i++)
                    {
                        MIB_TCPROW_OWNER_PID tcpRow = (MIB_TCPROW_OWNER_PID)Marshal
                            .PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                        tTable[i] = tcpRow;
                        // next entry
                        rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));
                    }
                }
                finally
                {
                    // Free the Memory
                    Marshal.FreeHGlobal(buffTable);
                }
                return tTable;
            }

            public static int GetChromeListeningPortWithInterOp(int pid)
            {
                int port = 0;
                try
                {
                    MIB_TCPROW_OWNER_PID[] tcpConnections = GetAllTcpConnections();
                    for (int i = 0; i < tcpConnections.Length; i++)
                    {
                        MIB_TCPROW_OWNER_PID mIB_TCPROW_OWNER_PID = tcpConnections[i];
                        if (mIB_TCPROW_OWNER_PID.localAddr == 16777343u && mIB_TCPROW_OWNER_PID.owningPid == pid)
                        {
                            port = (int)mIB_TCPROW_OWNER_PID.LocalPort;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("GetChromeListeningPortWithInterOp,exp=" + ex.Message);
                    Trace.Assert(false, ex.Message);
                }
                return port;
            }
        }

        private bool hadevalinsertjs;
        private int Id;
        private Action<string> _onDetached;
        private bool _isEnableChromeConsoleListening;
        private bool _isEnableChromeInspectorListening;
        public ChromeSession Session
        {
            get;
            private set;
        }

        private ChromeOperator(ChromeSession session)
        {
            hadevalinsertjs = false;
            Id = RandomEx.Rand.Next();
            _isEnableChromeConsoleListening = false;
            _isEnableChromeInspectorListening = false;
            Session = session;
        }
        public ChromeOperator(string sessionInfoUrl, HashSet<string> titles, bool isChatRecord)
        {
            hadevalinsertjs = false;
            Id = RandomEx.Rand.Next();
            _isEnableChromeConsoleListening = false;
            _isEnableChromeInspectorListening = false;
            Session = CreateChromeSession(sessionInfoUrl, titles, isChatRecord);
        }

        public static ChromeOperator Create(string wsurl, string title)
        {
            var session = new ChromeSessionFactory().Create(wsurl, title);
            return new ChromeOperator(session);
        }
        public static ChromeOperator Create(string sessionInfoUrl, List<string> titleClues, out List<ChromeSessionInfo> otherWsUrls)
        {
            var chromeSession = CreateChromeSession(sessionInfoUrl, titleClues, out otherWsUrls);
            return (chromeSession == null) ? null : new ChromeOperator(chromeSession);
        }
        private static ChromeSession CreateChromeSession(string sessionInfoUrl, HashSet<string> titleClues, bool isChatRecord)
        {
            var qnSessions = GetWebSocketSessionInfos(sessionInfoUrl);
            var chatSessions = qnSessions.Where(k=> titleClues!=null && titleClues.Contains(k.Title)).Select(k=>k).ToList();
            if (chatSessions.xIsNullOrEmpty())
            {
                throw new TitleNotExistException("");
            }
            if (chatSessions.Count((x) => string.IsNullOrEmpty(x.WebSocketDebuggerUrl)) > 0)
            {
                throw new SessionOcupiedException("");
            }
            var sessionInfo = chatSessions[0];
            if (chatSessions.Count > 1 && isChatRecord)
            {
                sessionInfo = chatSessions.First((x) => !x.Url.ToLower().Contains("type=1&"));
            }
            return new ChromeSessionFactory().Create(sessionInfo.WebSocketDebuggerUrl, sessionInfo.Title);
        }
        private static ChromeSession CreateChromeSession(string sessionInfoUrl, List<string> titleClues, out List<ChromeSessionInfo> otherWsUrls)
        {
            otherWsUrls = null;
            var qnSessions = GetWebSocketSessionInfos(sessionInfoUrl);
            otherWsUrls = qnSessions.Where(k => !string.IsNullOrEmpty(k.WebSocketDebuggerUrl)).ToList();
            ChromeSessionInfo webSocketSessionInfo = null;
            foreach (var title in titleClues)
            {
                webSocketSessionInfo = otherWsUrls.FirstOrDefault(k => k.Title == title);
                if (webSocketSessionInfo != null)
                {
                    otherWsUrls.Remove(webSocketSessionInfo);
                    break;
                }
            }
            ChromeSession session = null;
            if (webSocketSessionInfo != null)
            {
                session = new ChromeSessionFactory().Create(webSocketSessionInfo.WebSocketDebuggerUrl, webSocketSessionInfo.Title);
            }
            return session;
        }

        public static string GetSessionInfoUrl(int hwnd)
        {
            if (hwnd == 0)
            {
                throw new Exception("GetSession,hwnd=0");
            }
            int pid = GetWindowThreadProcessId(hwnd);
            if (pid == 0)
            {
                throw new Exception("GetSession,pid=0");
            }
            int aliAppId = GetAliappId(pid);
            int port = GetChromeListeningPortWithInterOp(aliAppId);
            if (port == 0)
            {
                throw new Exception("GetSession,port=0");
            }
            return string.Format("http://localhost:{0}", port);
        }

        private static int GetAliappId(int workbenchPid)
        {
            var ps = WinApi.EnumProcesses();
            return ps.First(k => k.th32ParentProcessID == workbenchPid).th32ProcessID;
        }
        public MasterDevs.ChromeDevTools.Protocol.Chrome.Network.Cookie[] GetAllCookies()
        {
            MasterDevs.ChromeDevTools.Protocol.Chrome.Network.Cookie[] cookies = null;
            ICommandResponse res;
            if (SendCommandSafe<GetAllCookiesCommand>(out res, null))
            {
                var resCookies = res as CommandResponse<GetAllCookiesCommandResponse>;
                if (resCookies != null)
                {
                    cookies = resCookies.Result == null ? null : resCookies.Result.Cookies;
                }
            }
            return cookies;
        }

        public bool GetHtml(out string html)
        {
            bool hasGetHtml = false;
            html = "";
            ICommandResponse res;
            if (SendCommandSafe<GetDocumentCommand>(out res, null))
            {
                var resDoc = res as CommandResponse<GetDocumentCommandResponse>;
                long nodeId = resDoc.Result.Root.NodeId;
                GetOuterHTMLCommand parameter = new GetOuterHTMLCommand
                {
                    NodeId = nodeId
                };
                if (SendCommandSafe<GetOuterHTMLCommand>(out res, parameter))
                {
                    var resHtml = res as CommandResponse<GetOuterHTMLCommandResponse>;
                    html = (resHtml.Result.OuterHTML ?? "");
                    hasGetHtml = true;
                }
            }
            return hasGetHtml;
        }

        public void VerifySessionAlive()
        {
            try
            {
                if (!IsDisposed)
                {
                    
                }
            }
            catch (Exception ex)
            {
                Log.Error("VerifySessionAlive,exp=" + ex.Message);
                Dispose();
            }
        }

        public bool SendCommandSafe<T>(T parameter = null) where T : class
        {
            ICommandResponse commandResponse;
            return SendCommandSafe<T>(out commandResponse, parameter);
        }
        public bool SendCommandSafe<T>(out ICommandResponse rsp, T parameter = default(T)) where T : class
        {
            rsp = null;
            bool rt = true;
            try
            {
                if (Session == null)
                {
                    throw new Exception("Session=Null,parameter=" + parameter.ToString());
                }
                rsp = Session.SendCommand<T>(parameter);
                if (rsp == null)
                {
                    throw new Exception("SendCommand返回null");
                }
                if (rsp is IErrorResponse)
                {
                    IErrorResponse errorResponse = rsp as IErrorResponse;
                    throw new Exception(string.Concat(new object[]
					{
						"SendCommand error,msg=",
						errorResponse.Error.Message,
						",code=",
						errorResponse.Error.Code
					}));
                }
            }
            catch (Exception ex)
            {
                rt = false;
                Log.Error(ex.Message + ",ChromeService Id=" + Id);
                Log.Exception(ex);
            }
            return rt;
        }
        public bool Eval(out ICommandResponse rsp, string cmd)
        {
            EvaluateCommand parameter = EvaluateCommand.Create(cmd);
            return SendCommandSafe(out rsp, parameter);
        }
        public bool Eval(string cmd)
        {
            ICommandResponse commandResponse;
            return Eval(out commandResponse, cmd);
        }
        public bool Eval(string cmd, out string resultTxt)
        {
            resultTxt = null;
            ICommandResponse res;
            bool evalRt;
            if (evalRt = Eval(out res, cmd))
            {
                var resEval = res as CommandResponse<EvaluateCommandResponse>;
                var retVal = resEval.Result.Result.Value;
                resultTxt = ((retVal != null) ? retVal.ToString() : null);
            }
            return evalRt;
        }

        public bool EvalForStartJs()
        {
            string cmd = "imsdk.invoke('im.uiutil.GetCurrentConversationID',{}).then(res=>{console.log('onConversationChange,'+res.result.nick)});";
            return Eval(cmd);
        }

        public bool EvalForInsertJsSdk()
        {
            bool rt;
            if (hadevalinsertjs)
            {
                rt = true;
            }
            else
            {
                hadevalinsertjs = true;
                string cmd = " var oHead = document.getElementsByTagName('HEAD').item(0);                                             var oScript= document.createElement('script');                                             oScript.type = 'text/javascript';                                            oScript.src ='//g.alicdn.com/sj/qn/jssdk.js?t=20140115000000';                                            oHead.appendChild(oScript); ";
                var evalRt = Eval(cmd);
                cmd = " var oHead = document.getElementsByTagName('HEAD').item(0);                                             var oScript= document.createElement('script');                                             oScript.type = 'text/javascript';                                            oScript.src ='//g.alicdn.com/secdev/pointman/js/index.js#args=appkey%3d21619184';                                            oScript.app='QNPluginSecurity';                                            oHead.appendChild(oScript); ";
                rt = (evalRt && Eval(cmd));
            }
            return rt;
        }
        public bool IsListeningMessage()
        {
            string cmd = "typeof(window.___qnww)=='undefined'";
            string text;
            return Eval(cmd, out text) && text.ToLower() == "false";
        }
        public void EvalForMessageListen()
        {
            string cmd = @"if(typeof(window.___qnww)=='undefined'){ 
                            window.___qnww = window.onEventNotify;
                            window.onEventNotify = function (sid, name, a, data){
                                window.___qnww (sid, name, a, data);
                                name = JSON.parse(name);
                                if(sid.indexOf('onConversationChange')>=0){
	                                console.log('onConversationChange,'+name.nick);
                                }else if(sid.indexOf('onSendNewMsg')>=0){
	                                console.log('onSendNewMsg,'+name[0].cid.ccode);
                                }else if(sid.indexOf('onReceiveNewMsg')>=0){
                                    //imsdk.invoke('im.singlemsg.GetNewMsg', 
                                    //{
                                    //    ccode:name[0].ccode
                                    //}).then(res=>{                 
                                    //    console.log('onReceiveNewMsg,'+JSON.stringify(res));  
                                    //});
	                                //console.log('onReceiveNewMsg,'+name[0].ccode);
                            //        imsdk.invoke('im.singlemsg.GetRemoteHisMsg', 
                            //        {
                            //            cid:
                            //            {
                            //                ccode:name[0].ccode
                            //            },
                            //            count: 1,
		    		                    //gohistory: 1,
                            //            msgid:'-1',
                            //            msgtime: '-1',
                            //        }).then(res=>{
                            //            console.log('onReceiveNewMsg,'+JSON.stringify(res));  
                            //        });
                                }else if(sid.indexOf('onShopRobotReceriveNewMsgs')>=0){
	                                console.log('onShopRobotReceriveNewMsgs,'+name[0].cid.nick);
                                }else if(sid.indexOf('onConversationAdd')>=0){
	                                console.log('onConversationAdd,'+name.nick);
                                }else if(sid.indexOf('onConversationClose')>=0){
	                                console.log('onConversationClose,'+name.cid.nick);
                                }else if(sid.indexOf('onNetDisConnect')>=0){
	                                console.log('onNetDisConnect,'+name.nick);
                                }else if(sid.indexOf('onNetReConnectOK')>=0){
	                                console.log('onNetReConnectOK,'+name.nick);
                                }
                            }
                        }

                        if (typeof(window.onInvokeNotifyDelegate) == 'undefined') {
	                        window.onInvokeNotifyDelegate = window.onInvokeNotify;
	                        window.onInvokeNotify = function(sid, status, response) {
		                        window.onInvokeNotifyDelegate(sid, status, response);
		                        var task = TASK_CACHE[sid];
		                        if (task.config.fn == 'im.singlemsg.GetNewMsg') {
                                    console.log('onReceiveNewMsg,' + response);
                                }
                            }
                        }
                        
                        ";
            string text;
            if (Eval(cmd, out text) && text != null)
            {
                Log.Info("EvalForMessageListen,hwnd=" + Session.Title);
            }
        }
        public bool ClearChromeConsole()
        {
            return SendCommandSafe<ClearMessagesCommand>(null);
        }
        public void ListenChromeConsoleMessageAddedMessage(Action<ConsoleMessage> onReceivedMessage)
        {
            EnableChromeConsoleListening();
            Session.Subscribe<MessageAddedEvent>((x) =>
            {
                onReceivedMessage(x.Message);
            });
        }
        public void ListenChromeDetachedTurbo(Action<string> onDetached)
        {
            Detached(onDetached);
            TargetCrashed(() =>
            {
                onDetached("Chrome Crashed!");
            });
            _onDetached = onDetached;
            Session.ErrorOrClosed += Session_ErrorOrClosed;
        }
        
        private void Detached(Action<string> action)
        {
            EnableChromeInspectorListening();
            Session.Subscribe<DetachedEvent>((e) =>
            {
                action(e.Reason);
            });
        }
        private void Session_ErrorOrClosed(object sender, ErrorOrClosedEventArgs e)
        {
            if (_onDetached != null)
            {
                _onDetached(e.Reason);
            }
        }
        private void TargetCrashed(Action action)
        {
            EnableChromeInspectorListening();
            Session.Subscribe<TargetCrashedEvent>((e) =>
            {
                action();
            });
        }
        private bool EnableChromeConsoleListening()
        {
            if (!_isEnableChromeConsoleListening)
            {
                _isEnableChromeConsoleListening = SendCommandSafe<MasterDevs.ChromeDevTools.Protocol.Chrome.Console.EnableCommand>(null);
            }
            return _isEnableChromeConsoleListening;
        }
        private bool EnableChromeInspectorListening()
        {
            if (!_isEnableChromeInspectorListening)
            {
                _isEnableChromeInspectorListening = SendCommandSafe<MasterDevs.ChromeDevTools.Protocol.Chrome.Inspector.EnableCommand>(null);
            }
            return _isEnableChromeInspectorListening;
        }
        public static bool Connectable(int deskHwnd)
        {
            bool rt = false;
            try
            {
                var uri = GetSessionInfoUrl(deskHwnd);
                using (var myWebClient = new MyWebClient(2000))
                {
                    UriBuilder uriBuilder = new UriBuilder(uri);
                    uriBuilder.Path = "/json";
                    myWebClient.Encoding = Encoding.UTF8;
                    string value = myWebClient.DownloadString(uriBuilder.Uri);
                    var sessions = JsonConvert.DeserializeObject<List<ChromeSessionInfo>>(value);
                    rt = (sessions != null && sessions.xCount() > 0);
                }
            }
            catch
            {
                Log.Error("can't connect ws.");
            }
            return rt;
        }
        private static List<ChromeSessionInfo> GetWebSocketSessionInfos(string endpointUrl)
        {
            new List<ChromeSessionInfo>();
            List<ChromeSessionInfo> sessionInfos = null;
            using (WebClient webClient = new WebClient())
            {
                UriBuilder uriBuilder = new UriBuilder(endpointUrl);
                uriBuilder.Path = "/json";
                webClient.Encoding = Encoding.UTF8;
                string value = webClient.DownloadString(uriBuilder.Uri);
                sessionInfos = JsonConvert.DeserializeObject<List<ChromeSessionInfo>>(value);
            }
            return sessionInfos;
        }
        private static int GetChromeListeningPortWithInterOp(int pid)
        {
            int port = 0;
            if (pid > 0)
            {
                port = TcpConnectionInfo.GetChromeListeningPortWithInterOp(pid);
            }
            return port;
        }
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(int hWnd, ref int lpdwProcessId);

        private static int GetWindowThreadProcessId(int hwnd)
        {
            int lpdwProcessId = 0;
            GetWindowThreadProcessId(hwnd, ref lpdwProcessId);
            return lpdwProcessId;
        }
        protected override void CleanUp_Managed_Resources()
        {
            if (Session != null)
            {
                Session.Dispose();
            }
            if (_onDetached != null)
            {
                _onDetached("co disposed.");
            }
        }

    }
}
