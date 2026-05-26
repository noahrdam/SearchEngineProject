using Microsoft.AspNetCore.Mvc;
using Shared.Model;

namespace LoadBalancer.Controllers;

[ApiController]
[Route("api")]
public class LoadBalancerController : ControllerBase
{
    private static int next = 0;
    private static string[] instances = Environment.GetEnvironmentVariable("instances").Split(';');
    private readonly ILogger<LoadBalancerController> _logger;

    public LoadBalancerController(ILogger<LoadBalancerController> logger) => _logger = logger;

    [HttpGet]
    [Route("search/{query}/{maxAmount}/{offset}/{termNets?}")]
    public async Task<SearchResult> Search(string query, int maxAmount, int offset = 0, string? termNets = null)
    {
        HttpClient http = new HttpClient();
        var instance = instances[next];
        next = (next + 1) % instances.Length;
        _logger.LogInformation("Forwarding: query={Query} offset={Offset} -> {Instance}", query, offset, instance);
        var url = $"{instance}/api/search/{query}/{maxAmount}/{offset}";
        if (!string.IsNullOrEmpty(termNets)) url += $"/{termNets}";
        return await http.GetFromJsonAsync<SearchResult>(url);
    }

    [HttpGet]
    [Route("termnets")]
    public async Task<List<string>> GetTermNets()
    {
        HttpClient http = new HttpClient();
        var url = $"{instances[0]}/api/termnets";
        return await http.GetFromJsonAsync<List<string>>(url) ?? new();
    }

    [HttpGet]
    [Route("getfile")]
    public async Task<string> GetFile([FromQuery] int docId)
    {
        HttpClient http = new HttpClient();
        return await http.GetStringAsync($"{instances[0]}/api/getfile?docId={docId}");
    }
}