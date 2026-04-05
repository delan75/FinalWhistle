using FinalWhistle.Application.Services;
using FinalWhistle.Areas.Admin.Models;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AdminBracketController = FinalWhistle.Areas.Admin.Controllers.BracketController;
using DomainMatch = FinalWhistle.Domain.Entities.Match;

namespace FinalWhistle.Tests.Controllers;

public class AdminBracketControllerTests
{
    [Fact]
    public async Task Index_ReturnsNotFound_WhenTournamentDoesNotExist()
    {
        using var context = TestDbContextFactory.CreateContext();
        var bracketService = new Mock<IBracketService>();
        var controller = new AdminBracketController(context, bracketService.Object);

        var result = await controller.Index();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsViewModelWithReadinessSummary()
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
        context.Matches.AddRange(
            new DomainMatch
            {
                Id = 1,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                GroupId = 1,
                HomeTeamId = 1,
                AwayTeamId = 2,
                KickoffTime = DateTime.UtcNow.AddDays(-3),
                Status = MatchStatus.Completed,
                IsLockedForPredictions = true
            },
            new DomainMatch
            {
                Id = 2,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                GroupId = 1,
                HomeTeamId = 2,
                AwayTeamId = 1,
                KickoffTime = DateTime.UtcNow.AddDays(-2),
                Status = MatchStatus.Completed,
                IsLockedForPredictions = true
            });
        await context.SaveChangesAsync();

        var bracketMatches = new List<DomainMatch>
        {
            new()
            {
                Id = 10,
                TournamentId = 1,
                Stage = MatchStage.RoundOf16,
                HomeTeamId = 1,
                AwayTeamId = 2,
                HomeTeam = new Team { Id = 1, Name = "Team A", FlagUrl = "/flags/a.png" },
                AwayTeam = new Team { Id = 2, Name = "Team B", FlagUrl = "/flags/b.png" },
                Result = new MatchResult { HomeGoals = 2, AwayGoals = 1, RevisionNumber = 1 },
                KickoffTime = DateTime.UtcNow.AddDays(2),
                Status = MatchStatus.Completed
            }
        };

        var bracketService = new Mock<IBracketService>();
        bracketService.Setup(s => s.GetBracketMatchesAsync(1)).ReturnsAsync(bracketMatches);
        var controller = new AdminBracketController(context, bracketService.Object);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminBracketIndexViewModel>(view.Model);

        Assert.Equal("Test Cup", model.TournamentName);
        Assert.Equal(2, model.CompletedGroupMatches);
        Assert.Equal(2, model.TotalGroupMatches);
        Assert.True(model.AllGroupMatchesCompleted);
        Assert.Equal(1, model.CompletedRoundOf16Matches);
        Assert.False(model.Bracket.CanGenerateR16);
        Assert.Single(model.Bracket.RoundOf16);
    }

    [Fact]
    public async Task GenerateRoundOf16_SetsSuccessMessage_WhenGenerationSucceeds()
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
        await context.SaveChangesAsync();

        var bracketService = new Mock<IBracketService>();
        bracketService.Setup(s => s.GenerateRoundOf16Async(1)).ReturnsAsync(true);
        var controller = new AdminBracketController(context, bracketService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext("admin1")
        };
        controller.TempData = ControllerTestHelpers.CreateTempData(controller.HttpContext);

        var result = await controller.GenerateRoundOf16();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminBracketController.Index), redirect.ActionName);
        Assert.Equal("Round of 16 bracket generated successfully!", controller.TempData["Success"]);
        bracketService.Verify(s => s.GenerateRoundOf16Async(1), Times.Once);
    }

    [Fact]
    public async Task AdvanceWinners_SetsErrorMessage_WhenAdvanceFails()
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
        await context.SaveChangesAsync();

        var bracketService = new Mock<IBracketService>();
        bracketService.Setup(s => s.AdvanceWinnersAsync(MatchStage.RoundOf16, 1)).ReturnsAsync(false);
        var controller = new AdminBracketController(context, bracketService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext("admin1")
        };
        controller.TempData = ControllerTestHelpers.CreateTempData(controller.HttpContext);

        var result = await controller.AdvanceWinners(MatchStage.RoundOf16);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminBracketController.Index), redirect.ActionName);
        Assert.Equal("Failed to advance winners. Ensure all RoundOf16 matches are completed.", controller.TempData["Error"]);
        bracketService.Verify(s => s.AdvanceWinnersAsync(MatchStage.RoundOf16, 1), Times.Once);
    }
}
