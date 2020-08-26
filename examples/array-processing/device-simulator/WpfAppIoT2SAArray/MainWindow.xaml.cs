using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfAppIoT2SAArray
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        List<string> msgTypes = new List<string>();
        Random rnd;
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbCS.Text = System.Configuration.ConfigurationManager.AppSettings["iothub-connection-string"];
            msgTypes.Add("type-a");
            msgTypes.Add("type-b");
            msgTypes.Add("type-c");
            lbMessageType.ItemsSource = msgTypes;
            rnd = new Random(DateTime.Now.Millisecond);
        }

        DeviceClient deviceClient = null;
        private async void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(tbCS.Text);
                await deviceClient.OpenAsync();
                buttonConnect.IsEnabled = false;
                buttonSend.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (lbMessageType.SelectedItem == null)
            {
                MessageBox.Show("Please select message type!");
                return;
            }
            string msgType = lbMessageType.SelectedItem.ToString();
            // type-a no:3, type-b no:3 but semantics diff, type-c no:5
            var dataItems = new List<int>();
            int itemNo = 0;
            switch (msgType)
            {
                case "type-a":
                case "type-b":
                    itemNo = 3;
                    break;
                case "type-c":
                    itemNo = 5;
                    break;
                default:
                    itemNo = 1;
                    break;
                    
            }
            for(var i = 0; i < itemNo; i++)
            {
                dataItems.Add(rnd.Next(10));
            }
            var msg = new
            {
                dataItems = dataItems.ToArray(),
                timestamp = DateTime.Now.ToString("yyyy/MM/ddTHH:mm:ss")
            };
            var msgJson = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
            var iotMsg = new Message(System.Text.Encoding.UTF8.GetBytes(msgJson));
            iotMsg.Properties.Add("msgtype", msgType);
            try
            {
                await deviceClient.SendEventAsync(iotMsg);
                var sb = new StringBuilder();
                var writer = new StringWriter(sb);
                writer.WriteLine("Send Message Type:" + msgType);
                writer.WriteLine("Message Body:");
                writer.WriteLine(msgJson);
                writer.WriteLine("Message Properties:");
                foreach(var k in iotMsg.Properties.Keys)
                {
                    writer.WriteLine(" " + k + ":" + iotMsg.Properties[k]);
                }
                tbDesc.Text = sb.ToString();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
