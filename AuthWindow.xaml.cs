using System.Windows;
using ClassLibrary2;

namespace Diplom
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            AuthView.LoginSucceeded += AuthView_LoginSucceeded;

        }

        private void AuthView_LoginSucceeded(ClassLibrary2.UserLog userLog)
        {
            var dashboard = new MainWindow();
            dashboard.Show();
            Close();
        }
    }
}
