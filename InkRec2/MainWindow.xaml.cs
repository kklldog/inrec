using System;
using System.Windows;
using System.Net;
using System.Diagnostics;
using Contoso.NoteTaker.Services.Ink;
using System.Windows.Threading;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Windows.Graphics.Display;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using Windows.UI.Input.Inking;
using System.Collections.Generic;
using System.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InkRec2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DisplayInformation displayInfo;

        public MainWindow()
        {
            InitializeComponent();
        }
        private void inkCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
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
                    var result = await res.Content.ReadAsStringAsync();

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

        private InkData GetInkData()
        {
            var data = new InkData();
            data.language = "zh-CN";
            data.strokes = new List<InkStroke>();

            int id = 0;
            foreach (var stroke in this.inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                var points = stroke.GetInkPoints();

                var convertPoints = ConvertPixelsToMillimeters(points);

                var inkStorke = new InkStroke();
                inkStorke.id = id++;

                var sb = new StringBuilder();
                foreach (var point in convertPoints)
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

        private async void Button_InkRec(object sender, RoutedEventArgs e)
        {
            var inkData = GetInkData();
            var response = await InkRec(inkData);

            var jsonObj = JsonConvert.DeserializeObject<InkRecResponse>(response);

            var recognizedText = jsonObj.recognitionUnits.First(o => o.category == "line").recognizedText;

            this.output.Text = recognizedText;
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

    public class InkRecResponse
    {
        public List<InkRecResponseUnit> recognitionUnits { get; set; }
    }

    public class InkRecResponseUnit
    {
        public string category { get; set; }

        public string recognizedText { get; set; }
    }
}

