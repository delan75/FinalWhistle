using FinalWhistle.Application.Services;
using FinalWhistle.Areas.Admin.Controllers;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using DomainMatch = FinalWhistle.Domain.Entities.Match;

namespace FinalWhistle.Tests.Controllers;

public class ResultsControllerTests
{
    [Fact]
    public async Task Enter_Get_RedirectsToIndex_WhenMatchHasTbdTeams()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Matches.Add(new DomainMatch
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.QuarterFinal,
            Status = MatchStatus.Scheduled
        });
        await context.SaveChangesAsync();

        var userManager = ControllerTestHelpers.CreateUserManagerMock();
        var predictionsService = new Mock<IPredictionsService>();
        var controller = new ResultsController(context, userManager.Object, predictionsService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext("admin1")
        };
        controller.TempData = ControllerTestHelpers.CreateTempData(controller.HttpContext);

        var result = await controller.Enter(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ResultsController.Index), redirect.ActionName);
        Assert.Equal("Cannot enter result for a match with TBD teams.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Enter_Post_CreatesResult_CompletesMatch_AndAwardsPoints()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Matches.Add(new DomainMatch
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            Status = MatchStatus.Live,
            IsLockedForPredictions = false
        });
        await context.SaveChangesAsync();

        var admin = new ApplicationUser { Id = "admin1", Email = "admin@example.com", UserName = "admin@example.com", CreatedAt = DateTime.UtcNow };
        var userManager = ControllerTestHelpers.CreateUserManagerMock();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(admin);
        var predictionsService = new Mock<IPredictionsService>();

        var controller = new ResultsController(context, userManager.Object, predictionsService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext("admin1")
        };
        controller.TempData = ControllerTestHelpers.CreateTempData(controller.HttpContext);

        var result = await controller.Enter(1, new MatchResult
        {
            HomeGoals = 3,
            AwayGoals = 2,
            HasExtraTime = false,
            HasPenalties = false
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ResultsController.Index), redirect.ActionName);

        var match = await context.Matches.FindAsync(1);
        var savedResult = await context.MatchResults.SingleAsync(r => r.MatchId == 1);

        Assert.NotNull(match);
        Assert.Equal(MatchStatus.Completed, match!.Status);
        Assert.True(match.IsLockedForPredictions);
        Assert.Equal(3, savedResult.HomeGoals);
        Assert.Equal(2, savedResult.AwayGoals);
        Assert.Equal("admin1", savedResult.EnteredByAdminId);
        Assert.Equal(1, savedResult.RevisionNumber);
        predictionsService.Verify(s => s.AwardPointsForMatchAsync(1), Times.Once);
    }
}
