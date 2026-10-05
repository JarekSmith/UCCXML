using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UCCXML;

public class Details
{
    public bool? Verbose { get; set; }
    public int? WaitTimeSeconds { get; set; }
    public string? OutputPath { get; set; }
    public ContactInfo ContactInfo;
    public OrganizationNames Filer;
    public required string AccountNumber { get; set; }
    public required string VersionNumber { get; set; }
    public required Dictionary<string, string> CollateralText;
    static readonly JsonSerializerOptions _options = new() { IncludeFields = true };

    static public Details BuildDetails(string path)
    {
        if (!File.Exists(path)) throw new Exception("Missing details.json");
        string? jsonText = File.ReadAllText(path);
        Details? details = JsonSerializer.Deserialize<Details>(jsonText, _options);
        if (details is not null) return details;
        throw new Exception("details.json is incorrectly shaped");
    }

    static public (string ApiKey, string SosKey) GetKeys()
    {
        string? ApiKey = Environment.GetEnvironmentVariable("CALICO_API_KEY");
        string? SosKey = Environment.GetEnvironmentVariable("CALICO_SOS_KEY");
        if (ApiKey is null || SosKey is null) throw new Exception("Environment variables 'CALICO_API_KEY' and 'CALICO_SOS_KEY' must be set.");
        return (ApiKey, SosKey);
    }

    public async Task WaitAndDisplayTimeRemainingAsync()
    {
        int seconds = WaitTimeSeconds ?? 3600; // Wait 1 hour by default;
        Console.WriteLine($"WaitTimeSeconds is null {WaitTimeSeconds is null}");
        var duration = TimeSpan.FromSeconds(seconds);
        var endTime = DateTime.UtcNow + duration;
        var endTimeLocal = DateTime.Now + duration;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (true)
        {
            var remaining = endTime - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) break;
            Console.Write($"\rDocuments will be ready at {endTimeLocal:hh\\:mm\\:ss} - Time remaining: {remaining:hh\\:mm\\:ss}  ");
            await timer.WaitForNextTickAsync();
        }
        Console.WriteLine("");
    }
}

public struct OrganizationNames(string name, string stAddress, string city, string state, string zipCode)
{
    public string OrganizationName = name;
    public string StAddress = stAddress;
    public string City = city;
    public string State = state;
    public string ZipCode = zipCode;
}

public struct ContactInfo
{
    public string PhoneNumber;
    public string Name;
    public string Email;
}