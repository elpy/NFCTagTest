using NdefLibrary.Ndef;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Networking.Proximity;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Windows.UI.Popups;

namespace NFCTagTest
{
    public sealed partial class MainPage : Page
    {
        private ProximityDevice device;
        private long subscribingId = -1L;


        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            device = ProximityDevice.GetDefault();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

        }

        private void PublishMessageButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            device.DeviceArrived += Device_DeviceArrived;
        }

        private void Device_DeviceArrived(ProximityDevice sender)
        {
            var data = new { Employee = new { Id = 2572923226000L, Name = "TestUser" } };
            var payload = JsonConvert.SerializeObject(data);

            var spRecord = new NdefSpRecord
            {
                NfcAction = NdefSpActRecord.NfcActionType.OpenForEditing,
                Payload = Encoding.UTF8.GetBytes(payload),
                NfcSize = (uint)payload.Length,
                Type = Encoding.UTF8.GetBytes("application/json; charset=\"utf-8\"")
            };

            var msg = new NdefMessage { spRecord };

            long publishingId = device.PublishBinaryMessage("NDEF:WriteTag", msg.ToByteArray().AsBuffer(), (d0, id) => {
                device.DeviceArrived -= Device_DeviceArrived;
                Debug.WriteLine(" published. id = " + id);
                var notify = new MessageDialog("The message has been published.");
            });
        }

        private void SubscribeForMessageButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            subscribingId = device.SubscribeForMessage("NDEF", receivedHandler);
        }

        private void receivedHandler(ProximityDevice sender, ProximityMessage message)
        {
            var data = message.Data.ToArray();
            var ndefMessage = NdefMessage.FromByteArray(data);

            foreach (NdefRecord record in ndefMessage)
            {
                string type = Encoding.UTF8.GetString(record.Type, 0, record.Type.Length);
                Debug.WriteLine("Record type: " + Encoding.UTF8.GetString(record.Type, 0, record.Type.Length));

                if ("application/json; charset=\"utf-8\"".Equals(type))
                {
                    string json = Encoding.UTF8.GetString(record.Payload, 0, record.Payload.Length);
                    var prototype = new { Employee = new { Id = -1L, Name = string.Empty } };
                    var t = JsonConvert.DeserializeAnonymousType(json, prototype);
                    Debug.WriteLine(string.Format("Employee id = {0}, name = {1}.", t.Employee.Id, t.Employee.Name));
                }
            }

            device.StopSubscribingForMessage(subscribingId);
        }
    }
}
