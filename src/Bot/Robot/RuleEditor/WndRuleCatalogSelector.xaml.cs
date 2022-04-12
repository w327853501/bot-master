using Bot.Common;
using Bot.Common.Account;
using Bot.Common.Windows;
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bot.Robot.RuleEditor
{
    public partial class WndRuleCatalogSelector : EtWindow
    {
        private string _oldcid;
        public string Result;
        public string DbAccountOfResult;
        public WndRuleCatalogSelector(string oldcid)
        {
            InitializeComponent();
            _oldcid = oldcid;
        }
		private void EtWindow_Loaded(object sender, RoutedEventArgs e)
		{
			string dbAccount = AccountHelper.GetPubDbAccount(Seller);
			RobotRuleCatalogEntity et = null;
			if (!string.IsNullOrEmpty(this._oldcid))
			{
				var ruleCataEt = DbHelper.FirstOrDefault<RobotRuleCatalogEntity>(this._oldcid);
				if (ruleCataEt != null && AccountHelper.IsPubAccountEqual(ruleCataEt.DbAccount, Seller))
				{
					et = ruleCataEt;
				}
			}
			this.ctlPub.Init(Seller, dbAccount, (et != null) ? et.EntityId : null);
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.Result = null;
			Close();
		}

		private void btnSelect_Click(object sender, RoutedEventArgs e)
		{
			this.Result = ctlPub.GetSelectedCatalogId(true);
			if (!string.IsNullOrEmpty(this.Result))
			{
				Close();
			}
		}
		public static void SelectedCatalog(string oldcid, string seller, Window owner, Action<string> callback)
		{
			var wnd = new WndRuleCatalogSelector(oldcid);
			wnd.FirstShow(seller, owner, ()=> {
				callback(wnd.Result);
			});
		}
	}
}
