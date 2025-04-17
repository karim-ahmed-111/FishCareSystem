namespace FishCareSystem.API.Models
{
    public class Tank
    {
        public int Id { get; set; }
        public int FarmId { get; set; }
        public string Name { get; set; }
        public double Capacity { get; set; }
        public string FishSpecies { get; set; }
        public Farm Farm { get; set; }
        public List<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
        public List<Alert> Alerts { get; set; } = new List<Alert>();
        public List<Device> Devices { get; set; } = new List<Device>();
    }
}
