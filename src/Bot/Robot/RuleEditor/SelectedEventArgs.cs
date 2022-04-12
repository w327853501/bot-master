using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Robot.RuleEditor
{
	public class SelectedEventArgs : EventArgs
	{
		public bool IsCanceled;
		public string SelectedCatalogId;
		public SelectedEventArgs()
		{
			this.IsCanceled = false;
		}
	}
}
