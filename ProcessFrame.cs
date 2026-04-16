using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Diplom
{
    public class ProcessFrame
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        // Очередь исходных кадров
        private readonly Queue<byte[]> _frameQueue = new Queue<byte[]>();

        public ProcessFrame(string baseUrl = "http://127.0.0.1:8000")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<bool> StartModelAsync(string modelPath, CancellationToken token = default(CancellationToken))
        {
            var request = new StartModelRequest();
            request.model_path = modelPath;

            string json = JsonSerializer.Serialize(request);

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await _httpClient.PostAsync(_baseUrl + "/start_model", content, token))
            {
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Ошибка запуска модели: " + responseText);

                return true;
            }
        }

        public async Task<bool> StopModelAsync(CancellationToken token = default(CancellationToken))
        {
            using (var content = new StringContent("", Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await _httpClient.PostAsync(_baseUrl + "/stop_model", content, token))
            {
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Ошибка остановки модели: " + responseText);

                return true;
            }
        }

        public async Task<ProcessFrameResult> ProcessFrameAsync(byte[] frameBytes, float confThreshold = 0.25f, CancellationToken token = default(CancellationToken))
        {
            // Кладем исходный кадр в очередь перед отправкой
            lock (_frameQueue)
            {
                _frameQueue.Enqueue(frameBytes);
            }

            string frameBase64 = Convert.ToBase64String(frameBytes);

            var request = new ProcessFrameRequest();
            request.frame_base64 = frameBase64;
            request.conf_threshold = confThreshold;

            string json = JsonSerializer.Serialize(request);

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await _httpClient.PostAsync(_baseUrl + "/process_frame", content, token))
            {
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Ошибка обработки кадра: " + responseText);

                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;

                ProcessFrameResponse responseObject =
                    JsonSerializer.Deserialize<ProcessFrameResponse>(responseText, options);

                if (responseObject == null)
                    throw new Exception("Пустой ответ от Python сервера.");

                var result = new ProcessFrameResult();

                // Обработанный кадр
                result.FrameBytes = Convert.FromBase64String(responseObject.frame_base64);
                result.Objects = responseObject.objects ?? new List<DetectedObjectInfo>();

                // Достаем исходный кадр из очереди
                lock (_frameQueue)
                {
                    if (_frameQueue.Count > 0)
                        result.SourceFrameBytes = _frameQueue.Dequeue();
                }

                return result;
            }
        }

    }

    public class StartModelRequest
    {
        public string model_path { get; set; }
    }

    public class ProcessFrameRequest
    {
        public string frame_base64 { get; set; }
        public float conf_threshold { get; set; }
    }

    public class ProcessFrameResponse
    {
        public List<DetectedObjectInfo> objects { get; set; }
        public string frame_base64 { get; set; }
    }

    public class ProcessFrameResult
    {
        // Обработанный кадр
        public byte[] FrameBytes { get; set; }

        // Исходный кадр, который ушел на обработку
        public byte[] SourceFrameBytes { get; set; }

        public List<DetectedObjectInfo> Objects { get; set; }
    }

    public class DetectedObjectInfo
    {
        public int class_id { get; set; }
        public string class_name { get; set; }
        public float confidence { get; set; }
        public int x1 { get; set; }
        public int y1 { get; set; }
        public int x2 { get; set; }
        public int y2 { get; set; }
    }
}