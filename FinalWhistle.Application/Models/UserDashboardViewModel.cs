using FinalWhistle.Domain.Entities;

namespace FinalWhistle.Application.Models;

public class UserDashboardViewModel
{
    public int TotalPoints { get; set; }
    public int Rank { get; set; }
    public int TotalPredictions { get; set; }
    public int ExactScores { get; set; }
    public int CorrectResults { get; set; }
    public int WrongPredictions { get; set; }
    public List<Prediction> RecentPredictions { get; set; } = new();
    public List<Prediction> UpcomingMatches { get; set; } = new();
}
