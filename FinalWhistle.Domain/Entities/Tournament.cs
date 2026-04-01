using FinalWhistle.Domain.Enums;

namespace FinalWhistle.Domain.Entities;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Season { get; set; }
    public TournamentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
