using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.ChatRecord
{
    public class NewItemEventArgs : EventArgs
    {
        public string Buyer { get; set; }
        public HashSet<long> Numiids { get; set; }
        public NewItemEventArgs(string buyer,HashSet<long> numiids)
        {
            Buyer = buyer;
            Numiids = numiids;
        }
    }
}
