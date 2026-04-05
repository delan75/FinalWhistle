using FinalWhistle.Application.Models;
using FinalWhistle.Application.Services;
using FinalWhistle.Controllers;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinalWhistle.Tests.Controllers;

public class GroupsControllerTests
{
    [Fact]
    public async Task Index_ReturnsNotFound_WhenTournamentDoesNotExist()
    {
        using var context = TestDbContextFactory.CreateContext();
        var standingsService = new Mock<IStandingsService>();
        var controller = new GroupsController(context, standingsService.Object);

        var result = await controller.Index();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsViewWithStandings_WhenTournamentExists()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Tournaments.Add(new Tournament { Id = 1, Name = "Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var standings = new List<GroupStanding>
        {
            new() { GroupId = 1, GroupName = "Group A" }
        };
        var standingsService = new Mock<IStandingsService>();
        standingsService.Setup(s => s.GetAllGroupStandingsAsync(1)).ReturnsAsync(standings);
        var controller = new GroupsController(context, standingsService.Object);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(standings, view.Model);
    }
}
