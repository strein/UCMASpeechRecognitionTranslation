using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace UCMASpeechRecognitionTranslation
{
    public class SpeechEngine
    {

        private CancellationToken _cancellationToken;
        private string authToken;
        private string region;

        public event EventHandler Disconnected;
        public event EventHandler<Exception> Failed;
        public event EventHandler<String> OnTextRecognized;

        public async Task<bool> Authenticate()
        {
            string azureKey = ConfigurationManager.AppSettings["SubscriptionKey"];
            region = ConfigurationManager.AppSettings["AzureRegion"];
            Microsoft.Speech.API.AzureAuthToken tokenSource = new Microsoft.Speech.API.AzureAuthToken(azureKey, region);
            string token = await tokenSource.GetAccessTokenAsync();
            if (token.Length > 10)
            {
                this.authToken = token;
                return true;
            }
            return false;
        }

        public async Task StartRecognition()
        {

            var config = SpeechConfig.FromAuthorizationToken(authToken, region);

            var stopRecognition = new TaskCompletionSource<int>();

            // Creates a speech recognizer using file as audio input.
            // Replace with your own audio file name.
            using (var audioInput = AudioConfig.FromWavFileInput(@"whatstheweatherlike.wav"))
            {
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    // Subscribes to events.

                    /* Recognizing process
                    recognizer.Recognizing += (s, e) =>
                    {
                        Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                    };
                    */

                    recognizer.Recognized += (s, e) =>
                                        {
                                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                                            {
                                                // Send to SfB Chat here
                                                //this.OnTextRecognized(this, e.Result.Text);
                                                Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                                            }
                                            else if (e.Result.Reason == ResultReason.NoMatch)
                                            {
                                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                                            }
                                        };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("\n    Session started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\n    Session stopped event.");
                        Console.WriteLine("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }

        }
        public void SendMessage(ArraySegment<byte> content)
        {
            //this.outgoingMessageQueue.Add(content);
        }
    }
}
