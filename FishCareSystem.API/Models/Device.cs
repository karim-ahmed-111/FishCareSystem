namespace FishCareSystem.API.Models
{
    public class Device
    {
        public int Id { get; set; }
        public int TankId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // e.g., Heater, Pump
        public string Status { get; set; } // On, Off
        public Tank Tank { get; set; }
    }
}