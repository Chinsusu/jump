using System.Globalization;
using System.Linq;
using System.Text;
using ShadowFox.Core.Models;

namespace ShadowFox.Infrastructure.Cef;

public static class SpoofingScriptBuilder
{
    /// <summary>
    /// Build full spoofing script (Ultra stealth 2025).
    /// Output is intentionally verbose for readability; minify at inject-time if desired.
    /// </summary>
    public static string BuildSpoofingScript(Fingerprint fingerprint)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ShadowFox Ultra Stealth Script - 2025");
        sb.AppendLine("(function() {");
        sb.AppendLine("  'use strict';");
        sb.AppendLine("  if (window.__shadowFoxInjected) return;");
        sb.AppendLine("  window.__shadowFoxInjected = true;");

        AppendNavigatorSpoof(sb, fingerprint);
        AppendChromeRuntime(sb);
        AppendCanvasSpoof(sb, fingerprint);
        AppendWebGlSpoof(sb, fingerprint);
        AppendAudioSpoof(sb, fingerprint);
        AppendFontsSpoof(sb, fingerprint);
        AppendScreenAndWindow(sb, fingerprint);
        AppendTimezoneAndLocale(sb, fingerprint);

        sb.AppendLine("})();");
        return sb.ToString();
    }

    private static void AppendNavigatorSpoof(StringBuilder sb, Fingerprint fingerprint)
    {
        var userAgent = JsString(fingerprint.UserAgent);
        var appVersion = JsString(fingerprint.UserAgent.Split(' ').FirstOrDefault() ?? fingerprint.UserAgent);
        var platform = JsString(fingerprint.Platform);
        var language = JsString(fingerprint.Locale);
        var languagesArray = string.Join(", ", fingerprint.Languages.Select(l => $"\"{JsString(l)}\""));

        sb.AppendLine("  // === Navigator & window properties ===");
        sb.AppendLine($"  Object.defineProperty(navigator, 'userAgent', {{ value: '{userAgent}', configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(navigator, 'appVersion', {{ value: '{appVersion}', configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(navigator, 'platform', {{ value: '{platform}', configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(navigator, 'language', {{ value: '{language}', configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(navigator, 'languages', {{ value: [{languagesArray}], configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(navigator, 'hardwareConcurrency', {{ value: {fingerprint.HardwareConcurrency}, configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(navigator, 'deviceMemory', {{ value: {fingerprint.DeviceMemory}, configurable: true }});");
        sb.AppendLine("  Object.defineProperty(navigator, 'maxTouchPoints', { value: 0, configurable: true });");

        sb.AppendLine("  Object.defineProperty(navigator, 'plugins', {");
        sb.AppendLine("    value: Object.setPrototypeOf([");
        sb.AppendLine("      { name: 'Chrome PDF Plugin', filename: 'mhjfbmdgcfjbbpaeojofohoefgiehjai', description: 'Portable Document Format' },");
        sb.AppendLine("      { name: 'Chrome PDF Viewer', filename: 'internal-pdf-viewer' },");
        sb.AppendLine("      { name: 'Native Client', filename: 'internal-nacl-plugin' }");
        sb.AppendLine("    ], NavigatorPlugins.prototype),");
        sb.AppendLine("    configurable: true");
        sb.AppendLine("  });");

        // WebRTC block
        sb.AppendLine("  (function blockWebRTC() {");
        sb.AppendLine("    const originalRTCPeerConnection = window.RTCPeerConnection || window.webkitRTCPeerConnection;");
        sb.AppendLine("    if (originalRTCPeerConnection) {");
        sb.AppendLine("      window.RTCPeerConnection = function() { return null; };");
        sb.AppendLine("      window.webkitRTCPeerConnection = window.RTCPeerConnection;");
        sb.AppendLine("    }");
        sb.AppendLine("    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {");
        sb.AppendLine("      Object.defineProperty(navigator.mediaDevices, 'getUserMedia', { value: () => Promise.reject(new Error('Permission denied')), configurable: true });");
        sb.AppendLine("    }");
        sb.AppendLine("  })();");
    }

    private static void AppendChromeRuntime(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("  // === Chrome runtime spoof ===");
        sb.AppendLine("  if (!window.chrome) {");
        sb.AppendLine("    window.chrome = { runtime: {}, loadTimes: function(){}, csi: function(){}, app: {} };");
        sb.AppendLine("  }");
    }

    private static void AppendCanvasSpoof(StringBuilder sb, Fingerprint fingerprint)
    {
        var noise = Invariant(fingerprint.CanvasNoiseLevel);
        sb.AppendLine();
        sb.AppendLine("  // === Canvas fingerprint spoof ===");
        sb.AppendLine("  function addCanvasNoise(ctx) {");
        sb.AppendLine("    const imageData = ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);");
        sb.AppendLine("    const pixels = imageData.data;");
        sb.AppendLine($"    const noise = {noise};");
        sb.AppendLine("    for (let i = 0; i < pixels.length; i += 4) {");
        sb.AppendLine("      if (Math.random() < noise) {");
        sb.AppendLine("        pixels[i] += Math.floor(Math.random() * 9 - 4);");
        sb.AppendLine("        pixels[i + 1] += Math.floor(Math.random() * 9 - 4);");
        sb.AppendLine("        pixels[i + 2] += Math.floor(Math.random() * 9 - 4);");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("    ctx.putImageData(imageData, 0, 0);");
        sb.AppendLine("  }");

        sb.AppendLine("  const __originalGetContext = HTMLCanvasElement.prototype.getContext;");
        sb.AppendLine("  HTMLCanvasElement.prototype.getContext = function(type, options) {");
        sb.AppendLine("    const ctx = __originalGetContext.call(this, type, options);");
        sb.AppendLine("    if (type === '2d' && ctx) {");
        sb.AppendLine("      const _fill = ctx.fillText;");
        sb.AppendLine("      ctx.fillText = function() { addCanvasNoise(this); return _fill.apply(this, arguments); };");
        sb.AppendLine("      const _stroke = ctx.strokeText;");
        sb.AppendLine("      ctx.strokeText = function() { addCanvasNoise(this); return _stroke.apply(this, arguments); };");
        sb.AppendLine("    }");
        sb.AppendLine("    return ctx;");
        sb.AppendLine("  };");

        sb.AppendLine("  if (window.OffscreenCanvas) {");
        sb.AppendLine("    const __originalOffscreen = OffscreenCanvas.prototype.getContext;");
        sb.AppendLine("    OffscreenCanvas.prototype.getContext = function(type, options) {");
        sb.AppendLine("      const ctx = __originalOffscreen.call(this, type, options);");
        sb.AppendLine("      if (type === '2d' && ctx) addCanvasNoise(ctx);");
        sb.AppendLine("      return ctx;");
        sb.AppendLine("    };");
        sb.AppendLine("  }");
    }

    private static void AppendWebGlSpoof(StringBuilder sb, Fingerprint fingerprint)
    {
        var vendor = JsString(fingerprint.WebGlUnmaskedVendor);
        var renderer = JsString(fingerprint.WebGlUnmaskedRenderer);

        sb.AppendLine();
        sb.AppendLine("  // === WebGL spoof ===");
        sb.AppendLine("  const __getParameter = WebGLRenderingContext.prototype.getParameter;");
        sb.AppendLine("  WebGLRenderingContext.prototype.getParameter = function(parameter) {");
        sb.AppendLine($"    if (parameter === 37445) return '{vendor}';"); // UNMASKED_VENDOR_WEBGL
        sb.AppendLine($"    if (parameter === 37446) return '{renderer}';"); // UNMASKED_RENDERER_WEBGL
        sb.AppendLine("    return __getParameter.call(this, parameter);");
        sb.AppendLine("  };");

        sb.AppendLine("  if ('WebGL2RenderingContext' in window) {");
        sb.AppendLine("    WebGL2RenderingContext.prototype.getParameter = WebGLRenderingContext.prototype.getParameter;");
        sb.AppendLine("  }");
    }

    private static void AppendAudioSpoof(StringBuilder sb, Fingerprint fingerprint)
    {
        var noise = Invariant(fingerprint.AudioNoiseLevel);
        sb.AppendLine();
        sb.AppendLine("  // === AudioContext spoof ===");
        sb.AppendLine("  (function patchAudio() {");
        sb.AppendLine("    const OriginalCtx = window.AudioContext || window.webkitAudioContext;");
        sb.AppendLine("    if (!OriginalCtx) return;");
        sb.AppendLine("    window.AudioContext = function() {");
        sb.AppendLine("      const ctx = new OriginalCtx();");
        sb.AppendLine("      const originalDecode = ctx.decodeAudioData;");
        sb.AppendLine("      ctx.decodeAudioData = function() {");
        sb.AppendLine("        const args = arguments;");
        sb.AppendLine("        const promise = originalDecode.apply(this, args);");
        sb.AppendLine("        return promise.then((buffer) => {");
        sb.AppendLine("          const channelData = buffer.getChannelData(0);");
        sb.AppendLine($"          for (let i = 0; i < channelData.length; i++) channelData[i] += (Math.random() * {noise}) - ({noise}/2);");
        sb.AppendLine("          return buffer;");
        sb.AppendLine("        });");
        sb.AppendLine("      };");
        sb.AppendLine("      return ctx;");
        sb.AppendLine("    };");
        sb.AppendLine("    window.webkitAudioContext = window.AudioContext;");
        sb.AppendLine("  })();");
    }

    private static void AppendFontsSpoof(StringBuilder sb, Fingerprint fingerprint)
    {
        var fontList = fingerprint.FontList.Select(JsString).ToArray();
        var fontArrayLiteral = string.Join(", ", fontList.Select(f => $"'{f}'"));

        sb.AppendLine();
        sb.AppendLine("  // === Fonts spoof ===");
        sb.AppendLine("  (function patchFonts() {");
        sb.AppendLine("    const style = document.createElement('style');");
        sb.AppendLine($"    const fonts = [{fontArrayLiteral}];");
        sb.AppendLine("    style.innerHTML = fonts.map(f => `@font-face { font-family: '${f}'; src: local('${f}'); }`).join('');");
        sb.AppendLine("    document.head.appendChild(style);");
        sb.AppendLine("    if (navigator.fonts && navigator.fonts.query) {");
        sb.AppendLine("      const originalQuery = navigator.fonts.query;");
        sb.AppendLine("      navigator.fonts.query = () => Promise.resolve(fonts.map(f => ({ family: f })));");
        sb.AppendLine("    }");
        sb.AppendLine("  })();");
    }

    private static void AppendScreenAndWindow(StringBuilder sb, Fingerprint fingerprint)
    {
        sb.AppendLine();
        sb.AppendLine("  // === Screen & window spoof ===");
        sb.AppendLine($"  Object.defineProperty(screen, 'width', {{ value: {fingerprint.ScreenWidth}, configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(screen, 'height', {{ value: {fingerprint.ScreenHeight}, configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(screen, 'availWidth', {{ value: {fingerprint.ScreenWidth}, configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(screen, 'availHeight', {{ value: {fingerprint.ScreenHeight - 80}, configurable: true }});");
        sb.AppendLine("  Object.defineProperty(screen, 'pixelDepth', { value: 24, configurable: true });");
        sb.AppendLine("  Object.defineProperty(screen, 'colorDepth', { value: 24, configurable: true });");

        sb.AppendLine($"  Object.defineProperty(window, 'outerWidth', {{ value: {fingerprint.ScreenWidth}, configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(window, 'outerHeight', {{ value: {fingerprint.ScreenHeight}, configurable: true }});");
        sb.AppendLine($"  Object.defineProperty(window, 'devicePixelRatio', {{ value: {Invariant(fingerprint.DevicePixelRatio)}, configurable: true }});");
    }

    private static void AppendTimezoneAndLocale(StringBuilder sb, Fingerprint fingerprint)
    {
        var locale = JsString(fingerprint.Locale);
        var tz = JsString(fingerprint.Timezone);

        sb.AppendLine();
        sb.AppendLine("  // === Timezone & locale spoof ===");
        sb.AppendLine($"  const __shadowTz = '{tz}';");
        sb.AppendLine($"  const __shadowLocale = '{locale}';");

        sb.AppendLine("  const __toLocaleString = Date.prototype.toLocaleString;");
        sb.AppendLine("  Date.prototype.toLocaleString = function(locales, options) {");
        sb.AppendLine("    const opt = Object.assign({}, options || {}, { timeZone: __shadowTz });");
        sb.AppendLine("    return __toLocaleString.call(this, __shadowLocale, opt);");
        sb.AppendLine("  };");

        sb.AppendLine("  const __DateTimeFormat = Intl.DateTimeFormat;");
        sb.AppendLine("  Intl.DateTimeFormat = function(locales, options) {");
        sb.AppendLine("    const merged = Object.assign({}, options || {}, { timeZone: __shadowTz });");
        sb.AppendLine("    const formatter = new __DateTimeFormat(__shadowLocale, merged);");
        sb.AppendLine("    return formatter;");
        sb.AppendLine("  };");

        sb.AppendLine("  Intl.DateTimeFormat.prototype.resolvedOptions = function() {");
        sb.AppendLine("    return { timeZone: __shadowTz, locale: __shadowLocale };");
        sb.AppendLine("  };");
    }

    private static string JsString(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);

    private static string Invariant(double value) => value.ToString(CultureInfo.InvariantCulture);
}
