namespace FinalWhistle.Application.Models;

public class GroupStanding
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<TeamStanding> Teams { get; set; } = new();
}
