using Microsoft.AspNetCore.Identity;

namespace FinalWhistle.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; }
    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
}
