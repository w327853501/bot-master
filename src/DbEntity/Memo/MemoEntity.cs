using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbEntity
{
	public class MemoEntity
	{
		public string SellerMain { get; set; }
		public string Memo { get; set; }
		public int Flag { get; set; }
		public string TidStr { get; set; }
		public MemoEntity(string sellerMain, string memo, int flag, string tidStr)
		{
			SellerMain = sellerMain;
			Memo = memo;
			Flag = flag;
			TidStr = tidStr;
		}
	}
}
