using LyncAsyncExtensionMethods;
using Microsoft.Rtc.Collaboration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UCMASpeedRecognitionTranslation
{
    public class IMCallHandler
    {
        private InstantMessagingFlow _imFlow;
        private InstantMessagingCall _call;
        private SpeechEngine _speech;
        
        private const string messageToIM = "{0} [{1}]";  //{0} = translated text, {1} = original text

        public IMCallHandler(SpeechEngine speech)
        {
            this._speech = speech;
            _speech.OnEndOfTextData += _speech_OnEndOfTextData;
        }

        public async Task Init(Conversation conversation)
        {
            try
            {
                _call = new InstantMessagingCall(conversation);
                _call.InstantMessagingFlowConfigurationRequested += _call_InstantMessagingFlowConfigurationRequested;
                await _call.EstablishAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on init: " + ex.ToString());
            }
        }

        private void _call_InstantMessagingFlowConfigurationRequested(object sender, InstantMessagingFlowConfigurationRequestedEventArgs e)
        {
            _imFlow = e.Flow;
        }

        private async void _speech_OnEndOfTextData(object sender, ArraySegment<byte> e)
        {
            try
            {
                var result = DeserializeJsonToObject(e.Array);
                await SendInstantMessageResult(String.Format(messageToIM, result.translation, result.recognition));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception sending IM:" + ex.ToString());
            }
        }

        private async Task SendInstantMessageResult(string message)
        {
            if (_imFlow != null && _imFlow.State == MediaFlowState.Active)
            {
                await _imFlow.SendInstantMessageAsync(message);
            }
        }

        public SpeechResult DeserializeJsonToObject(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return JsonSerializer.Create().Deserialize(reader, typeof(SpeechResult)) as SpeechResult;
        }
    }
}
