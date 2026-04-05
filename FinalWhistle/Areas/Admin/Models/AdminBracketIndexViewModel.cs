using FinalWhistle.Application.Models;

namespace FinalWhistle.Areas.Admin.Models;

public class AdminBracketIndexViewModel
{
    public string TournamentName { get; set; } = string.Empty;
    public int Season { get; set; }
    public int CompletedGroupMatches { get; set; }
    public int TotalGroupMatches { get; set; }
    public int CompletedRoundOf16Matches { get; set; }
    public int CompletedQuarterFinalMatches { get; set; }
    public int CompletedSemiFinalMatches { get; set; }
    public BracketViewModel Bracket { get; set; } = new();

    public bool AllGroupMatchesCompleted => TotalGroupMatches > 0 && CompletedGroupMatches == TotalGroupMatches;
}
