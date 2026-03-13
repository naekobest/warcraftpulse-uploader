// Services/UploadService.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WarcraftPulseUploader.Parser;

namespace WarcraftPulseUploader.Services;

// UploadService is intentionally kept as a long-lived singleton in MainForm (_uploader field).
// HttpClient must NOT be created per-request — that causes socket exhaustion.
// MainForm recreates the UploadService only when the server URL changes (settings saved).
public sealed class UploadService
{
    private readonly HttpClient _http;

    public UploadService(string serverUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(serverUrl.TrimEnd('/') + '/') };
    }

    public async Task<UploadResult> UploadAsync(
        CombatLogData data,
        string apiToken,
        CancellationToken ct = default)
    {
        int[] backoffMs = [0, 1000, 2000];
        Exception? lastEx = null;
        HttpResponseMessage? response = null;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(backoffMs[attempt], ct);

            // HttpRequestMessage cannot be reused after SendAsync — re-create on each attempt
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/reports/upload-parsed");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
            request.Content = JsonContent.Create(data);

            try
            {
                response = await _http.SendAsync(request, ct);
                lastEx = null;
                break;
            }
            catch (HttpRequestException ex) { lastEx = ex; }
            catch (TaskCanceledException) { throw; }  // propagate cancellation
        }

        if (lastEx is not null)
            return UploadResult.Fail($"Network error after 3 attempts: {lastEx.Message}");

        if (response!.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return UploadResult.Ok(
                json.GetProperty("report_code").GetString()!,
                json.GetProperty("status_url").GetString()!
            );
        }

        // Parse Laravel validation errors (422) into a human-readable message
        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            try
            {
                var err = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                if (err.TryGetProperty("errors", out var errors))
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString() ?? ""))
                        .Where(s => !string.IsNullOrEmpty(s));
                    return UploadResult.Fail("Validation failed: " + string.Join("; ", messages));
                }
            }
            catch { /* fall through to generic error */ }
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return UploadResult.Fail("Invalid or expired API token. Check Settings → Access Tokens.");

        return UploadResult.Fail($"Server error {(int)response.StatusCode}.");
    }
}

public sealed class UploadResult
{
    public bool    Success    { get; private init; }
    public string? ReportCode { get; private init; }
    public string? StatusUrl  { get; private init; }
    public string? Error      { get; private init; }

    public static UploadResult Ok(string reportCode, string statusUrl) =>
        new() { Success = true, ReportCode = reportCode, StatusUrl = statusUrl };

    public static UploadResult Fail(string error) =>
        new() { Success = false, Error = error };
}
