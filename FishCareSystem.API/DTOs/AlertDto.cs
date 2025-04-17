using System.ComponentModel.DataAnnotations;

namespace FishCareSystem.API.DTOs
{
    public class AlertDto
    {
        public int Id { get; set; }
        public int TankId { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAlertDto
    {
        [Required]
        public int TankId { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string Severity { get; set; }
    }
}
