using System.Diagnostics;
using System.Text.Json;

namespace RouteBoard.Api.Tests;

public sealed class RestTranscriptTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Curl_observes_the_dispatch_contract_over_loopback()
    {
        fixture.UseKestrel(0);
        using var client = fixture.CreateClient();
        var origin = client.BaseAddress!;
        var dispatcher = fixture.Token();
        var viewer = fixture.Token(role: "viewer");
        var transcript = new List<string>();

        async Task<string> Request(string path, int expected, string? body = null, string? token = null,
            bool preflight = false, bool competingUpdate = false, bool stream = false)
        {
            var start = new ProcessStartInfo("curl")
            {
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            foreach (var arg in new[] { "-si", "--max-time", stream ? "2" : "10", "--noproxy", "*", new Uri(origin, path).AbsoluteUri })
                start.ArgumentList.Add(arg);
            if (body != null)
            {
                start.ArgumentList.Add("--json");
                start.ArgumentList.Add(body);
            }
            if (preflight)
            {
                foreach (var arg in new[] { "-X", "OPTIONS", "-H", "Origin: https://board.example.test",
                    "-H", "Access-Control-Request-Method: POST" })
                    start.ArgumentList.Add(arg);
            }
            if (token != null)
            {
                start.ArgumentList.Add("-H");
                start.ArgumentList.Add("@-");
            }
            using var process = Process.Start(start)!;
            if (token != null) await process.StandardInput.WriteLineAsync($"Authorization: Bearer {token}");
            process.StandardInput.Close();
            var raw = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            Assert.Equal(stream ? 28 : 0, process.ExitCode);
            lock (transcript) transcript.Add("curl " + string.Join(" ", start.ArgumentList) + "\n" + raw);
            var parts = raw.Replace("\r\n", "\n").Split("\n\n", 2);
            var status = int.Parse(parts[0].Split(' ')[1]);
            if (competingUpdate) Assert.Contains(status, new[] { 200, 409 });
            else Assert.Equal(expected, status);
            if (status >= 400)
            {
                Assert.Contains("application/problem+json", parts[0]);
                using var problem = JsonDocument.Parse(parts[1]);
                Assert.Equal(status, problem.RootElement.GetProperty("status").GetInt32());
                Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("title").GetString()));
            }
            return raw;
        }

        await Request("/healthz", 200);
        await Request("/api/system", 200);
        await Request("/WeatherForecast", 200);
        await Request("/api/catalog", 200);
        await Request("/api/positions", 200);
        await Request("/api/trips", 200);
        await Request("/api/trips?page=0", 400);
        await Request("/api/trips/999999", 404);
        await Request($"/api/routes/{fixture.RouteId}/estimate", 200);
        await Request("/api/routes/999999/estimate", 404);
        await Request("/api/events?after=-1", 400);
        await Request("/api/auth/authorize", 302);
        var preflight = await Request("/api/trips", 204, preflight: true);
        Assert.Contains("Access-Control-Allow-Origin: https://board.example.test", preflight);
        var create = JsonSerializer.Serialize(new { routeId = fixture.RouteId, vehicleId = fixture.VehicleId,
            scheduledDeparture = "2026-01-01T10:00:00Z" });
        await Request("/api/trips", 401, create);
        await Request("/api/trips", 403, create, viewer);
        await Request("/api/trips", 400, "{", dispatcher);
        var created = await Request("/api/trips", 201, create, dispatcher);
        var location = created.Split('\n').Single(line => line.StartsWith("Location: ", StringComparison.OrdinalIgnoreCase));
        var tripPath = new Uri(location["Location: ".Length..].Trim()).AbsolutePath;
        var detail = await Request(tripPath, 200);
        using var trip = JsonDocument.Parse(detail[(detail.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4)..]);
        var update = JsonSerializer.Serialize(new { version = trip.RootElement.GetProperty("version").GetInt32(),
            kind = "delay", delayMinutes = 4 });
        var competing = await Task.WhenAll(
            Request(tripPath + "/updates", 200, update, dispatcher, competingUpdate: true),
            Request(tripPath + "/updates", 200, update, dispatcher, competingUpdate: true));
        Assert.Single(competing, response => response.StartsWith("HTTP/1.1 200 "));
        Assert.Single(competing, response => response.StartsWith("HTTP/1.1 409 "));
        var events = await Request("/api/events", 200, stream: true);
        Assert.Contains("event: trip", events);
        var destination = Environment.GetEnvironmentVariable("ROUTEBOARD_HTTP_EVIDENCE");
        if (destination != null) await File.WriteAllLinesAsync(destination, transcript);
    }
}
