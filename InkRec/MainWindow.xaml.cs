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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
            {
                return;
            }

            // Display the image file.
            string filePath = openDlg.FileName;

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            img.Source = bitmapSource;

           var response = await InkRec(filePath);

            this.txt.Text = response;
        }

        /// <summary>
        /// 开始识别笔迹
        /// </summary>
        /// <param name="imgPath"></param>
        /// <returns></returns>
        private async Task<string> InkRec(string imgPath)
        {
           string inkRecognitionUrl = "/inkrecognizer/v1.0-preview/recognize";
            // Replace the dataPath string with a path to the JSON formatted ink stroke data.
            // Optionally, use the example-ink-strokes.json file of this sample. Add to your bin\Debug\netcoreapp3.0 project folder.
            string dataPath = @"PATH_TO_INK_STROKE_DATA";
            string endPoint = "https://inkrec-01.cognitiveservices.azure.com/";
            string subscriptionKey = "e51e7d9ff5ea4cf3bc5b24f2bc078c7e";
            using (HttpClient client = new HttpClient { BaseAddress = new Uri(endPoint) })
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

                var imgData = ReaderImgJsonString(imgPath);
                var content = new StringContent(imgData, Encoding.UTF8, "application/json");
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

        /// <summary>
        /// 把图片数据转成json数据
        /// </summary>
        /// <param name="fileLocation"></param>
        /// <returns></returns>
        public string ReaderImgJsonString(string fileLocation)
        {
            var jsonObj = new JObject();

            using (StreamReader file = File.OpenText(fileLocation))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                jsonObj = (JObject)JToken.ReadFrom(reader);
            }
            return jsonObj.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
