namespace FinalWhistle.Domain.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string FlagUrl { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }

    public Group? Group { get; set; }
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
    public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
}
