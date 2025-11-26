using RestSharp;
using System;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;

public class CfpTeam
{
    public int Rank { get; set; }
    public string Team { get; set; }
    public string Record { get; set; }
    public string LogoUrl { get; set; }
}

public class Program
{
    public static void Main()
    {
        int year = 2025;
        var teamsList = new List<CfpTeam>();

        var client = new RestClient("https://site.api.espn.com");
        var request = new RestRequest("/apis/site/v2/sports/football/college-football/rankings", Method.Get);
        request.AddParameter("year", year);

        var response = client.ExecuteAsync(request).Result;

        if (!response.IsSuccessful || response.Content == null)
        {
            Console.WriteLine($"Error: {response.ErrorMessage ?? "Unknown error"}");
            return;
        }

        using var doc = JsonDocument.Parse(response.Content);
        var root = doc.RootElement;

        if (!root.TryGetProperty("rankings", out var rankings))
        {
            Console.WriteLine("No rankings property found.");
            return;
        }

        foreach (var ranking in rankings.EnumerateArray())
        {
            if (!ranking.TryGetProperty("shortName", out var sn)) continue;
            if (sn.GetString() != "CFP Rankings") continue;

            if (!ranking.TryGetProperty("ranks", out var ranks)) continue;

            foreach (var r in ranks.EnumerateArray())
            {
                try
                {
                    int rank = r.GetProperty("current").GetInt32();
                    string record = r.GetProperty("recordSummary").GetString() ?? "";

                    var team = r.GetProperty("team");

                    string nickname = team.TryGetProperty("nickname", out var nn) ? nn.GetString() ?? "" : "";
                    string displayName = team.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? nickname : nickname;

                    // Optional: check FBS safely
                    string groupName = "";
                    if (team.TryGetProperty("groups", out var groups) &&
                        groups.TryGetProperty("parent", out var parent) &&
                        parent.TryGetProperty("shortName", out var gs))
                    {
                        groupName = gs.GetString() ?? "";
                    }

                    if (groupName != "FBS")
                        continue;

                    // ✅ Your confirmed logo path
                    string logoUrl = "";
                    if (team.TryGetProperty("logos", out var logos) &&
                        logos.ValueKind == JsonValueKind.Array &&
                        logos.GetArrayLength() > 0)
                    {
                        logoUrl = logos[0].GetProperty("href").GetString() ?? "";
                    }

                    teamsList.Add(new CfpTeam
                    {
                        Rank = rank,
                        Team = displayName,
                        Record = record,
                        LogoUrl = logoUrl
                    });
                }
                catch
                {
                    continue; // skip malformed entries
                }
            }
        }

        // Write JSON to file
        string outputJson = JsonSerializer.Serialize(teamsList, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText("cfp_rankings.json", outputJson);
        Console.WriteLine("Saved JSON → cfp_rankings.json");
    }
}
