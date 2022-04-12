using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Bot.Common
{
    public static class Commands
    {
        public static RoutedUICommand New
        {
            get
            {
                return V.NewCommand;
            }
        }

        public static RoutedUICommand Edit
        {
            get
            {
                return V.EditCommand;
            }
        }

        public static RoutedUICommand Clear
        {
            get
            {
                return V.ClearCommand;
            }
        }

        public static RoutedUICommand Delete
        {
            get
            {
                return V.DeleteCommand;
            }
        }

        public static RoutedUICommand Submit
        {
            get
            {
                return V.SubmitCommand;
            }
        }

        public static RoutedUICommand Cancel
        {
            get
            {
                return V.CancelCommand;
            }
        }

        public static RoutedUICommand Toggle
        {
            get
            {
                return V.ToggleCommand;
            }
        }

        private class V
        {

            static V()
            {
                NewCommand = new RoutedUICommand("新建", "New", typeof(Commands), new InputGestureCollection(new KeyGesture[]
				{
					new KeyGesture(Key.N, ModifierKeys.Alt)
				}));
                EditCommand = new RoutedUICommand("编辑", "Edit", typeof(Commands), new InputGestureCollection(new KeyGesture[]
				{
					new KeyGesture(Key.E, ModifierKeys.Alt)
				}));
                DeleteCommand = new RoutedUICommand("删除", "Delete", typeof(Commands), new InputGestureCollection(new KeyGesture[]
				{
					new KeyGesture(Key.Delete)
				}));
                ClearCommand = new RoutedUICommand("清空", "Clear", typeof(Commands), new InputGestureCollection(new KeyGesture[]
				{
					new KeyGesture(Key.K, ModifierKeys.Alt)
				}));
                SubmitCommand = new RoutedUICommand("确定", "Submit", typeof(Commands), new InputGestureCollection(new KeyGesture[]
				{
					new KeyGesture(Key.Y, ModifierKeys.Alt)
				}));
                CancelCommand = new RoutedUICommand("取消", "Cancel", typeof(Commands), new InputGestureCollection(new KeyGesture[]
				{
					new KeyGesture(Key.Escape)
				}));
                ToggleCommand = new RoutedUICommand("切换", "Toggle", typeof(Commands), new InputGestureCollection(new KeyGesture[]
				{
					new KeyGesture(Key.T, ModifierKeys.Alt)
				}));
            }

            public static readonly RoutedUICommand NewCommand;

            public static readonly RoutedUICommand EditCommand;

            public static readonly RoutedUICommand DeleteCommand;

            public static readonly RoutedUICommand ClearCommand;

            public static readonly RoutedUICommand SubmitCommand;

            public static readonly RoutedUICommand CancelCommand;

            public static readonly RoutedUICommand ToggleCommand;
        }
    }
}
