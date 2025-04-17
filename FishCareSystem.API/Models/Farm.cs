using System.Threading.Tasks;

namespace FishCareSystem.API.Models
{
    public class Farm
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string OwnerId { get; set; }
        public ApplicationUser Owner { get; set; }
        public List<Tank> Tanks { get; set; } = new List<Tank>();
    }
}
