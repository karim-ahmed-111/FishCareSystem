using System.ComponentModel.DataAnnotations;

namespace FishCareSystem.API.DTOs
{
    public class SensorReadingDto
    {
        public int Id { get; set; }
        public int TankId { get; set; }
        public string Type { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CreateSensorReadingDto
    {
        [Required]
        public int TankId { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public double Value { get; set; }

        [Required]
        public string Unit { get; set; }
    }
}
