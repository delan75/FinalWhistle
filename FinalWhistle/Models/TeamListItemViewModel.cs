using FinalWhistle.Domain.Entities;

namespace FinalWhistle.Models;

public class TeamListItemViewModel
{
    public Team Team { get; set; } = null!;
    public string DisplayFlagUrl { get; set; } = string.Empty;
    public CountryProfile? CountryProfile { get; set; }
}
