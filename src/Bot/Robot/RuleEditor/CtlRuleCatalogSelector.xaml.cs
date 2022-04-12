using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bot.Robot.RuleEditor
{
    public partial class CtlRuleCatalogSelectorV2 : UserControl
    {
        public string Seller;
        public string DbAccount;
        public event EventHandler<SelectedEventArgs> EvSelect;

        public CtlRuleCatalogSelectorV2()
        {
            InitializeComponent();
        }
        public void Init(string seller, string dbAccount, string targetNodeId = null)
        {
            this.Seller = seller;
            this.DbAccount = dbAccount;
            this.tvRule.Init(new RuleTreeViewController(this.DbAccount, this.Seller), tn => {
                return !(tn is RobotRuleEntity);
            }, null, false, true, targetNodeId, null, null);
        }
        private void menuSelect_Click(object sender, EventArgs e)
        {
            var selectedCatalogId = this.GetSelectedCatalogId(true);
            if (selectedCatalogId != null)
            {
                if (EvSelect != null)
                {
                    EvSelect(this, new SelectedEventArgs
                    {
                        SelectedCatalogId = selectedCatalogId,
                        IsCanceled = (selectedCatalogId == null)
                    });
                }
            }
        }

        public string GetSelectedCatalogId(bool showTip)
        {
            var it = this.tvRule.tvMain.SelectedItem as TreeViewItem;
            if (it == null || !this.tvRule.IsCatalogType(it))
            {
                if (showTip)
                {
                    this.tvRule.ShowMessage("请选择一个“分组”", "提示");
                }
            }
            return (it ==null || it.Tag ==null ) ? string.Empty : it.Tag as string;
        }

    }
}
