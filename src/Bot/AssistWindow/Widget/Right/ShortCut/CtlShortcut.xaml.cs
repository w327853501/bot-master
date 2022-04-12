using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Bot.Common;
using Bot.Common.Account;
using Bot.Common.TreeviewHelper;
using Bot.Common.Trivial;
using Bot.Automation.ChatDeskNs;
using BotLib;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf.Extensions;
using Xceed.Wpf.Toolkit;
using DbEntity;
using Bot.Options;
using Bot.Robot.Rule.QaCiteTable;
using Bot.Common.Db;
using System.Threading.Tasks;
using Bot.Common.ImageHelper;

namespace Bot.AssistWindow.Widget.Right.ShortCut
{
    public partial class CtlShortcut : UserControl
    {
        private ContextMenu _menuShortcut;
        private bool _isDoubleClick;
        private DateTime _doubleClickTime;
        private string _seller;
        private ShortcutTreeviewController _pubTvController;
        private ShortcutTreeviewController _prvTvController;
        private ChatDesk _desk;
        private RightPanel _rightPanel;
        private bool _needInitTvSearch;
        private DateTime _prvSearchMouseDownTime;
        private DateTime _pubSearchMouseDownTime;
        private TreeNode _nodeToBeCopy;
        private class TabItemTag
        {
            public TabItemTag(ShortcutCatalogEntity cataEntity, bool isShopShortcut)
            {
                CatEntity = cataEntity;
                IsShopShortcut = isShopShortcut;
            }

            public ShortcutCatalogEntity CatEntity;

            public bool IsShopShortcut;
        }

        public CtlShortcut(ChatDesk desk, RightPanel rp)
        {
            _isDoubleClick = false;
            _doubleClickTime = DateTime.MinValue;
            _needInitTvSearch = true;
            _prvSearchMouseDownTime = DateTime.MinValue;
            _pubSearchMouseDownTime = DateTime.MinValue;
            InitializeComponent();
            _desk = desk;
            _rightPanel = rp;
            _seller = _desk.Seller;
            SetContentVisible();
            InitTabControl(null, null, null);
            LoadDatas();
            SetTitleButtonsVisible();
            ShowTitleButtons();
            DbSyner.EvHasShortcutDowned += OnEvHasShortcutDowned;
        }

        private void OnEvHasShortcutDowned(object sender, EventArgs e)
        {
            LoadDatas();
        }

        public void SetTitleButtonsVisible()
        {
            grdTitleButtons.xIsVisible(Params.Shortcut.GetIsShowTitleButtons(_seller));
        }

        public void SetContentVisible()
        {
            Params.Shortcut.ShowType showType = Params.Shortcut.GetShowType(_seller);
            if (showType != Params.Shortcut.ShowType.ShopOnly)
            {
                if (showType != Params.Shortcut.ShowType.SelfOnly)
                {
                    Grid.SetRow(gboxPub, 0);
                    Grid.SetRow(gboxPrv, 1);
                    rd2.Height = new GridLength(1.0, GridUnitType.Star);
                    gboxPrv.Visibility = Visibility.Visible;
                    gboxPub.Visibility = Visibility.Visible;
                }
                else
                {
                    Grid.SetRow(gboxPrv, 0);
                    rd2.Height = GridLength.Auto;
                    gboxPrv.Visibility = Visibility.Visible;
                    gboxPub.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Grid.SetRow(gboxPub, 0);
                rd2.Height = GridLength.Auto;
                gboxPub.Visibility = Visibility.Visible;
                gboxPrv.Visibility = Visibility.Collapsed;
            }
            if (IsNotShopOnly())
            {
                tvPrvSearch.CollapseSearch();
                BindEvent(tvPrvSearch, false);
            }
            if (IsNotSelfOnly())
            {
                tvPubSearch.CollapseSearch();
                BindEvent(tvPubSearch, false);
            }
        }

        private void BindEvent(CtlTreeView treeview, bool useDrop)
        {
            if (useDrop)
            {
                treeview.EvDrop -= OnEvDrop;
                treeview.EvDrop += OnEvDrop; ;
            }
            treeview.tvMain.MouseDoubleClick -= OnTreeviewDoubleClick;
            treeview.tvMain.MouseDoubleClick += OnTreeviewDoubleClick;
            treeview.tvMain.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            treeview.tvMain.MouseLeftButtonUp += OnMouseLeftButtonUp;
            treeview.tvMain.ContextMenu = MenuShortcut;
        }

        private void OnEvDrop(object sender, CommonEventArgs<TreeViewItem> e)
        {
            LoadDatas(sender as CtlTreeView, e.Data.Tag as string);
        }

        public ContextMenu MenuShortcut
        {
            get
            {
                if (_menuShortcut == null)
                {
                    _menuShortcut = (ContextMenu)base.FindResource("menuShortcut");
                }
                return _menuShortcut;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((DateTime.Now - _doubleClickTime).TotalMilliseconds > 500)
            {
                _isDoubleClick = false;
                _doubleClickTime = DateTime.Now;
                TreeView tv = sender as TreeView;
                CtlTreeView ctv = tv.xFindAncestor<CtlTreeView>();
                bool isCatalog;
                if (ctv.IsPointInsideItem(false, out isCatalog))
                {
                    if (!isCatalog)
                    {
                        DelayCaller.CallAfterDelayInUIThread(() =>
                        {
                            var shortcut = ctv.ReadNodeFromSelectedItem() as ShortcutEntity;
                            SetOrSendShortcut(shortcut, false);
                        }, 80);
                    }
                }
            }
        }

        private void OnTreeviewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _isDoubleClick = true;
            _doubleClickTime = DateTime.Now;
            TreeView tv = sender as TreeView;
            CtlTreeView ctv = tv.xFindAncestor<CtlTreeView>();
            bool isCatalog;
            if (ctv.IsPointInsideItem(false, out isCatalog))
            {
                if (!isCatalog)
                {
                    ShortcutEntity shortcut = ctv.ReadNodeFromSelectedItem() as ShortcutEntity;
                    SetOrSendShortcut(shortcut, true);
                    e.Handled = true;
                }
                else
                {
                    Util.Assert(false, "节点为空");
                }
            }
        }

        private void SetOrSendShortcut(ShortcutEntity shortcut, bool isSend)
        {
            if (shortcut != null)
            {
                shortcut.SetOrSendShortcutAsync(_desk, isSend, true);
            }
        }

        private void InitTabControl(string tabRootNodeId = null, string subTabRootNodeId = null, string targetNodeId = null)
        {
            try
            {
                tabMain.Items.Clear();
                if (IsNotSelfOnly())
                {
                    InitPubTvController();
                }
                if (IsNotShopOnly())
                {
                    InitPrvTvController();
                }
                InitTabData(tabRootNodeId, subTabRootNodeId, targetNodeId);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                MsgBox.ShowErrTip(ex.Message,null);
            }
        }

        private void InitTabData(string rootId, string subRootId, string targetNodeId)
        {
            if (string.IsNullOrEmpty(rootId)) return;

            TabItem mainTabItem = FindCatalogTabItemById(tabMain, rootId);
            if (mainTabItem == null) return;
            if (!string.IsNullOrEmpty(subRootId))
            {
                TabControl tabControl = mainTabItem.Content as TabControl;
                if (tabControl != null)
                {
                    TabItem tabItem = FindCatalogTabItemById(tabControl, subRootId);
                    if (tabItem != null)
                    {
                        if (targetNodeId != null)
                        {
                            CtlTreeView ctv = tabItem.Content as CtlTreeView;
                            if (ctv != null)
                            {
                                ctv.ReloadTreeViewData(subRootId);
                            }
                            else
                            {
                                ReloadTabItemData(tabItem, targetNodeId);
                            }
                        }
                        tabControl.SelectedItem = tabItem;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(targetNodeId))
            {
                var ctv = mainTabItem.Content as CtlTreeView;
                if (ctv != null)
                {
                    ctv.ReloadTreeViewData(subRootId);
                }
                else
                {
                    ReloadTabItemData(mainTabItem, targetNodeId);
                }
            }
            tabMain.SelectedItem = mainTabItem;
        }

        private bool IsNotShopOnly()
        {
            return Params.Shortcut.GetShowType(_seller) != Params.Shortcut.ShowType.ShopOnly;
        }

        private bool IsNotSelfOnly()
        {
            return Params.Shortcut.GetShowType(_seller) != Params.Shortcut.ShowType.SelfOnly;
        }

        private void InitPrvTvController()
        {
            if (_prvTvController == null)
            {
                _prvTvController = new ShortcutTreeviewController(AccountHelper.GetPrvDbAccount(_seller), _seller);
                tvPrvSearch.Init(_prvTvController);
            }
            LoadTvControllerData(_prvTvController, false);
        }

        private void InitPubTvController()
        {
            if (_pubTvController == null)
            {
                _pubTvController = new ShortcutTreeviewController(AccountHelper.GetPubDbAccount(_seller), _seller);
                tvPubSearch.Init(_pubTvController);
            }
            LoadTvControllerData(_pubTvController, true);
        }

        private void LoadTvControllerData(ShortcutTreeviewController tvController, bool isShopShortcut)
        {
            List<TreeNode> cNodes = tvController.DbAccessor.ReadAllCataNodes(tvController.DbAccessor.Root.EntityId);
            foreach (var node in cNodes)
            {
                var cataEt = node as ShortcutCatalogEntity;
                var tabItem = CreateTabItem(tabMain, cataEt.Name, cataEt, true, isShopShortcut);
                var subCataNodes = tvController.DbAccessor.ReadAllCataNodes(cataEt.EntityId);
                if (subCataNodes.xCount() > 0)
                {
                    var tabControl = new TabControl();
                    CreateTabItem(tabControl, cataEt.Name + "*", cataEt, false, isShopShortcut);
                    foreach (var catNode in subCataNodes)
                    {
                        var subCataEt = catNode as ShortcutCatalogEntity;
                        CreateTabItem(tabControl, subCataEt.Name, subCataEt, false, isShopShortcut);
                    }
                    tabControl.SelectionChanged += tabMain_SelectionChanged;
                    tabItem.Content = tabControl;
                }
            }
            tabMain.SelectionChanged -= tabMain_SelectionChanged;
            tabMain.SelectionChanged += tabMain_SelectionChanged;
            CreateTabItem(tabMain, ShortcutTitle(isShopShortcut), tvController.DbAccessor.Root as ShortcutCatalogEntity, true, isShopShortcut);
        }

        private string ShortcutTitle(bool isShopShortcut)
        {
            return isShopShortcut ? "所有公用短语" : "所有私人短语";
        }

        private TabItem CreateTabItem(TabControl tabControl, string header, ShortcutCatalogEntity cataEt, bool level1Style, bool isShopShortcut)
        {
            TabItem tabItem = new TabItem();
            tabItem.Header = header;
            tabItem.Tag = new TabItemTag(cataEt, isShopShortcut);
            tabItem.xHoverSelect();
            tabItem.Style = GetTabStyle(level1Style, isShopShortcut);
            tabControl.Items.Add(tabItem);
            return tabItem;
        }

        private Style GetTabStyle(bool level1Style, bool isShopShortcut)
        {
            Style s;
            if (isShopShortcut)
            {
                if (level1Style)
                {
                    s = (Style)base.FindResource("tabPubLevel1");
                }
                else
                {
                    s = (Style)base.FindResource("tabPubLevel2");
                }
            }
            else if (level1Style)
            {
                s = (Style)base.FindResource("tabPrvLevel1");
            }
            else
            {
                s = (Style)base.FindResource("tabPrvLevel2");
            }
            return s;
        }

        private TabItem FindCatalogTabItemById(TabControl tab, string cateId)
        {
            TabItem tabItem = null;
            foreach (TabItem item in tab.Items)
            {
                TabItemTag tabItemTag = item.Tag as TabItemTag;
                ShortcutCatalogEntity catEntity = tabItemTag.CatEntity;
                if (catEntity.EntityId == cateId)
                {
                    tabItem = item;
                    break;
                }
            }
            return tabItem;
        }

        private void tabMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == e.OriginalSource)
            {
                var tabControl = sender as TabControl;
                if (tabControl != null && tabControl.Items != null && tabControl.Items.Count != 0 && tabControl.SelectedIndex >= 0)
                {
                    var tabItem = tabControl.Items[tabControl.SelectedIndex] as TabItem;
                    if (tabItem.Content == null)
                    {
                        ReloadTabItemData(tabItem, null);
                    }
                }
            }
        }

        private void ReloadTabItemData(TabItem tabItem, string targetNodeId = null)
        {
            TabItemTag tag = GetTag(tabItem);
            var catEt = tag.CatEntity;
            if (catEt != null)
            {
                var ctv = new CtlTreeView();
                ctv.CollapseSearch();
                if (tag.IsShopShortcut)
                {
                    TreeNode showFrom = catEt;
                    var entityId = catEt.EntityId;
                    ctv.Init(_pubTvController, null, null, false, entityId == ((_pubTvController != null) ? _pubTvController.DbAccessor.Root.EntityId : null), targetNodeId, showFrom, CanEditShortCut);
                }
                else
                {
                    var showFrom = catEt;
                    var entityId = catEt.EntityId;
                    ctv.Init(_prvTvController, null, null, false, entityId == ((_prvTvController != null) ? _prvTvController.DbAccessor.Root.EntityId : null), targetNodeId, showFrom, CanEditShortCut);
                }
                BindEvent(ctv, true);
                SetTooltip(ctv);
                tabItem.Content = ctv;
            }
        }

        private void SetTooltip(CtlTreeView ctv)
        {
            ctv.tvMain.xTraverse((it) =>
            {
                var ctlLeaf = it.Header as CtlLeaf;
                if (ctlLeaf == null) return;
                var shortcut = ctv.ReadNode(it) as ShortcutEntity;
                var text = shortcut.Text.xLimitCharCountPerLine(70);
                it.ToolTip = text;
                if (text.Length > 50)
                {
                    it.SetValue(ToolTipService.ShowDurationProperty, text);
                }
            });
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            Create();
        }

        private void Create()
        {
            var ctv = GetShowingTreeview();
            if (CanEdit(ctv))
            {
                ctv.Create(null, (node) =>
                {
                    LoadDatas(ctv, node.EntityId);
                });
            }
        }

        private bool CanEdit(CtlTreeView ctv)
        {
            return IsInsidePrvTabItem(ctv) || QnHelper.Auth.CanEditShortCut(_seller, "编辑话术");;
        }

        private bool IsInsidePrvTabItem(CtlTreeView ctv)
        {
            bool rt;
            if (grdSearch.IsVisible)
            {
                rt = (_prvSearchMouseDownTime > _pubSearchMouseDownTime);
            }
            else
            {
                TabItemTag tag = FindRootTabItemTag(ctv);
                rt = !tag.IsShopShortcut;
            }
            return rt;
        }

        private TabItemTag FindRootTabItemTag(CtlTreeView ctv)
        {
            TabItemTag tag = null;
            if (ctv != null)
            {
                tag = (ctv.xFindAncestor<TabItem>().Tag as TabItemTag);
            }
            return tag;
        }

        private TabItemTag GetTag(TabItem tabItem)
        {
            return tabItem.Tag as TabItemTag;
        }

        private void ClearTreeViewExcept(CtlTreeView tv)
        {
            TabItem tabItem = tv.xFindAncestor<TabItem>();
            TabControl tabControl = tabItem.xFindAncestor<TabControl>();
            if (tabControl != null)
            {
                Util.Assert(tabControl.SelectedItem == tabItem);
                ClearTreeViewExcept(tabControl, tabItem);
            }
            if (tabControl != tabMain)
            {
                TabItem tabItem_ = tabMain.SelectedItem as TabItem;
                ClearTreeViewExcept(tabMain, tabItem_);
            }
            _needInitTvSearch = true;
        }

        private void ClearTreeViewExcept(TabControl tabControl, TabItem exceptTabItem)
        {
            if (tabControl != null && tabControl.Items != null)
            {
                foreach (TabItem tabItem in tabControl.Items)
                {
                    if (tabItem != exceptTabItem)
                    {
                        tabItem.Content = null;
                    }
                }
            }
        }

        private CtlTreeView GetShowingTreeview(TabControl tc = null)
        {
            CtlTreeView ctv = null;
            try
            {
                if (grdSearch.IsVisible)
                {
                    ctv = GetShowingSearchTreeview();
                }
                else
                {
                    tc = (tc ?? tabMain);
                    TabItem tabItem = tc.SelectedItem as TabItem;
                    if (tabItem != null)
                    {
                        if (tabItem.Content is TabControl)
                        {
                            ctv = GetShowingTreeview(tabItem.Content as TabControl);
                        }
                        else
                        {
                            Util.Assert(tabItem.Content is CtlTreeView);
                            ctv = (tabItem.Content as CtlTreeView);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            return ctv;
        }

        private CtlTreeView GetShowingSearchTreeview()
        {
            Params.Shortcut.ShowType showType = Params.Shortcut.GetShowType(_seller);
            CtlTreeView ctv = null;
            if (showType != Params.Shortcut.ShowType.ShopOnly)
            {
                if (showType != Params.Shortcut.ShowType.SelfOnly)
                {
                    ctv = ((_pubSearchMouseDownTime > _prvSearchMouseDownTime) ? tvPubSearch : tvPrvSearch);
                }
                else
                {
                    ctv = tvPrvSearch;
                }
            }
            else
            {
                ctv = tvPubSearch;
            }
            return ctv;
        }

        public void LoadDatas(CtlTreeView ctv = null, string targetNodeId = null)
        {
            if (grdSearch.IsVisible)
            {
                InitTvController();
                InitTabControl(null, null, null);
            }
            else
            {
                string parentId = null;
                var rootId = GetCtvRoot(ctv, targetNodeId, out parentId);
                InitTabControl(rootId, parentId, targetNodeId);
            }
        }

        private string GetCtvRoot(CtlTreeView ctv, string targetNodeId, out string parentId)
        {
            string rootId = null;
            parentId = null;
            ctv = (ctv ?? GetShowingTreeview());
            if (ctv != null && ctv != tvPrvSearch && ctv != tvPubSearch)
            {
                var rootTag = FindRootTabItemTag(ctv);
                if (rootTag != null)
                {
                    rootId = FindParentNode(GetShortcutTreeviewController(rootTag), rootTag.CatEntity, out parentId);
                }
            }
            return rootId;
        }

        private ShortcutTreeviewController GetShortcutTreeviewController(TabItemTag tag)
        {
            return tag.IsShopShortcut ? _pubTvController : _prvTvController;
        }

        private string FindParentNode(ShortcutTreeviewController tvController, TreeNode n, out string parentId)
        {
            var selfId = string.Empty;
            parentId = null;
            var ancestorList = tvController.DbAccessor.ReadAncestorList(n, true, false);
            if (ancestorList.Count > 0)
            {
                selfId = ancestorList[0].EntityId;
                if (ancestorList.Count > 1)
                {
                    parentId = ancestorList[1].EntityId;
                }
                else if (tvController.DbAccessor.ReadChildCatalogById(selfId).xCount() > 0)
                {
                    parentId = selfId;
                }
            }
            else
            {
                bool isRoot = false;
                if (n.EntityId != ((_prvTvController != null) ? _prvTvController.DbAccessor.Root.EntityId : null))
                {
                    isRoot = (n.EntityId == ((_pubTvController != null) ? _pubTvController.DbAccessor.Root.EntityId : null));
                }
                else
                {
                    isRoot = true;
                }
                if (isRoot)
                {
                    selfId = n.EntityId;
                }
            }
            return selfId;
        }

        private void btnCreateCat_Click(object sender, RoutedEventArgs e)
        {
            CreateCat();
        }

        private void CreateCat()
        {
            var ctv = GetShowingTreeview();
            if (CanEdit(ctv))
            {
                ctv.CreateCatalog(null, (node) =>
                {
                    LoadDatas(ctv, node.EntityId);
                    CiteTableManager.AddOrUpdateShortcutToInputPromptWordCite(node as ShortcutEntity);
                });
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }

        private void Edit()
        {
            var ctv = GetShowingTreeview();
            if (CanEdit(ctv))
            {
                ctv.EditAsync((node) =>
                {
                    LoadDatas(ctv, node.EntityId);
                    CiteTableManager.AddOrUpdateShortcutToInputPromptWordCite(node as ShortcutEntity);
                });
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void Delete()
        {
            var ctv = GetShowingTreeview();
            if (CanEdit(ctv))
            {
                var tn = ctv.Delete();
                if (tn != null)
                {
                    LoadDatas(ctv, null);
                    if (!ctv.IsCatalogType(tn))
                    {
                        var shortcut = tn as ShortcutEntity;
                        if (shortcut != null && !string.IsNullOrEmpty(shortcut.ImageName))
                        {
                            ShortcutImageHelper.DeleteImage(shortcut.ImageName);
                        }
                    }
                }
            }
        }

        private void Help()
        {
        }

        private void tbxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = tbxSearch.Text.Trim();
            ShowSearchGrid(text == "");
            if (text != "")
            {
                if (_needInitTvSearch)
                {
                    _needInitTvSearch = false;
                    InitTvController();
                }
                if (IsNotSelfOnly())
                {
                    tvPubSearch.tbxSearch.Text = text;
                }
                if (IsNotShopOnly())
                {
                    tvPrvSearch.tbxSearch.Text = text;
                }
            }
        }

        public void InitTvController()
        {
            if (_pubTvController != null)
            {
                tvPubSearch.Init(_pubTvController, null, null, false, false, null, null, null);
            }
            if (_prvTvController != null)
            {
                tvPrvSearch.Init(_prvTvController, null, null, false, false, null, null, null);
            }
        }

        private bool CanEditShortCut()
        {
            return QnHelper.Auth.CanEditShortCut(_seller);
        }

        private void ShowSearchGrid(bool showSearchGrd)
        {
            if (showSearchGrd)
            {
                tabMain.Visibility = Visibility.Visible;
                grdSearch.Visibility = Visibility.Collapsed;
            }
            else
            {
                tabMain.Visibility = Visibility.Collapsed;
                grdSearch.Visibility = Visibility.Visible;
            }
        }

        private void OnClearCommand(object sender, ExecutedRoutedEventArgs e)
        {
            tbxSearch.Text = "";
        }

        private void mCreate_Click(object sender, RoutedEventArgs e)
        {
            Create();
        }

        private void OnInsertShortcutToInputbox(object sender, RoutedEventArgs e)
        {
            ShortcutEntity shortcut = GetSelectedShortcut();
            if (shortcut != null)
            {
                SetOrSendShortcut(shortcut, true);
            }
        }

        private void OnSendShortcut(object sender, RoutedEventArgs e)
        {
            ShortcutEntity shortcut = GetSelectedShortcut();
            if (shortcut != null)
            {
                SetOrSendShortcut(shortcut, false);
            }
        }

        private ShortcutEntity GetSelectedShortcut()
        {
            ShortcutEntity sc = null;
            var ctv = GetShowingTreeview();
            TreeNode n = ctv.ReadNodeFromSelectedItem();
            if (n is ShortcutEntity)
            {
                sc = (n as ShortcutEntity);
            }
            return sc;
        }

        private TreeNode ReadSelectedNode()
        {
            var ctv = GetShowingTreeview();
            return (ctv != null) ? ctv.ReadNodeFromSelectedItem() : null;
        }

        private string GetSelectedNodeText()
        {
            string rtText = null;
            var n = ReadSelectedNode();
            if (n is ShortcutEntity)
            {
                rtText = (n as ShortcutEntity).Text;
            }
            else if (n is ShortcutCatalogEntity)
            {
                rtText = (n as ShortcutCatalogEntity).Name;
            }
            return rtText;
        }

        private void mCopy_Click(object sender, RoutedEventArgs e)
        {
            var text = GetSelectedNodeText();
            if (!string.IsNullOrEmpty(text))
            {
                ClipboardEx.SetTextSafe(text);
            }
        }

        private void mHelp_Click(object sender, RoutedEventArgs e)
        {
            Help();
        }

        private void mCreateCata_Click(object sender, RoutedEventArgs e)
        {
            CreateCat();
        }

        private void mDelete_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void mEdit_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }

        private void mImport_Click(object sender, RoutedEventArgs e)
        {
            WndShortcutImporter.MyShow(_seller, this.xFindParentWindow(), 
                ()=>{
                    ShowTip("正在导入话术");
                    spTip.Visibility = Visibility.Visible;
                    grdMain.IsEnabled = false;
                },()=>{
                    DispatcherEx.xInvoke(()=>{
                        LoadDatas();
                        HideTip();
                    });
                });
        }

        private void mQnImport_Click(object sender, RoutedEventArgs e)
        {
            MsgBox.ShowNotTipAgain("确定要导入千牛快捷短语？", "操作提示", "CtlShortcut.mQnImport_Click", async (b1, isOkClicked) =>
            {
                await Task.Factory.StartNew(async () => {
                    var res = await _desk.GetQuickPhrases();
                    new QuickPhraseImporter(_pubTvController.DbAccessor,res).Import();
                }, TaskCreationOptions.LongRunning);
            });
        }


        private void ShowTip(string msg)
        {
            tboxTip.Text = msg;
            spTip.Visibility = Visibility.Visible;
            grdMain.IsEnabled = false;
        }

        private void HideTip()
        {
            spTip.Visibility = Visibility.Collapsed;
            grdMain.IsEnabled = true;
        }

        private bool IsTipVisible()
        {
            return spTip.Visibility == Visibility.Visible;
        }

        private void mExport_Click(object sender, RoutedEventArgs e)
        {
            string mainPart = TbNickHelper.GetMainPart(_seller);
            TreeDbAccessor pubDbAccessor = (_pubTvController != null) ? _pubTvController.DbAccessor : null;
            //Exporter.Export(mainPart, pubDbAccessor, (_prvTvController != null) ? _prvTvController.DbAccessor : null);
        }

        private void OnOpenContextMenu(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.ContextMenu.IsOpen = true;
        }

        public void ShowTitleButtons()
        {
            ContextMenu contextMenu = FindResource("menuShortcut") as ContextMenu;
            if (contextMenu != null)
            {
                bool isShowTitleButtons = Params.Shortcut.GetIsShowTitleButtons(_desk.Seller);
                contextMenu.xSetMenuItemVisibilityByTag("ShowTitle", !isShowTitleButtons);
                contextMenu.xSetMenuItemVisibilityByTag("HideTitle", isShowTitleButtons);
            }
        }

        private void mHideTitle_Click(object sender, RoutedEventArgs e)
        {
            MsgBox.ShowTip("确定要隐藏第一行的按钮？", isYesButtonClicked =>
            {
                if (isYesButtonClicked)
                {
                    MsgBox.ShowTip("右击任意的分类短语，弹出的菜单中，可以恢复显示这些按钮！", "提示");
                    grdTitleButtons.xIsVisible(false);
                    Params.Shortcut.SetIsShowTitleButtons(_desk.Seller, false);
                    ShowTitleButtons();
                }
            }, null, null);
        }

        private void mShowTitle_Click(object sender, RoutedEventArgs e)
        {
            grdTitleButtons.xIsVisible(true);
            Params.Shortcut.SetIsShowTitleButtons(_desk.Seller, true);
            ShowTitleButtons();
        }

        private void btnClearSearchText_Click(object sender, RoutedEventArgs e)
        {
            tbxSearch.Text = "";
        }

        private void tvPrvSearch_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _prvSearchMouseDownTime = DateTime.Now;
        }

        private void tvPubSearch_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _pubSearchMouseDownTime = DateTime.Now;
        }

        private void mSetting_Click(object sender, RoutedEventArgs e)
        {
            WndOption.MyShow(_seller, null, OptionEnum.Shortcut, null);
        }

        private void mPasteNode_Click(object sender, RoutedEventArgs e)
        {
            var ctv = GetShowingTreeview();
            if (CanEdit(ctv))
            {
                if (_nodeToBeCopy == null)
                {
                    MsgBox.ShowErrTip("请先复制节点");
                }
                else
                {
                    bool isPub = IsPub(_nodeToBeCopy);
                    var targetNode = ReadSelectedNode() ?? GetTabItemCatalog();
                    if (targetNode == null)
                    {
                        Util.Beep();
                    }
                    else if (isPub == IsPub(targetNode))
                    {
                        MsgBox.ShowErrTip(string.Format("只能将【{0}】节点，粘贴成【{1}】短语。请先复制【{0}】节点", isPub ? "店铺公用" : "个人私用", isPub ? "个人私用" : "店铺公用"));
                    }
                    else
                    {
                        PasteNode(targetNode);
                        _nodeToBeCopy = null;
                    }
                }
            }
        }

        private TreeNode GetTabItemCatalog()
        {
            var ctv = GetShowingTreeview();
            return FindRootTabItemTag(ctv).CatEntity;
        }

        private void PasteNode(TreeNode targetNode)
        {
            var toDbAccessor = GetDbAccessor(targetNode);
            var fromDbAccessor = GetDbAccessor(_nodeToBeCopy);
            TreeNode n;
            if (IsCatalog(targetNode))
            {
                n = PasteCataNode(targetNode, _nodeToBeCopy, toDbAccessor, fromDbAccessor);
            }
            else
            {
                n = PasteShortcutNode(targetNode, toDbAccessor, fromDbAccessor);
            }
            LoadDatas(null, n.EntityId);
        }

        private TreeNode PasteShortcutNode(TreeNode targetNode, TreeDbAccessor toDbAccessor, TreeDbAccessor fromDbAccessor)
        {
            var newNode = CreateNode(_nodeToBeCopy, toDbAccessor.DbAccount);
            toDbAccessor.AddNext(newNode, targetNode);
            if (IsCatalog(newNode))
            {
                PasteChildNodes(newNode, fromDbAccessor.ReadDescendantNode(_nodeToBeCopy.EntityId, true), toDbAccessor, fromDbAccessor);
            }
            return newNode;
        }

        private void PasteChildNodes(TreeNode cat, List<TreeNode> childNodes, TreeDbAccessor toDbAccessor, TreeDbAccessor fromDbAccessor)
        {
            foreach (TreeNode node in childNodes)
            {
                PasteCataNode(cat, node, toDbAccessor, fromDbAccessor);
            }
        }

        private TreeNode CreateNode(TreeNode n, string dbAccount)
        {
            TreeNode toNode;
            if (n is ShortcutCatalogEntity)
            {
                var cat = n as ShortcutCatalogEntity;
                toNode = CreateCatalog(cat, dbAccount);
            }
            else
            {
                var shortcut = n as ShortcutEntity;
                toNode = CreateShortcut(shortcut, dbAccount);
            }
            return toNode;
        }

        private TreeNode CreateShortcut(ShortcutEntity fromShortcut, string dbAccount)
        {
            var shortcut = EntityHelper.Create<ShortcutEntity>(dbAccount);
            shortcut.Text = fromShortcut.Text;
            shortcut.ImageName = ShortcutImageHelper.AddNewImage(fromShortcut.ImageName);
            shortcut.Code = fromShortcut.Code;
            return shortcut;
        }

        private TreeNode CreateCatalog(ShortcutCatalogEntity fromCat, string dbAccount)
        {
            var shortcutCatalog = EntityHelper.Create<ShortcutCatalogEntity>(dbAccount);
            shortcutCatalog.Name = fromCat.Name;
            return shortcutCatalog;
        }

        private TreeNode PasteCataNode(TreeNode toNode, TreeNode fromNode, TreeDbAccessor toDbAccessor, TreeDbAccessor fromDbAccessor)
        {
            var pasteNode = CreateNode(fromNode, toDbAccessor.DbAccount);
            toDbAccessor.AddNodeToTargetNode(pasteNode, toNode.EntityId);
            if (IsCatalog(pasteNode))
            {
                PasteChildNodes(pasteNode, fromDbAccessor.ReadDescendantNode(fromNode.EntityId, true), toDbAccessor, fromDbAccessor);
            }
            return pasteNode;
        }

        private bool IsCatalog(TreeNode n)
        {
            return n is ShortcutCatalogEntity;
        }

        private TreeDbAccessor GetDbAccessor(TreeNode n)
        {
            TreeDbAccessor dbAccessor = null;
            if (n.DbAccount == ((_pubTvController != null) ? _pubTvController.DbAccount : null))
            {
                dbAccessor = ((_pubTvController != null) ? _pubTvController.DbAccessor : null);
            }
            else
            {
                dbAccessor = ((_prvTvController != null) ? _prvTvController.DbAccessor : null);
            }
            return dbAccessor;
        }

        private bool IsPub(TreeNode n)
        {
            return n.DbAccount == _pubTvController.DbAccount;
        }

        private void mCopyNode_Click(object sender, RoutedEventArgs e)
        {
            _nodeToBeCopy = ReadSelectedNode();
        }

    }
}
