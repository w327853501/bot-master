using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BotLib.Extensions;
using Bot.AssistWindow.Widget.Right.ShortCut;
using Bot.AssistWindow.Widget.Bottom;
using System.Linq;
using DbEntity;
using Bot.AI.WordSplitterNs;

namespace Bot.Robot.Rule.QaCiteTable
{
	public class CiteTable
	{
        private ConcurrentDictionary<string, WordCiteData> _citeDict;
        private string _dbAccount;
        private InputSugestionHelper _inputSugestHelper;
        public string DbAccount { get { return _dbAccount; } }
        private RuleCiteHelper _ruleHelper;
        private bool _isInited;
        private bool _isIniting;
        private object _initSynObj;


        public ConcurrentDictionary<string, WordCiteData> CiteDict {
            get { return _citeDict; }
        }

        public CiteTable(string dbAccount)
		{
			_isInited = false;
			_isIniting = false;
			_initSynObj = new object();
			_dbAccount = dbAccount;
			InitData();
		}

		private void InitData()
		{
			_citeDict = new ConcurrentDictionary<string, WordCiteData>();
			_inputSugestHelper = new InputSugestionHelper(this);
            _ruleHelper = new RuleCiteHelper(this);
        }

        public void AddInputPromptWordCite(string txt)
		{
			_inputSugestHelper.AddInputPromptWordCite(txt);
		}

        public void ReadFromDb(bool force = false)
		{
			lock (_initSynObj)
			{
                if (force || (!_isInited && !_isIniting))
				{
					_isIniting = true;
                    Task.Factory.StartNew(Init, TaskCreationOptions.LongRunning);
				}
			}
		}


		private void Init()
		{
			InitData();
            InitShortcuts();
            InitRobotRules();
			_isIniting = false;
			_isInited = true;
		}

        private void InitRobotRules()
        {
            RobotRuleCatalogEntityEx.FixNodeOutInTheTree(DbAccount);
            var rules = RobotRuleEntityEx.GetRobotRules(DbAccount);
            foreach (var r in rules.xSafeForEach())
            {
                _ruleHelper.AddOrUpdateRule(r);
            }
        }

        public WordCiteData TryGetWordCiteData(string wdKey)
        {
            var wordCiteData = _citeDict.xTryGetValue(wdKey, null);
            if (wordCiteData == null)
            {
                wordCiteData = new WordCiteData();
                _citeDict.TryAdd(wdKey, wordCiteData);
            }
            return wordCiteData;
        }

        public List<SmartAnswerMatchInfo> GetAnswers(string question)
        {
            return _ruleHelper.GetAnswers(question);
        }

        public HashSet<RobotRuleEntity> GetRules(string question)
        {
            var smartAnswers = _ruleHelper.GetAnswers(question);
            if (smartAnswers == null) {
                smartAnswers = new List<SmartAnswerMatchInfo>();
            }
            var ruleDict = new Dictionary<string, RobotRuleEntity>();
            foreach (var ansMch in smartAnswers)
            {
                if (((ansMch != null) ? ansMch.Rule : null) != null)
                {
                    ruleDict[ansMch.Rule.EntityId] = ansMch.Rule;
                }
            }
            return HashSetEx.Create(ruleDict.Values);
        }

        private void InitShortcuts()
        {
            string mainNick = TbNickHelper.GetWwMainNickFromPubOrPrvDbAccount(_dbAccount);
            var ses = ShortcutHelper.GetShopShortcuts(mainNick);
            foreach (var et in ses)
            {
                AddOrUpdateInputPromptWordCite(et);
            }
        }

        public void AddOrUpdateInputPromptWordCite(ShortcutEntity shortcut)
        {
            _inputSugestHelper.AddOrUpdateInputPromptWordCite(shortcut);
        }

        public void RemoveRule(RobotRuleEntity ent)
        {
            if (ent != null)
            {
                _ruleHelper.RemoveRule(ent.EntityId);
            }
        }

        public List<CtlAnswer.Item4Show> GetInputSugestion(string input, Dictionary<long, double> contextNumiid = null, int maxCount = 5)
		{
           var prompts = _inputSugestHelper.GetInputSugestion(input, contextNumiid, maxCount);
            prompts = prompts ?? new List<InputPromptString>();
            return prompts.Select(k =>
            {
                CtlAnswer.Item4Show rt;
                if (k.Tag is ShortcutEntity)
                {
                    rt = new CtlAnswer.Item4Show(k.Tag as ShortcutEntity);
                }
                else
                {
                    string text = (k.Text != null) ? k.Text.Replace("{u:图片}", "") : null;
                    rt = new CtlAnswer.Item4Show(text, text);
                }
                return rt;
            }).ToList();
		}

        public void InitAsync(bool forceInit = false)
        {
            lock (_initSynObj)
            {
                if (forceInit || (!_isInited && !_isIniting))
                {
                    _isIniting = true;
                    Task.Factory.StartNew(Init, TaskCreationOptions.LongRunning);
                }
            }
        }

        public void AddOrUpdateRule(RobotRuleEntity ent)
        {
            InitAsync(true);
            _ruleHelper.AddOrUpdateRule(ent);
        }
	}
}
