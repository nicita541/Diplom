using Microsoft.Win32;
using OpenCvSharp;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Diplom
{
    public partial class MainWindow : System.Windows.Window
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly ProcessFrame _processFrame = new ProcessFrame("http://127.0.0.1:8000");

        private CancellationTokenSource _streamCts;
        private CancellationTokenSource _processingCts;

        private bool _isStreaming = false;
        private bool _isProcessing = false;
        private bool _hasShownActiveStatus = false;
        private int _frameCounter = 0;
        private int _mp4FrameCounter = 0;

        private DispatcherTimer _queueUiTimer;

        private readonly ConcurrentQueue<byte[]> _frameQueue = new ConcurrentQueue<byte[]>();

        private const int MaxQueueSize = 100;

        private enum VideoSourceType
        {
            None,
            Http,
            Mp4
        }

        private VideoSourceType _currentSourceType = VideoSourceType.None;
        private string _currentHttpUrl = string.Empty;
        private string _currentMp4Path = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            SetStatus("Не запущено");

            _queueUiTimer = new DispatcherTimer();
            _queueUiTimer.Interval = TimeSpan.FromMilliseconds(250);
            _queueUiTimer.Tick += QueueUiTimer_Tick;
            _queueUiTimer.Start();
        }

        private void QueueUiTimer_Tick(object sender, EventArgs e)
        {
            UpdateQueueIndicator();
        }

        // =========================
        // ФАЙЛ -> ОТКРЫТЬ HTTP
        // =========================
        private async void MenuItem_open_http_Click(object sender, RoutedEventArgs e)
        {
            string url = TextBoxStreamUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Введите URL потока.");
                return;
            }

            _currentSourceType = VideoSourceType.Http;
            _currentHttpUrl = url;

            await StartCurrentVideoSourceAsync();
        }

        // =========================
        // ФАЙЛ -> ОТКРЫТЬ MP4
        // =========================
        private async void MenuItem_open_mp4_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Выберите MP4 видео",
                Filter = "MP4 video (*.mp4)|*.mp4|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            _currentSourceType = VideoSourceType.Mp4;
            _currentMp4Path = dialog.FileName;

            await StartCurrentVideoSourceAsync();
        }


        // =========================
        // КНОПКИ СТАРТ/СТОП ВИДЕО
        // =========================
        private async void ButtonVideoStart_Click(object sender, RoutedEventArgs e)
        {
            if (_isStreaming)
            {
                MessageBox.Show("Видео уже запущено.");
                return;
            }

            if (_currentSourceType == VideoSourceType.None)
            {
                string url = TextBoxStreamUrl.Text.Trim();

                if (string.IsNullOrWhiteSpace(url))
                {
                    MessageBox.Show("Сначала выберите источник видео.");
                    return;
                }

                _currentSourceType = VideoSourceType.Http;
                _currentHttpUrl = url;
            }

            await StartCurrentVideoSourceAsync();
        }

        private void ButtonVideoStop_Click(object sender, RoutedEventArgs e)
        {
            StopVideoSource();
        }

        // =========================
        // МЕНЮ ОБРАБОТКА
        // =========================
        private void MenuProcessingStart_Click(object sender, RoutedEventArgs e)
        {
            StartProcessing();
        }

        private void MenuProcessingStop_Click(object sender, RoutedEventArgs e)
        {
            StopProcessing();
        }

        private async void StartProcessing()
        {
            if (_isProcessing)
            {
                MessageBox.Show("Обработка уже запущена.");
                return;
            }

            try
            {
                SetStatus("Запуск модели...");
                await _processFrame.StartModelAsync("yolo11n.pt");
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка запуска модели");
                MessageBox.Show("Не удалось запустить модель:\n" + ex.Message);
                return;
            }

            _processingCts = new CancellationTokenSource();
            _isProcessing = true;

            SetStatus(_isStreaming ? "Видео активно, обработка идет" : "Обработка запущена");

            Task.Run(() => ProcessFramesAsync(_processingCts.Token));
        }

        private async void StopProcessing()
        {
            if (!_isProcessing)
            {
                MessageBox.Show("Обработка уже остановлена.");
                return;
            }

            _processingCts.Cancel();
            _isProcessing = false;

            try
            {
                await _processFrame.StopModelAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка остановки модели:\n" + ex.Message);
            }

            if (_isStreaming)
                SetStatus("Видео активно");
            else
                SetStatus("Обработка остановлена");
        }

        // =========================
        // СТАРТ ИСТОЧНИКА
        // =========================
        private async Task StartCurrentVideoSourceAsync()
        {
            StopVideoSource();
            ClearQueue();

            _frameCounter = 0;
            _mp4FrameCounter = 0;
            _hasShownActiveStatus = false;

            _streamCts = new CancellationTokenSource();
            _isStreaming = true;

            try
            {
                if (_currentSourceType == VideoSourceType.Http)
                {
                    SetStatus("Подключение к HTTP потоку...");
                    await StartReconnectableMjpegStreamAsync(_currentHttpUrl, _streamCts.Token);
                }
                else if (_currentSourceType == VideoSourceType.Mp4)
                {
                    SetStatus("Загрузка MP4 в очередь...");
                    await ReadMp4FileAsync(_currentMp4Path, _streamCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Видео остановлено");
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка видео");
                MessageBox.Show("Ошибка открытия видео:\n" + ex.Message);
            }
            finally
            {
                _isStreaming = false;
            }
        }

        private void StopVideoSource()
        {
            if (_streamCts != null)
            {
                _streamCts.Cancel();
            }

            _isStreaming = false;

            if (_isProcessing)
                SetStatus("Обработка идет");
            else
                SetStatus("Видео остановлено");
        }

        // =========================
        // HTTP MJPEG
        // =========================
        private async Task StartReconnectableMjpegStreamAsync(string url, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    SetStatus(_isProcessing ? "Видео активно, обработка идет" : "Подключение к потоку...");
                    _hasShownActiveStatus = false;

                    await ReadSingleMjpegSessionAsync(url, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    SetStatus("Поток потерян. Ожидание...");
                }

                if (!token.IsCancellationRequested)
                {
                    await Task.Delay(1500, token);
                    SetStatus("Повторное подключение...");
                }
            }
        }

        private async Task ReadSingleMjpegSessionAsync(string url, CancellationToken token)
        {
            using (var response = await _httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                token))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var frameBuffer = new MemoryStream())
                {
                    byte[] buffer = new byte[4096];
                    bool capturing = false;
                    byte previousByte = 0;

                    while (!token.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                        if (bytesRead <= 0)
                            throw new IOException("Поток прерван.");

                        for (int i = 0; i < bytesRead; i++)
                        {
                            byte currentByte = buffer[i];

                            if (!capturing && previousByte == 0xFF && currentByte == 0xD8)
                            {
                                frameBuffer.SetLength(0);
                                frameBuffer.WriteByte(0xFF);
                                frameBuffer.WriteByte(0xD8);
                                capturing = true;
                            }
                            else if (capturing)
                            {
                                frameBuffer.WriteByte(currentByte);

                                if (previousByte == 0xFF && currentByte == 0xD9)
                                {
                                    _frameCounter++;

                                    // берем через кадр
                                    if (_frameCounter % 2 == 0)
                                    {
                                        byte[] jpegBytes = frameBuffer.ToArray();

                                        ShowImage(OriginalImage, jpegBytes);
                                        EnqueueFrame(jpegBytes);

                                        if (!_hasShownActiveStatus)
                                        {
                                            SetStatus(_isProcessing ? "Видео активно, обработка идет" : "Видео активно");
                                            _hasShownActiveStatus = true;
                                        }
                                    }

                                    capturing = false;
                                }
                            }

                            previousByte = currentByte;
                        }
                    }
                }
            }
        }

        // =========================
        // MP4 -> через кадр + ожидание места в очереди
        // =========================
        private async Task ReadMp4FileAsync(string filePath, CancellationToken token)
        {
            await Task.Run(async () =>
            {
                using (var capture = new VideoCapture(filePath))
                using (var frame = new Mat())
                {
                    if (!capture.IsOpened())
                        throw new Exception("Не удалось открыть MP4 файл.");

                    bool firstShown = false;

                    while (!token.IsCancellationRequested)
                    {
                        bool ok = capture.Read(frame);

                        if (!ok || frame.Empty())
                            break;

                        _mp4FrameCounter++;

                        // Берем только каждый второй кадр
                        if (_mp4FrameCounter % 2 != 0)
                            continue;

                        Cv2.ImEncode(".jpg", frame, out byte[] jpegBytes);

                        ShowImage(OriginalImage, jpegBytes);

                        // Ждем, пока в очереди освободится место
                        while (!token.IsCancellationRequested && _frameQueue.Count >= MaxQueueSize)
                        {
                            await Task.Delay(20, token);
                        }

                        if (token.IsCancellationRequested)
                            break;

                        _frameQueue.Enqueue(jpegBytes);

                        if (!firstShown)
                        {
                            firstShown = true;
                            SetStatus(_isProcessing
                                ? "MP4 загружается, обработка идет"
                                : "MP4 загружается");
                        }
                    }
                }
            }, token);

            if (!token.IsCancellationRequested)
            {
                SetStatus(_isProcessing
                    ? "MP4 загружено, обработка идет"
                    : "MP4 загружено в очередь");
            }
        }

        // =========================
        // ОЧЕРЕДЬ
        // =========================
        private void EnqueueFrame(byte[] jpegBytes)
        {
            if (_frameQueue.Count >= MaxQueueSize)
            {
                _frameQueue.TryDequeue(out _);
            }

            _frameQueue.Enqueue(jpegBytes);
        }

        private void ClearQueue()
        {
            while (_frameQueue.TryDequeue(out _))
            {
            }
        }

        // =========================
        // ОБРАБОТКА
        // =========================
        private async Task ProcessFramesAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_frameQueue.TryDequeue(out byte[] frameBytes))
                    {
                        try
                        {
                            ProcessFrameResult result = await _processFrame.ProcessFrameAsync(frameBytes, 0.25f, token);

                            if (result.Objects != null && result.Objects.Count > 0)
                            {
                                SaveSignPlasment(result.SourceFrameBytes);
                            }

                            byte[] processedBytes = result.FrameBytes;
                            ShowImage(ProcessedImage, processedBytes);

                            // Если потом захочешь использовать координаты объектов:
                            // var objects = result.Objects;
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                SetStatus("Ошибка обработки кадра");
                            });

                            await Task.Delay(100, token);
                        }
                    }
                    else
                    {
                        await Task.Delay(20, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isProcessing = false;

                if (_isStreaming)
                    SetStatus("Видео активно");
                else
                    SetStatus("Обработка остановлена");
            }
        }

        private void SaveSignPlasment(byte[] photo)
        {
            BdClass bd = new BdClass();

            bd.AddSignPlacementToCurrentPath(3, 200, "хорошая", 100, "хорошый", 80, "", photo);
        }

        // =========================
        // UI
        // =========================
        private void SetStatus(string text)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TextBlockStatus.Text = text;

                if (text.Contains("обработка идет") || text.Contains("Видео активно") || text.Contains("загружено"))
                    TextBlockStatus.Foreground = Brushes.Green;
                else if (text.Contains("Подключение") || text.Contains("Повторное") || text.Contains("загрузка"))
                    TextBlockStatus.Foreground = Brushes.DarkOrange;
                else if (text.Contains("остановлена") || text.Contains("остановлено") || text == "Не запущено")
                    TextBlockStatus.Foreground = Brushes.Gray;
                else
                    TextBlockStatus.Foreground = Brushes.Red;
            }));
        }

        private void ShowImage(Image imageControl, byte[] jpegBytes)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    using (var ms = new MemoryStream(jpegBytes))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        imageControl.Source = bitmap;
                    }
                }
                catch
                {
                }
            });
        }

        private void UpdateQueueIndicator()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                int count = _frameQueue.Count;

                QueueProgressBar.Maximum = MaxQueueSize;
                QueueProgressBar.Value = Math.Min(count, MaxQueueSize);
                TextBlockQueueStatus.Text = count + " / " + MaxQueueSize;

                if (count < MaxQueueSize * 0.5)
                    QueueProgressBar.Foreground = Brushes.Green;
                else if (count < MaxQueueSize * 0.8)
                    QueueProgressBar.Foreground = Brushes.DarkOrange;
                else
                    QueueProgressBar.Foreground = Brushes.Red;
            }));
        }

        private void MenuItem_exit_Click(object sender, RoutedEventArgs e)
        {
            _processingCts?.Cancel();
            _streamCts?.Cancel();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _processingCts?.Cancel();
            _streamCts?.Cancel();
            _httpClient.Dispose();
            base.OnClosed(e);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            BdReg window = new BdReg();
            window.Owner = this;
            window.ShowDialog();
        }
    }
}