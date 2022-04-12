using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.ChromeNs
{
    public class RecieveNewMessageEventArgs : ChromeAdapterEventArgs
    {
        public string Buyer { get; set; }
        public string Message { get; set; }
    }

    public class ShopRobotReceriveNewMessageEventArgs : ChromeAdapterEventArgs
    {
        public string Buyer { get; set; }
    }
}

