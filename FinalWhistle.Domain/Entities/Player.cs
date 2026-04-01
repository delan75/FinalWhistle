using FinalWhistle.Domain.Enums;

namespace FinalWhistle.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlayerPosition Position { get; set; }
    public int JerseyNumber { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int MinutesPlayed { get; set; }

    public Team Team { get; set; } = null!;
}
