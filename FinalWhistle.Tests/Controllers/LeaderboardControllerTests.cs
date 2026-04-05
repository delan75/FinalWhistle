using FinalWhistle.Application.Models;
using FinalWhistle.Application.Services;
using FinalWhistle.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinalWhistle.Tests.Controllers;

public class LeaderboardControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewWithTopUsers()
    {
        var leaderboard = new List<LeaderboardEntry>
        {
            new() { Rank = 1, UserId = "user1", Username = "fan@example.com", TotalPoints = 10 }
        };
        var service = new Mock<ILeaderboardService>();
        service.Setup(s => s.GetTopUsersAsync(100)).ReturnsAsync(leaderboard);
        var controller = new LeaderboardController(service.Object);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(leaderboard, view.Model);
    }
}
