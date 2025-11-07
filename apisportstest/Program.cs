using RestSharp;
using System;
using System.IO;
using System.Text.Json;

public class Program
{
    public static void Main()
    {
        var client = new RestClient("https://site.api.espn.com");
        var request = new RestRequest("/apis/site/v2/sports/football/college-football/rankings", Method.Get);
        request.AddParameter("year", 2025);

        var response = client.ExecuteAsync(request).Result;

        if (response.IsSuccessful && response.Content != null)
        {
            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;

            if (root.TryGetProperty("rankings", out JsonElement rankings))
            {
                // Find the CFP Rankings entry
                foreach (var ranking in rankings.EnumerateArray())
                {
                    if (ranking.TryGetProperty("shortName", out JsonElement shortName) &&
                        shortName.GetString() == "CFP Rankings")
                    {
                        Console.WriteLine("=== CFP Rankings (FBS Teams Only) ===\n");

                        if (ranking.TryGetProperty("ranks", out JsonElement ranks))
                        {
                            foreach (var r in ranks.EnumerateArray())
                            {
                                try
                                {
                                    var current = r.GetProperty("current").GetInt32();
                                    var record = r.GetProperty("recordSummary").GetString();
                                    var team = r.GetProperty("team");
                                    var nickname = team.GetProperty("nickname").GetString();

                                    // Filter for FBS
                                    var group = team.GetProperty("groups");
                                    var parent = group.GetProperty("parent");
                                    var subdivision = parent.GetProperty("shortName").GetString();

                                    if (subdivision == "FBS")
                                    {
                                        Console.WriteLine($"{current}. {nickname} ({record})");
                                    }
                                }
                                catch
                                {
                                    // Some ranks may be missing team data
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No 'rankings' property found in the JSON.");
            }
        }
        else
        {
            Console.WriteLine($"Error: {response.ErrorMessage ?? "Unknown error"}");
        }
    }
}
