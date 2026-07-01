using System.Net;
using System.Net.Http.Json;
using CV.Shared;

namespace CV.Web.Services;

public class CvService(HttpClient http)
{
    /// <summary>Fetches the CV document. Returns null if it has not been published yet (404).</summary>
    public async Task<CvDto?> GetCvAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("api/cv", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CvDto>(cancellationToken: ct);
    }
}
