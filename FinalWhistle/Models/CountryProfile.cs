namespace FinalWhistle.Models;

public class CountryProfile
{
    public string CommonName { get; set; } = string.Empty;
    public string OfficialName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Capital { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Subregion { get; set; } = string.Empty;
    public long Population { get; set; }
    public List<string> Timezones { get; set; } = new();
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public string OpenStreetMapsUrl { get; set; } = string.Empty;
    public string FlagSvgUrl { get; set; } = string.Empty;
    public string FlagPngUrl { get; set; } = string.Empty;
}
