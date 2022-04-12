using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;

namespace DbEntity
{
    public class ChatlogEntity : EntityBase
	{
		public string SellerNick { get; set; }

		public string FromNick { get; set; }

		public string ToNick { get; set; }

		public string Content { get; set; }

		public string ItemId { get; set; }

		public string ImageUrl { get; set; }

		public DateTime Time
		{
			get
			{
				return SendTime.xTimeStampToDate();
			}
		}


		public long SendTime { get; set; }
	}
}
