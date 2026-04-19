using ClassLibrary2;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;



namespace Diplom
{
    // TODO: Доработать трекер, снизить влияние неточной детекции на стабильность сопровождения объектов.
    // TODO: Дообучить модель детекции и проверить улучшение качества распознавания.
    // TODO: Добавить минимальную оценку знаков и сохранить её в результатах.
    // TODO: Реализовать кнопку "Извлечь кадры".
    // TODO: Реализовать кнопку "Открыть журнал".
    // TODO: Доделать отображение статистики: количество найденных знаков.
    // TODO: Доделать отображение статистики: количество записей, сохранённых в БД.
    // TODO: Связать результаты анализа с карточками SignPlacement.
    // TODO: Проверить полный сценарий работы: анализ, сохранение, статистика, журнал.


    /// <summary>
    /// Логика взаимодействия для AiVideoProcessingWindow.xaml
    /// </summary>
    /// 

    public partial class AiVideoProcessingWindow : System.Windows.Window
    {
        ProcessFrame processFrame;
        Tracker tracker;
        SaveBdImage saveBdImage;

        List<DetectedObjectInfo> boxes = new List<DetectedObjectInfo>();

        int? id_rout;
        int? id_direct;
        int? id_picket;

        int fullVideoCadr = 0;

        bool StartStop = false;
        private Task _extractTask;
        private CancellationTokenSource _extractCts;

        private DispatcherTimer _timer;
        public AiVideoProcessingWindow()
        {
            InitializeComponent();
            UpdateRout_cbx();

            processFrame = new ProcessFrame(); 
            saveBdImage = new SaveBdImage(SingnSave_StPan);
            tracker = new Tracker(saveBdImage);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateCadt_tbx();
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

        private async void Start_Analiz_Button_Click(object sender, RoutedEventArgs e)
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
            if (StartStop)
            {
                MessageBox.Show("Процесс уже запущен");
                return;
            }

            try
            {
                StartStop = true;
                _extractCts = new CancellationTokenSource();

                _extractTask = ExtractFramesAsync(Path_tbx.Text, _extractCts.Token);
                await _extractTask;
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                StartStop = false;
                _extractTask = null;
                _extractCts?.Dispose();
                _extractCts = null;
            }
        }

        private void UpdateCadt_tbx()
        {
            Cadr_tbx.Text = $"{fullVideoCadr}";
        }

        private void Parh_New_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Выберите видеофайл",
                Filter = "Видео файлы (*.mp4;*.avi;*.mov)|*.mp4;*.avi;*.mov|Все файлы (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                // сюда подставь имя своего TextBox
                Path_tbx.Text = dialog.FileName;
            }
        }

        private async Task ExtractFramesAsync(string videoPath, CancellationToken token)
        {
            await Task.Run(async () =>
            {
                using (var capture = new VideoCapture(videoPath))
                {
                    if (!capture.IsOpened())
                        throw new Exception("Не удалось открыть видео.");

                    capture.Set(VideoCaptureProperties.PosFrames, fullVideoCadr);


                    double fps = capture.Fps;
                    if (fps <= 0)
                        fps = 25;

                    int delay = (int)(1000.0 / fps);


                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        using (Mat frame = new Mat())
                        {
                            bool success = capture.Read(frame);

                            if (!success || frame.Empty())
                                break;

                            fullVideoCadr++;

                            BitmapImage image = MatToBitmapImage(frame);

                            if (fullVideoCadr % 3 == 0)
                            {
                                boxes = await processFrame.Process(image);
                                tracker.add(boxes, image);
                            }

                            await Dispatcher.InvokeAsync(() =>
                            {
                                OutImage(image, boxes);
                            });
                        }

                        await Task.Delay(delay, token);
                    } 
                }
            }, token);
        }


        private void OutImage(BitmapImage image, List<DetectedObjectInfo> boxes)
        {
            if (image == null)
                return;

            var visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawImage(image, new System.Windows.Rect(0, 0, image.PixelWidth, image.PixelHeight));

                if (boxes != null)
                {
                    foreach (var box in boxes)
                    {
                        var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Lime, 2);

                        dc.DrawRectangle(
                            null,
                            pen,
                            new System.Windows.Rect(
                                box.x1,
                                box.y1,
                                Math.Max(1, box.x2 - box.x1),
                                Math.Max(1, box.y2 - box.y1))
                        );

                        string label = $"{box.class_name} {box.track_id} {box.confidence * 100:F1}%";

                        var text = new FormattedText(
                            label,
                            System.Globalization.CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Arial"),
                            18,
                            System.Windows.Media.Brushes.Lime,
                            1.0
                        );

                        double textX = box.x1;
                        double textY = Math.Max(0, box.y1 - 22);

                        dc.DrawRectangle(
                            System.Windows.Media.Brushes.Black,
                            null,
                            new System.Windows.Rect(textX, textY, text.Width + 6, text.Height + 4)
                        );

                        dc.DrawText(text, new System.Windows.Point(textX + 3, textY + 2));
                    }
                }
            }

            var renderedBitmap = new RenderTargetBitmap(
                image.PixelWidth,
                image.PixelHeight,
                image.DpiX > 0 ? image.DpiX : 96,
                image.DpiY > 0 ? image.DpiY : 96,
                PixelFormats.Pbgra32
            );

            renderedBitmap.Render(visual);
            VideoFrameImage.Source = renderedBitmap;
        }





        private BitmapImage MatToBitmapImage(Mat mat)
        {
            Cv2.ImEncode(".bmp", mat, out byte[] buffer);

            using (var ms = new MemoryStream(buffer))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!StartStop || _extractCts == null)
            {
                return;
            }

            _extractCts.Cancel();
        }
    }



}
