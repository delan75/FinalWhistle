using FinalWhistle.Domain.Enums;

namespace FinalWhistle.Application.Models;

public class BracketMatch
{
    public int MatchId { get; set; }
    public MatchStage Stage { get; set; }
    public int? HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = "TBD";
    public string HomeTeamFlag { get; set; } = string.Empty;
    public int? AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = "TBD";
    public string AwayTeamFlag { get; set; } = string.Empty;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public bool HasPenalties { get; set; }
    public int? PenaltiesHomeScore { get; set; }
    public int? PenaltiesAwayScore { get; set; }
    public MatchStatus Status { get; set; }
    public DateTime KickoffTime { get; set; }
}
