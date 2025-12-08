namespace ShadowFox.Core.Models;

public record Fingerprint
{
    public string UserAgent { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public int HardwareConcurrency { get; init; }
    public int DeviceMemory { get; init; }
    public int ScreenWidth { get; init; }
    public int ScreenHeight { get; init; }
    public double DevicePixelRatio { get; init; }
    public string Timezone { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public string[] Languages { get; init; } = Array.Empty<string>();
    public string WebGlUnmaskedVendor { get; init; } = string.Empty;
    public string WebGlUnmaskedRenderer { get; init; } = string.Empty;
    public double CanvasNoiseLevel { get; init; }
    public double AudioNoiseLevel { get; init; }
    public string[] FontList { get; init; } = Array.Empty<string>();
    public SpoofLevel SpoofLevel { get; init; } = SpoofLevel.Ultra;
}
