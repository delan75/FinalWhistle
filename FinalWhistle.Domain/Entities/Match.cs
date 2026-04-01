using FinalWhistle.Domain.Enums;

namespace FinalWhistle.Domain.Entities;

public class Match
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public MatchStage Stage { get; set; }
    public int? GroupId { get; set; }
    public int? HomeTeamId { get; set; }
    public int? AwayTeamId { get; set; }
    public DateTime KickoffTime { get; set; }
    public MatchStatus Status { get; set; }
    public bool IsLockedForPredictions { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public Group? Group { get; set; }
    public Team? HomeTeam { get; set; }
    public Team? AwayTeam { get; set; }
    public MatchResult? Result { get; set; }
    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
}
