using System.ComponentModel.DataAnnotations;

namespace FishCareSystem.API.DTOs
{
    public class DeviceDto
    {
        public int Id { get; set; }
        public int TankId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }

    public class CreateDeviceDto
    {
        [Required]
        public int TankId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string Status { get; set; }
    }

    public class UpdateDeviceDto
    {
        [Required]
        public string Status { get; set; }
    }
}
