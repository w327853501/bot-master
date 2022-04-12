using Bot.Common.Windows;
using BotLib;
using BotLib.Wpf;
using BotLib.Wpf.Extensions;
using DbEntity;
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

namespace Bot.ChatRecord
{
    public partial class WndSelectEmployee : EtWindow
    {
        private bool _isClosedByOkButton;
        private List<Employee> _employees;

        private WndSelectEmployee(string seller,List<Employee> employees)
        {
            _isClosedByOkButton = false;
            InitializeComponent();
            Seller = seller;
            _employees = employees;
            var nicks = employees.Select(k => k.nickName).ToList();
            nicks.Insert(0, TbNickHelper.GetMainPart(Seller));
            cboxInput.ItemsSource = nicks;
        }
        public string Result
        {
            get
            {
                return cboxInput.SelectedValue.ToString();
            }
        }

        public static void MyShow(string seller, List<Employee> employees, Action<string> callback, Window owner = null, bool startUpCenterOwner = false)
        {
            var editor = new WndSelectEmployee(seller, employees);
            editor.Owner = owner;
            editor.Closed += (sender, e) =>
            {
                if (editor._isClosedByOkButton)
                {
                    callback(editor.Result);
                }
            };
            WindowEx.xSetStartUpLocation(editor, startUpCenterOwner);
            WindowEx.xShowFirstTime(editor);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (WindowEx.xIsModal(this))
            {
                DialogResult = false;
            }
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (WindowEx.xIsModal(this))
            {
                DialogResult = true;
            }
            _isClosedByOkButton = true;
            Close();
            
        }

    }
}
