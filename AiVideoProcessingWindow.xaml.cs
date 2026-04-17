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
using System.Windows.Shapes;
using ClassLibrary2;


namespace Diplom
{

    // TODO:
    // 1. Реализовать кнопку выбора пути к видеофайлу.
    // 2. Реализовать кнопку "Извлечь кадры".
    // 3. Реализовать кнопку "Запустить полный анализ".
    // 4. Реализовать кнопку "Остановить процесс".
    // 5. Реализовать кнопку "Открыть журнал".
    // 6. Доделать отображение статистики:
    //    - сколько кадров обработано / всего кадров,
    //    - сколько знаков найдено,
    //    - сколько записей сохранено в БД.
    // 7. Реализовать вывод видео в блок "Главный просмотр".
    // 8. Реализовать показ текущего кадра и лучшего снимка найденного знака.
    // 9. Связать результаты анализа с карточками SignPlacement.
    // 10. Подключить автоматическую запись результатов в БД.


    /// <summary>
    /// Логика взаимодействия для AiVideoProcessingWindow.xaml
    /// </summary>
    /// 

    public partial class AiVideoProcessingWindow : Window
    {

        int? id_rout;
        int? id_direct;
        int? id_picket;

        public AiVideoProcessingWindow()
        {
            InitializeComponent();
            UpdateRout_cbx();
        }

        private void UpdateRout_cbx()
        {
            RouteComboBox.Items.Clear();
            try
            {
                using (var db = new dataBase())
                {
                    foreach(var item in db.Route)
                    {
                        RouteComboBox.Items.Add(item.code);
                    }
                }
            }
            catch
            {
                MessageBox.Show("не удалось загрузить маршрут");
            }
        }

        private void UpdateDirect_cbx()
        {
            DirectionComboBox.Items.Clear();
            try
            {
                using (var db = new dataBase())
                {
                    foreach (var item in db.Direction)
                    {
                        if(item.route_id == id_rout)
                            DirectionComboBox.Items.Add(item.direction_type);
                    }
                }
            }
            catch
            {
                MessageBox.Show("не удалось загрузить направление");
            }
        }

        private void UpdatePicket_cbx()
        {
            PicketComboBox.Items.Clear();
            try
            {
                using (var db = new dataBase())
                {
                    foreach (var item in db.Picket)
                    {
                        if (item.direction_id == id_direct)
                            PicketComboBox.Items.Add(item.picket_number.ToString());
                    }
                }
            }
            catch
            {
                MessageBox.Show("не удалось загрузить пикет");
            }
        }

        private void RouteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string temp = RouteComboBox.SelectedItem.ToString();
            using (var db = new dataBase()) {
                id_rout = db.Route.FirstOrDefault(x => x.code == temp).id;
            }
            UpdateDirect_cbx();
        }

        private void DirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string temp = DirectionComboBox.SelectedItem.ToString();
            using (var db = new dataBase())
            {
                id_direct = db.Direction.FirstOrDefault(x => x.direction_type == temp).id;
            }
            UpdatePicket_cbx();
        }

        private void PicketComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int temp = Convert.ToInt32(PicketComboBox.SelectedItem.ToString());
            using (var db = new dataBase())
            {
                id_picket = db.Picket.FirstOrDefault(x => x.picket_number == temp && x.direction_id == id_direct).id;
            }
            
        }

        private void Start_Analiz_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Path_tbx.Text == "")
            {
                MessageBox.Show("Путь к видео пуст");
                return;
            }

            if (id_direct == null || id_picket == null || id_rout == null)
            {
                MessageBox.Show("Не выбран путь сохранения");
                return;
            }
        }
    }
}
