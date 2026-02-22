namespace SPEAK.Shared.DTO_s
{
    public class SSIDetectionRequestDto
    {
        public bool IsReader { get; set; }
        public int DistractingSounds { get; set; }
        public int FacialGrimaces { get; set; }
        public int HeadMovements { get; set; }
        public int Extremities { get; set; }
    }
}
