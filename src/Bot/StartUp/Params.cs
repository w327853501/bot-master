using BotLib;
using BotLib.Db.Sqlite;
using BotLib.Extensions;
using BotLib.Misc;
using Bot.Common;
using Bot.Options.HotKey;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bot.Common.Account;
using DbEntity;
using Bot.AssistWindow;
using Bot.Common.Db;

namespace Bot
{
    public class Params
    {
        public const int Version = 90302;
        private static string _server = "http://112.74.19.214:8088/api/bot/";
        public static string VersionStr;
        public const string CreateDateStr = "2021.11.20";
        public static string HelpRoot;
        public const int KeepInstalledVersionsCount = 3;
        public const string AppName = "千牛机器人";
        public const int CtlGoodsListGoodsPerPageMin = 3;
        public const int CtlGoodsListGoodsPerPageMax = 20;
        public const int CtlGoodsListGoodsPerPageDefault = 4;
        public const int MaxQACountForChatRecordManager = 2000;
        public const int MaxSynableQuestionAnswersCount = 2000;
        public const int MaxAddQaCountForQuestionAndAnswersCiteTableManager = 30000;
        public const int MaxSynableQuestionTimeoutDays = 10;
        public static int BottomPannelAnswerCount;
        public static bool RulePatternMatchStrict;
        private static string _pcGuid;
        private static string _instanceGuid;
        public static readonly DateTime AppStartTime;
        public static bool IsAppClosing;

        public static string Server
        {
            get
            {
                return _server;
            }
        }

        public static string RealServer
        {
            get
            {
                return Server;
            }
        }

        public static bool ForceActiveIME
        {
            get
            {
                return PersistentParams.GetParam("ForceActiveIME", true);
            }
            set
            {
                PersistentParams.TrySaveParam("ForceActiveIME", value);
            }
        }

        public static bool SetIsFirstLogin(string nick)
        {
            bool param2Key;
            if (param2Key = PersistentParams.GetParam2Key("IsFirstLogin", nick, true))
            {
                PersistentParams.TrySaveParam2Key("IsFirstLogin", nick, false);
            }
            return param2Key;
        }

        public static bool IsFirstLogin(string nick)
        {
            bool param2Key;
            if (param2Key = PersistentParams.GetParam2Key("IsFirstLogin", nick, true))
            {
                PersistentParams.TrySaveParam2Key("IsFirstLogin", nick, false);
            }
            return param2Key;
        }

        public static bool IsShowGoodsKnowledgeWhenBuyerTalkIt
        {
            get
            {
                return PersistentParams.GetParam("IsShowGoodsKnowledgeWhenBuyerTalkIt", true);
            }
            set
            {
                PersistentParams.TrySaveParam("IsShowGoodsKnowledgeWhenBuyerTalkIt", value);
            }
        }

        public static void SaveLatestCheckDbDeleteTime(string dbAccount)
        {
            PersistentParams.TrySaveParam2Key("LatestCheckDbDeleteTime", dbAccount, BatTime.Now);
        }

        public static DateTime GetLatestCheckDbDeleteTime(string dbAccount)
        {
            return PersistentParams.GetParam2Key("LatestCheckDbDeleteTime", dbAccount, DateTime.MinValue);
        }

        public static DateTime GetLatestSynOkTime(string dbAccount)
        {
            return PersistentParams.GetParam2Key("LatestSynOkTime", dbAccount, DateTime.MinValue);
        }

        public static void SetLatestSynOkTime(string dbAccount)
        {
            PersistentParams.TrySaveParam2Key("LatestSynOkTime", dbAccount, BatTime.Now);
        }

        public static string SystemInfo
        {
            get
            {
                return string.Format("{4} {0},千牛版本={1}，PcId={2},{3}", new object[]
				{
					VersionStr,
					QnHelper.QnVersion,
					PcId,
					ComputerInfo.SysInfoForLog,
					"软件"
				});
            }
        }

        public static string PcId
        {
            get
            {
                if (_pcGuid == null)
                {
                    _pcGuid = ComputerInfo.GetCpuID();
                }
                return _pcGuid;
            }
        }

        public static string InstanceGuid
        {
            get
            {
                if (_instanceGuid == null)
                {
                    string text = PersistentParams.GetParam("InstanceGuid", "");
                    string param = PersistentParams.GetParam("PcId4InstanceGuid", "");
                    if (string.IsNullOrEmpty(text) || param != PcId)
                    {
                        text = StringEx.xGenGuidB64Str();
                        PersistentParams.TrySaveParam("InstanceGuid", text);
                        PersistentParams.TrySaveParam("PcId4InstanceGuid", PcId);
                    }
                    _instanceGuid = text;
                }
                return _instanceGuid;
            }
        }


        public static bool IsAppStartMoreThan10Second
        {
            get
            {
                return (DateTime.Now - AppStartTime).TotalSeconds > 10.0;
            }
        }

        public static bool IsAppStartMoreThan20Second
        {
            get
            {
                return (DateTime.Now - AppStartTime).TotalSeconds > 20.0;
            }
        }

        public static bool HadUniformShortcutCode
        {
            get
            {
                return PersistentParams.GetParam("HadUniformShortcutCode", false);
            }
            set
            {
                PersistentParams.TrySaveParam("HadUniformShortcutCode", value);
            }
        }

        public static bool NeedClearDb
        {
            get
            {
                return PersistentParams.GetParam("NeedClearDb", false);
            }
            set
            {
                PersistentParams.TrySaveParam("NeedClearDb", value);
            }
        }

        public static int BottomPanelAnswerCount { get; set; }

        public static bool NeedTipReSynDataOk
        {
            get
            {
                return PersistentParams.GetParam("NeedTipReSynDataOk", false);
            }
            set
            {
                PersistentParams.TrySaveParam("NeedTipReSynDataOk", value);
            }
        }

        static Params()
        {
            VersionStr = ShareUtil.ConvertVersionToString(Version);
            HelpRoot = "https://github.com/renchengxiaofeixia";
            BottomPannelAnswerCount = 5;
            RulePatternMatchStrict = true;
            AppStartTime = DateTime.Now;
            IsAppClosing = false;
        }

        public static void SetProcessPath(string processName, string processPath)
        {
            string key = GetProcessPathKey(processName);
            PersistentParams.TrySaveParam(key, processPath);
        }

        private static string GetProcessPathKey(string processName)
        {
            return "ProcessPath#" + processName;
        }

        public static string GetProcessPath(string processName)
        {
            string key = GetProcessPathKey(processName);
            return PersistentParams.GetParam(key, "");
        }

        public class Other
        {
            public static int FontSize
            {
                get
                {
                    int fs = PersistentParams.GetParam("Other.FontSize", 12);
                    if (fs < 12)
                    {
                        fs = 12;
                        FontSize = 12;
                    }
                    else if (fs > 14)
                    {
                        fs = 14;
                        FontSize = 14;
                    }
                    return fs;
                }
                set
                {
                    PersistentParams.TrySaveParam("Other.FontSize", value);
                }
            }

            public const int FontSizeDefault = 12;
        }

        public class BuyerNote
        {
            public static bool IsPreferSelfNoteDefault;

            public static bool IsShowDetailAsTooltipDefault;
            static BuyerNote()
            {
                IsPreferSelfNoteDefault = true;
                IsShowDetailAsTooltipDefault = true;
            }
            public static bool GetSetIsPreferSelfNote(string nick)
            {
                return PersistentParams.GetParam2Key("BuyerNote.IsPreferSelfNote", nick, BuyerNote.IsPreferSelfNoteDefault);
            }

            public static void SetIsPreferSelfNote(string nick, bool isPreferSelfNote)
            {
                PersistentParams.TrySaveParam2Key("BuyerNote.IsPreferSelfNote", nick, isPreferSelfNote);
            }

            public static bool GetIsShowDetailAsTooltip(string nick)
            {
                return PersistentParams.GetParam2Key("BuyerNote.IsShowDetailAsTooltip", nick, BuyerNote.IsShowDetailAsTooltipDefault);
            }

            public static void SetIsShowDetailAsTooltip(string nick, bool isShowDetailAsTooltip)
            {
                PersistentParams.TrySaveParam2Key("BuyerNote.IsShowDetailAsTooltip", nick, isShowDetailAsTooltip);
            }
        }

        public class Auth
        {
            public static bool IsAllAccountEditShortCutDefault;
            public static bool IsAllAccountEditKnowledgeDefault;
            public static bool IsAllAccountEditRobotDefault;
            static Auth()
            {
                IsAllAccountEditShortCutDefault = true;
                IsAllAccountEditKnowledgeDefault = true;
                IsAllAccountEditRobotDefault = true;
            }

            public static HashSet<string> GetSuperAccounts(string mainnick)
            {
                //TbNickHelper.AssertMainNick(mainnick);
                return PersistentParams.GetParam2Key<HashSet<string>>("Auth.SuperAccounts", mainnick,null);
            }

            public static void SetSuperAccounts(string nick, string accounts)
            {
                PersistentParams.TrySaveParam2Key("Auth.SuperAccounts", nick, accounts);
            }

            public static bool GetIsAllAccountEditShortCut(string nick)
            {
                return PersistentParams.GetParam2Key("IsAllAccountEditShortCut", AccountHelper.GetPubDbAccount(nick), Auth.IsAllAccountEditShortCutDefault);
            }

            public static void SetIsAllAccountEditShortCut(string nick, bool isAllAccountEditShortCut)
            {
                PersistentParams.TrySaveParam2Key("IsAllAccountEditShortCut", AccountHelper.GetPubDbAccount(nick), isAllAccountEditShortCut);
            }

            public static bool GetIsAllAccountEditKnowledge(string nick)
            {
                return PersistentParams.GetParam2Key("IsAllAccountEditKnowledge", AccountHelper.GetPubDbAccount(nick), Auth.IsAllAccountEditKnowledgeDefault);
            }

            public static void SetIsAllAccountEditKnowledge(string nick, bool isAllAccountEditKnowledge)
            {
                PersistentParams.TrySaveParam2Key("IsAllAccountEditKnowledge", AccountHelper.GetPubDbAccount(nick), isAllAccountEditKnowledge);
            }

            public static bool GetIsAllAccountEditRobot(string nick)
            {
                return PersistentParams.GetParam2Key("IsAllAccountEditRobot", AccountHelper.GetPubDbAccount(nick), Auth.IsAllAccountEditRobotDefault);
            }

            public static void SetIsAllAccountEditRobot(string nick, bool isAllAccountEditRobot)
            {
                PersistentParams.TrySaveParam2Key("IsAllAccountEditRobot", AccountHelper.GetPubDbAccount(nick), isAllAccountEditRobot);
            }
        }


        public class Shortcut
        {
            public enum ShowType
            {
                ShopOnly,
                ShopAndSelf,
                SelfOnly
            }

            public static ShowType ShowTypeDefault;
            public const bool IsShowTitleButtonsDefault = true;

            static Shortcut()
            {
                ShowTypeDefault = ShowType.ShopAndSelf;
            }

            public static bool GetIsShowTitleButtons(string nick)
            {
                return PersistentParams.GetParam2Key("Robot.IsShowTitleButtons", nick, false);
            }

            public static void SetIsShowTitleButtons(string nick, bool isShowTitleButtons)
            {
                PersistentParams.TrySaveParam2Key("Robot.IsShowTitleButtons", nick, isShowTitleButtons);
            }

            public static ShowType GetShowType(string nick)
            {
                return PersistentParams.GetParam2Key("Shortcut.ShowType", nick, ShowTypeDefault);
            }

            public static void SetShowType(string nick, ShowType showType)
            {
                PersistentParams.TrySaveParam2Key("Shortcut.ShowType", nick, showType);
            }
        }

        public class Robot
        {
            public const bool BanRobotForTest2020_1_7 = false;
            public static bool CanUseRobot;
            public const int AutoModeBringForegroundIntervalSecond = 5;
            public const int AutoModeCloseUnAnsweredBuyerIntervalSecond = 10;
            public const bool RuleIncludeExceptDefault = false;
            public const int AutoModeReplyDelaySecDefault = 0;
            public const int SendModeReplyDelaySecDefault = 0;
            public const bool QuoteModeSendAnswerWhenFullMatchDefault = false;
            public const double AutoModeAnswerMiniScore = 0.5;
            public const double QuoteOrSendModeAnswerMiniScore = 0.5;
            public const bool CancelAutoOnResetDefault = true;
            public const string AutoModeNoAnswerTipDefault = "亲,目前是机器人值班.这个问题机器人无法回答,等人工客服回来后再回复您.";

            static Robot()
            {
                CanUseRobot = true;
            }
            public enum OperationEnum
            {
                None,
                Auto,
                Send,
                Quote
            }
            public static bool CanUseRobotReal
            {
                get
                {
                    return CanUseRobot;
                }
            }
            public static bool GetRuleIncludeExcept(string nick)
            {
                return PersistentParams.GetParam2Key("RuleIncludeExcept", AccountHelper.GetPubDbAccount(nick), false);
            }
            public static void SetRuleIncludeExcept(string nick, bool ruleIncludeExcept)
            {
                PersistentParams.TrySaveParam2Key("RuleIncludeExcept", AccountHelper.GetPubDbAccount(nick), ruleIncludeExcept);
            }
            public static int GetAutoModeReplyDelaySec(string nick)
            {
                return PersistentParams.GetParam2Key("AutoModeReplyDelaySec", nick, 0);
            }
            public static void SetAutoModeReplyDelaySec(string nick, int autoModeReplyDelaySec)
            {
                PersistentParams.TrySaveParam2Key("AutoModeReplyDelaySec", nick, autoModeReplyDelaySec);
            }
            public static int GetSendModeReplyDelaySec(string nick)
            {
                return PersistentParams.GetParam2Key("SendModeReplyDelaySec", nick, 0);
            }
            public static void SetSendModeReplyDelaySec(string nick, int sendModeReplyDelaySec)
            {
                PersistentParams.TrySaveParam2Key("SendModeReplyDelaySec", nick, sendModeReplyDelaySec);
            }
            public static bool GetQuoteModeSendAnswerWhenFullMatch(string nick)
            {
                return PersistentParams.GetParam2Key("QuoteModeSendAnswerWhenFullMatch", nick, false);
            }
            public static void SetQuoteModeSendAnswerWhenFullMatch(string nick, bool quoteModeSendAnswerWhenFullMatch)
            {
                PersistentParams.TrySaveParam2Key("QuoteModeSendAnswerWhenFullMatch", nick, quoteModeSendAnswerWhenFullMatch);
            }
            public static bool CancelAutoOnReset
            {
                get
                {
                    return PersistentParams.GetParam("CancelAutoOnReset", true);
                }
                set
                {
                    PersistentParams.TrySaveParam("CancelAutoOnReset", value);
                }
            }
            public static OperationEnum GetOperation(string nick)
            {
                return PersistentParams.GetParam2Key("Robot.Operation", nick, OperationEnum.None);
               
            }
            public static void SetOperation(string nick, OperationEnum operation)
            {
                PersistentParams.TrySaveParam2Key("Robot.Operation", nick, operation);
            }
            public static string GetAutoModeNoAnswerTip(string nick)
            {
                return PersistentParams.GetParam2Key("Robot.AutoModeNoAnswerTip", nick, AutoModeNoAnswerTipDefault);
            }
            public static void SetAutoModeNoAnswerTip(string nick, string autoModeNoAnswerTip)
            {
                PersistentParams.TrySaveParam2Key("Robot.AutoModeNoAnswerTip", nick, autoModeNoAnswerTip);
            }

        }

        public class HotKey
        {
            public const string HotKeyQingKongDefault = "327755";
            public const string HotKeyZhiSiDefault = "327757";
            public static bool GetHotKey(HotKeyHelper.HotOp op, out Keys k)
            {
                k = Keys.BrowserFavorites;
                bool rt = false;
                string defv = "";
                switch (op)
                {
                    case HotKeyHelper.HotOp.QinKong:
                        defv = "327755";
                        break;
                    case HotKeyHelper.HotOp.ZhiSi:
                        defv = "327757";
                        break;
                }
                string hotOp = PersistentParams.GetParam("HotOp" + ((int)op).ToString(), defv);
                if (hotOp != "" && hotOp != "171")
                {
                    try
                    {
                        k = (Keys)Convert.ToUInt32(hotOp);
                        rt = true;
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e);
                    }
                }
                return rt;
            }
            public static void SaveHotKey(HotKeyHelper.HotOp hotOp, string value)
            {
                Keys keys = Keys.BrowserFavorites;
                if (value == "无")
                {
                    SaveHotKey(hotOp, true, keys);
                }
                else
                {
                    try
                    {
                        keys = (Keys)Convert.ToUInt32(value);
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e);
                    }
                    SaveHotKey(hotOp, false, keys);
                }
            }
            public static void SaveHotKey(HotKeyHelper.HotOp op, bool isclear, Keys keys)
            {
                PersistentParams.TrySaveParam("HotOp" + ((int)op).ToString(), isclear ? "" : ((uint)keys).ToString());
            }
        }

        public class InputSuggestion
        {
            public const bool IsUseDownKeyDefault = true;
            public static SourceTypeEnum SourceTypeDefault;
            public const bool UseSingleQuoteDefault = false;
            public enum SourceTypeEnum
            {
                Unknown,
                Shortcut,
                ScAndRuleAnswer,
                All
            }
            static InputSuggestion()
            {
                SourceTypeDefault = SourceTypeEnum.All;
            }
            public static bool UseSingleQuote
            {
                get
                {
                    return true;
                }
                set
                {
                    PersistentParams.TrySaveParam("UseSingleQuote", value);
                }
            }
            public static int BanUseDownKeyCount
            {
                get
                {
                    return PersistentParams.GetParam("BanUseDownKeyCount", 0);
                }
                set
                {
                    PersistentParams.TrySaveParam("BanUseDownKeyCount", value);
                }
            }
            public static bool IsUseDownKey
            {
                get
                {
                    return PersistentParams.GetParam("IsUseDownKey", true);
                }
                set
                {
                    PersistentParams.TrySaveParam("IsUseDownKey", value);
                }
            }
            public static bool IsUseLearnedAnswer
            {
                get
                {
                    SourceTypeEnum sourceType = SourceType;
                    return sourceType == SourceTypeEnum.All;
                }
            }
            public static bool IsUseRuleAnswer
            {
                get
                {
                    var sourceType = SourceType;
                    return sourceType == SourceTypeEnum.All || sourceType == SourceTypeEnum.ScAndRuleAnswer;
                }
            }

            public static bool IsUseShortcut
            {
                get
                {
                    SourceTypeEnum sourceType = SourceType;
                    return sourceType == SourceTypeEnum.All || sourceType == SourceTypeEnum.ScAndRuleAnswer || sourceType == SourceTypeEnum.Shortcut;
                }
            }
            public static SourceTypeEnum SourceType
            {
                get
                {
                    SourceTypeEnum sourceTypeEnum = PersistentParams.GetParam<SourceTypeEnum>("SourceType", SourceTypeDefault);
                    if (sourceTypeEnum == SourceTypeEnum.Unknown)
                    {
                        sourceTypeEnum = SourceTypeEnum.All;
                    }
                    return sourceTypeEnum;
                }
                set
                {
                    PersistentParams.TrySaveParam("SourceType", value);
                }
            }
        }

        public class Goods
        {
            public const bool IsOrderKnByClickCountDescDefault = true;
            public const bool ShowGoodsAtRightPanelAfterSearchLinkDefault = true;
            private const int GoodsLinkRefreshCountPerDay = 5;
            public static bool GetIsOrderKnByClickCountDesc(string seller)
            {
                return PersistentParams.GetParam2Key("IsOrderKnByClickCountDesc", seller, true);
            }
            public static void SetIsOrderKnByClickCountDesc(string seller, bool isOrderKnByClickCountDesc)
            {
                PersistentParams.TrySaveParam2Key("IsOrderKnByClickCountDesc", seller, isOrderKnByClickCountDesc);
            }
            public static bool ShowGoodsAtRightPanelAfterSearchLink
            {
                get
                {
                    return PersistentParams.GetParam("ShowGoodsAtRightPanelAfterSearchLink", true);
                }
                set
                {
                    PersistentParams.TrySaveParam("ShowGoodsAtRightPanelAfterSearchLink", value);
                }
            }
            public static void SetRefreshLinkTodayUseCount(string seller)
            {
                var dateUseCount = PersistentParams.GetParam2Key("RefreshLinkTodayUseCount", seller, new DateUseCount());
                if (!dateUseCount.IsToday)
                {
                    dateUseCount = new DateUseCount();
                }
                dateUseCount.Count++;
                PersistentParams.TrySaveParam2Key("RefreshLinkTodayUseCount", seller, dateUseCount);
            }
            public static bool RefreshLinkTodayUseCountMoreThen5(string seller)
            {
                return GetRefreshLinkTodayUseCount(seller) >= 5;
            }
            private static int GetRefreshLinkTodayUseCount(string seller)
            {
                int cnt = 0;
                var param2Key = PersistentParams.GetParam2Key("RefreshLinkTodayUseCount", seller, new DateUseCount());
                if (param2Key.IsToday)
                {
                    cnt = param2Key.Count;
                }
                return cnt;
            }
            private class DateUseCount
            {
                public DateTime Date;
                public int Count;
                public DateUseCount()
				{
					Date = BatTime.Now.Date;
					Count = 0;
                }
                public bool IsToday
                {
                    get
                    {
                        return Date == BatTime.Now.Date;
                    }
                }

            }
        }

        public static class Memo
        {
            public const bool IsBottomPanelMemoUploadToLatest24HourTradesDefault = true;
            public const bool IsAppendTimeToMemoDefault = true;
            public const bool IsMemoTagAppendBeforeDefault = false;
            public const AppendTypeEnum MemoAppendTypeDefault = AppendTypeEnum.Replace;
            public const int FlagDefault = 0;

            public static bool GetIsBottomPanelMemoUploadToLatest24HourTrades(string seller)
            {
                return PersistentParams.GetParam2Key("Memo.IsBottomPanelMemoUploadToLatest24HourTrades", seller, true);
            }
            public static void SetIsBottomPanelMemoUploadToLatest24HourTrades(string seller, bool isBottomPanelMemoUploadToLatest24HourTrades)
            {
                PersistentParams.TrySaveParam2Key("Memo.IsBottomPanelMemoUploadToLatest24HourTrades", seller, isBottomPanelMemoUploadToLatest24HourTrades);
            }
            public static bool GetIsAppendTimeToMemo(string seller)
            {
                return PersistentParams.GetParam2Key("Memo.IsAppendTimeToMemo", seller, true);
            }
            public static void SetIsAppendTimeToMemo(string seller, bool isAppendTimeToMemo)
            {
                PersistentParams.TrySaveParam2Key("Memo.IsAppendTimeToMemo", seller, isAppendTimeToMemo);
            }
            public static bool GetIsMemoTagAppendBefore(string seller)
            {
                return PersistentParams.GetParam2Key("Memo.IsMemoTagAppendBefore", seller, false);
            }
            public static void SetIsMemoTagAppendBefore(string seller, bool isMemoTagAppendBefore)
            {
                PersistentParams.TrySaveParam2Key("Memo.IsMemoTagAppendBefore", seller, isMemoTagAppendBefore);
            }
            public static int GetFlag(string seller)
            {
                return PersistentParams.GetParam2Key("Memo.Flag", seller, 0);
            }
            public static void SaveFlag(string seller, int flag)
            {
                Util.Assert(flag >= 0 && flag <= 5);
                PersistentParams.TrySaveParam2Key("Memo.Flag", seller, flag);
            }
            public static string GetSubPart(string seller)
            {
                if (!TbNickHelper.IsMainAccount(seller))
                {
                    seller = TbNickHelper.GetSubPart(seller);
                }
                return seller;
            }
            public static string GetMemoSellerCode(string seller)
            {
                return PersistentParams.GetParam2Key("Memo.SellerCode", seller, GetSubPart(seller));
            }
            public static void SetMemoSellerCode(string seller, string code)
            {
                PersistentParams.TrySaveParam2Key("Memo.SellerCode", seller, code);
            }
            public static void SetMemoAppendType(AppendTypeEnum appendType, string seller)
            {
                PersistentParams.TrySaveParam2Key("Memo.MemoAppendType", seller, appendType);
            }
            public static AppendTypeEnum GetMemoAppendType(string seller)
            {
                return PersistentParams.GetParam2Key("Memo.MemoAppendType", seller, AppendTypeEnum.Replace);
            }
            public static string[] GetFavoriteMemo(string seller)
            {
                return PersistentParams.GetParam2Key<string[]>("FavoriteMemo", seller, null);
            }
            public static void SetFavoriteMemo(string seller, string[] favMemos)
            {
                if (favMemos != null)
                {
                    favMemos = favMemos.Distinct().ToArray();
                }
                PersistentParams.TrySaveParam2Key("FavoriteMemo", seller, favMemos);
            }
            public enum AppendTypeEnum
            {
                Before,
                After,
                Replace
            }
        }

        public class Panel
        {
            public const string RightPanelCompOrderCsvDefault = "话术,商品,机器人,订单,优惠券";
            public const bool ShortcutIsVisibleDefault = true;
            public const bool GoodsKnowledgeIsVisibleDefault = true;
            public const bool RobotIsVisibleDefault = true;
            public const bool OrderIsVisibleDefault = true;
            public const bool LogisIsVisibleDefault = true;
            public const bool CouponIsVisibleDefault = true;
            public static string GetRightPanelCompOrderCsv(string seller)
            {
                return PersistentParams.GetParam2Key("RightPanelCompOrderCsv", seller, RightPanelCompOrderCsvDefault);
            }
            public static void SetRightPanelCompOrderCsv(string seller, string tabs)
            {
                PersistentParams.TrySaveParam2Key("RightPanelCompOrderCsv", seller, tabs);
            }
            public static bool GetShortcutIsVisible(string seller)
            {
                return PersistentParams.GetParam2Key("ShortcutIsVisible", seller, true);
            }
            public static void SetShortcutIsVisible(string seller, bool visible)
            {
                PersistentParams.TrySaveParam2Key("ShortcutIsVisible", seller, visible);
            }
            public static bool GetGoodsKnowledgeIsVisible(string seller)
            {
                return PersistentParams.GetParam2Key("GoodsKnowledgeIsVisible", seller, true);
            }
            public static void SetGoodsKnowledgeIsVisible(string seller, bool visible)
            {
                PersistentParams.TrySaveParam2Key("GoodsKnowledgeIsVisible", seller, visible);
            }
            public static bool GetRobotIsVisible(string seller)
            {
                return PersistentParams.GetParam2Key("RobotIsVisible", seller, true);
            }
            public static void SetRobotIsVisible(string seller, bool visible)
            {
                PersistentParams.TrySaveParam2Key("RobotIsVisible", seller, visible);
            }
            public static bool GetOrderIsVisible(string seller)
            {
                return PersistentParams.GetParam2Key("OrderIsVisible", seller, true);
            }
            public static void SetOrderIsVisible(string seller, bool visible)
            {
                PersistentParams.TrySaveParam2Key("OrderIsVisible", seller, visible);
            }
            public static bool GetCouponIsVisible(string seller)
            {
                return PersistentParams.GetParam2Key("CouponIsVisible", seller, true);
            }
            public static void SetCouponIsVisible(string seller, bool visible)
            {
                PersistentParams.TrySaveParam2Key("CouponIsVisible", seller, visible);
            }
            public static bool GetPanelOptionVisible(string seller, string tabName)
            {
                if (tabName == "话术")
                {
                    return GetShortcutIsVisible(seller);
                }
                if (tabName == "订单")
                {
                    return GetOrderIsVisible(seller);
                }
                if (tabName == "机器人")
                {
                    return GetRobotIsVisible(seller);
                }
                if (tabName == "商品")
                {
                    return GetGoodsKnowledgeIsVisible(seller);
                }
                if (tabName == "优惠券")
                {
                    return GetCouponIsVisible(seller);
                }
                return false;
            }
        }

        public class Trade
        {
            public static DateTime GetDoOnceUploadTime(string seller)
            {
                return PersistentParams.GetParam2Key<DateTime>("DoOnceUploadTradeTime", seller, DateTime.MinValue);
            }
            public static void SetDoOnceUploadTime(string seller, DateTime updateTradeTime)
            {
                PersistentParams.TrySaveParam2Key<DateTime>("DoOnceUploadTradeTime", seller, updateTradeTime);
            }
        }

        public class Booter
        {
            public static string GetAliWorkbenchExePath()
            {
                return PersistentParams.GetParam<string>("AliWorkbenchExePath", string.Empty);
            }
            public static void SetAliWorkbenchExePath(string qnPath)
            {
                PersistentParams.TrySaveParam("AliWorkbenchExePath", qnPath);
            }
        }
    }

}
