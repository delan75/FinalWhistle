using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using FinalWhistle.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FinalWhistle.Services;

public interface ICountryMetadataService
{
    Task<CountryProfile?> GetCountryProfileAsync(string teamName, CancellationToken cancellationToken = default);
}

public class RestCountriesCountryMetadataService : ICountryMetadataService
{
    private const string CountriesCacheKey = "country-metadata:restcountries:all";
    private static readonly IReadOnlyDictionary<string, string> TeamNameAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Korea Republic"] = "South Korea",
            ["IR Iran"] = "Iran",
            ["UAE"] = "United Arab Emirates"
        };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RestCountriesCountryMetadataService> _logger;

    public RestCountriesCountryMetadataService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<RestCountriesCountryMetadataService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CountryProfile?> GetCountryProfileAsync(string teamName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return null;
        }

        var countries = await GetCountriesAsync(cancellationToken);
        if (countries.Count == 0)
        {
            return null;
        }

        foreach (var candidate in GetSearchCandidates(teamName))
        {
            var match = countries.FirstOrDefault(country => MatchesCountry(country, candidate));
            if (match is not null)
            {
                return MapCountry(match);
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<RestCountryDto>> GetCountriesAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CountriesCacheKey, out IReadOnlyList<RestCountryDto>? cachedCountries) &&
            cachedCountries is not null)
        {
            return cachedCountries;
        }

        try
        {
            var countries = await _httpClient.GetFromJsonAsync<List<RestCountryDto>>(
                "all?fields=name,capital,region,subregion,population,timezones,maps,flags,cca2,altSpellings",
                cancellationToken);

            var result = (IReadOnlyList<RestCountryDto>)(countries ?? []);
            _cache.Set(
                CountriesCacheKey,
                result,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = result.Count > 0
                        ? TimeSpan.FromHours(12)
                        : TimeSpan.FromMinutes(15)
                });

            return result;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Unable to retrieve country metadata from REST Countries.");
            var fallback = Array.Empty<RestCountryDto>();
            _cache.Set(CountriesCacheKey, fallback, TimeSpan.FromMinutes(15));
            return fallback;
        }
    }

    private static IEnumerable<string> GetSearchCandidates(string teamName)
    {
        yield return teamName;

        if (TeamNameAliases.TryGetValue(teamName, out var alias) &&
            !string.Equals(alias, teamName, StringComparison.OrdinalIgnoreCase))
        {
            yield return alias;
        }
    }

    private static bool MatchesCountry(RestCountryDto country, string candidate)
    {
        var normalizedCandidate = Normalize(candidate);
        if (string.IsNullOrWhiteSpace(normalizedCandidate))
        {
            return false;
        }

        foreach (var countryName in GetCountryNames(country))
        {
            if (Normalize(countryName) == normalizedCandidate)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetCountryNames(RestCountryDto country)
    {
        if (!string.IsNullOrWhiteSpace(country.Name?.Common))
        {
            yield return country.Name.Common;
        }

        if (!string.IsNullOrWhiteSpace(country.Name?.Official))
        {
            yield return country.Name.Official;
        }

        if (country.AltSpellings is null)
        {
            yield break;
        }

        foreach (var altSpelling in country.AltSpellings.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            yield return altSpelling;
        }
    }

    private static CountryProfile MapCountry(RestCountryDto country)
    {
        return new CountryProfile
        {
            CommonName = country.Name?.Common ?? string.Empty,
            OfficialName = country.Name?.Official ?? string.Empty,
            CountryCode = country.CountryCode ?? string.Empty,
            Capital = country.Capital?.FirstOrDefault() ?? string.Empty,
            Region = country.Region ?? string.Empty,
            Subregion = country.Subregion ?? string.Empty,
            Population = country.Population,
            Timezones = country.Timezones?.Where(value => !string.IsNullOrWhiteSpace(value)).ToList() ?? new List<string>(),
            GoogleMapsUrl = country.Maps?.GoogleMaps ?? string.Empty,
            OpenStreetMapsUrl = country.Maps?.OpenStreetMaps ?? string.Empty,
            FlagSvgUrl = country.Flags?.Svg ?? string.Empty,
            FlagPngUrl = country.Flags?.Png ?? string.Empty
        };
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    private sealed class RestCountryDto
    {
        [JsonPropertyName("name")]
        public RestCountryNameDto? Name { get; set; }

        [JsonPropertyName("capital")]
        public List<string>? Capital { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("subregion")]
        public string? Subregion { get; set; }

        [JsonPropertyName("population")]
        public long Population { get; set; }

        [JsonPropertyName("timezones")]
        public List<string>? Timezones { get; set; }

        [JsonPropertyName("maps")]
        public RestCountryMapsDto? Maps { get; set; }

        [JsonPropertyName("flags")]
        public RestCountryFlagsDto? Flags { get; set; }

        [JsonPropertyName("cca2")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("altSpellings")]
        public List<string>? AltSpellings { get; set; }
    }

    private sealed class RestCountryNameDto
    {
        [JsonPropertyName("common")]
        public string? Common { get; set; }

        [JsonPropertyName("official")]
        public string? Official { get; set; }
    }

    private sealed class RestCountryMapsDto
    {
        [JsonPropertyName("googleMaps")]
        public string? GoogleMaps { get; set; }

        [JsonPropertyName("openStreetMaps")]
        public string? OpenStreetMaps { get; set; }
    }

    private sealed class RestCountryFlagsDto
    {
        [JsonPropertyName("png")]
        public string? Png { get; set; }

        [JsonPropertyName("svg")]
        public string? Svg { get; set; }
    }
}
