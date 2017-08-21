using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCMASpeechRecognitionTranslation
{
    public class SpeechResult
    {
        public string type { get; set; }
        public int id { get; set; }
        public string recognition { get; set; }
        public string translation { get; set; }
        public Int64 audioStreamPosition { get; set; }
        public Int64 audioSizeBytes { get; set; }
        public Int64 audioTimeOffset { get; set; }
        public Int64 audioTimeSize { get; set; }
    }
}
