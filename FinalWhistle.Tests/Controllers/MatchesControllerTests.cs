using FinalWhistle.Controllers;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinalWhistle.Tests.Controllers;

public class MatchesControllerTests
{
    [Fact]
    public async Task Index_ReturnsMatchesOrderedByKickoff()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "Group A", DisplayOrder = 1 });
        context.Teams.AddRange(
            new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow },
            new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 1, CreatedAt = DateTime.UtcNow });
        context.Matches.AddRange(
            new Match
            {
                Id = 1,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                GroupId = 1,
                HomeTeamId = 1,
                AwayTeamId = 2,
                KickoffTime = DateTime.UtcNow.AddHours(2),
                Status = MatchStatus.Scheduled
            },
            new Match
            {
                Id = 2,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                GroupId = 1,
                HomeTeamId = 2,
                AwayTeamId = 1,
                KickoffTime = DateTime.UtcNow.AddHours(1),
                Status = MatchStatus.Scheduled
            });
        await context.SaveChangesAsync();

        var controller = new MatchesController(context);

        var result = await controller.Index(null, null, null);

        var view = Assert.IsType<ViewResult>(result);
        var matches = Assert.IsType<List<Match>>(view.Model);
        Assert.Equal(new[] { 2, 1 }, matches.Select(m => m.Id).ToArray());
    }

    [Fact]
    public async Task Index_AppliesGroupStageAndTeamFilters()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Groups.AddRange(
            new Group { Id = 1, TournamentId = 1, Name = "Group A", DisplayOrder = 1 },
            new Group { Id = 2, TournamentId = 1, Name = "Group B", DisplayOrder = 2 });
        context.Teams.AddRange(
            new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow },
            new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 1, CreatedAt = DateTime.UtcNow },
            new Team { Id = 3, Name = "Team C", Slug = "team-c", GroupId = 2, CreatedAt = DateTime.UtcNow });
        context.Matches.AddRange(
            new Match
            {
                Id = 1,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                GroupId = 1,
                HomeTeamId = 1,
                AwayTeamId = 2,
                KickoffTime = DateTime.UtcNow,
                Status = MatchStatus.Scheduled
            },
            new Match
            {
                Id = 2,
                TournamentId = 1,
                Stage = MatchStage.QuarterFinal,
                HomeTeamId = 1,
                AwayTeamId = 3,
                KickoffTime = DateTime.UtcNow.AddHours(1),
                Status = MatchStatus.Scheduled
            },
            new Match
            {
                Id = 3,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                GroupId = 2,
                HomeTeamId = 3,
                AwayTeamId = 2,
                KickoffTime = DateTime.UtcNow.AddHours(2),
                Status = MatchStatus.Scheduled
            });
        await context.SaveChangesAsync();

        var controller = new MatchesController(context);

        var result = await controller.Index(groupId: 1, stage: MatchStage.GroupStage, teamId: 1);

        var view = Assert.IsType<ViewResult>(result);
        var matches = Assert.IsType<List<Match>>(view.Model);
        Assert.Single(matches);
        Assert.Equal(1, matches[0].Id);
        Assert.Equal(1, controller.ViewBag.SelectedGroupId);
        Assert.Equal(MatchStage.GroupStage, controller.ViewBag.SelectedStage);
        Assert.Equal(1, controller.ViewBag.SelectedTeamId);
    }
}
