using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib;
using BotLib.Extensions;
using Bot.AssistWindow.Widget.Right;
using DbEntity;
using Bot.Common.ImageHelper;

namespace Bot.Common.TreeviewHelper
{
    public class TreeDbAccessor
    {
        private Type _catalogType;

        private Type _leafType;

        private string _dbAccount;

        public string DbAccount
        {
            get { return _dbAccount; }
            private set { _dbAccount = value; }
        }

        private TreeNode _root;

        private const string RootCatalogHeader = "Root!@#$Catalog$%&Header*&()";

        public TreeDbAccessor(Type catalogType, Type leafType, string dbAccount)
        {
            _catalogType = catalogType;
            _leafType = leafType;
            _dbAccount = dbAccount;
        }

        public TreeNode Root
        {
            get
            {
                if (_root == null)
                {
                    _root = ReadRootFromDb();
                    if (_root == null)
                    {
                        _root = DowRootFromServerAndSave();
                    }
                    Util.Assert(_root != null);
                }
                return _root;
            }
        }

        public TreeCatalog CreateRootAndSaveToDb(string entityId)
        {
            TreeCatalog TreeCatalog = EntityHelper.Create(_catalogType, _dbAccount) as TreeCatalog;
            TreeCatalog.Name = "Root!@#$Catalog$%&Header*&()";
            TreeCatalog.EntityId = entityId;
            SaveNode(TreeCatalog);
            return TreeCatalog;
        }

        private TreeNode DowRootFromServerAndSave()
        {
            TreeNode TreeNode = null;
            string entityId = StringEx.xGenGuidB32Str();
            if (!string.IsNullOrEmpty(entityId))
            {
                TreeNode = CreateRootAndSaveToDb(entityId);
            }
            Util.Assert(TreeNode != null);
            return TreeNode;
        }

        public void FixNodeOutInTheTree()
        {
            HashSet<string> nodeIds;
            var nlist = FixAllNodeInTheTree(out nodeIds);
            if (!nlist.xIsNullOrEmpty())
            {
                Log.Info(string.Format("树外节点数={0},树内节点数={1}", (nlist != null) ? new int?(nlist.Count) : null, (nodeIds != null) ? new int?(nodeIds.Count) : null));
                nlist.ForEach(x =>
                {
                    if (x.ParentId == x.EntityId)
                    {
                        x.ParentId = null;
                    }
                });
                var notInList = TakeNodesThatParentNotInList(nlist);
                foreach (var tn in notInList.xSafeForEach())
                {
                    AppendToParentNode(tn, nodeIds, nlist);
                }
                if (!nlist.xIsNullOrEmpty())
                {
                    Log.Info(string.Format("将{0}个节点，挂到根分类下", nlist.Count));
                    AppendToRootNode(nlist);
                }
            }
        }

        private void AppendToParentNode(TreeNode n, HashSet<string> ids, List<TreeNode> nlist)
        {
            var entityId = ids.Contains(n.ParentId) ? n.ParentId : Root.EntityId;
            SaveToDb(n, entityId);
            if (IsCatalogType(n))
            {
                TakeNodesThatParentNotInList(n, nlist);
            }
        }

        private void AppendToRootNode(List<TreeNode> nlist)
        {
            foreach (var n in nlist)
            {
                SaveToDb(n, Root.EntityId);
            }
        }

        public void SaveToDb(TreeNode n, string entityId)
        {
            n.ParentId = entityId;
            n.NextId = null;
            var lastNode = ReadLastChildNode(entityId);
            if (lastNode != null)
            {
                n.PrevId = lastNode.EntityId;
                lastNode.NextId = n.EntityId;
            }
            else
            {
                n.PrevId = null;
            }
            DbHelper.BatchSaveOrUpdateToDb(new EntityBase[]
			{
				n,
				lastNode
			});
        }

        private List<TreeNode> TakeNodesThatParentNotInList(List<TreeNode> nodes)
        {
            var idset = new HashSet<string>(nodes.Select(k => k.EntityId));
            return nodes.xRemove(k => idset.Contains(k.ParentId));
        }

        private List<TreeNode> TakeNodesThatParentNotInList(TreeNode n, List<TreeNode> nodes)
		{
			return nodes.xRemove(k=>k.ParentId == n.EntityId);
		}

        public void AddNodeToTargetNode(TreeNode n, string targetNodeId)
        {
            n.ParentId = targetNodeId;
            n.NextId = null;
            var lastNode = ReadLastChildNode(targetNodeId);
            if (lastNode != null)
            {
                n.PrevId = lastNode.EntityId;
                lastNode.NextId = n.EntityId;
            }
            else
            {
                n.PrevId = null;
            }
            DbHelper.BatchSaveOrUpdateToDb(new EntityBase[]
	        {
		        n,
		        lastNode
	        });
        }

        private TreeNode ReadLastChildNode(string entityId)
        {
            var desNs = ReadDescendantNode(entityId, true);
            TreeNode n;
            if (desNs != null && desNs.Count > 0)
            {
                n = desNs.Last();
            }
            else
            {
                n = null;
            }
            return n;
        }

        private List<TreeNode> FixAllNodeInTheTree(out HashSet<string> nodeIds)
        {
            var invalidNodes = new List<TreeNode>();
            nodeIds = new HashSet<string>();
            var nodes = ReadAllNodeFromDb();
            if (!nodes.xIsNullOrEmpty())
            {
                nodeIds = (ReadNodeIdInTheTree() ?? new HashSet<string>());
                foreach (var et in nodes)
                {
                    if (!nodeIds.Contains(et.EntityId))
                    {
                        var etNode = et as TreeNode;
                        if (etNode != null)
                        {
                            invalidNodes.Add(etNode);
                        }
                    }
                }
            }
            return invalidNodes;
        }

        private HashSet<string> ReadNodeIdInTheTree()
        {
            var nodes = ReadNodeInTheTree();
            return new HashSet<string>(nodes.Select(k => k.EntityId));
        }

        public List<TreeNode> ReadNodeInTheTree()
        {
            var nodes = new List<TreeNode>();
            nodes.Add(Root);
            MakeNodeSort(Root.EntityId, nodes);
            return nodes;
        }

        private void MakeNodeSort(string entityId, List<TreeNode> nlist)
        {
            var nodes = ReadDescendantNode(entityId, false);
            foreach (var node in nodes)
            {
                nlist.Add(node);
                if (IsCatalogType(node))
                {
                    MakeNodeSort(node.EntityId, nlist);
                }
            }
        }

        private List<EntityBase> ReadAllNodeFromDb()
        {
            var lst = new List<EntityBase>();
            lst.AddRange(DbHelper.Fetch(DbAccount, _catalogType, null));
            lst.AddRange(DbHelper.Fetch(DbAccount, _leafType, null));
            return lst;
        }

        private TreeNode ReadRootFromDb()
        {
            var nds = DbHelper.Fetch<TreeNode>(_catalogType, _dbAccount, (TreeNode x) =>
            {
                TreeCatalog TreeCatalog = (TreeCatalog)x;
                return TreeCatalog.Name == "Root!@#$Catalog$%&Header*&()";
            });
            TreeNode result = null;
            int cnt = nds.xCount();
            if (cnt == 1)
            {
                result = nds[0];
            }
            else if (cnt > 1)
            {
                result = FixExtraRoot(nds);
            }
            return result;
        }

        private TreeNode FixExtraRoot(List<TreeNode> nlist)
        {
            TreeNode node = null;
            int descendantCnt = 0;
            foreach (TreeNode n in nlist)
            {
                var descendants = ReadDescendant(n.EntityId, false);
                if (descendants != null && descendants.Count >= descendantCnt)
                {
                    descendantCnt = descendants.Count;
                    node = n;
                }
            }
            nlist.Remove(node);
            foreach (TreeNode n in nlist)
            {
                n.IsDeleted = true;
            }
            DbHelper.BatchSaveOrUpdateToDb(nlist.ToArray());
            Log.Error(string.Format("删除多余的root,count={0},dba={1}", nlist.Count, _dbAccount));
            return node;
        }

        public bool ExistCatalogName(string parentId, string name)
        {
            return DbHelper.FirstOrDefault(_catalogType, DbAccount, (k) =>
            {
                var et = k as TreeCatalog;
                return et.ParentId == parentId && et.Name == name;
            }) != null;
        }

        public bool HasSameNameCatalogChild(string parentId, string name)
        {
            return DbHelper.FirstOrDefault(_catalogType, _dbAccount,  x =>
            {
                TreeCatalog TreeCatalog = (TreeCatalog)x;
                return TreeCatalog.ParentId == parentId && TreeCatalog.Name == name && TreeCatalog.PrevId != TreeCatalog.ParentId;
            }) != null;
        }

        private List<TreeNode> ReadChildNodes(Type t, string dbAccount, string parentId)
        {
            return DbHelper.Fetch<TreeNode>(t, dbAccount, (x) => x.ParentId == parentId);
        }

        public void SaveNode(TreeNode n)
        {
            DbHelper.SaveToDb(n, true);
        }

        public void SaveRecordsInTransaction(params EntityBase[] arr)
        {
            DbHelper.BatchSaveOrUpdateToDb(arr);
        }

        public List<TreeNode> ReadAncestorList(TreeNode targetNode, bool includeSelf = true, bool includeRoot = false)
        {
            List<TreeNode> ancestors = new List<TreeNode>();
            if (includeSelf)
            {
                ancestors.Add(targetNode);
            }
            while (!string.IsNullOrEmpty(targetNode.ParentId))
            {
                targetNode = ReadCatalogById(targetNode.ParentId);
                Util.Assert(targetNode != null);
                ancestors.Add(targetNode);
            }
            if (!includeRoot && ancestors.Count > 0)
            {
                ancestors.RemoveAt(ancestors.Count - 1);
            }
            ancestors.Reverse();
            return ancestors;
        }

        private TreeNode ReadCatalogById(string entityId)
        {
            return ReadNodeByIdAndType(entityId, _catalogType);
        }

        public TreeNode ReadNodeByIdAndType(string entityId, Type type)
        {
            return (TreeNode)DbHelper.FirstOrDefault(type, _dbAccount, entityId);
        }

        public TreeNode ReadNodeById(string entityId)
        {
            TreeNode nd;
            if (entityId == null)
            {
                nd = null;
            }
            else
            {
                nd = (ReadNodeByIdAndType(entityId, _leafType) ?? ReadNodeByIdAndType(entityId, _catalogType));
            }
            return nd;
        }

        public void DeleteNode(TreeNode node)
        {
            var uptEts = new List<TreeNode>();
            node.IsDeleted = true;
            uptEts.Add(node);
            var prev = ReadNodeById(node.PrevId);
            var next = ReadNodeById(node.NextId);
            if (prev != null)
            {
                prev.NextId = ((next != null) ? next.EntityId : null);
                uptEts.Add(prev);
            }
            if (next != null)
            {
                next.PrevId = ((prev != null) ? prev.EntityId : null);
                uptEts.Add(next);
            }
            DbHelper.BatchSaveOrUpdateToDb(uptEts.ToArray());
        }

        public void AppendNode(TreeNode node, string parentId)
        {
            node.ParentId = parentId;
            node.NextId = null;
            var lastNode = ReadLastChildNode(parentId);
            if (lastNode != null)
            {
                node.PrevId = lastNode.EntityId;
                lastNode.NextId = node.EntityId;
            }
            else
            {
                node.PrevId = null;
            }
            DbHelper.BatchSaveOrUpdateToDb(new EntityBase[]
			{
				node,
				lastNode
			});
        }

        public void AddNext(TreeNode n, TreeNode prev)
        {
            InsertNewNodeBetween(n, prev.ParentId, prev, null, null);
        }

        public void InsertNewNodeBetween(TreeNode n, string parentId, TreeNode prev, TreeNode next, HashSet<TreeNode> modifiedSet = null)
        {
            Util.Assert(!string.IsNullOrEmpty(parentId));
            n.ParentId = parentId;
            if (string.IsNullOrEmpty(n.DbAccount))
            {
                if (prev != null && !string.IsNullOrEmpty(prev.DbAccount))
                {
                    n.DbAccount = prev.DbAccount;
                }
                else
                {
                    n.DbAccount = next.DbAccount;
                }
            }
            if (prev != null)
            {
                n.PrevId = prev.EntityId;
                n.NextId = prev.NextId;
                prev.NextId = n.EntityId;
                next = ReadNodeById(n.NextId);
                if (next != null)
                {
                    next.PrevId = n.EntityId;
                }
            }
            else
            {
                if (next != null)
                {
                    n.NextId = next.EntityId;
                    n.PrevId = next.PrevId;
                    next.PrevId = n.EntityId;
                    prev = ReadNodeById(n.PrevId);
                    if (prev != null)
                    {
                        prev.NextId = n.EntityId;
                    }
                }
                else
                {
                    n.PrevId = null;
                    n.NextId = null;
                    Util.Assert(!HasChildren(n.ParentId));
                }
            }
            if (modifiedSet == null)
            {
                DbHelper.BatchSaveOrUpdateToDb(n,prev,next);
            }
            else
            {
                if (n != null)
                {
                    modifiedSet.Add(n);
                }
                if (prev != null)
                {
                    modifiedSet.Add(prev);
                }
                if (next != null)
                {
                    modifiedSet.Add(next);
                }
            }
        }

        public void InsertNewNodeBetween(TreeNode n, string parentId, TreeNode prev, TreeNode next)
        {
            n.ParentId = parentId;
            if (string.IsNullOrEmpty(n.DbAccount))
            {
                string dbAccount;
                if (prev != null)
                {
                    dbAccount = prev.DbAccount;
                    if (!string.IsNullOrEmpty(dbAccount))
                    {
                        n.DbAccount = dbAccount;
                    }
                }
                else
                {
                    dbAccount = next.DbAccount;
                    n.DbAccount = dbAccount;
                }
            }
            if (prev != null)
            {
                n.PrevId = prev.EntityId;
                n.NextId = prev.NextId;
                prev.NextId = n.EntityId;
                next = ReadNodeById(n.NextId);
                if (next != null)
                {
                    next.PrevId = n.EntityId;
                }
            }
            else if (next != null)
            {
                n.NextId = next.EntityId;
                n.PrevId = next.PrevId;
                next.PrevId = n.EntityId;
                prev = ReadNodeById(n.PrevId);
                if (prev != null)
                {
                    prev.NextId = n.EntityId;
                }
            }
            else
            {
                n.PrevId = null;
                n.NextId = null;
                Util.Assert(!HasChildren(n.ParentId));
            }
            DbHelper.BatchSaveOrUpdateToDb(new EntityBase[]
            {
                n,
                prev,
                next
            });
        }

        public bool IsRoot(TreeNode n)
        {
            return n.EntityId == Root.EntityId;
        }

        public void DeleteCatalog(TreeNode et)
        {
            var uptEts = new List<TreeNode>();
            if (!IsRoot(et))
            {
                et.IsDeleted = true;
                uptEts.Add(et);
                var prev = ReadNodeById(et.PrevId);
                var next = ReadNodeById(et.NextId);
                if (prev != null)
                {
                    prev.NextId = ((next != null) ? next.EntityId : null);
                    uptEts.Add(prev);
                }
                if (next != null)
                {
                    next.PrevId = ((prev != null) ? prev.EntityId : null);
                    uptEts.Add(next);
                }
            }
            var descendants = ReadDescendant(et.EntityId, false);
            foreach (var n in descendants)
            {
                n.IsDeleted = true;
                if (n is ShortcutEntity)
                {
                    DeleteShortcutImageIfHas(n as ShortcutEntity);
                }
                uptEts.Add(n);
            }
            DbHelper.BatchSaveOrUpdateToDb(uptEts.ToArray());
        }

        private void DeleteShortcutImageIfHas(ShortcutEntity se)
        {
            try
            {
                if (se != null && !string.IsNullOrEmpty(se.ImageName))
                {
                    ShortcutImageHelper.DeleteImage(se.ImageName);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public bool HasChildren(TreeNode groupEntity)
        {
            return HasChildren(groupEntity.EntityId);
        }

        public bool HasChildren(string parentId)
        {
            var leafs = DbHelper.Fetch<TreeNode>(_leafType, _dbAccount, (TreeNode x) => x.ParentId == parentId);
            var catas = DbHelper.Fetch<TreeNode>(_catalogType, _dbAccount, (TreeNode x) => x.ParentId == parentId);
            return (leafs != null && leafs.Count() > 0) || (catas != null && catas.Count() > 0);
        }

        public List<TreeNode> ReadDescendant(string rootId, bool sortByChain = true)
        {
            var allNodes = new List<TreeNode>();
            var desNodes = ReadDescendantNode(rootId, sortByChain);
            allNodes.AddRange(desNodes);
            foreach (var TreeNode in desNodes)
            {
                if (IsCatalogType(TreeNode))
                {
                    allNodes.AddRange(ReadDescendant(TreeNode.EntityId, sortByChain));
                }
            }
            return allNodes;
        }

        public bool IsCatalogType(TreeNode x)
        {
            return x.GetType().Equals(_catalogType);
        }

        public bool IsLeafType(TreeNode x)
        {
            return x.GetType().Equals(_leafType);
        }

        public List<TreeNode> ReadDescendantNode(string parentId, bool sortByChain = true)
        {
            List<TreeNode> childNodes = new List<TreeNode>();
            var cataNodes = ReadChildNodes(_catalogType, _dbAccount, parentId);
            if (!cataNodes.xIsNullOrEmpty())
            {
                childNodes.AddRange(cataNodes);
            }
            var leafNodes = ReadChildNodes(_leafType, _dbAccount, parentId);
            if (!leafNodes.xIsNullOrEmpty())
            {
                childNodes.AddRange(leafNodes);
            }
            if (sortByChain)
            {
                childNodes = childNodes.OrderByDescending(k => k.ModifyTick).ToList();
            }
            return childNodes;
        }

        public List<TreeNode> ReadChildCatalogById(string entityId)
        {
            List<TreeNode> source = ReadDescendantNode(entityId, true);
            return source.Where(n => IsCatalogType(n)).ToList();
        }

        public List<TreeNode> ReadChildNodeById(string entityId)
        {
            List<TreeNode> source = ReadDescendantNode(entityId, true);
            return source.Where(n => IsLeafType(n)).ToList();
        }

        public List<TreeNode> ReadAllCataNodes(string rootId)
        {
            var nodes = ReadDescendantNode(rootId, true);
            return nodes.Where(k => IsCatalogType(k)).ToList();
        }


    }

}
