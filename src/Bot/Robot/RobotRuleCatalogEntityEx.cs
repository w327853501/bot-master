using Bot.Common;
using Bot.Common.Account;
using Bot.Common.TreeviewHelper;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Robot
{
    public static class RobotRuleCatalogEntityEx
    {
        public static RobotRuleCatalogEntity GetCataRoot(string dbAccount)
        {
            var dbAccessor = new TreeDbAccessor(typeof(RobotRuleCatalogEntity), typeof(RobotRuleEntity), dbAccount);
            return dbAccessor.Root as RobotRuleCatalogEntity;
        }

        public static RobotRuleCatalogEntity FindOne(string entityId, string dbAccount = null)
        {
            return DbHelper.FirstOrDefault<RobotRuleCatalogEntity>(entityId, dbAccount);
        }

        public static bool IsRoot(this RobotRuleCatalogEntity et)
        {
            return et.ParentId == null;
        }

        private static string GetCataName(this RobotRuleCatalogEntity et)
        {
            return et.IsRoot() ? "根分组" : et.Name;
        }

        public static string GetCataBreadcrumbBySeller(string entityId, string seller, int maxLen = -1)
        {
            return GetCataBreadcrumb(entityId, AccountHelper.GetPubDbAccount(seller), maxLen);
        }

        private static string GetCataBreadcrumb(string entityId, string dbAccount, int maxLen = -1)
        {
            var dbAccessor = new TreeDbAccessor(typeof(RobotRuleCatalogEntity), typeof(RobotRuleEntity), dbAccount);
            var cata = FindOne(entityId, dbAccount);
            if (cata == null) return null;
            var ancestorList = dbAccessor.ReadAncestorList(cata, false);
            var txt = string.Empty;
            if (ancestorList != null)
            {
                foreach (var an in ancestorList)
                {
                    var ancestorCata = an as RobotRuleCatalogEntity;
                    txt = txt + ancestorCata.GetCataName() + " > ";
                }
            }
            txt += cata.GetCataName();
            if (maxLen > 0 && txt.Length > maxLen)
            {
                txt = "..." + txt.Substring(txt.Length - 30);
            }
            return txt;
        }

        public static void FixNodeOutInTheTree(string dbAccount)
        {
            var dbAccessor = new TreeDbAccessor(typeof(RobotRuleCatalogEntity), typeof(RobotRuleEntity), dbAccount);
            dbAccessor.FixNodeOutInTheTree();
        }
    }
}
