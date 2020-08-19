using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;

namespace InkRec
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 开始识别笔迹
        /// </summary>
        /// <param name="imgPath"></param>
        /// <returns></returns>
        private async Task<string> InkRec(InkData data)
        {
           string inkRecognitionUrl = "/inkrecognizer/v1.0-preview/recognize";
            string endPoint = "https://inkrec-01.cognitiveservices.azure.com/";
            string subscriptionKey = "e51e7d9ff5ea4cf3bc5b24f2bc078c7e";

            using (HttpClient client = new HttpClient { BaseAddress = new Uri(endPoint) })
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var res = await client.PutAsync(inkRecognitionUrl, content);
                if (res.IsSuccessStatusCode)
                {
                    var result =  await res.Content.ReadAsStringAsync();

                    return result;
                }
                else
                {
                    var err = $"ErrorCode: {res.StatusCode}";

                    return err;
                }
            }
        }

        private List<System.Windows.Point> ConvertPixelsToMillimeters(IReadOnlyList<InkPoint> pointsInPixels)
        {
            float dpiX = 96.0f;
            float dpiY = 96.0f;
            var transformedInkPoints = new List<System.Windows.Point>();
            const float inchToMillimeterFactor = 25.4f;


            foreach (var point in pointsInPixels)
            {
                var transformedX = (point.Position.X / dpiX) * inchToMillimeterFactor;
                var transformedY = (point.Position.Y / dpiY) * inchToMillimeterFactor;

                transformedInkPoints.Add(new System.Windows.Point(transformedX, transformedY));
            }

            return transformedInkPoints;
        }


        private async void Button_InkRec(object sender, RoutedEventArgs e)
        {
            var inkData = GetInkData();
            var response = await InkRec(inkData);

            this.txt.Text = response;
        }

        private InkData GetInkData()
        {
            var data = new InkData();
            data.language = "zh-CN";
            data.strokes = new List<InkStroke>();

            int id = 0;
            foreach (var stroke in this.inkCanvas.InkPresenter.StrokeContainer.Strokes)
            {
                var points = stroke.get;

                var inkStorke = new InkStroke();
                inkStorke.id = id++;

                var sb = new StringBuilder();
                foreach (var point in points)
                {
                    sb.Append(point.X);
                    sb.Append(",");
                    sb.Append(point.Y);
                    sb.Append(",");
                }
                inkStorke.points = sb.ToString().TrimEnd(',');

                data.strokes.Add(inkStorke);
            }

            return data;
        }
    }

    public class InkStroke
    {
        public int id { get; set; }

        public string points { get; set; } 
    }

    public class InkData
    {
        public string language { get; set; }

        public List<InkStroke> strokes { get; set; }
    }
}
