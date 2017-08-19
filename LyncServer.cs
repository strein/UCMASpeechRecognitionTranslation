using LyncAsyncExtensionMethods;
using Microsoft.Rtc.Collaboration;
using Microsoft.Rtc.Collaboration.AudioVideo;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace UCMASpeedRecognitionTranslation
{
    public class LyncServer
    {
        private string _appUserAgent = "UCMA Speech Recognition Translation";
        private string _appID = ConfigurationManager.AppSettings["UCMAAppID"];
        private CollaborationPlatform _collabPlatform;
        private ApplicationEndpoint _endpoint;
       
        public event EventHandler<EventArgs> LyncServerReady = delegate { };
        public event EventHandler<CallReceivedEventArgs<AudioVideoCall>> IncomingCall = delegate { };

        public async Task Start()
        {
            try
            {
                Console.WriteLine("Starting Collaboration Platform");
                ProvisionedApplicationPlatformSettings settings = new ProvisionedApplicationPlatformSettings(_appUserAgent, _appID);
                _collabPlatform = new CollaborationPlatform(settings);
                _collabPlatform.RegisterForApplicationEndpointSettings(OnNewApplicationEndpointDiscovered);
                await _collabPlatform.StartupAsync();
                Console.WriteLine("Platform Started");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error establishing collaboration platform: {0}", ex.ToString());
            }
        }

        public async Task Stop()
        {
            Console.WriteLine("Stopping Lync Server");
            await _endpoint.TerminateAsync();
            await _collabPlatform.ShutdownAsync();
        }     

        private async void OnNewApplicationEndpointDiscovered(object sender, ApplicationEndpointSettingsDiscoveredEventArgs e)
        {
            Console.WriteLine(string.Format("New Endpoint {0} discovered", e.ApplicationEndpointSettings.OwnerUri));
            _endpoint = new ApplicationEndpoint(_collabPlatform, e.ApplicationEndpointSettings);
            _endpoint.RegisterForIncomingCall<AudioVideoCall>(OnIncomingCall);
            await _endpoint.EstablishAsync();
            Console.WriteLine("Endpoint established");
            
            LyncServerReady(this, new EventArgs());
        }

        //new incoming audio call
        private void OnIncomingCall(object sender, CallReceivedEventArgs<AudioVideoCall> e)
        {
            Console.WriteLine(string.Format("Incoming call! Caller: {0}", e.Call.RemoteEndpoint.Uri));
            IncomingCall(this, e);
        }

    

    }
}
