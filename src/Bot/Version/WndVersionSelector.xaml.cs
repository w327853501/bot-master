using Bot.Common;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bot.Version
{
    public partial class WndVersionSelector : EtWindow
    {
		public WndVersionSelector(List<InstalledVersion> installedVersions)
		{
			InitializeComponent();
			foreach (var iv in installedVersions)
			{
				RadioButton radioButton = new RadioButton();
				string text = ShareUtil.ConvertVersionToString(iv.Version);
				if (iv.Version == Params.Version)
				{
					text += "(当前版本)";
				}
				radioButton.Content = text;
				radioButton.Tag = iv;
				lboxVersion.Items.Add(radioButton);
			}
		}

		public static void MyShow()
		{
			var vers = InstalledVersionManager.GetAllInstalledVersionAndSortByVersionDesc();
			if (vers.Count < 2)
			{
				MsgBox.ShowTip("没有可选择的版本（本电脑只安装过一个版本）", "提示");
			}
			else
			{
				ShowOneInstance(()=> { return new WndVersionSelector(vers); });
			}
		}

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			RadioButton radioButton = GetSelectedVersion();
			if (radioButton == null)
			{
				MsgBox.ShowTip("请选择一个版本", "提示");
			}
			else
			{
				InstalledVersion installedVersion = radioButton.Tag as InstalledVersion;
				if (installedVersion.Version != Params.Version)
				{
					InstalledVersionManager.SaveVersionToConfigFile(installedVersion.Version);
					MsgBox.ShowTip("版本已修改，需要【重启软件】才能【生效】，是否立即重启？", isok =>
					{
						if (isok)
						{
							ClientUpdater.Reboot();
						}
					}, "提示");
				}
				Close();
			}
		}

		private RadioButton GetSelectedVersion()
		{
			foreach (var lstObj in lboxVersion.Items)
			{
				var rdbtn = (RadioButton)lstObj;
				if (rdbtn.IsChecked ?? false)
				{
					return rdbtn;
				}
			}
			return null;
		}
	}
}
