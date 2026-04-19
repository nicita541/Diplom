using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Diplom
{
    public class ProcessFrame
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        //TODO сделал локалхост пока тестирую докер

        //http://127.0.0.1:8000
        public ProcessFrame(string baseUrl = "http://localhost:8000")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<DetectedObjectInfo>> Process(BitmapImage imag, float confThreshold = 0.5f)
        {
            byte[] image =  ConvertNew.BitmapImageToBytes(imag);

            if (image == null || image.Length == 0)
                throw new ArgumentException("Изображение пустое", nameof(image));

            var request = new DetectRequest
            {
                image_base64 = Convert.ToBase64String(image),
                conf_threshold = confThreshold
            };

            string json = JsonSerializer.Serialize(request);

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await _httpClient.PostAsync(_baseUrl + "/detect", content))
            {
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Ошибка Python сервера: " + responseText);

                DetectResponse result = JsonSerializer.Deserialize<DetectResponse>(responseText, _jsonOptions);

                if (result == null)
                    throw new Exception("Пустой ответ от сервера");

                return result.objects ?? new List<DetectedObjectInfo>();
            }
        }
    }



    public class DetectRequest
    {
        public string image_base64 { get; set; }
        public float conf_threshold { get; set; }
    }

    public class DetectResponse
    {
        public List<DetectedObjectInfo> objects { get; set; }
        public int image_width { get; set; }
        public int image_height { get; set; }
        public string model_path { get; set; }
        public string device { get; set; }
    }

    public class DetectedObjectInfo
    {
        public int track_id { get; set; }
        public int class_id { get; set; }
        public string class_name { get; set; }
        public float confidence { get; set; }
        public int x1 { get; set; }
        public int y1 { get; set; }
        public int x2 { get; set; }
        public int y2 { get; set; }
    }
}
