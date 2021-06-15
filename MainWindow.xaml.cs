using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Windows.Data.Json;

namespace Ares
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool isRunning;
        public bool isInjectActive;
        private string injectionCode = "";
        ProxyServer proxyServer = new ProxyServer();
        ExplicitProxyEndPoint explicitEndPoint;

        public MainWindow()
        {
            InitializeComponent();
            changeStopRunning();
            isInjectActive = false;
            // locally trust root certificate used by this proxy 

            // optionally set the Certificate Engine
            // Under Mono only BouncyCastle will be supported
            //proxyServer.CertificateManager.CertificateEngine = Network.CertificateEngine.BouncyCastle;

            proxyServer.BeforeRequest += OnRequest;
            //proxyServer.BeforeResponse += OnResponse;
            //proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            //proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true)
            {
                // Use self-issued generic certificate on all https requests
                // Optimizes performance by not creating a certificate for each https-enabled domain
                // Useful when certificate trust is not required by proxy clients
                //GenericCertificate = new X509Certificate2(Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genericcert.pfx"), "password")
            };

            // Fired when a CONNECT request is received
            //explicitEndPoint.BeforeTunnelConnectRequest += onBeforeTunnelConnectRequest;

            // An explicit endpoint is where the client knows about the existence of a proxy
            // So client sends request in a proxy friendly manner
            
            
        }

        private Task onBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            throw new NotImplementedException();
        }

        private Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            throw new NotImplementedException();
        }

        private Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            throw new NotImplementedException();
        }

        private Task OnResponse(object sender, SessionEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async Task OnRequest(object sender, SessionEventArgs e)
        {
            if (e.HttpClient.Request.RequestUri.AbsoluteUri.Contains("fileManager/save")){
                string bS = await e.GetRequestBodyAsString();
                dynamic result = JsonConvert.DeserializeObject(bS);
                if (!isInjectActive){
                    String decoded = result.content;
                    Log(decoded);
                    Log("Found Save With Name " + result.path + " Content Length " + decoded.Length + " Successfully Copied To Your Clipboard!");
                    clipboardSetText(decoded);
                }
                else{
                    if (injectionCode == "" || injectionCode.Length == 0){
                        Log("Injection Code Empty? Wtf");
                        return;
                    }
                    result.content = injectionCode;
                    var ob = JsonConvert.SerializeObject(result);
                    e.SetRequestBodyString(ob);
                    Log("Found Save With Name " + result.path + " Content Length " + ob.Length + " Successfully Injected The Code!");

                    this.Dispatcher.Invoke(() =>
                    {
                        mInject.Content = "Start Injector";
                        isInjectActive = false;
                        mInject.Background = new SolidColorBrush(Colors.OrangeRed);
                    });
                }
            }

            if (e.HttpClient.Request.RequestUri.AbsoluteUri.Contains("fileManager/modify?path="))
            {
                String fname = FindTextBetween(e.HttpClient.Request.RequestUri.AbsoluteUri, "fileManager/modify?path=", "&");
                string bodyString = await e.GetRequestBodyAsString();
                if (bodyString.Contains("content"))
                {
                    if (isInjectActive)
                    {
                        if (injectionCode == "" || injectionCode.Length == 0)
                        {
                            Log("Injection Code Empty? Wtf");
                        }
                        else
                        {
                            string encoded = HttpUtility.UrlEncode(injectionCode);
                            string body = "content=" + encoded;
                            e.SetRequestBodyString(body);
                            Log("Found Save With Name " + fname + " Content Length " + encoded.Length + " Successfully Injected The Code!");
                        }
                        this.Dispatcher.Invoke(() =>
                        {
                            mInject.Content = "Start Injector";
                            isInjectActive = false;
                            mInject.Background = new SolidColorBrush(Colors.OrangeRed);
                        });
                    }
                    else
                    {
                        string content = bodyString.Substring(bodyString.LastIndexOf("content=") + 8);
                        string decoded = HttpUtility.UrlDecode(content);

                        Log(decoded);
                        Log("Found Save With Name " + fname + " Content Length " + decoded.Length + " Successfully Copied To Your Clipboard!");
                        clipboardSetText(decoded);
                    }
                }
                else
                    Log("Found Save With Name " + fname + " But Failed To Parse For Data :((");

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
                changeStopRunning();
            else
                changeStartRunning();
        }

        private void changeStartRunning()
        {
            isRunning = true;
            mButton.Content = "Stop Proxy";
            mButton.Background = new SolidColorBrush(Colors.Green);

            if (!proxyServer.ProxyRunning)
            {
                proxyServer.AddEndPoint(explicitEndPoint);
                proxyServer.Start();
                proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
                proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);
                Log("Successfully started server and set system proxy!");
            }
            else Log("Server is already running!");
        }

        private void changeStopRunning()
        {
            isRunning = false;
            mButton.Content = "Start Proxy";
            mButton.Background = new SolidColorBrush(Colors.OrangeRed);

            if (proxyServer.ProxyRunning)
            {
                proxyServer.Stop();
                //proxyServer.RestoreOriginalProxySettings();
                Log("Successfully stopped server!");
            }
            else Log("Server is already stopped!");
        }


        private void Log(String message)
        {
            this.Dispatcher.Invoke(() =>
            {
                TextBoxRequest.Text += message+"\n";
                TextBoxRequest.ScrollToEnd();
            });
            

        }

        public string FindTextBetween(string text, string left, string right)
        {
            // TODO: Validate input arguments

            int beginIndex = text.IndexOf(left); // find occurence of left delimiter
            if (beginIndex == -1)
                return string.Empty; // or throw exception?

            beginIndex += left.Length;

            int endIndex = text.IndexOf(right, beginIndex); // find occurence of right delimiter
            if (endIndex == -1)
                return string.Empty; // or throw exception?

            return text.Substring(beginIndex, endIndex - beginIndex).Trim();
        }

        protected void clipboardSetText(string inTextToCopy)
        {
            var clipboardThread = new Thread(() => clipBoardThreadWorker(inTextToCopy));
            clipboardThread.SetApartmentState(ApartmentState.STA);
            clipboardThread.IsBackground = false;
            clipboardThread.Start();
        }
        private void clipBoardThreadWorker(string inTextToCopy)
        {
            System.Windows.Clipboard.SetText(inTextToCopy);
        }

        private void Injector_Click(object sender, RoutedEventArgs e)
        {
            if (!proxyServer.ProxyRunning) {
                Log("You need proxy server running to use this function!!");
                return;
            }
            if (isInjectActive){
                mInject.Content = "Start Injector";
                mInject.Background = new SolidColorBrush(Colors.OrangeRed);
                
            }
            else{
                var dialog = new MyDialog();
                if (dialog.ShowDialog() == true)
                {
                    injectionCode = dialog.ResponseText;
                }
                mInject.Content = "Stop Injector";
                mInject.Background = new SolidColorBrush(Colors.Green);
            }

            isInjectActive = !isInjectActive;
        }
    }

    
}
