using Microsoft.Rtc.Collaboration.AudioVideo;
using NAudio.Wave;
using NAudio.WindowsMediaFormat;
using System;
using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;

namespace UCMASpeechRecognitionTranslation
{
    class AudioCallHandler
    {
        private SpeechRecognitionConnector _speechConnector = new SpeechRecognitionConnector();
        private SpeechAudioFormatInfo audioformat = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, System.Speech.AudioFormat.AudioChannel.Mono);
        private AudioVideoFlow _flow;
        private LyncServer _server;
        private SpeechEngine _speech;

        public AudioCallHandler(SpeechEngine speech)
        {
            this._speech = speech;
        }

        public void Start(AudioVideoFlow flow, LyncServer server)
        {
            _flow = flow;
            _server = server;

            Recorder recorder = new Recorder();
            recorder.AttachFlow(_flow);
            WmaFileSink sink = new WmaFileSink("voice_input.wma");
            recorder.SetSink(sink);
            recorder.Start();

            var wmafileReader = new WMAFileReader("voice_input.wma");
            WaveStream waveStream = WaveFormatConversionStream.CreatePcmStream(wmafileReader);
            int reader = 0;
            byte[] buffer = new byte[4096];
            var header = GetWaveHeader(waveStream.WaveFormat);
            _speech.SendMessage(new ArraySegment<byte>(header, 0, header.Length));

            while ((reader = waveStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                AudioDataAvailable(new ArraySegment<byte>(buffer, 0, buffer.Length));
                Thread.Sleep(10);
            }

        }

        private byte[] GetWaveHeader(WaveFormat format)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);
                writer.Write(Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(0);
                writer.Write(Encoding.UTF8.GetBytes("WAVE"));
                writer.Write(Encoding.UTF8.GetBytes("fmt "));
                format.Serialize(writer);
                writer.Write(Encoding.UTF8.GetBytes("data"));
                writer.Write(0);

                stream.Position = 0;
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private void AudioDataAvailable(ArraySegment<byte> data)
        {
            Console.Write(".");
            _speech.SendMessage(new ArraySegment<byte>(data.Array, data.Offset, data.Count));
        }

    }
}
