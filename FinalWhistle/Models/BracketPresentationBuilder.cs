using FinalWhistle.Application.Models;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;

namespace FinalWhistle.Models;

public static class BracketPresentationBuilder
{
    public static BracketViewModel Build(IEnumerable<Match> matches, bool allGroupMatchesCompleted)
    {
        var matchList = matches
            .OrderBy(m => m.Stage)
            .ThenBy(m => m.KickoffTime)
            .ToList();

        return new BracketViewModel
        {
            RoundOf16 = matchList.Where(m => m.Stage == MatchStage.RoundOf16)
                .Select(MapToBracketMatch)
                .ToList(),
            QuarterFinals = matchList.Where(m => m.Stage == MatchStage.QuarterFinal)
                .Select(MapToBracketMatch)
                .ToList(),
            SemiFinals = matchList.Where(m => m.Stage == MatchStage.SemiFinal)
                .Select(MapToBracketMatch)
                .ToList(),
            Final = matchList.FirstOrDefault(m => m.Stage == MatchStage.Final) is { } final
                ? MapToBracketMatch(final)
                : null,
            ThirdPlace = matchList.FirstOrDefault(m => m.Stage == MatchStage.ThirdPlace) is { } third
                ? MapToBracketMatch(third)
                : null,
            CanGenerateR16 = allGroupMatchesCompleted && !matchList.Any(m => m.Stage == MatchStage.RoundOf16),
            CanAdvanceToQF = matchList.Count(m => m.Stage == MatchStage.RoundOf16 && m.Status == MatchStatus.Completed) == 8
                && !matchList.Any(m => m.Stage == MatchStage.QuarterFinal),
            CanAdvanceToSF = matchList.Count(m => m.Stage == MatchStage.QuarterFinal && m.Status == MatchStatus.Completed) == 4
                && !matchList.Any(m => m.Stage == MatchStage.SemiFinal),
            CanAdvanceToFinal = matchList.Count(m => m.Stage == MatchStage.SemiFinal && m.Status == MatchStatus.Completed) == 2
                && !matchList.Any(m => m.Stage == MatchStage.Final)
        };
    }

    private static BracketMatch MapToBracketMatch(Match match)
    {
        return new BracketMatch
        {
            MatchId = match.Id,
            Stage = match.Stage,
            HomeTeamId = match.HomeTeamId,
            HomeTeamName = match.HomeTeam?.Name ?? "TBD",
            HomeTeamFlag = match.HomeTeam?.FlagUrl ?? string.Empty,
            AwayTeamId = match.AwayTeamId,
            AwayTeamName = match.AwayTeam?.Name ?? "TBD",
            AwayTeamFlag = match.AwayTeam?.FlagUrl ?? string.Empty,
            HomeScore = match.Result?.HomeGoals,
            AwayScore = match.Result?.AwayGoals,
            HasPenalties = match.Result?.HasPenalties ?? false,
            PenaltiesHomeScore = match.Result?.PenaltiesHomeScore,
            PenaltiesAwayScore = match.Result?.PenaltiesAwayScore,
            Status = match.Status,
            KickoffTime = match.KickoffTime
        };
    }
}
