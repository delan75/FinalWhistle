using FinalWhistle.Domain.Entities;
using FinalWhistle.Models;

namespace FinalWhistle.Areas.Admin.Models;

public class AdminTeamEditorViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FlagUrl { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public AdminTeamCountryMetadataViewModel CountryPreview { get; set; } = new();

    public static AdminTeamEditorViewModel FromTeam(Team team, CountryProfile? countryProfile)
    {
        return new AdminTeamEditorViewModel
        {
            Id = team.Id,
            Name = team.Name,
            FlagUrl = team.FlagUrl,
            GroupId = team.GroupId,
            IsVerified = team.IsVerified,
            CreatedAt = team.CreatedAt,
            CountryPreview = AdminTeamCountryMetadataViewModel.From(team.Name, team.FlagUrl, countryProfile)
        };
    }

    public Team ToTeam()
    {
        return new Team
        {
            Id = Id,
            Name = Name.Trim(),
            FlagUrl = FlagUrl.Trim(),
            GroupId = GroupId,
            IsVerified = IsVerified,
            CreatedAt = CreatedAt == default ? DateTime.UtcNow : CreatedAt
        };
    }
}

public class AdminTeamCountryMetadataViewModel
{
    public string TeamName { get; set; } = string.Empty;
    public string CurrentFlagUrl { get; set; } = string.Empty;
    public string SuggestedFlagUrl { get; set; } = string.Empty;
    public CountryProfile? CountryProfile { get; set; }

    public bool HasTeamName => !string.IsNullOrWhiteSpace(TeamName);
    public bool HasCountryProfile => CountryProfile is not null;
    public bool HasCurrentFlag => !string.IsNullOrWhiteSpace(CurrentFlagUrl);
    public bool HasSuggestedFlag => !string.IsNullOrWhiteSpace(SuggestedFlagUrl);
    public bool CanApplySuggestedFlag =>
        HasSuggestedFlag &&
        !string.Equals(CurrentFlagUrl, SuggestedFlagUrl, StringComparison.OrdinalIgnoreCase);

    public static AdminTeamCountryMetadataViewModel From(
        string? teamName,
        string? currentFlagUrl,
        CountryProfile? countryProfile)
    {
        return new AdminTeamCountryMetadataViewModel
        {
            TeamName = teamName?.Trim() ?? string.Empty,
            CurrentFlagUrl = currentFlagUrl?.Trim() ?? string.Empty,
            SuggestedFlagUrl = ResolveSuggestedFlagUrl(countryProfile),
            CountryProfile = countryProfile
        };
    }

    private static string ResolveSuggestedFlagUrl(CountryProfile? countryProfile)
    {
        if (!string.IsNullOrWhiteSpace(countryProfile?.FlagSvgUrl))
        {
            return countryProfile.FlagSvgUrl;
        }

        if (!string.IsNullOrWhiteSpace(countryProfile?.FlagPngUrl))
        {
            return countryProfile.FlagPngUrl;
        }

        return string.Empty;
    }
}
