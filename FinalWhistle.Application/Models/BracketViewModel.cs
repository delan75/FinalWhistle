namespace FinalWhistle.Application.Models;

public class BracketViewModel
{
    public List<BracketMatch> RoundOf16 { get; set; } = new();
    public List<BracketMatch> QuarterFinals { get; set; } = new();
    public List<BracketMatch> SemiFinals { get; set; } = new();
    public BracketMatch? Final { get; set; }
    public BracketMatch? ThirdPlace { get; set; }
    public bool CanGenerateR16 { get; set; }
    public bool CanAdvanceToQF { get; set; }
    public bool CanAdvanceToSF { get; set; }
    public bool CanAdvanceToFinal { get; set; }
}
