namespace FinalWhistle.Domain.Entities;

public class Prediction
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PredictedHomeGoals { get; set; }
    public int PredictedAwayGoals { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int? PointsAwarded { get; set; }

    public Match Match { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
