using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net.Http;
using System.Threading.Tasks;

using Windows.UI.Xaml.Media.Imaging;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Tools.WindowsDevicePortal;


using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using System.Windows;

using System.Windows.Input;


using static Microsoft.Tools.WindowsDevicePortal.DevicePortal;
using Windows.Security.Cryptography.Certificates;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //DevicePortalAPIClient client = new DevicePortalAPIClient();
        private DevicePortal portal;
        private Certificate certificate;
        static string HoloLensUrl = "http://127.0.0.1:10080/";
        static string ApiTakePhoto = "api/holographic/mrc/photo?holo=true&pv=true";
        static string ApiGetFile = "api/holographic/mrc/file?filename={0}&op=stream";
        public MainPage()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// TextChanged handler for the address text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        

        private async void btnTakePicture2_Click(object sender, RoutedEventArgs e)
        {
            
            portal = new DevicePortal(
                new DefaultDevicePortalConnection(HoloLensUrl, "Tolegen","*****"));

            StringBuilder sb = new StringBuilder();

            sb.Append(this.messageb.Text);
            sb.AppendLine("Connecting...");
            this.messageb.Text = sb.ToString();
            portal.ConnectionStatus += (portal, connectArgs) =>
            {
                if (connectArgs.Status == DeviceConnectionStatus.Connected)
                {
                    sb.Append("Connected to: ");
                    sb.AppendLine(portal.Address);
                    sb.Append("OS version: ");
                    sb.AppendLine(portal.OperatingSystemVersion);
                    sb.Append("Device family: ");
                    sb.AppendLine(portal.DeviceFamily);
                    sb.Append("Platform: ");
                    sb.AppendLine(String.Format("{0} ({1})",
                        portal.PlatformName,
                        portal.Platform.ToString()));
                }
                else if (connectArgs.Status == DeviceConnectionStatus.Failed)
                {
                    sb.AppendLine("Failed to connect to the device.");
                    sb.AppendLine(connectArgs.Message);
                }
            };
            try
            {
                // If the user wants to allow untrusted connections, make a call to GetRootDeviceCertificate
                // with acceptUntrustedCerts set to true. This will enable untrusted connections for the
                // remainder of this session.

                    this.certificate = await portal.GetRootDeviceCertificateAsync(true);
                
                await portal.ConnectAsync(manualCertificate: this.certificate);
            }
            catch (Exception exception)
            {
                sb.AppendLine(exception.Message);
            }
            this.messageb.Text = sb.ToString();

        }
        private async void GetWifiInfo_Click(object sender, RoutedEventArgs e)
        {
           

            StringBuilder sb = new StringBuilder();

            sb.Append(messageb.Text);
            sb.AppendLine("Getting WiFi interfaces and networks...");
            messageb.Text = sb.ToString();

            try
            {
                WifiInterfaces wifiInterfaces = await portal.GetWifiInterfacesAsync();
                sb.AppendLine("WiFi Interfaces:");
                foreach (WifiInterface wifiInterface in wifiInterfaces.Interfaces)
                {
                    sb.Append(" ");
                    sb.AppendLine(wifiInterface.Description);
                    sb.Append("  GUID: ");
                    sb.AppendLine(wifiInterface.Guid.ToString());

                    WifiNetworks wifiNetworks = await portal.GetWifiNetworksAsync(wifiInterface.Guid);
                    sb.AppendLine("  Networks:");
                    foreach (WifiNetworkInfo network in wifiNetworks.AvailableNetworks)
                    {
                        sb.Append("   SSID: ");
                        sb.AppendLine(network.Ssid);
                        sb.Append("   Profile name: ");
                        sb.AppendLine(network.ProfileName);
                        sb.Append("   is connected: ");
                        sb.AppendLine(network.IsConnected.ToString());
                        sb.Append("   Channel: ");
                        sb.AppendLine(network.Channel.ToString());
                        sb.Append("   Authentication algorithm: ");
                        sb.AppendLine(network.AuthenticationAlgorithm);
                        sb.Append("   Signal quality: ");
                        sb.AppendLine(network.SignalQuality.ToString());
                    }
                };
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to get WiFi info.");
                sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
            }

            messageb.Text = sb.ToString();

        }
        private async void btnTakePicture_Click(object sender, RoutedEventArgs e)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential("Tolegen", "hassAn10");

            HttpClient client = new HttpClient(handler);
            
            HttpResponseMessage message = await client.PostAsync(HoloLensUrl + ApiTakePhoto, null);
            Picture picture = new Picture();

            if (message.StatusCode == HttpStatusCode.OK)
            {
                //content contains json payload with name of picture taken
                string content = await message.Content.ReadAsStringAsync();

                picture = JsonConvert.DeserializeObject<Picture>(content);
            }

            tbResult.Text = picture.PhotoFileName;

            Stream stream = await GetFileAsStream(picture.PhotoFileName);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.DecodePixelWidth = 600;
            await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
            imgPicture.Source = bitmapImage;

        }
        public async Task<Stream> GetFileAsStream(string fileName)
        {
            string url = string.Format(HoloLensUrl + ApiGetFile, Base64Encode(fileName));
            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential("Tolegen", "hassAn10");
            HttpClient client = new HttpClient(handler);

            HttpResponseMessage message = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);

            return await message.Content.ReadAsStreamAsync();
        }
        private string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
    public class Picture
    {
        public string PhotoFileName { get; set; }
    }
}
