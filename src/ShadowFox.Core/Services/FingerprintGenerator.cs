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

    // Spoof level specific configurations
    private static readonly int[] BasicResolutions = { 1920, 1366, 1280 };
    private static readonly int[] AdvancedResolutions = { 1920, 1366, 1536, 1440, 1280, 1600 };
    private static readonly int[] UltraResolutions = { 1920, 1366, 1536, 1440, 1280, 1600, 2560, 3440, 3840 };

    private static readonly double[] BasicDprs = { 1.0 };
    private static readonly double[] AdvancedDprs = { 1.0, 1.25, 1.5 };
    private static readonly double[] UltraDprs = { 1.0, 1.25, 1.5, 1.75, 2.0, 2.25, 2.5 };

    private static readonly int[] BasicConcurrency = { 4, 8 };
    private static readonly int[] AdvancedConcurrency = { 4, 6, 8, 12 };
    private static readonly int[] UltraConcurrency = { 4, 6, 8, 12, 16, 20, 24 };

    private static readonly int[] BasicMemory = { 8, 16 };
    private static readonly int[] AdvancedMemory = { 4, 8, 16, 32 };
    private static readonly int[] UltraMemory = { 4, 6, 8, 12, 16, 24, 32, 64 };

    private static readonly string[] BasicLocales = { "en-US", "en-GB" };
    private static readonly string[] AdvancedLocales = { "en-US", "en-GB", "de-DE", "fr-FR", "es-ES" };
    private static readonly string[] UltraLocales = { "en-US", "en-GB", "de-DE", "fr-FR", "es-ES", "zh-CN", "ja-JP", "pt-BR", "ru-RU", "it-IT" };

    private static readonly string[] BasicTimezones = { "America/New_York", "Europe/London" };
    private static readonly string[] AdvancedTimezones =
    {
        "America/New_York", "America/Chicago", "America/Los_Angeles",
        "Europe/London", "Europe/Paris", "Europe/Berlin"
    };
    private static readonly string[] UltraTimezones =
    {
        "America/New_York", "America/Chicago", "America/Los_Angeles", "America/Denver",
        "Europe/London", "Europe/Paris", "Europe/Berlin", "Europe/Madrid", "Europe/Rome",
        "Asia/Shanghai", "Asia/Tokyo", "Asia/Ho_Chi_Minh", "Asia/Kolkata", "Australia/Sydney"
    };

    private static readonly string[] BasicFonts = 
    { 
        "Arial", "Helvetica", "Times New Roman", "Segoe UI", "Calibri", "Verdana", "Tahoma", "Georgia",
        "Courier New", "Impact", "Comic Sans MS", "Trebuchet MS", "Palatino", "Garamond", "Bookman",
        "Avant Garde", "Helvetica Neue", "Lucida Grande", "Century Gothic", "Franklin Gothic Medium"
    };
    private static readonly string[] AdvancedFonts = 
    { 
        "Arial", "Helvetica", "Times New Roman", "Segoe UI", "Roboto", "Calibri", "Verdana", "Tahoma", 
        "Trebuchet MS", "Georgia", "Courier New", "Impact", "Comic Sans MS", "Palatino", "Garamond", 
        "Bookman", "Avant Garde", "Helvetica Neue", "Lucida Grande", "Century Gothic", "Franklin Gothic Medium",
        "Arial Black", "Arial Narrow", "Book Antiqua", "Candara", "Consolas", "Constantia", "Corbel",
        "Ebrima", "Gabriola", "Gadugi", "HoloLens MDL2 Assets", "Javanese Text", "Leelawadee UI",
        "Malgun Gothic", "Microsoft Himalaya", "Microsoft JhengHei", "Microsoft New Tai Lue", 
        "Microsoft PhagsPa", "Microsoft Sans Serif", "Microsoft Tai Le", "Microsoft YaHei", 
        "Microsoft Yi Baiti", "MingLiU-ExtB", "Mongolian Baiti", "MS Gothic", "MV Boli", "Myanmar Text",
        "Nirmala UI", "Palatino Linotype", "Segoe MDL2 Assets", "Segoe Print", "Segoe Script", 
        "Segoe UI Historic", "Segoe UI Symbol", "SimSun", "Sitka", "Sylfaen", "Symbol", "Yu Gothic"
    };
    private static readonly string[] UltraFonts = 
    { 
        "Arial", "Helvetica", "Times New Roman", "Segoe UI", "Roboto", "Calibri", "Verdana", "Tahoma", 
        "Trebuchet MS", "Georgia", "Courier New", "Impact", "Comic Sans MS", "Palatino", "Garamond", 
        "Bookman", "Avant Garde", "Helvetica Neue", "Lucida Grande", "Century Gothic", "Franklin Gothic Medium",
        "Arial Black", "Arial Narrow", "Book Antiqua", "Candara", "Consolas", "Constantia", "Corbel",
        "Ebrima", "Gabriola", "Gadugi", "HoloLens MDL2 Assets", "Javanese Text", "Leelawadee UI",
        "Malgun Gothic", "Microsoft Himalaya", "Microsoft JhengHei", "Microsoft New Tai Lue", 
        "Microsoft PhagsPa", "Microsoft Sans Serif", "Microsoft Tai Le", "Microsoft YaHei", 
        "Microsoft Yi Baiti", "MingLiU-ExtB", "Mongolian Baiti", "MS Gothic", "MV Boli", "Myanmar Text",
        "Nirmala UI", "Palatino Linotype", "Segoe MDL2 Assets", "Segoe Print", "Segoe Script", 
        "Segoe UI Historic", "Segoe UI Symbol", "SimSun", "Sitka", "Sylfaen", "Symbol", "Yu Gothic",
        "Agency FB", "Algerian", "Bauhaus 93", "Bell MT", "Berlin Sans FB", "Bernard MT Condensed",
        "Blackadder ITC", "Bodoni MT", "Britannic Bold", "Broadway", "Brush Script MT", "Californian FB",
        "Centaur", "Chiller", "Colonna MT", "Cooper Black", "Copperplate Gothic", "Curlz MT", "Edwardian Script ITC",
        "Elephant", "Engravers MT", "Felix Titling", "Forte", "Franklin Gothic Book", "Freestyle Script",
        "French Script MT", "Gigi", "Gloucester MT Extra Condensed", "Goudy Old Style", "Goudy Stout",
        "Haettenschweiler", "Harlow Solid Italic", "Harrington", "High Tower Text", "Jokerman", "Juice ITC",
        "Kristen ITC", "Kunstler Script", "Lucida Bright", "Lucida Calligraphy", "Lucida Fax", "Magneto",
        "Maiandra GD", "Old English Text MT", "Onyx", "Parchment", "Playbill", "Poor Richard", "Ravie",
        "Informal Roman", "Showcard Gothic", "Snap ITC", "Stencil", "Tempus Sans ITC", "Vivaldi", "Vladimir Script",
        "Wide Latin", "Wingdings", "Wingdings 2", "Wingdings 3"
    };

    public Fingerprint Generate(SpoofLevel level = SpoofLevel.Ultra)
    {
        var chromeVersion = Faker.Random.Int(126, 131); // Chromium 126-131 (2025)
        var baseUa = Faker.PickRandom(UserAgentBases);

        // Select appropriate arrays based on spoof level
        var (resolutions, dprs, concurrency, memory, locales, timezones, fonts) = level switch
        {
            SpoofLevel.Basic => (BasicResolutions, BasicDprs, BasicConcurrency, BasicMemory, BasicLocales, BasicTimezones, BasicFonts),
            SpoofLevel.Advanced => (AdvancedResolutions, AdvancedDprs, AdvancedConcurrency, AdvancedMemory, AdvancedLocales, AdvancedTimezones, AdvancedFonts),
            SpoofLevel.Ultra => (UltraResolutions, UltraDprs, UltraConcurrency, UltraMemory, UltraLocales, UltraTimezones, UltraFonts),
            _ => (UltraResolutions, UltraDprs, UltraConcurrency, UltraMemory, UltraLocales, UltraTimezones, UltraFonts)
        };

        var screenWidth = Faker.PickRandom(resolutions);
        var screenHeight = level switch
        {
            SpoofLevel.Basic => Faker.Random.Int(900, 1080),
            SpoofLevel.Advanced => Faker.Random.Int(900, 1200),
            SpoofLevel.Ultra => Faker.Random.Int(800, 1440),
            _ => Faker.Random.Int(900, 1440)
        };
        screenHeight -= screenHeight % 8; // align

        var selectedLocale = Faker.PickRandom(locales);
        var languageCount = level switch
        {
            SpoofLevel.Basic => 1,
            SpoofLevel.Advanced => Faker.Random.Int(1, 2),
            SpoofLevel.Ultra => Faker.Random.Int(1, 4),
            _ => Faker.Random.Int(1, 4)
        };

        var languages = new List<string> { selectedLocale };
        if (languageCount > 1)
        {
            languages.AddRange(Faker.Make(languageCount - 1, () => Faker.PickRandom(locales))
                .Where(l => l != selectedLocale)
                .Distinct());
        }

        // Generate WebGL vendors/renderers based on spoof level
        var (vendor, renderer) = GenerateWebGlInfo(level);

        // Generate noise levels based on spoof level
        var (canvasNoise, audioNoise) = GenerateNoiselevels(level);

        // Generate font list based on spoof level
        var fontCount = level switch
        {
            SpoofLevel.Basic => Faker.Random.Int(10, 20),
            SpoofLevel.Advanced => Faker.Random.Int(40, 80),
            SpoofLevel.Ultra => Faker.Random.Int(80, 140),
            _ => Faker.Random.Int(80, 140)
        };

        // Take a random subset of the available fonts
        var shuffledFonts = fonts.OrderBy(x => Faker.Random.Int()).ToArray();
        var finalFontList = shuffledFonts.Take(Math.Min(fontCount, fonts.Length)).ToArray();

        var fingerprint = new Fingerprint
        {
            UserAgent = string.Format(baseUa.UaPattern, chromeVersion),
            Platform = baseUa.Platform,
            HardwareConcurrency = Faker.PickRandom(concurrency),
            DeviceMemory = Faker.PickRandom(memory),
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            DevicePixelRatio = Faker.PickRandom(dprs),
            Timezone = Faker.PickRandom(timezones),
            Locale = selectedLocale,
            Languages = languages.ToArray(),
            WebGlUnmaskedVendor = vendor,
            WebGlUnmaskedRenderer = renderer,
            CanvasNoiseLevel = canvasNoise,
            AudioNoiseLevel = audioNoise,
            FontList = finalFontList,
            SpoofLevel = level
        };

        return fingerprint;
    }

    public Fingerprint Clone(Fingerprint source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // Use the fingerprint's own cloning method
        return source.CloneWithNewNoise();
    }

    private static (string vendor, string renderer) GenerateWebGlInfo(SpoofLevel level)
    {
        var vendors = level switch
        {
            SpoofLevel.Basic => new[] { "Intel Inc." },
            SpoofLevel.Advanced => new[] { "Intel Inc.", "NVIDIA Corporation", "AMD" },
            SpoofLevel.Ultra => new[] 
            { 
                "Intel Inc.", "NVIDIA Corporation", "AMD",
                "Google Inc. (Intel)", "Google Inc. (NVIDIA)", "Google Inc. (AMD)"
            },
            _ => new[] { "Intel Inc." }
        };

        var renderers = level switch
        {
            SpoofLevel.Basic => new[] { "Intel(R) Iris(R) Xe Graphics" },
            SpoofLevel.Advanced => new[] 
            { 
                "Intel(R) Iris(R) Xe Graphics",
                "NVIDIA GeForce RTX 3080",
                "AMD Radeon RX 6800"
            },
            SpoofLevel.Ultra => new[] 
            { 
                "Intel(R) Iris(R) Xe Graphics",
                "NVIDIA GeForce RTX 3080", "NVIDIA GeForce RTX 4080", "NVIDIA GeForce GTX 1650",
                "AMD Radeon RX 6800", "AMD Radeon RX 7800",
                "ANGLE (Intel, Intel(R) UHD Graphics Direct3D11 vs_5_0 ps_5_0)",
                "ANGLE (NVIDIA, NVIDIA GeForce GTX 1650 Direct3D11 vs_5_0 ps_5_0)"
            },
            _ => new[] { "Intel(R) Iris(R) Xe Graphics" }
        };

        return (Faker.PickRandom(vendors), Faker.PickRandom(renderers));
    }

    private static (double canvasNoise, double audioNoise) GenerateNoiselevels(SpoofLevel level)
    {
        return level switch
        {
            SpoofLevel.Basic => (0, 0),
            SpoofLevel.Advanced => (
                Faker.Random.Double(0.005, 0.025),
                Faker.Random.Double(0.0002, 0.001)
            ),
            SpoofLevel.Ultra => (
                Faker.Random.Double(0.02, 0.08),
                Faker.Random.Double(0.0001, 0.0015)
            ),
            _ => (0, 0)
        };
    }
}
