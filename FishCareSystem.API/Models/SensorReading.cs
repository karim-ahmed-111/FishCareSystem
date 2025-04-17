namespace FishCareSystem.API.Models
{
    public class SensorReading
    {
        public int Id { get; set; }
        public int TankId { get; set; }
        public string Type { get; set; } // e.g., Temperature, pH, Oxygen
        public double Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
        public Tank Tank { get; set; }
    }
}
