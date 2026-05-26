using Microsoft.AspNetCore.Mvc;
using Shared.Model;
using TermNet.Logic;

namespace TermNet.Controllers;

[ApiController]
[Route("api")]
public class SynonymController : ControllerBase
{
    private readonly TermNetRegistry _registry;
    private readonly ILogger<SynonymController> _logger;

    public SynonymController(TermNetRegistry registry, ILogger<SynonymController> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    [HttpGet("synonyms/{word}")]
    public List<SynonymEntry> GetSynonyms(string word, [FromQuery] string? nets = null)
    {
        var netNames = nets?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var result = _registry.GetSynonyms(word, netNames);
        _logger.LogInformation("Synonym lookup: word={Word} nets={Nets} found={Count}", word, nets ?? "all", result.Count);
        return result;
    }

    [HttpGet("termnets")]
    public List<string> GetTermNets() => _registry.GetNames();
}
