using Shared.Model;
using Microsoft.AspNetCore.Mvc;
using SearchAPI.Logic;
using System.Net.Http.Json;
using System.Text.Json;
using StackExchange.Redis;
using ISearchDatabase = SearchAPI.Logic.IDatabase;

namespace SearchAPI.Controllers;

[ApiController]
[Route("api")]
public class SearchController : ControllerBase
{
    private static readonly ISearchDatabase mDatabase;
    private static readonly string? mTermNetUrl = Environment.GetEnvironmentVariable("TERMNET_URL");
    private static readonly HttpClient mHttp = new();
    private static readonly IConnectionMultiplexer? mRedis;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ILogger<SearchController> logger)
    {
        _logger = logger;
    }

    static SearchController()
    {
        ISearchDatabase a = new DatabasePostgres();
        var cs2 = Shared.Paths.POSTGRES_DATABASE_2;
        mDatabase = !string.IsNullOrEmpty(cs2)
            ? new CompositeDatabase(a, new DatabasePostgres(cs2))
            : a;

        var redisUrl = Shared.Paths.REDIS_URL;
        if (!string.IsNullOrEmpty(redisUrl))
        {
            try { mRedis = ConnectionMultiplexer.Connect(redisUrl); }
            catch { }
        }
    }

    [HttpGet("search/{query}/{maxAmount}/{offset}/{termNets?}")]
    public async Task<SearchResult> Search(string query, int maxAmount, int offset = 0, string? termNets = null)
    {
        var cacheKey = $"search:{query}:{maxAmount}:{offset}:{termNets}";

        if (mRedis != null)
        {
            var db = mRedis.GetDatabase();
            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                _logger.LogInformation("GET search: word={Query} nets={TermNets} [CACHE HIT]", query, termNets ?? "none");
                return JsonSerializer.Deserialize<SearchResult>((string)cached!)!;
            }
        }

        var words = query.Split(",");
        if (!string.IsNullOrEmpty(termNets) && !string.IsNullOrEmpty(mTermNetUrl))
            words = await ExpandWithSynonyms(words, termNets);

        var result = new SearchLogic(mDatabase).Search(words, maxAmount, offset);
        _logger.LogInformation("GET search: word={Query} offset={Offset} nets={TermNets} hits={Hits} [CACHE MISS]", query, offset, termNets ?? "none", result.NoOfHits);

        if (mRedis != null)
        {
            var db = mRedis.GetDatabase();
            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromMinutes(30));
        }

        return result;
    }

    [HttpGet("termnets")]
    public async Task<List<string>> GetTermNets()
    {
        if (string.IsNullOrEmpty(mTermNetUrl)) return new();
        try
        {
            var nets = await mHttp.GetFromJsonAsync<List<string>>($"{mTermNetUrl}/api/termnets") ?? new();
            _logger.LogInformation("GetTermNets: returned={Count}", nets.Count);
            return nets;
        }
        catch { return new(); }
    }

    [HttpGet("ping")]
    public string? Ping() => Environment.GetEnvironmentVariable("id");

    [HttpGet("getfile")]
    public IActionResult GetFile([FromQuery] int docId)
    {
        _logger.LogInformation("GetFile: docId={DocId}", docId);
        var content = mDatabase.GetFileContent(docId);
        if (content == null) return NotFound();
        return Ok(content);
    }

    private async Task<string[]> ExpandWithSynonyms(string[] words, string termNets)
    {
        var tasks = words.Select(async word =>
        {
            try
            {
                var url = $"{mTermNetUrl}/api/synonyms/{word}?nets={termNets}";
                var syns = await mHttp.GetFromJsonAsync<List<SynonymEntry>>(url);
                return syns?.Select(s => s.Word) ?? Enumerable.Empty<string>();
            }
            catch { return Enumerable.Empty<string>(); }
        });
        var results = await Task.WhenAll(tasks);
        return words.Concat(results.SelectMany(r => r)).Distinct().ToArray();
    }
}
