using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;
using Bot.Common.Db;
using DbEntity;

namespace Bot.Common.Account
{
    public class AccountHelper
    {
        public static bool IsPubAccountEqual(string dbAccount, string seller)
        {
            return GetPubDbAccount(seller) == dbAccount;
        }

        public static string GetMainPart(string seller)
        {
            return TbNickHelper.GetMainPart(seller);
        }

        public static string GetPubDbAccount(string seller)
        {
            return HybridHelper.GetValue<string>(seller, HybridKey.PubDbAccount.ToString(), TbNickHelper.ConvertNickToPubDbAccount(seller));
        }

        public static string GetShopDbAccount(string seller)
        {
            return TbNickHelper.ConvertNickToShopDbAccount(seller);
        }

        public static string GetWwMainNick(string seller)
        {
            string pubDba = GetPubDbAccount(seller);
            return TbNickHelper.GetWwMainNickFromPubDbAccount(pubDba);
        }

        public static string GetPrvDbAccount(string seller)
        {
            return HybridHelper.GetValue(seller, HybridKey.PrvDbAccount.ToString(), TbNickHelper.ConvertNickToPrvDbAccount(seller));
        }

        public static void GetPubDbAccount(string mainNick, string pubDbAccount)
        {
            //TbNickHelper.AssertMainNick(mainNick);
            //TbNickHelper.AssertPubDbAccount(pubDbAccount);
            HybridHelper.GetValue(mainNick, HybridKey.PubDbAccount.ToString(), pubDbAccount);
        }

        public static HashSet<string> GetDbAccounts()
        {
            return ConvertNicksToDbAccount(QnHelper.Detected.GetSellers());
        }

        public static HashSet<string> ConvertNicksToDbAccount(string[] nicks)
        {
            var set = new HashSet<string>();
            foreach (var nick in nicks.xSafeForEach())
            {
                set.Add(GetPubDbAccount(nick));
                set.Add(GetPrvDbAccount(nick));
                set.Add(nick);
                set.Add(TbNickHelper.GetMainPart(nick));
                set.Add(GetShopDbAccount(nick));
            }
            return set;
        }
    }

}
