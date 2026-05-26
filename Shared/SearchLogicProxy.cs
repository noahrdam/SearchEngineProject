using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Shared.Model;

namespace Shared;

public class SearchLogicProxy
{
    private string serverEndPoint;
    private HttpClient mHttp;

    public SearchLogicProxy(string baseUrl = "http://localhost:5203", HttpClient? http = null)
    {
        serverEndPoint = baseUrl.TrimEnd('/') + "/api";
        mHttp = http ?? new HttpClient();
    }

    public async Task<SearchResult> Search(string[] query, int maxAmount, string[]? termNets = null, int offset = 0)
    {
        var q = String.Join(",", query);
        var url = $"{serverEndPoint}/search/{q}/{maxAmount}/{offset}";
        if (termNets != null && termNets.Length > 0)
            url += $"/{String.Join(",", termNets)}";
        return await mHttp.GetFromJsonAsync<SearchResult>(url);
    }

    public async Task<List<string>> GetTermNets()
    {
        try { return await mHttp.GetFromJsonAsync<List<string>>($"{serverEndPoint}/termnets") ?? new(); }
        catch { return new(); }
    }

    public async Task<string> GetFileContent(int docId)
    {
        return await mHttp.GetStringAsync($"{serverEndPoint}/getfile?docId={docId}");
    }
}
