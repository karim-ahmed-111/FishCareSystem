namespace FishCareSystem.API.Models
{
    public class Alert
    {
        public int Id { get; set; }
        public int TankId { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; } // Info, Warning, Critical
        public DateTime CreatedAt { get; set; }
        public Tank Tank { get; set; }
    }
}
