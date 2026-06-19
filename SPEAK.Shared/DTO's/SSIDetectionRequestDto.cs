using System.Text.Json.Serialization;

namespace SPEAK.Shared.DTO_s
{
    public class SSIDetectionRequestDto
    {
        [JsonPropertyName("is_reader")]
        public bool IsReader { get; set; }

        [JsonPropertyName("distracting_sounds")]
        public int DistractingSounds { get; set; }

        [JsonPropertyName("facial_grimaces")]
        public int FacialGrimaces { get; set; }

        [JsonPropertyName("head_movements")]
        public int HeadMovements { get; set; }

        [JsonPropertyName("extremities")]
        public int Extremities { get; set; }
    }
}
