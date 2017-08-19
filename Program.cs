using LyncAsyncExtensionMethods;
using Microsoft.Rtc.Collaboration.AudioVideo;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace UCMASpeedRecognitionTranslation
{
    class Program
    {
        private static LyncServer _server;
        private static SpeechEngine _speech;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;            //quick and dirty error handling :)
            _server = new LyncServer();
            var t = _server.Start();
            _server.IncomingCall += server_IncomingCall;

            var cancellationToken = new CancellationToken();
            _speech = new SpeechEngine(cancellationToken);

            _speech.Disconnected += _speech_Disconnected;
            _speech.Failed += _speech_Failed;            
            
            var authenticated = _speech.Authenticate().Result;
            if (authenticated)
            {
                _speech.Connect().Wait();
            }

            Console.WriteLine("Press any key to stop server and exit");
            Console.ReadLine();
            _server.Stop().Wait();
        }

     
       

        private static void _speech_Failed(object sender, Exception e)
        {
            Console.WriteLine("_speech_Failed:" + e.ToString() );
        }

        private static void _speech_Disconnected(object sender, EventArgs e)
        {
            Console.WriteLine("_speech_Disconnected");
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
        }

        private static void server_LyncServerReady(object sender, EventArgs e)
        {
            Console.WriteLine("Lync Server Ready");
        }


        static async void server_IncomingCall(object sender, Microsoft.Rtc.Collaboration.CallReceivedEventArgs<Microsoft.Rtc.Collaboration.AudioVideo.AudioVideoCall> e)
        {
            try
            {
                e.Call.AudioVideoFlowConfigurationRequested += Call_AudioVideoFlowConfigurationRequested;
                await e.Call.AcceptAsync();
                var imCallHandler = new IMCallHandler(_speech);
                imCallHandler.Init(e.Call.Conversation).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Incoming Call Handler:" + ex.ToString());
            }
        }
       

        static void Call_AudioVideoFlowConfigurationRequested(object sender, AudioVideoFlowConfigurationRequestedEventArgs e)
        {
            e.Flow.StateChanged += Flow_StateChanged;
        }

        static void Flow_StateChanged(object sender, Microsoft.Rtc.Collaboration.MediaFlowStateChangedEventArgs e)
        {
            if (e.State == Microsoft.Rtc.Collaboration.MediaFlowState.Active)
            {

                var flow = sender as AudioVideoFlow;

                var audioCallHandler = new AudioCallHandler(_speech);
                Console.WriteLine("Starting audio call handler");
                var startTask = Task.Run(() => audioCallHandler.Start(flow, _server));

            }
        }


    }
}
