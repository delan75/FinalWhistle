using FinalWhistle.Application.Services;
using FinalWhistle.Areas.Admin.Models;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using AdminGroupsController = FinalWhistle.Areas.Admin.Controllers.GroupsController;
using DomainMatch = FinalWhistle.Domain.Entities.Match;

namespace FinalWhistle.Tests.Controllers;

public class AdminGroupsControllerTests
{
    [Fact]
    public async Task Index_ReturnsNotFound_WhenTournamentDoesNotExist()
    {
        using var context = TestDbContextFactory.CreateContext();
        var controller = new AdminGroupsController(context, new StandingsService(context));

        var result = await controller.Index();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsViewModelWithProgressAndStandings()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Tournaments.Add(new Tournament
        {
            Id = 1,
            Name = "Test Cup",
            Season = 2026,
            Status = TournamentStatus.Live,
            CreatedAt = DateTime.UtcNow
        });
        context.Groups.Add(new Group
        {
            Id = 10,
            TournamentId = 1,
            Name = "A",
            DisplayOrder = 1
        });
        context.Teams.AddRange(
            new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 10, CreatedAt = DateTime.UtcNow },
            new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 10, CreatedAt = DateTime.UtcNow });
        context.Matches.AddRange(
            new DomainMatch
            {
                Id = 100,
                TournamentId = 1,
                GroupId = 10,
                Stage = MatchStage.GroupStage,
                HomeTeamId = 1,
                AwayTeamId = 2,
                KickoffTime = DateTime.UtcNow.AddDays(-2),
                Status = MatchStatus.Completed,
                IsLockedForPredictions = true
            },
            new DomainMatch
            {
                Id = 101,
                TournamentId = 1,
                GroupId = 10,
                Stage = MatchStage.GroupStage,
                HomeTeamId = 2,
                AwayTeamId = 1,
                KickoffTime = DateTime.UtcNow.AddDays(1),
                Status = MatchStatus.Scheduled,
                IsLockedForPredictions = false
            });
        context.MatchResults.Add(new MatchResult
        {
            MatchId = 100,
            HomeGoals = 2,
            AwayGoals = 0,
            EnteredAt = DateTime.UtcNow,
            RevisionNumber = 1
        });
        await context.SaveChangesAsync();

        var controller = new AdminGroupsController(context, new StandingsService(context));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminGroupsIndexViewModel>(view.Model);
        var group = Assert.Single(model.Groups);
        var leader = Assert.Single(group.Teams, team => team.Position == 1);

        Assert.Equal("Test Cup", model.TournamentName);
        Assert.Equal(2026, model.Season);
        Assert.Equal(1, model.CompletedGroupMatches);
        Assert.Equal(2, model.TotalGroupMatches);
        Assert.Equal(0, model.CompletedGroups);
        Assert.False(model.CanGenerateRoundOf16);
        Assert.False(model.KnockoutAlreadyGenerated);
        Assert.Equal("A", group.GroupName);
        Assert.Equal(1, group.CompletedMatches);
        Assert.Equal(2, group.TotalMatches);
        Assert.False(group.IsComplete);
        Assert.NotNull(group.NextKickoff);
        Assert.Equal("Team A", leader.TeamName);
        Assert.Equal(3, leader.Points);
    }
}
