namespace FinalWhistle.Domain.Entities;

public class MatchResult
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
    public bool HasExtraTime { get; set; }
    public int? ExtraTimeHomeGoals { get; set; }
    public int? ExtraTimeAwayGoals { get; set; }
    public bool HasPenalties { get; set; }
    public int? PenaltiesHomeScore { get; set; }
    public int? PenaltiesAwayScore { get; set; }
    public string? EnteredByAdminId { get; set; }
    public DateTime EnteredAt { get; set; }
    public int RevisionNumber { get; set; }

    public Match Match { get; set; } = null!;
}
