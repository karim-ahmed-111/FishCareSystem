using System.ComponentModel.DataAnnotations;

namespace FishCareSystem.API.DTOs
{
    public class FarmDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
    }

    public class CreateFarmDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Location { get; set; }
    }
}
