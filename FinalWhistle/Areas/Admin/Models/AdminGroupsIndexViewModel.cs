using FinalWhistle.Application.Models;

namespace FinalWhistle.Areas.Admin.Models;

public class AdminGroupsIndexViewModel
{
    public string TournamentName { get; set; } = string.Empty;
    public int Season { get; set; }
    public int CompletedGroupMatches { get; set; }
    public int TotalGroupMatches { get; set; }
    public int CompletedGroups { get; set; }
    public bool KnockoutAlreadyGenerated { get; set; }
    public List<AdminGroupOverviewViewModel> Groups { get; set; } = new();

    public bool AllGroupMatchesCompleted => TotalGroupMatches > 0 && CompletedGroupMatches == TotalGroupMatches;
    public bool CanGenerateRoundOf16 => AllGroupMatchesCompleted && !KnockoutAlreadyGenerated;
}

public class AdminGroupOverviewViewModel
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int CompletedMatches { get; set; }
    public int TotalMatches { get; set; }
    public DateTime? NextKickoff { get; set; }
    public List<TeamStanding> Teams { get; set; } = new();

    public int RemainingMatches => Math.Max(0, TotalMatches - CompletedMatches);
    public bool IsComplete => TotalMatches > 0 && CompletedMatches == TotalMatches;
}
