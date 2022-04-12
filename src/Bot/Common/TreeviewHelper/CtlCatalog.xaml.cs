using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BotLib.Wpf.Controls;

namespace Bot.Common.TreeviewHelper
{
    public partial class CtlCatalog : UserControl
    {
        public CtlCatalog()
        {
            InitializeComponent();
        }

        public CtlCatalog(string text, string[] highlightKeys)
        {
            InitializeComponent();
			tbkHeader.Text= text;
			tbkHeader.HighlightKeys= highlightKeys;
		}

        public void Toggle(bool isExpand)
        {
            if (isExpand)
            {
                imgOpen.Visibility = Visibility.Visible;
                imgClose.Visibility = Visibility.Collapsed;
            }
            else
            {
                imgOpen.Visibility = Visibility.Collapsed;
                imgClose.Visibility = Visibility.Visible;
            }
        }

        public string Header
        {
            get
            {
                return tbkHeader.Text;
            }
            set
            {
                tbkHeader.Text = (value ?? "");
            }
        }
    }
}
