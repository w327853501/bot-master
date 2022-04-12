using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotLib;
using BotLib.Collection;
using BotLib.Extensions;
using Newtonsoft.Json;

namespace Bot.ChatRecord
{
	public class ChatDialog
	{
		private string _seller;
		private ConcurrentDictionary<string, List<ChatMessage>> _cache;
		private ConcurrentDictionary<string, HashSet<long>> _preNumiids;
		private ConcurrentDictionary<string, HashSet<long>> _numiids;
		private ConcurrentDictionary<string, long> _preReplyMessage;
		private ConcurrentDictionary<string, long> _newestBuyerMessage;
		public event EventHandler<NewItemEventArgs> EvAddNewItem;

		public ChatDialog(string seller)
		{
			_seller = seller;
			_cache = new ConcurrentDictionary<string, List<ChatMessage>>();
			_preNumiids = new ConcurrentDictionary<string, HashSet<long>>();
			_numiids = new ConcurrentDictionary<string, HashSet<long>>();
			_preReplyMessage = new ConcurrentDictionary<string, long>();
			_newestBuyerMessage = new ConcurrentDictionary<string, long>();
		}

		public void AddMessages(string messageResponse)
		{
			var chatRes = JsonConvert.DeserializeObject<ChatResponse>(messageResponse);
			var messages = chatRes.result;
			if (messages == null || messages.Count < 1) return;
			var buyer = GetBuyerFromMessage(messages);
			AddItemFromMessage(buyer, messageResponse);
			AddMessages(buyer, messages);
		}

        private string GetBuyerFromMessage(List<ChatMessage> messages)
        {
			Util.Assert(messages.Count > 0,"没有消息内容");
			var message = messages[0];
			return message.fromid.nick == _seller  ?message.toid.nick : message.fromid.nick ;
		}

        private void AddItemFromMessage(string buyer, string messageRes)
        {
			var iidset = GetItemIds(messageRes.ToLower());
			if (_numiids.ContainsKey(buyer))
			{
				iidset.xAddRange(_numiids.xTryGetValue(buyer));
			}
			_numiids.AddOrUpdate(buyer, iidset, (k, v) => iidset);

			if (HasNewItem(buyer))
			{
				_preNumiids[buyer] = _numiids[buyer];
				if (EvAddNewItem != null) EvAddNewItem(this,new NewItemEventArgs(buyer,HashSetEx.Create(_numiids[buyer])));
			}
		}

		private HashSet<long> GetItemIds(string msg)
		{
			var iidset = new HashSet<long>();
			var matchs = Regex.Matches(msg, "(?<=://detail.tmall.com/item.htm\\?.*?id=)\\d+");
			if (matchs != null)
			{
				foreach (Match mch in matchs)
				{
					var mchtxt = mch.ToString();
					if (!string.IsNullOrEmpty(mchtxt))
					{
						iidset.Add(Convert.ToInt64(mchtxt));
					}
				}
			}
			matchs = Regex.Matches(msg, "(?<=://item.taobao.com/item.htm\\?.*?id=)\\d+");
			if (matchs != null)
			{
				foreach (Match mch in matchs)
				{
					var mchtxt = mch.ToString();
					if (!string.IsNullOrEmpty(mchtxt))
					{
						iidset.Add(Convert.ToInt64(mchtxt));
					}
				}
			}
			return iidset;
		}

		public bool HasNewItem(string buyer)
		{
			return  (!_preNumiids.ContainsKey(buyer) && _numiids.ContainsKey(buyer)) 
				||_preNumiids[buyer].Count != _numiids[buyer].Count;
		}

		public void AddMessages(string buyer,List<ChatMessage> messages)
		{
			var message = messages[0];
			if (message.fromid.nick == _seller)
			{
				_preReplyMessage[buyer] = long.Parse(message.mcode.clientId);
			}
			if (_cache.ContainsKey(buyer))
			{
				messages.AddRange(_cache.xTryGetValue(buyer));
			}
			_cache.AddOrUpdate(buyer,messages, (k, v) => messages);
		}

		public List<ChatMessage> GetUnReplyMessages(string buyerName)
		{
			//只能调用一次，不能多次调用
			var messages = new List<ChatMessage>();
			if (_cache.TryGetValue(buyerName, out messages)) {
				var preReplyId = 0L;
				if (_preReplyMessage.ContainsKey(buyerName))
				{
					preReplyId = _preReplyMessage[buyerName];
				}
				messages = messages.Where(k=> long.Parse(k.mcode.clientId) > preReplyId).OrderByDescending(k=>k.mcode.clientId).ToList();
				//记录最近的一条消息
				if (messages.Count > 0)
				{
					_preReplyMessage[buyerName] = long.Parse(messages.First().mcode.clientId);
				}
			}
			return messages;
		}

		public List<ChatMessage> GetNewestMessages(string buyerName)
		{
			var messages = new List<ChatMessage>();
			if (_cache.TryGetValue(buyerName, out messages))
			{
				var newestBuyerMessageId = 0L;
				if (_newestBuyerMessage.ContainsKey(buyerName))
				{
					newestBuyerMessageId = _newestBuyerMessage[buyerName];
				}
				messages = messages.Where(k => k.loginid.nick == k.toid.nick && long.Parse(k.mcode.clientId) > newestBuyerMessageId).OrderByDescending(k => k.mcode.clientId).ToList();
				if (messages.Count > 0)
				{
					_newestBuyerMessage[buyerName] = long.Parse(messages.First().mcode.clientId);
				}
			}
			return messages;
		}

		public List<ChatMessage> GetBuyerMessages(string buyerName)
		{
			var messages = new List<ChatMessage>();
			if (_cache.TryGetValue(buyerName, out messages))
			{
				messages = messages.Where(k => k.loginid.nick == k.toid.nick).ToList();
			}
			return messages;
		}

		public HashSet<long> GetBuyerNumiids(string buyerName)
		{
			var ids = new HashSet<long>();
			_numiids.TryGetValue(buyerName, out ids);
			return ids;
		}
	}
}
