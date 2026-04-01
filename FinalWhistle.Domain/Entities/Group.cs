namespace FinalWhistle.Domain.Entities;

public class Group
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
