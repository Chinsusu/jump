using Bogus;
using ShadowFox.Core.Models;

namespace ShadowFox.Core.Services;

public sealed class FingerprintGenerator
{
    private static readonly Faker Faker = new();

    // Dataset UA base (2025, StatCounter + internal sampling)
    private static readonly List<(string Os, string UaPattern, string Platform)> UserAgentBases = new()
    {
        ("Windows 10", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{0}.0.0.0 Safari/537.36", "Win64"),
        ("Windows 11", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{0}.0.0.0 Safari/537.36", "Win64"),
        ("macOS", "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_6_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{0}.0.0 Safari/537.36", "MacIntel")
    };

    private static readonly int[] CommonResolutions = { 1920, 1366, 1536, 1440, 1280, 1600, 2560 };
    private static readonly double[] CommonDprs = { 1.0, 1.25, 1.5, 1.75, 2.0 };
    private static readonly int[] Concurrency = { 4, 6, 8, 12, 16 };
    private static readonly int[] DeviceMemory = { 4, 6, 8, 12, 16, 32 };
    private static readonly string[] Locales = { "en-US", "en-GB", "de-DE", "fr-FR", "es-ES", "zh-CN", "ja-JP" };
    private static readonly string[] Timezones =
    {
        "America/New_York", "America/Chicago", "America/Los_Angeles",
        "Europe/London", "Europe/Paris", "Europe/Berlin", "Europe/Madrid",
        "Asia/Shanghai", "Asia/Tokyo", "Asia/Ho_Chi_Minh"
    };

    public Fingerprint Generate(SpoofLevel level = SpoofLevel.Ultra)
    {
        var chromeVersion = Faker.Random.Int(126, 131); // Chromium 126-131 (2025)
        var baseUa = Faker.PickRandom(UserAgentBases);

        var screenWidth = Faker.PickRandom(CommonResolutions);
        var screenHeight = Faker.Random.Int(900, 1440);
        screenHeight -= screenHeight % 8; // align

        var languages = Faker.Make(Faker.Random.Int(1, 4), () => Faker.PickRandom(Locales))
            .Distinct()
            .ToArray();

        var fingerprint = new Fingerprint
        {
            UserAgent = string.Format(baseUa.UaPattern, chromeVersion),
            Platform = baseUa.Platform,
            HardwareConcurrency = Faker.PickRandom(Concurrency),
            DeviceMemory = Faker.PickRandom(DeviceMemory),
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            DevicePixelRatio = Faker.PickRandom(CommonDprs),
            Timezone = Faker.PickRandom(Timezones),
            Locale = Faker.PickRandom(Locales),
            Languages = languages,
            WebGlUnmaskedVendor = Faker.PickRandom(
                "Intel Inc.", "NVIDIA Corporation", "AMD",
                "Google Inc. (Intel)", "Google Inc. (NVIDIA)", "Google Inc. (AMD)"),
            WebGlUnmaskedRenderer = Faker.PickRandom(
                "Intel(R) Iris(R) Xe Graphics",
                "NVIDIA GeForce RTX 3080",
                "AMD Radeon RX 6800",
                "ANGLE (Intel, Intel(R) UHD Graphics Direct3D11 vs_5_0 ps_5_0)",
                "ANGLE (NVIDIA, NVIDIA GeForce GTX 1650 Direct3D11 vs_5_0 ps_5_0)"),
            CanvasNoiseLevel = level == SpoofLevel.Ultra ? Faker.Random.Double(0.02, 0.08) : 0,
            AudioNoiseLevel = level == SpoofLevel.Ultra ? Faker.Random.Double(0.0001, 0.0015) : 0,
            FontList = Faker.Make(Faker.Random.Int(80, 140), () => Faker.PickRandom(
                    "Arial", "Helvetica", "Times New Roman", "Segoe UI", "Roboto", "Calibri", "Verdana",
                    "Tahoma", "Trebuchet MS", "Georgia", "Courier New", "Impact", "Comic Sans MS"))
                .Distinct()
                .ToArray()
        };

        return fingerprint with { SpoofLevel = level };
    }
}
