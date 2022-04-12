using Bot.Common.TreeviewHelper;
using BotLib.Misc;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;
using BotLib;
using Bot.Common;
using Bot.Common.Db;
using BotLib.Collection;
using System.IO;
using Bot.Common.ImageHelper;

namespace Bot.AssistWindow.Widget.Right.ShortCut
{
    public abstract class Importer
    {
        protected abstract List<Node> ReadNodesFromFile(List<List<string>> dlist, out string importDbAccount);
        protected abstract void AssertFileFormatOk();

        protected string _filename;
        protected bool _isAllReplace;
        protected string _dbAccount;
        protected TreeDbAccessor _dbAccessor;

        public Importer(string fn, bool isAllReplace, string dbAccount)
		{
			_filename = fn;
			_isAllReplace = isAllReplace;
			_dbAccount = dbAccount;
			_dbAccessor = new TreeDbAccessor(typeof(ShortcutCatalogEntity), typeof(ShortcutEntity), dbAccount);
		}

        public virtual void Import()
        {
            try
            {
                AssertFileFormatOk();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                MsgBox.ShowErrDialog("无法导入，文件格式出错！");
                throw ex;
            }
            var dlist = CsvFileHelper.ReadCsvFile(_filename, -1);
            string dbAccount;
            var nodes = ReadNodesFromFile(dlist, out dbAccount);
            FixNodeCode(nodes);
            var tree = ConvertNodesToTree(nodes);
            if (tree != null && !tree.Root.Children.xIsNullOrEmpty())
            {
                DbSyner.Syn(false);
                DbSyner.IsBanSyn = true;
                try
                {
                    if (_isAllReplace)
                    {
                        ImportReplaceNode(tree);
                    }
                    else
                    {
                        ImportNewNode(tree.Root, _dbAccessor.Root);
                    }
                    CopyImageIntoCache();
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
        private void ImportNewNode(SimpleTreeNode<Node> simpNode, TreeNode parentNode)
        {
            var desNs = _dbAccessor.ReadDescendant(parentNode.EntityId);
            foreach (var nd in simpNode.Children)
            {
                if (nd.Data.IsCatalog)
                {
                    CreateNewCata(parentNode, desNs, nd);
                }
                else
                {
                    CreateNewShortcut(parentNode, desNs, nd);
                }
            }
        }


        private void CreateNewShortcut(TreeNode parentNode, List<TreeNode> nds, SimpleTreeNode<Node> n)
		{
			ShortcutEntity shortcut;
			if (string.IsNullOrEmpty(n.Data.ImageName))
			{
				shortcut = (nds.FirstOrDefault(k=>{
                    var sc = k as ShortcutEntity;
                    return sc.Text == n.Data.Answer;
                }) as ShortcutEntity);
			}
			else
			{
                shortcut = (nds.FirstOrDefault(k =>
                {
                    var sc = k as ShortcutEntity;
                    return sc.ImageName == n.Data.ImageName;
                }) as ShortcutEntity);
			}
			if (shortcut == null)
			{
				shortcut = EntityHelper.Create<ShortcutEntity>(_dbAccount);
				shortcut.Text = n.Data.Answer;
				shortcut.Code = n.Data.Code;
				shortcut.ImageName =n.Data.ImageName;
				_dbAccessor.AddNodeToTargetNode(shortcut, parentNode.EntityId);
			}
			else if (shortcut.Code != n.Data.Code)
			{
				shortcut.Code = n.Data.Code;
				_dbAccessor.SaveNode(shortcut);
			}
		}

        private void CreateNewCata(TreeNode tn, List<TreeNode> nds, SimpleTreeNode<Node> n)
		{
            var cata = nds.FirstOrDefault(k =>
            {
                var sc = k as ShortcutCatalogEntity;
                return sc.Name == n.Data.Answer;
            }) as ShortcutCatalogEntity;
			if (cata == null)
			{
                TraverseCreateNewNode(tn, n);
			}
			else
			{
				ImportNewNode(n, cata);
			}
		}

        private void TraverseCreateNewNode(TreeNode tn, SimpleTreeNode<Node> n)
		{
			var cata = EntityHelper.Create<ShortcutCatalogEntity>(_dbAccount);
			cata.Name = n.Data.Answer;
			_dbAccessor.AddNodeToTargetNode(cata, tn.EntityId);
			foreach (var simpNode in n.Children)
			{
				if (simpNode.Data.IsCatalog)
				{
                    TraverseCreateNewNode(cata, simpNode);
				}
				else
				{
					var shortcut = EntityHelper.Create<ShortcutEntity>(_dbAccount);
					shortcut.Text = simpNode.Data.Answer;
					shortcut.Code = simpNode.Data.Code;
					shortcut.ImageName = simpNode.Data.ImageName;
					_dbAccessor.AddNodeToTargetNode(shortcut, cata.EntityId);
				}
			}
		}

        private void ImportReplaceNode(SimpleTree<Node> simpTree)
        {
            var desNsFromDb =_dbAccessor.ReadDescendant(_dbAccessor.Root.EntityId);
            Dictionary<string, List<ShortcutEntity>> shortcutDict;
            var shortcutCatalogDict = ConvertNodesToEntity(desNsFromDb, out shortcutDict);
            simpTree.Root.Tag = _dbAccessor.Root;
            CreateTreeNode(simpTree.Root, shortcutCatalogDict, shortcutDict);
            DeleteNode(shortcutCatalogDict, shortcutDict, simpTree);
            var needUpdateNodes = NeedUpdateNodes(simpTree);
            DbHelper.BatchSaveOrUpdateToDb(needUpdateNodes.ToArray());
        }

        private List<TreeNode> NeedUpdateNodes(SimpleTree<Node> simpTree)
        {
	        var nlist = new List<TreeNode>();
            simpTree.TraverseTreeNode(simpNode =>
            {
                if (simpNode.Data.IsCatalog)
                {
                    var cataFromNode = simpNode.Tag as ShortcutCatalogEntity;
                    var cataFromDb = _dbAccessor.ReadNodeByIdAndType(cataFromNode.EntityId, typeof(ShortcutCatalogEntity)) as ShortcutCatalogEntity;
                    if (cataFromDb == null || IsCataNotEqualTo(cataFromNode, cataFromDb))
                    {
                        nlist.Add(cataFromNode);
                    }
                }
                else
                {
                    var shortcut = simpNode.Tag as ShortcutEntity;
                    var shortcutFromDb = _dbAccessor.ReadNodeByIdAndType(shortcut.EntityId, typeof(ShortcutEntity)) as ShortcutEntity;
                    if (shortcutFromDb == null || IsShortcutNotEqualTo(shortcut, shortcutFromDb))
                    {
                        nlist.Add(shortcut);
                    }
                }
            }, false);
	        return nlist;
        }

        private bool IsCataNotEqualTo(ShortcutCatalogEntity c1, ShortcutCatalogEntity c2)
        {
            return c1.Name != c2.Name || c1.NextId != c2.NextId || c1.PrevId != c2.PrevId || c1.ParentId != c2.ParentId;
        }

        private bool IsShortcutNotEqualTo(ShortcutEntity s1, ShortcutEntity s2)
        {
            return s1.Code != s2.Code || s1.NextId != s2.NextId || s1.PrevId != s2.PrevId || s1.ParentId != s2.ParentId || s1.Text != s2.Text || s1.ImageName != s2.ImageName;
        }

        private void DeleteNode(Dictionary<string, List<ShortcutCatalogEntity>> shortcutCatalogList, Dictionary<string, List<ShortcutEntity>> shortcutList, SimpleTree<Node> nTree)
        {
	        var nodes = new List<TreeNode>();
	        foreach (var cataKv in shortcutCatalogList)
	        {
                nodes.AddRange(cataKv.Value);
	        }
	        var imgfns = new HashSet<string>();
            foreach (var shortcutKv in shortcutList)
	        {
                nodes.AddRange(shortcutKv.Value);
                foreach (var shortcut in shortcutKv.Value)
		        {
			        if (!string.IsNullOrEmpty(shortcut.ImageName))
			        {
				        imgfns.Add(shortcut.ImageName);
			        }
		        }
	        }
	        foreach (TreeNode nd in nodes)
	        {
		        nd.IsDeleted = true;
	        }
	        DbHelper.BatchSaveOrUpdateToDb(nodes.ToArray());
	        if (imgfns.Count > 0)
	        {
                nTree.TraverseTreeNode(simpNode => {
                    if (!simpNode.Data.IsCatalog) return;
                    var shortcut = simpNode.Tag as ShortcutEntity;
                    if (!string.IsNullOrEmpty(shortcut.ImageName) && imgfns.Contains(shortcut.ImageName))
                    {
                        imgfns.Remove(shortcut.ImageName);
                    }
                }, false);
	        }
        }

        private void CreateTreeNode(SimpleTreeNode<Node> rootNode, Dictionary<string, List<ShortcutCatalogEntity>> shortcutCatalogList, Dictionary<string, List<ShortcutEntity>> shortcutList)
        {
            foreach (var nd in rootNode.Children)
            {
                if (nd.Data.IsCatalog)
                {
                    CreateCata(nd, shortcutCatalogList, shortcutList);
                }
                else
                {
                    CreateShortcut(nd, shortcutList);
                }
            }
            CreateTreeNode(rootNode);
        }


        private void CreateCata(SimpleTreeNode<Node> simpNode, Dictionary<string, List<ShortcutCatalogEntity>> shortcutCatalogList, Dictionary<string, List<ShortcutEntity>> shortcutList)
        {
            string answer = simpNode.Data.Answer;
            if (shortcutCatalogList.ContainsKey(answer))
            {
                var catas = shortcutCatalogList[answer];
                var tag = RemoveCata(catas, simpNode.Data.Id);
                if (catas.Count == 0)
                {
                    shortcutCatalogList.Remove(answer);
                }
                simpNode.Tag = tag;
            }
            else
            {
                var cata = EntityHelper.Create<ShortcutCatalogEntity>(_dbAccount);
                cata.Name = answer;
                simpNode.Tag = cata;
            }
            CreateTreeNode(simpNode, shortcutCatalogList, shortcutList);
        }

        private object RemoveCata(List<ShortcutCatalogEntity> shortcutCatalogs, string id)
        {
	        var catalog = shortcutCatalogs.FirstOrDefault(k=>k.EntityId == id) ?? shortcutCatalogs[0];
	        shortcutCatalogs.Remove(catalog);
	        return catalog;
        }


        private void CreateTreeNode(SimpleTreeNode<Node> simpNode)
        {
            var entityId = (simpNode.Tag as TreeNode).EntityId;
            for (int i = 0; i < simpNode.Children.Count; i++)
            {
                var n = simpNode.Children[i].Tag as TreeNode;
                var prev = (i > 0) ? (simpNode.Children[i - 1].Tag as TreeNode) : null;
                var next = (i < simpNode.Children.Count - 1) ? (simpNode.Children[i + 1].Tag as TreeNode) : null;
                n.ParentId = entityId;
                n.PrevId = ((prev != null) ? prev.EntityId : null);
                n.NextId = ((next != null) ? next.EntityId : null);
            }
        }

        private string GetKey(string answer, string imageName)
        {
            return answer ?? ("!@#" + imageName);
        }

        private void CreateShortcut(SimpleTreeNode<Node> simpNode, Dictionary<string, List<ShortcutEntity>> shortcutList)
        {
            var key = GetKey(simpNode.Data.Answer, simpNode.Data.ImageName);
            if (shortcutList.ContainsKey(key))
            {
                var shortcuts = shortcutList[key];
                var shortcut = RemoveShortcut(shortcuts, simpNode.Data.Id);
                if (shortcuts.Count == 0)
                {
                    shortcutList.Remove(key);
                }
                shortcut.Text = simpNode.Data.Answer;
                shortcut.ImageName = simpNode.Data.ImageName;
                shortcut.Code = simpNode.Data.Code;
                if (string.IsNullOrEmpty(shortcut.Title) && !string.IsNullOrEmpty(simpNode.Data.Question))
                {
                    shortcut.Title = simpNode.Data.Question;
                }
                simpNode.Tag = shortcut;
            }
            else
            {
                var shortcut = EntityHelper.Create<ShortcutEntity>(_dbAccount);
                shortcut.Text = simpNode.Data.Answer;
                shortcut.Code = simpNode.Data.Code;
                shortcut.ImageName = simpNode.Data.ImageName;
                shortcut.Title = simpNode.Data.Question;
                simpNode.Tag = shortcut;
            }
        }

        private ShortcutEntity RemoveShortcut(List<ShortcutEntity> shortcuts, string id)
		{
			var shortcut = shortcuts.FirstOrDefault(k=>k.EntityId == id) ?? shortcuts[0];
            shortcuts.Remove(shortcut);
            return shortcut;
		}


        private Dictionary<string, List<ShortcutCatalogEntity>> ConvertNodesToEntity(List<TreeNode> ns, out Dictionary<string, List<ShortcutEntity>> shortcutList)
        {
            var shortcutCatalogDict = new Dictionary<string, List<ShortcutCatalogEntity>>();
            shortcutList = new Dictionary<string, List<ShortcutEntity>>();
            foreach (var n in ns)
            {
                if (n is ShortcutEntity)
                {
                    var shortcut = n.Clone<ShortcutEntity>(false);
                    var key = shortcut.Text ?? ("!@#" + shortcut.ImageName);
                    if (!shortcutList.ContainsKey(key))
                    {
                        shortcutList[key] = new List<ShortcutEntity>();
                    }
                    shortcutList[key].Add(shortcut);
                }
                else if (n is ShortcutCatalogEntity)
                {
                    var shortcutCatalog = n.Clone<ShortcutCatalogEntity>(false);
                    if (!shortcutCatalogDict.ContainsKey(shortcutCatalog.Name))
                    {
                        shortcutCatalogDict[shortcutCatalog.Name] = new List<ShortcutCatalogEntity>();
                    }
                    shortcutCatalogDict[shortcutCatalog.Name].Add(shortcutCatalog);
                }
            }
            return shortcutCatalogDict;
        }

        private void CopyImageIntoCache()
        {
            var imgDir = GetImgDir();
            if (!string.IsNullOrEmpty(imgDir))
            {
                var files = Directory.GetFiles(imgDir);
                foreach (var fn in files.xSafeForEach())
                {
                    ShortcutImageHelper.CopyImageIntoCache(fn);
                }
            }
        }

        private string GetImgDir()
        {
            var dir = Path.GetDirectoryName(_filename);
            var imgDir = Path.Combine(dir, "图片");
            if (!Directory.Exists(imgDir))
            {
                imgDir = null;
            }
            return imgDir;
        }

        private SimpleTree<Node> ConvertNodesToTree(List<Node> nlist)
        {
            var nTree = new SimpleTree<Node>();
            if (nlist.Count > 0)
            {
                for (int i = 0; i < nlist.Count; i++)
                {
                    Util.Assert(nlist[i].Level == 1);
                    var treeNode = new SimpleTreeNode<Node>(nlist[i]);
                    nTree.Root.Children.Add(treeNode);
                    if (treeNode.Data.IsCatalog)
                    {
                        i++;
                        ConstructNodeTree(treeNode, nlist, ref i);
                    }
                }
            }
            return nTree;
        }

        private void ConstructNodeTree(SimpleTreeNode<Node> tn, List<Node> nlist, ref int idx)
        {
            int lv = tn.Data.Level + 1;
            while (idx < nlist.Count)
            {
                Node node = nlist[idx];
                if (node.Level < lv)
                {
                    idx--;
                    return;
                }
                if (node.Level > lv && node.IsCatalog)
                {
                    var n = FindAndRemoveNearestCatalogOfLevel(nlist, lv, idx + 1);
                    if (n != null)
                    {
                        var treeNode = new SimpleTreeNode<Node>(n);
                        tn.Children.Add(treeNode);
                        ConstructNodeTree(treeNode, nlist, ref idx);
                    }
                    else
                    {
                        var treeNode = new SimpleTreeNode<Node>(node);
                        tn.Children.Add(treeNode);
                        if (node.IsCatalog)
                        {
                            idx++;
                            ConstructNodeTree(treeNode, nlist, ref idx);
                        }
                    }
                }
                else
                {
                    var treeNode = new SimpleTreeNode<Node>(node);
                    tn.Children.Add(treeNode);
                    if (node.IsCatalog)
                    {
                        idx++;
                        ConstructNodeTree(treeNode, nlist, ref idx);
                    }
                }
                idx++;
            }
        }

        private Node FindAndRemoveNearestCatalogOfLevel(List<Node> nlist, int lv, int idx0)
        {
            Node n = null;
            try
            {
                for (int i = idx0; i < nlist.Count; i++)
                {
                    var node = nlist[i];
                    if (node.IsCatalog && node.Level == lv)
                    {
                        n = node;
                        nlist.RemoveAt(i);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            return n;
        }

        private void FixNodeCode(List<Node> ns)
        {
            foreach (Node node in ns)
            {
                if (!string.IsNullOrEmpty(node.Code) && char.IsDigit(node.Code[0]))
                {
                    node.Code = "x" + node.Code;
                }
            }
        }


        public static void Import(string fn, bool isAllReplace, string dbAccount)
        {
            try
            {
                switch (GetFileType(fn))
                {
                    case FileTypeEnum.Qn:
                        new QnShortcutImporter(fn, isAllReplace, dbAccount).Import();
                        break;
                    case FileTypeEnum.Bot:
                        new BotShortcutImporter(fn, isAllReplace, dbAccount).Import();
                        break;
                    default:
                        throw new Exception("未知的导入文件格式");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                MsgBox.ShowErrDialog(ex.Message);
            }
        }

        private static FileTypeEnum GetFileType(string fn)
        {
            var fileType = FileTypeEnum.Unknown;
            var firstLine = CsvFileHelper.ReadCsvFile(fn, 1);
            if (firstLine.Count == 1)
            {
                var firstLineStr = firstLine[0].xToCsvStringWithoutEscape();
                if (BotShortcutImporter.IsFileType(firstLine[0][0]))
                {
                    fileType = FileTypeEnum.Bot;
                }
                else if (QnShortcutImporter.IsFileType(firstLineStr))
                {
                    fileType = FileTypeEnum.Qn;
                }
            }
            return fileType;
        }

        private enum FileTypeEnum
        {
            Unknown,
            Qn,
            Bot,
        }

        protected class Node
        {
            public int Level;
            public bool IsCatalog;
            public string Code;
            public string Question;
            public string Answer;
            public string ImageName;
            public string Id;
        }
    }
}
