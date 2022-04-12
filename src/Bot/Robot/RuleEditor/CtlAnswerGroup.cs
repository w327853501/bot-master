using DbEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BotLib.Extensions;
using Bot.Common;

namespace Bot.Robot.RuleEditor
{
    public class CtlAnswerGroup : ScrollViewer
    {
        private const string Title = "答案";
        private StackPanel _spanel;
        private double _rowHeight;

        private int _visibleRows;

        public CtlAnswerGroup()
		{
			_spanel = new StackPanel();
			_visibleRows = -1;
			if (!DesignerProperties.GetIsInDesignMode(this))
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				AddChild(_spanel);
				InitUI();
				Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				_rowHeight = DesiredSize.Height;
				VisibleRows = 10;
			}
		}

        public int VisibleRows
        {
            get
            {
                return _visibleRows;
            }
            set
            {
                if (value != _visibleRows)
                {
                    _visibleRows = value;
                    MaxHeight = _rowHeight * (double)value;
                }
            }
        }

        public void Init(List<AnswerWithImage> answers)
        {
            _spanel.Children.Clear();
            if (answers.xIsNullOrEmpty())
            {
                InitUI(null, null);
            }
            else
            {
                foreach (var answer in answers)
                {
                    InitUI(answer);
                }
            }
        }

        public List<string> GetAddNewImages()
        {
            var lst = new List<string>();
            for (int i = 0; i < _spanel.Children.Count; i++)
            {
                var ctlOneAnswer = _spanel.Children[i] as CtlOneAnswer;
                if (ctlOneAnswer != null && !ctlOneAnswer.AddNewImageNames.xIsNullOrEmpty())
                {
                    lst.AddRange(ctlOneAnswer.AddNewImageNames);
                }
            }
            return lst;
        }

        public List<string> GetDeleteImages()
        {
            var lst = new List<string>();
            for (int i = 0; i < _spanel.Children.Count; i++)
            {
                var ctlOneAnswer = _spanel.Children[i] as CtlOneAnswer;
                if (ctlOneAnswer != null && !ctlOneAnswer.DeleteImageNames.xIsNullOrEmpty())
                {
                    lst.AddRange(ctlOneAnswer.DeleteImageNames);
                }
            }
            return lst;
        }

        public List<AnswerWithImage> Answers
        {
            get
            {
                List<AnswerWithImage> list = new List<AnswerWithImage>();
                for (int i = 0; i < _spanel.Children.Count; i++)
                {
                    CtlOneAnswer ctlOneAnswer = _spanel.Children[i] as CtlOneAnswer;
                    if (ctlOneAnswer != null)
                    {
                        list.Add(ctlOneAnswer.Answer);
                    }
                }
                return list;
            }
            set
            {
                _spanel.Children.Clear();
                foreach (AnswerWithImage answerWithImage_ in value.xSafeForEach())
                {
                    InitUI(answerWithImage_, null);
                }
            }
        }

        public bool IsEmptyOrHasSyntaxError()
        {
            foreach (CtlOneAnswer ctlOneAnswer in _spanel.Children)
            {
                if (ctlOneAnswer.IsEmptyOrHasSyntaxError())
                {
                    MsgBox.ShowTip(string.Format("{0} 的 文字 和 图片 不能同时为空！", ctlOneAnswer.tbkTitle.Text), string.Empty);
                    return true;
                }
            }
            return false;
        }

        public void FocusOneAnswer()
        {
            if (_spanel.Children.Count > 0)
            {
                var ctlOneAnswer = _spanel.Children[0] as CtlOneAnswer;
                ctlOneAnswer.FocusContent();
            }
        }

        public event RoutedEventHandler EvRemove
        {
            add
            {
                AddHandler(CtlOneAnswer.EvRemoveEvent, value);
            }
            remove
            {
                RemoveHandler(CtlOneAnswer.EvRemoveEvent, value);
            }
        }

        public event RoutedEventHandler EvAdd
        {
            add
            {
                AddHandler(CtlOneAnswer.EvAddEvent, value);
            }
            remove
            {
                RemoveHandler(CtlOneAnswer.EvAddEvent, value);
            }
        }

        private CtlOneAnswer InitUI(AnswerWithImage answer = null, object prevCtl = null)
        {
            var ctlOneAnswer = new CtlOneAnswer(answer);
            ctlOneAnswer.EvAdd += ctlOneAnswer_EvAdd;
            ctlOneAnswer.EvRemove += ctlOneAnswer_EvRemove;
            ctlOneAnswer.Margin = new Thickness(5.0);
            if (prevCtl == null)
            {
                _spanel.Children.Add(ctlOneAnswer);
            }
            else
            {
                int index = _spanel.Children.IndexOf(prevCtl as UIElement) + 1;
                _spanel.Children.Insert(index, ctlOneAnswer);
            }
            RefreshUI();
            return ctlOneAnswer;
        }

        private void RefreshUI()
        {
            for (int i = 0; i < _spanel.Children.Count; i++)
            {
                var ctlOneAnswer = _spanel.Children[i] as CtlOneAnswer;
                if (ctlOneAnswer != null)
                {
                    ctlOneAnswer.SetTitle("答案", i + 1);
                }
            }
        }

        private void ctlOneAnswer_EvRemove(object sender, RoutedEventArgs e)
        {
            RemoveOneAnswer(sender);
        }

        private void RemoveOneAnswer(object ctlOneAnswer)
        {
            if (ctlOneAnswer != null)
            {
                _spanel.Children.Remove(ctlOneAnswer as UIElement);
                if (_spanel.Children.Count == 0)
                {
                    InitUI();
                }
                RefreshUI();
            }
        }

        private void ctlOneAnswer_EvAdd(object sender, RoutedEventArgs e)
        {
            InitUI(null, sender);
        }

    }
}
