using LyncAsyncExtensionMethods;
using Microsoft.Rtc.Collaboration.AudioVideo;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Rtc.Collaboration;

namespace UCMASpeechRecognitionTranslation
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
            _server.IncomingConference += _server_IncomingConference;

            //var cancellationToken = new CancellationToken();
            _speech = new SpeechEngine();

            _speech.Disconnected += _speech_Disconnected;
            _speech.Failed += _speech_Failed;

            var authenticated = _speech.Authenticate().Result;
            if (authenticated)
            {
                _speech.StartRecognition().Wait();
            }

            Console.WriteLine("Press any key to stop server and exit");
            Console.ReadLine();
            _server.Stop().Wait();
        }



        private static void _speech_Failed(object sender, Exception e)
        {
            Console.WriteLine("_speech_Failed:" + e.ToString());
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

        private async static void _server_IncomingConference(object sender, Microsoft.Rtc.Collaboration.ConferenceInvitationReceivedEventArgs e)
        {
            try
            {
                await e.Invitation.AcceptAsync();
               await e.Invitation.Conversation.ConferenceSession.JoinAsync(new ConferenceJoinOptions());
                Console.WriteLine("joined");
                
                var audioCall = new AudioVideoCall(e.Invitation.Conversation);
                audioCall.AudioVideoFlowConfigurationRequested += Call_AudioVideoFlowConfigurationRequested;
                await audioCall.EstablishAsync();
              
                
                var imCallHandler = new IMCallHandler(_speech);
               await  imCallHandler.Init(e.Invitation.Conversation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
          
        }


        static void Call_AudioVideoFlowConfigurationRequested(object sender, AudioVideoFlowConfigurationRequestedEventArgs e)
        {
            Console.WriteLine("flow config requested");
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
