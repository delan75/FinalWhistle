using System.Net;
using System.Text;
using FinalWhistle.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinalWhistle.Tests.Services;

public class CountryMetadataServiceTests
{
    [Fact]
    public async Task GetCountryProfileAsync_ReturnsCountryForCommonName()
    {
        var handler = new StubHttpMessageHandler("""
            [
              {
                "name": { "common": "Brazil", "official": "Federative Republic of Brazil" },
                "capital": ["Brasilia"],
                "region": "Americas",
                "subregion": "South America",
                "population": 213421037,
                "timezones": ["UTC-03:00"],
                "maps": {
                  "googleMaps": "https://maps.example/brazil",
                  "openStreetMaps": "https://osm.example/brazil"
                },
                "flags": {
                  "png": "https://flagcdn.com/w320/br.png",
                  "svg": "https://flagcdn.com/br.svg"
                },
                "cca2": "BR",
                "altSpellings": ["BR", "Brasil"]
              }
            ]
            """);
        var service = CreateService(handler);

        var result = await service.GetCountryProfileAsync("Brazil");

        Assert.NotNull(result);
        Assert.Equal("Brazil", result!.CommonName);
        Assert.Equal("Brasilia", result.Capital);
        Assert.Equal("https://flagcdn.com/br.svg", result.FlagSvgUrl);
    }

    [Fact]
    public async Task GetCountryProfileAsync_UsesAliasesAndAltSpellings()
    {
        var handler = new StubHttpMessageHandler("""
            [
              {
                "name": { "common": "United States", "official": "United States of America" },
                "capital": ["Washington, D.C."],
                "region": "Americas",
                "subregion": "North America",
                "population": 331893745,
                "timezones": ["UTC-05:00"],
                "maps": {
                  "googleMaps": "https://maps.example/us",
                  "openStreetMaps": "https://osm.example/us"
                },
                "flags": {
                  "png": "https://flagcdn.com/w320/us.png",
                  "svg": "https://flagcdn.com/us.svg"
                },
                "cca2": "US",
                "altSpellings": ["US", "USA", "U.S.A."]
              },
              {
                "name": { "common": "South Korea", "official": "Republic of Korea" },
                "capital": ["Seoul"],
                "region": "Asia",
                "subregion": "Eastern Asia",
                "population": 51780579,
                "timezones": ["UTC+09:00"],
                "maps": {
                  "googleMaps": "https://maps.example/kr",
                  "openStreetMaps": "https://osm.example/kr"
                },
                "flags": {
                  "png": "https://flagcdn.com/w320/kr.png",
                  "svg": "https://flagcdn.com/kr.svg"
                },
                "cca2": "KR",
                "altSpellings": ["KR", "Republic of Korea"]
              }
            ]
            """);
        var service = CreateService(handler);

        var usa = await service.GetCountryProfileAsync("USA");
        var korea = await service.GetCountryProfileAsync("Korea Republic");

        Assert.NotNull(usa);
        Assert.Equal("United States", usa!.CommonName);
        Assert.NotNull(korea);
        Assert.Equal("South Korea", korea!.CommonName);
    }

    [Fact]
    public async Task GetCountryProfileAsync_CachesCountryDataset()
    {
        var handler = new StubHttpMessageHandler("""
            [
              {
                "name": { "common": "Brazil", "official": "Federative Republic of Brazil" },
                "capital": ["Brasilia"],
                "region": "Americas",
                "subregion": "South America",
                "population": 213421037,
                "timezones": ["UTC-03:00"],
                "maps": {
                  "googleMaps": "https://maps.example/brazil",
                  "openStreetMaps": "https://osm.example/brazil"
                },
                "flags": {
                  "png": "https://flagcdn.com/w320/br.png",
                  "svg": "https://flagcdn.com/br.svg"
                },
                "cca2": "BR",
                "altSpellings": ["BR", "Brasil"]
              }
            ]
            """);
        var service = CreateService(handler);

        _ = await service.GetCountryProfileAsync("Brazil");
        _ = await service.GetCountryProfileAsync("Brasil");

        Assert.Equal(1, handler.CallCount);
    }

    private static RestCountriesCountryMetadataService CreateService(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://restcountries.com/v3.1/")
        };

        return new RestCountriesCountryMetadataService(
            httpClient,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<RestCountriesCountryMetadataService>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _content;

        public StubHttpMessageHandler(string content)
        {
            _content = content;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
        }
    }
}
