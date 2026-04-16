using System;
using System.Windows;

namespace Diplom
{
    public partial class CreatePathWindow : Window
    {
        public CreatePathWindow()
        {
            InitializeComponent();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TextBoxRoute.Text))
                {
                    MessageBox.Show("Введите Route");
                    return;
                }

                if (string.IsNullOrWhiteSpace(TextBoxDirection.Text))
                {
                    MessageBox.Show("Введите Direction");
                    return;
                }

                if (string.IsNullOrWhiteSpace(TextBoxPicket.Text))
                {
                    MessageBox.Show("Введите Picket");
                    return;
                }

                int picketNumber;
                if (!int.TryParse(TextBoxPicket.Text, out picketNumber))
                {
                    MessageBox.Show("Picket должен быть числом");
                    return;
                }

                BdClass bd = new BdClass();
                int picketId = bd.CreatePath(TextBoxRoute.Text, TextBoxDirection.Text, picketNumber, null);

                BdReg.CurrentRoute = TextBoxRoute.Text;
                BdReg.CurrentDirection = TextBoxDirection.Text;
                BdReg.CurrentPicket = TextBoxPicket.Text;


                BdReg.CurrentRouteId = bd.GetRouteIdByCurrentValues();
                BdReg.CurrentDirectionId = bd.GetDirectionIdByCurrentValues();
                BdReg.CurrentPicketId = bd.GetPicketIdByCurrentValues();

                MessageBox.Show("Данные успешно добавлены в БД");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}