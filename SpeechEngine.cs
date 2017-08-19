using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace UCMASpeedRecognitionTranslation
{
    public class SpeechEngine
    {
        private BlockingCollection<ArraySegment<byte>> outgoingMessageQueue = new BlockingCollection<ArraySegment<byte>>();
        private ClientWebSocket webSocketClient;
        private CancellationToken _cancellationToken;
        private string authToken;

        public event EventHandler Disconnected;
        public event EventHandler<Exception> Failed;
        public event EventHandler<ArraySegment<byte>> OnTextData;
        public event EventHandler<ArraySegment<byte>> OnEndOfTextData;
        public event EventHandler<ArraySegment<byte>> OnBinaryData;
        public event EventHandler<ArraySegment<byte>> OnEndOfBinaryData;

        private const string translatorAPI = "wss://dev.microsofttranslator.com/speech/translate?from=en-US&to=de-DE&features=TimingInfo&api-version=1.0";
        private const int ReceiveChunkSize = 8 * 1024;
        private const int SendChunkSize = 8 * 1024;
        private string clientID = ConfigurationManager.AppSettings["ClientID"];

        public SpeechEngine(CancellationToken cancellationToken)
        {
            this.webSocketClient = new ClientWebSocket();
            webSocketClient.Options.SetRequestHeader("X-ClientAppId", clientID);
            this._cancellationToken = cancellationToken;
        }

        public async Task<bool> Authenticate()
        {
            string azureKey = ConfigurationManager.AppSettings["TranslatorAPIKey"];
            Microsoft.Translator.API.AzureAuthToken tokenSource = new Microsoft.Translator.API.AzureAuthToken(azureKey);
            string token = await tokenSource.GetAccessTokenAsync();
            if (token.Length > 10)
            {
                this.authToken = token;
                return true;
            }
            return false;
        }


        public async Task Connect()
        {
            webSocketClient.Options.SetRequestHeader("Authorization", this.authToken);
            await webSocketClient.ConnectAsync(new Uri(translatorAPI), this._cancellationToken);

            var receiveTask = Task.Run(() => this.StartReceiving())
                .ContinueWith((t) => ReportError(t))
                .ConfigureAwait(false);
            var sendTask = Task.Run(() => this.StartSending())
                .ContinueWith((t) => ReportError(t))
                .ConfigureAwait(false);
        }

        public bool IsConnected()
        {
            WebSocketState wsState = WebSocketState.None;
            try
            {
                wsState = this.webSocketClient.State;

            }
            catch (ObjectDisposedException)
            {
                wsState = WebSocketState.None;
            }
            return ((this._cancellationToken.IsCancellationRequested == false)
                 && ((wsState == WebSocketState.Open) || (wsState == WebSocketState.CloseReceived)));
        }

        public async Task Disconnect()
        {
            if (this.IsConnected())
            {
                try
                {
                    await this.webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, this._cancellationToken);
                }
                finally
                {
                    if (this.Disconnected != null) this.Disconnected(this, EventArgs.Empty);
                }
            }
        }

        private void ReportError(Task task)
        {
            if (task.IsFaulted)
            {
                if (this.Failed != null) Failed(this, task.Exception);
            }
        }

        public void SendMessage(ArraySegment<byte> content)
        {
            this.outgoingMessageQueue.Add(content);
        }

        private async Task StartSending()
        {
            while (this.IsConnected())
            {
                ArraySegment<byte> item;
                if (this.outgoingMessageQueue.TryTake(out item, 100))
                {
                    try
                    {
                        await this.webSocketClient.SendAsync(item, WebSocketMessageType.Binary, true, CancellationToken.None);
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception iterating Queue: " + ex.ToString());
                    }
                }
            }
        }

        private async Task StartReceiving()
        {
            var buffer = new byte[ReceiveChunkSize];
            var arraySegmentBuffer = new ArraySegment<byte>(buffer);
            Task<WebSocketReceiveResult> receiveTask = null;
            bool disconnecting = false;
            while (this.IsConnected() && !disconnecting)
            {
                if (receiveTask == null)
                {
                    receiveTask = this.webSocketClient.ReceiveAsync(arraySegmentBuffer, this._cancellationToken);
                }
                if (receiveTask.Wait(100))
                {
                    WebSocketReceiveResult result = await receiveTask;
                    receiveTask = null;
                    EventHandler<ArraySegment<byte>> handler = null;
                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            disconnecting = true;
                            Console.WriteLine($"{DateTime.Now} : Disconnecting web socket with status {result.CloseStatus} for the following reason: {result.CloseStatusDescription}");
                            await this.Disconnect();
                            break;
                        case WebSocketMessageType.Binary:
                            handler = result.EndOfMessage ? this.OnEndOfBinaryData : this.OnBinaryData;
                            break;
                        case WebSocketMessageType.Text:

                            handler = result.EndOfMessage ? this.OnEndOfTextData : this.OnTextData;
                            break;
                    }
                    if (handler != null)
                    {
                        var data = new byte[result.Count];
                        Array.Copy(buffer, data, result.Count);
                        handler(this, new ArraySegment<byte>(data));
                    }
                }
            }
        }
    }
}
