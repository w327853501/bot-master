using BotLib;
using BotLib.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;
using Bot.Common.Db;
using Bot.Common;
using Bot.Common.TreeviewHelper;
using DbEntity;

namespace Bot.AssistWindow.Widget.Right.ShortCut
{
    public class QuickPhraseImporter
    {
        protected TreeDbAccessor _dbAccessor;
        private QuickPhraseResponse _quickPhraseResponse;
        public QuickPhraseImporter(TreeDbAccessor dbAccessor, QuickPhraseResponse quickPhraseResponse)
        {
            _dbAccessor = dbAccessor;
            _quickPhraseResponse = quickPhraseResponse;
        }

        public void Import()
        {
            DbSyner.Syn(false);
            DbSyner.IsBanSyn = true;
            try
            {
                var desNs = _dbAccessor.ReadDescendant(_dbAccessor.Root.EntityId);
                var groups = _quickPhraseResponse.data.result.groups;
                var phrases = _quickPhraseResponse.data.result.list;
                foreach (var group in groups)
                {
                    var cata = desNs.FirstOrDefault(k =>
                    {
                        var sc = k as ShortcutCatalogEntity;
                        return sc!=null && sc.Name == group.name;
                    }) as ShortcutCatalogEntity;
                    if (cata == null)
                    {
                        cata = EntityHelper.Create<ShortcutCatalogEntity>(_dbAccessor.DbAccount);
                        cata.Name = group.name;
                        _dbAccessor.AddNodeToTargetNode(cata, _dbAccessor.Root.EntityId);
                    }

                    var gphs = phrases.Where(p => p.groupId == group.id).ToList();
                    foreach (var ph in gphs)
                    {
                        var shortcut = (desNs.FirstOrDefault(k =>
                        {
                            var sc = k as ShortcutEntity;
                            return sc!=null && sc.Text == ph.content;
                        }) as ShortcutEntity);
                        if (shortcut == null)
                        {
                            shortcut = EntityHelper.Create<ShortcutEntity>(_dbAccessor.DbAccount);
                            shortcut.Text = ph.content;
                            shortcut.Code = ph.code;
                            _dbAccessor.AddNodeToTargetNode(shortcut, cata.EntityId);
                        }
                        else if (shortcut.Code != ph.code)
                        {
                            shortcut.Code = ph.code;
                            _dbAccessor.SaveNode(shortcut);
                        }
                    }
                }

                DbSyner.IsBanSyn = false;
                DbSyner.Syn();
                MsgBox.ShowTip("短语导入完毕。\r\n\r\n注：导入的数据已上传到服务器，其它电脑10分钟内即可同步到。", "导入提示");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            finally
            {
                DbSyner.IsBanSyn = false;
            }            
        }
    }
}
