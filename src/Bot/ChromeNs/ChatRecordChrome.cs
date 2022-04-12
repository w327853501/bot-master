using BotLib;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BotLib.Extensions;
using Bot.Automation.ChatDeskNs;
using Bot.Common;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Top.Api;
using System.Collections.Concurrent;
using Top.Api.Util;
using System.Threading.Tasks;
using System.Web;
using Top.Api.Response;
using Top.Api.Domain;
using DbEntity;

namespace Bot.ChromeNs
{
    public class ChatRecordChrome : ChromeConnector
    {
        private HashSet<string> _chatRecordChromeTitle;
        private DateTime _preListeningTime;
        public event EventHandler<BuyerSwitchedEventArgs> EvBuyerSwitched;
        public event EventHandler<RecieveNewMessageEventArgs> EvRecieveNewMessage;
        public event EventHandler<ShopRobotReceriveNewMessageEventArgs> EvShopRobotReceriveNewMessage;
        public string PreBuyer
        {
            get;
            private set;
        }
        public string CurBuyer
        {
            get;
            private set;
        }


        private ConcurrentDictionary<long, ManualResetEventSlim> _requestWaitHandles = new ConcurrentDictionary<long, ManualResetEventSlim>();
        private ConcurrentDictionary<long, TopResponse> _responses = new ConcurrentDictionary<long, TopResponse>();
        private ConcurrentDictionary<long, string> _imsdkResponses = new ConcurrentDictionary<long, string>();
        long _incrementCount = 0;

        public ChatRecordChrome(ChatDesk desk)
            : base(desk.Hwnd.Handle, "nick_" + desk.Seller)
        {
            _chatRecordChromeTitle = new HashSet<string>
			{
				"当前聊天窗口",
				"IMKIT.CLIENT.QIANNIU",
				"聊天窗口",
				"imkit.qianniu",
                "千牛聊天消息",
                "千牛消息聊天"
			};
            _preListeningTime = DateTime.Now;
            Timer.AddAction(FetchRecordLoop, 300, 300);
        }
        public bool GetHtml(out string html, int timeoutMs = 500)
        {
            html = "";
            bool result = false;
            if (WaitForChromeOk(timeoutMs))
            {
                result = true;
            }
            return result;
        }
        protected override void ClearStateValues()
        {
            base.ClearStateValues();
            CurBuyer = "";
            PreBuyer = "";
        }

        protected override ChromeOperator CreateChromeOperator(string chromeSessionInfoUrl)
        {
            var chromeOp = new ChromeOperator(chromeSessionInfoUrl, _chatRecordChromeTitle, true);
            chromeOp.ClearChromeConsole();
            chromeOp.ListenChromeConsoleMessageAddedMessage(DealChromeConsoleMessage);
            chromeOp.EvalForMessageListen();
            chromeOp.EvalForStartJs();
            return chromeOp;
        }

        private void DealChromeConsoleMessage(ConsoleMessage consoleMessage)
        {
            try
            {
                var text = consoleMessage.Text.Trim();
                if (text.StartsWith("ChromeImSdkConsoleLog,"))
                {
                    text = text.Substring("ChromeImSdkConsoleLog,".Length); var jObject = JObject.Parse(text);
                    var commandId = long.Parse(jObject["task_command_id"].ToString());
                    var apiName = jObject["api_name"].ToString();
                    var sdkResponse = jObject["sdk_response"].ToString();
                    HandleResponse(sdkResponse, commandId);
                }
                else if (text.StartsWith("onConversationChange,"))
                {
                    var buyer = text.Substring("onConversationChange,".Length);
                    BuyerSwitched(buyer, null);
                }
                else if (text.StartsWith("onSendNewMsg,"))
                {
                    var buyer = text.Substring("onSendNewMsg,".Length);
                    if (string.IsNullOrEmpty(CurBuyer) || CurBuyer == buyer)
                    {
                    }
                }
                else if (text.StartsWith("onReceiveNewMsg,"))
                {
                    var msgResponse = text.Substring("onReceiveNewMsg,".Length);
                    RecieveNewMessage(msgResponse);
                }
                else if (text.StartsWith("onShopRobotReceriveNewMsgs,"))
                {
                    var buyer = text.Substring("onShopRobotReceriveNewMsgs,".Length);
                    ShopRobotReceriveNewMessage(buyer);
                }
                else if (text.StartsWith("onConversationAdd,"))
                {
                    Util.WriteTrace("ChromeMessageConsumer" + text);
                }
                else if (text.StartsWith("onConversationClose,"))
                {
                    var buyer = text.Substring("onConversationClose,".Length);
                }
                else if (text.StartsWith("onNetDisConnect,"))
                {
                    Log.WriteLine("ChromeMessageConsumer:" + text, new object[0]);
                }
                else if (text.StartsWith("onNetReConnectOK,"))
                {
                    Util.WriteTrace("ChromeMessageConsumer" + text);
                    Log.WriteLine("ChromeMessageConsumer:" + text, new object[0]);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            finally {
                
            }
        }

        private void HandleResponse(string response, long commandId)
        {
            if (null == response) return;
            ManualResetEventSlim requestMre;
            if (_requestWaitHandles.TryGetValue(commandId, out requestMre))
            {
                _imsdkResponses.AddOrUpdate(commandId, id => response, (key, value) => response);
                requestMre.Set();
            }
            else
            {
                if (1 == _requestWaitHandles.Count)
                {
                    var requestId = _requestWaitHandles.Keys.First();
                    _requestWaitHandles.TryGetValue(requestId, out requestMre);
                    _imsdkResponses.AddOrUpdate(requestId, id => response, (key, value) => response);
                    requestMre.Set();
                }
            }
        }

        private void RecieveNewMessage(string msg)
        {
            if (EvRecieveNewMessage != null)
            {
                EvRecieveNewMessage(this, new RecieveNewMessageEventArgs
                {
                    Buyer = string.Empty,
                    Connector = this,
                    Message = msg
                });
            }
        }

        private void ShopRobotReceriveNewMessage(string buyer)
        {
            if (EvShopRobotReceriveNewMessage != null)
            {
                EvShopRobotReceriveNewMessage(this, new ShopRobotReceriveNewMessageEventArgs
                {
                    Buyer = buyer,
                });
            }
        }

        private void FetchRecordLoop()
        {
            try
            {
                if (IsChromeOk)
                {
                    if ((DateTime.Now - _preListeningTime).TotalSeconds > 5.0)
                    {
                        _preListeningTime = DateTime.Now;
                        if (ChromOp != null)
                        {
                            ChromOp.EvalForMessageListen();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private void BuyerSwitched(string preBuyer, string curBuyer = null)
        {
            if (preBuyer != curBuyer)
            {
                PreBuyer = (curBuyer ?? CurBuyer);
                CurBuyer = preBuyer;
                if (EvBuyerSwitched != null)
                {
                    EvBuyerSwitched(this, new BuyerSwitchedEventArgs
                    {
                        CurBuyer = preBuyer,
                        PreBuyer = PreBuyer,
                        Connector = this
                    });
                }
            }
        }

        public void SendMsg(string uid, string msg)
        {
            var cmd = @"
                imsdk.invoke('intelligentservice.SendSmartTipMsg', {
                    userId: '{uid}',
                    smartTip: decodeURIComponent('{msg}')
                }).then((data)=>{
                    
                });";
            cmd = cmd.Replace("{uid}", uid).Replace("{msg}", HttpUtility.HtmlEncode(msg));
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public void TransferContact(string contactID, string targetID,string reason="")
        {
            var cmd = @"
                imsdk.invoke('application.openChat',{nick:'{contactID}'})
                .then((data)=>{
                    imsdk.invoke('application.transferContact', {
                        contactID: '{contactID}',
                        targetID: '{targetID}',
                        reason: '{reason}'
                    });
                });";
            cmd = cmd.Replace("{contactID}", "cntaobao" + contactID).Replace("{targetID}", "cntaobao"+targetID).Replace("{reason}", reason);
            if (ChromOp != null)
            {
                OpenChat(contactID);
                ChromOp.Eval(cmd);
            }
        }

        public void InsertText2Inputbox(string uid, string text)
        {
            var cmd = @"
                QN.app.invoke({
                    api: 'insertText2Inputbox',
                    query: {
                      uid: '{uid}',
                      text : decodeURIComponent('{text}')  
                    },
                  });";
            cmd = cmd.Replace("{uid}", "cntaobao"+uid).Replace("{text}", HttpUtility.HtmlEncode(text));
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public void SendCheckOrderCard(string tid, string buyer)
        {
            var cmd = @"
                imsdk.invoke('application.invokeMTopChannelService', {
                    method: 'mtop.taobao.bcpush.order.check.card.send',
                    param:{bizOrderId:'{tid}',buyerNick:'{buyer}'},
                    httpMethod: 'post',
                    version: '1.0'
                });";
            cmd = cmd.Replace("{tid}", tid).Replace("{buyer}", buyer);
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public void SendRemindPayCard(string buyer,string tid,string payment,string tradeNum, string orderTitle,string orderPicUrl)
        {
            var cmd = @"
                imsdk.invoke('application.invokeMTopChannelService', {
                    method: 'mtop.taobao.dgw.card.send',
                    param:{
                            data: JSON.stringify({
                            cardCode: 'znkf_remind_pay_new',
                            cardParams:{
                                        title: '亲，您有一笔订单未付款',
                                        logo: '{orderPicUrl}',
                                        subTitle: '{orderTitle}',
                                        description: '合计￥{payment}',
                                        btnRight: '付款',
                                        //remark: '',phone: '13162548564',name: '张先生',fulladdress: '123',
                                        btnRightAction: 'https://tm.m.taobao.com/order/order_detail.htm?bizOrderId={tid}',
                                        extActionUrl: 'https://tm.m.taobao.com/order/order_detail.htm?bizOrderId={tid}'
                                    },
                                    bizUUid: '{uuid}',
                                    appkey: 23574652
                            }),
                            receiver: '{buyer}',
                            domain: 'cntaobao'
                    },
                    httpMethod: 'post',
                    version: '1.0'
                }); ";
            cmd = cmd.Replace("{tid}", tid).Replace("{buyer}","cntaobao"+ buyer)
                .Replace("{orderPicUrl}", orderPicUrl)
                .Replace("{orderTitle}", orderTitle)
                .Replace("{tradeNum}", tradeNum)
                .Replace("{payment}", payment)
                .Replace("{uuid}", Guid.NewGuid().ToString());
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public void SendMemberCard(string seller, string buyer)
        {
            var cmd = @"
                QN.app.invoke({
                    api: 'invokeMTopChannelService',
                    query: {
                      method: 'mtop.taobao.seattle.qianniu.card.send.post',
                      param: JSON.stringify({sellerNick: '{seller}',buyerNick: '{buyer}'}),
                      httpMethod: 'post',
                      version: '1.0',
                    },
                  }); ";
            cmd = cmd.Replace("{seller}", "cntaobao"+seller).Replace("{buyer}", "cntaobao"+buyer);
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public void BrowserUrl(string url)
        {
            var cmd = @"
                QN.app.invoke( {
                        api : 'browserUrl',
                        query : {
                             url : '{url}'  
                            
                        }
                });";
            cmd = cmd.Replace("{url}",url);
            var retVal = string.Empty;
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd, out retVal);
            }
        }

        public void RecallMessage(string ccode,string clientId,string messageId)
        {
            var cmd = @"
                imsdk.invoke('im.singlemsg.DoChatMsgWithdraw', {
                    cid: {ccode:'{ccode}'},
                    mcodes: [{clientId:'{clientId}',messageId:'{messageId}'}]
                }).then((data)=>{
                    
                });";
            cmd = cmd.Replace("{ccode}", ccode).Replace("{clientId}", clientId).Replace("{messageId}", messageId);
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public void OpenChat(string uid)
        {
            var cmd = @"
                imsdk.invoke('application.openChat',{nick:'{uid}'})
                .then((data)=>{
                    console.log(data);
                });";
            cmd = cmd.Replace("{uid}", uid);
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public void SendCoupon(string uid,string activityId)
        {
            var cmd = @"
                QN.app.invoke({
                    api: 'invokeMTopChannelService',
                    query: {
                      method: 'mtop.taobao.qianniu.airisland.coupon.send.card',
                      param: {activityId:'{activityId}',buyerNick:'{uid}'},
                      httpMethod: 'post',
                      version: '1.0',
                    },
                }); ";
            cmd = cmd.Replace("{uid}", uid).Replace("{activityId}", activityId);
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public void CloseChat(string uid)
        {
            var cmd = @"
                imsdk.invoke('application.closeChat',{contactID:'{uid}'})
                .then((data)=>{
                    console.log(data);
                });";
            cmd = cmd.Replace("{uid}", uid);
            if (ChromOp != null)
            {
                ChromOp.Eval(cmd);
            }
        }

        public async Task<EmployeeResponse> GetEmployees()
        {
            var cmd = @"
               imsdk.invoke('application.invokeMTopChannelService', {
                    method: 'mtop.taobao.worklink.task.group.tree',
                    param:{},
                    httpMethod: 'post',
                    version: '1.0'
                }).then(res=>console.log('ChromeImSdkConsoleLog,'+JSON.stringify({api_name:'{api_name}',task_command_id:'{task_command_id}',sdk_response: res})));";
            var taskId = Interlocked.Increment(ref _incrementCount);
            cmd = cmd.Replace("{api_name}", "mtop.taobao.worklink.task.group.tree")
                 .Replace("{task_command_id}", taskId.ToString());
            var res = await RequestQnApi(cmd, taskId);
            res = res.xRemoveLineBreak().xRemoveSpace();
            return Util.DeserializeNoTypeName<EmployeeResponse>(res);
        }

        public async Task<QuickPhraseResponse> GetQuickPhrases()
        {
            var cmd = @"
               QN.app.invoke({
                api: 'invokeMTopChannelService',
                query: {
                    method: 'mtop.taobao.qianniu.quickphrase.get',
                    param: {from:'',version:1.0},
                    httpMethod: 'post',
                    version: '1.0',
                },
               }).then(res=>console.log('ChromeImSdkConsoleLog,'+JSON.stringify({api_name:'{api_name}',task_command_id:'{task_command_id}',sdk_response: res})));";
            var taskId = Interlocked.Increment(ref _incrementCount);
            cmd = cmd.Replace("{api_name}", "mtop.taobao.qianniu.quickphrase.get")
                 .Replace("{task_command_id}", taskId.ToString());
            var res = await RequestQnApi(cmd, taskId);
            //res = res.xRemoveLineBreak().xRemoveSpace();
            var rt = Util.DeserializeNoTypeName<QuickPhraseResponse>(res);
            return rt;
        }

        public async Task<CouponResponse> GetCoupons()
        {
            var cmd = @"
               QN.app.invoke({
                api: 'invokeMTopChannelService',
                query: {
                  method: 'mtop.taobao.qianniu.airisland.coupon.get',
                  param: {buyerNick:'123456789'},
                  httpMethod: 'post',
                  version: '1.0',
                },
              }).then(res=>console.log('ChromeImSdkConsoleLog,'+JSON.stringify({api_name:'{api_name}',task_command_id:'{task_command_id}',sdk_response: res})));";
            var taskId = Interlocked.Increment(ref _incrementCount);
            cmd = cmd.Replace("{api_name}", "mtop.taobao.qianniu.airisland.coupon.get")
                 .Replace("{task_command_id}", taskId.ToString());
            var res = await RequestQnApi(cmd, taskId);
            res = res.xRemoveLineBreak();
            return Util.DeserializeNoTypeName<CouponResponse>(res);
        }

        public async Task<List<ChatlogEntity>> GetRecentNoReplyMessages(long btime,long etime = -1)
        {
            var cmd = @"
               imsdk.invoke('im.singlemsg.ListConvByLastMsgTime', {
                    btime: '{btime}',
                    etime: '{etime}'
                }).then(async res =>{
                    var loginUser = await imsdk.invoke('im.login.GetCurrentLoginID');
                    var msgs = [];
                    var promises = res.result.map(k =>{
                        return new Promise(resolve =>{            
                            imsdk.invoke('im.singlemsg.GetLocalPageMsg', {
                                cid: {
                                    ccode: k.cid.ccode
                                },
                                count: 1,
                                gohistory: 1
                            }).then(msgRes =>{
                                var localMsg = msgRes.result.msgs[0];
                                var loginMainUser = loginUser.result.nick;
                                if(loginMainUser.indexOf(':')>-1){
                                    loginMainUser = loginMainUser.substring(0,loginMainUser.indexOf(':'))
                                }
                                var fromMainUser = localMsg.fromid.nick;
                                if(fromMainUser.indexOf(':')>-1){
                                    fromMainUser = fromMainUser.substring(0,fromMainUser.indexOf(':'))
                                }
                                var toMainUser = localMsg.toid.nick;
                                if(toMainUser.indexOf(':')>-1){
                                    toMainUser = toMainUser.substring(0,toMainUser.indexOf(':'))
                                }
                                //当前登录的千牛的主号 是否是收到的消息的主号
                                if(loginMainUser == toMainUser && loginMainUser != fromMainUser)
                                {
                                    var content = '';
                                    var imageUrl = '';
                                    if(localMsg.templateId == 101)
                                    {
                                        content = localMsg.originalData.text;
                                    }
                                    else if(localMsg.templateId == 102)
                                    {
                                        content = '客户发来一张图片';
                                        imageUrl = localMsg.originalData.url;
                                    }
                                    else if(localMsg.templateId == 107)
                                    {
                                        content = '客户发来一个文件(文件名)：'+localMsg.originalData.jsFileInfo.nodeName;
                                    }
                                    else if(localMsg.templateId == 104)
                                    {
                                        content = '客户发来一段语音';
                                    }
                                    else if(localMsg.templateId == 105)
                                    {
                                        content = '客户发来一段视频';
                                    }
                                    else if(localMsg.templateId == 106)
                                    {
                                        content = '系统消息';
                                    }
                                    else if(localMsg.templateId == 106)
                                    {
                                        content = '系统消息';
                                    }
                                    else if(localMsg.templateId == 129)
                                    {
                                        content = localMsg.summary;
                                    }
                                    else if(localMsg.templateId == 110 
                                        || localMsg.templateId == 111 || localMsg.templateId == 112
                                        || localMsg.templateId == 113 || localMsg.templateId == 114
                                        || localMsg.templateId == 120 || localMsg.templateId == 128)
                                    {
                                        content = '客户发来一条卡片消息';
                                    }
                                    else if(localMsg.templateId == 116)
                                    {
                                        content = '客户发来一条位置信息';
                                    }
                                    else if(localMsg.templateId == 152002)
                                    {
                                        content = localMsg.originalData.title;
                                    }
                                    msgs.push({
                                        FromNick:localMsg.fromid.nick,
                                        ToNick:localMsg.toid.nick,
                                        Content:content,
                                        SendTime:localMsg.sendTime,
                                        ImageUrl:imageUrl,
                                    });
                                }
                                resolve(localMsg);
                            });
                        });
                    }); 
                    Promise.all(promises).then(res=>
                        console.log('ChromeImSdkConsoleLog,'+JSON.stringify({api_name:'{api_name}',task_command_id:'{task_command_id}',sdk_response: msgs}))
                    );
                });";
            var taskId = Interlocked.Increment(ref _incrementCount);
            cmd = cmd.Replace("{api_name}", "im.singlemsg.ListConvByLastMsgTime")
                 .Replace("{task_command_id}", taskId.ToString()).Replace("{btime}",btime.ToString())
                 .Replace("{etime}",etime.ToString());
            var res = await RequestQnApi(cmd, taskId);
            res = res.xRemoveLineBreak();
            return Util.DeserializeNoTypeName<List<ChatlogEntity>>(res);
        }

        public void ClearChromeConsole()
        {
            var retVal = string.Empty;
            if (ChromOp != null)
            {
                ChromOp.Eval("console.clear()", out retVal);
            }
        }

        public Task<string> RequestQnApi(string cmd, long taskId)
        {
            var requestResetEvent = new ManualResetEventSlim(false);
            _requestWaitHandles.AddOrUpdate(taskId, requestResetEvent, (id, r) => requestResetEvent);
            return System.Threading.Tasks.Task.Run(() =>
            {
                if (ChromOp != null)
                {
                    ChromOp.Eval(cmd);
                }
                requestResetEvent.Wait(3 * 60 * 1000);
                string response = null;
                _imsdkResponses.TryRemove(taskId, out response);
                _requestWaitHandles.TryRemove(taskId, out requestResetEvent);
                return response;
            });
        }
    }

}
