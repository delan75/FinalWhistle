using FinalWhistle.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinalWhistle.Controllers;

public class LeaderboardController : Controller
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    public async Task<IActionResult> Index()
    {
        var leaderboard = await _leaderboardService.GetTopUsersAsync(100);
        return View(leaderboard);
    }
}
