namespace FinalWhistle.Application.Models;

public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int PredictionsCount { get; set; }
    public int ExactScores { get; set; }
    public int CorrectResults { get; set; }
}
