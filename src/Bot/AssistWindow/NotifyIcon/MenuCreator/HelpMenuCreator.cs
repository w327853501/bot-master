using Bot.Common;
using Bot.Common.Db;
using Bot.Version;
using BotLib;
using BotLib.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bot.AssistWindow.NotifyIcon.MenuCreator
{
    public class HelpMenuCreator
    {
		public static void Create(CtlNotifyIcon notifyIcon)
		{
			var helpRootMenu = notifyIcon.GetRootMenu(0);
			//helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("快速入门", null));
			//helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("详细帮助", null));
			//helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("联系客服", null));
			//helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("打开官网", null));
			//helpRootMenu.DropDownItems.Add(notifyIcon.CreateSeparator());
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("打开安装目录", OnOpenAppCatalogClicked));
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("打开data目录", OnOpenDataCatalogClicked));
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateSeparator());
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("重新同步全部数据", OnSyncDataFromServerClicked));
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateSeparator());
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("升级到最新版", OnUpdateClicked));
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("切换到已安装的其它版本", OnSelectOtherVersion));
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateSeparator());
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("打开日志", OnShowLogClicked));
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("清空日志", OnClearLogClicked));
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateSeparator());
			helpRootMenu.DropDownItems.Add(notifyIcon.CreateItem("关于", OnAboutClicked));
		}

		private static void OnSelectOtherVersion(object sender, EventArgs e)
		{
			WndVersionSelector.MyShow();
		}

		private static void OnClearLogClicked(object sender, EventArgs e)
		{
			Log.Clear();
		}

		private static void OnShowLogClicked(object sender, EventArgs e)
		{
			Log.Show();
		}

		private static void OnAboutClicked(object sender, EventArgs e)
		{
			var sb = new StringBuilder();
			sb.Append("软件版本：");
			sb.AppendLine(Params.VersionStr + "\r\nQQ：731227356");
			MsgBox.ShowTip(sb.ToString(), "关于");
		}

		private static void OnOpenDataCatalogClicked(object sender, EventArgs e)
		{
			try
			{
				Process.Start(PathEx.DataDir);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		private static void OnOpenAppCatalogClicked(object sender, EventArgs e)
		{
			try
			{
				Process.Start(PathEx.StartUpPathOfExe);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		private static void OnSyncDataFromServerClicked(object sender, EventArgs e)
		{
			DbSyner.SynData();
		}

		private static void OnUpdateClicked(object sender, EventArgs e)
		{
			try
			{
				ClientUpdater.ManualUpdateAsync();
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

	}
}
