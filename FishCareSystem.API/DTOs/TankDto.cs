using System.ComponentModel.DataAnnotations;

namespace FishCareSystem.API.DTOs
{
    public class TankDto
    {
        public int Id { get; set; }
        public int FarmId { get; set; }
        public string Name { get; set; }
        public double Capacity { get; set; }
        public string FishSpecies { get; set; }
    }

    public class CreateTankDto
    {
        [Required]
        public int FarmId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Range(1, double.MaxValue)]
        public double Capacity { get; set; }

        public string FishSpecies { get; set; }
    }
}
