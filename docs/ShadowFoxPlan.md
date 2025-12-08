# ShadowFox – Antidetect Browser Offline (Windows 10/11)

## 1. Tong quan & Muc tieu san pham
- Chay 100% offline tren Windows 10/11.
- Fake fingerprint sau o muc GoLogin/Incogniton/Dolphin Anty.
- Quan ly hang nghin profile, moi profile la mot "ca nhan ao" rieng (fingerprint, proxy, lich lam viec, hanh vi).
- Ho tro "train AI" (hanh vi giong nguoi) per profile.
- Code C# sach, de bao tri, moi file <= 500 dong, chia theo domain/object.

## 2. Tech stack
| Thanh phan           | Lua chon                             | Ly do |
|----------------------|--------------------------------------|-------|
| Language             | C# .NET 8.0 (LTS)                    | Hieu nang cao, manh Windows, ho tro AOT |
| UI Framework         | WPF (MVVM)                           | UI dep, multi-window tot |
| Browser Engine       | CefSharp 126+ (Chromium 126+)        | Nhung Chromium, inject spoofing JS/CSS |
| Database             | SQLite + EF Core 8                   | Offline, nhe, co the encrypt (SQLCipher) |
| Fake Data Gen        | Bogus + Faker custom                 | Sinh UA, resolution, timezone, fonts, hardware |
| Proxy Engine         | Titanium.Web.Proxy hoac custom       | HTTP/SOCKS5, per-profile, rotate, checker |
| AI/Behavior Engine   | Lua.NET hoac Roslyn Scripting + ML.NET (optional) | Hanh vi giong nguoi, scriptable |
| JSON/Config          | System.Text.Json                     | Nhanh, nho |
| Logging              | Serilog                              | Structured log, file + console |

## 3. Functional requirements
### Core features
1) Profile Management: create/clone/import/export/delete, random fingerprint (Basic/Advanced/Ultra), edit thu cong, tag/group/search/bulk.
2) Fingerprint spoofing: UA, canvas noise, WebGL vendor/renderer spoof, AudioContext noise, fonts override, WebRTC fake/block, HW concurrency, screen/dpr, timezone, languages, navigator.* spoof, chrome.runtime spoof, OffscreenCanvas/SVG patch.
3) Proxy Management: add list HTTP/SOCKS5/SSH, checker (speed/anonymity/geo), bind per profile hoac rotate theo list, schedule rotate (time-based hoac request-based).
4) Browser Launcher: moi profile mo mot cua so Chromium rieng, auto inject spoofing JS/CSS, cookie import/export, extension per profile.
5) AI/Human Behavior Engine: Personality profile (Aggressive/Normal/Cautious/Random), schedule lam viec, kieu di chuyen chuot (Bezier), toc do go + typo rate, scroll pattern, delay chuyen tab, read time, break/idle; mode rule-based (JSON), script mode (Lua/C#), optional ML.NET tu log.
6) Automation: script recorder (mouse+keyboard), task scheduler per profile, chay hang tram profile song song.

## 4. Architecture (Clean Architecture + MVVM)
```
ShadowFox/
├─ src/
│  ├─ ShadowFox.Core/           # Domain + Application logic (no UI)
│  │  ├─ Models/                # Profile, Fingerprint, Proxy, Personality
│  │  ├─ Services/              # ProfileService, FingerprintGenerator, ProxyService, BehaviorEngine
│  │  ├─ Interfaces/
│  │  ├─ Repositories/          # IProfileRepository, ...
│  │  └─ Common/                # Constants, Extensions, Result<T>
│  ├─ ShadowFox.Infrastructure/ # EF Core, SQLite, CefSharp config, File system
│  │  ├─ Data/                  # AppDbContext, Migrations
│  │  ├─ Repositories/
│  │  └─ Cef/                   # CefInitializer, SpoofingScriptBuilder
│  ├─ ShadowFox.UI/             # WPF (Views, ViewModels, Controls, Converters)
│  │  ├─ Views/                 # MainWindow.xaml, ProfileListView.xaml, BrowserWindow.xaml
│  │  ├─ ViewModels/            # MainViewModel, ProfileViewModel, BrowserViewModel
│  │  └─ Services/              # DialogService, NavigationService
│  └─ ShadowFox.Automation/     # Automation module (script, behavior, recorder)
├─ tests/                       # xUnit tests
├─ docs/
└─ ShadowFox.sln
```

## 5. Coding standards
- Naming: Project/Folder/Class/File PascalCase; Interface IPascalCase; Method/Property PascalCase; private field _camelCase; local camelCase; constants SCREAMING_SNAKE_CASE.
- File rules: 1 public class per file; <= 500 dong/file; class lon -> tach partial/child class; ViewModel <= 400 dong (tach command/service).
- Style: async/await, guard clauses, immutable (record/init) khi co the; DI = Microsoft.Extensions.DependencyInjection; regions chi dung trong ViewModel neu can.
- Structure: moi feature co folder rieng trong Services/ViewModels/Views (Profile/, Proxy/, Fingerprint/, Automation/).

## 6. Database schema (SQLite)
```sql
Profiles (
    Id INTEGER PRIMARY KEY,
    Name TEXT,
    Tags TEXT,
    "Group" TEXT,
    FingerprintId INTEGER,
    ProxyId INTEGER,
    PersonalityId INTEGER,
    Notes TEXT,
    CreatedAt TEXT
);

Fingerprints (
    Id INTEGER PRIMARY KEY,
    UserAgent TEXT,
    Platform TEXT,
    HardwareConcurrency INTEGER,
    DeviceMemory INTEGER,
    ScreenWidth INTEGER,
    ScreenHeight INTEGER,
    Timezone TEXT,
    Locale TEXT,
    WebGlVendor TEXT,
    WebGlRenderer TEXT,
    CanvasNoise INTEGER,
    AudioNoise REAL,
    FontList TEXT, -- json array
    SpoofLevel TEXT -- Basic | Advanced | Ultra
);

Proxies (
    Id INTEGER PRIMARY KEY,
    Host TEXT,
    Port INTEGER,
    Type TEXT, -- HTTP, SOCKS5
    Username TEXT,
    Password TEXT,
    Country TEXT,
    LastCheckedAt TEXT,
    IsWorking BOOLEAN
);

Personalities (
    Id INTEGER PRIMARY KEY,
    Name TEXT,
    TypingSpeedMin INTEGER,
    TypingSpeedMax INTEGER,
    TypoRate REAL,
    MouseStyle TEXT, -- bezier | linear
    ReadTimeMin INTEGER,
    ReadTimeMax INTEGER,
    WorkHours TEXT, -- JSON ["08:00-12:00", "13:00-17:00"]
    BreakPattern TEXT,
    BehaviorScript TEXT -- Lua or C# script
);
```

## 7. Development phases (uoc tinh cho 1 dev full-time)
| Phase | Noi dung | Thoi gian | Ket qua |
|-------|----------|-----------|---------|
| 1 | Setup project, WPF shell, MVVM base, DI, SQLite | 5-7 ngay | UI chinh chay duoc |
| 2 | Profile CRUD + Fingerprint Generator (Ultra) | 10-14 ngay | Tao profile fake sau |
| 3 | CefSharp integration + Spoofing JS injection | 10-12 ngay | Browser mo, spoof fingerprint |
| 4 | Proxy manager + per-profile proxy | 7-10 ngay | Proxy chay, checker |
| 5 | Personality & Behavior Engine | 12-16 ngay | Hanh vi giong nguoi |
| 6 | Automation recorder + scheduler | 10-14 ngay | Record/chay task |
| 7 | Polish, bulk actions, import/export, encrypt DB | 10 ngay | Ban 1 on dinh |

## 8. Spoofing JS injection (tom tat)
- File builder: `SpoofingScriptBuilder` tao string JS ~800 dong, inject vao moi page.
- Patch: canvas noise/hash, WebGL unmasked vendor/renderer override, AudioContext hash spoof, fonts override (defineProperty), navigator.* override, chrome.runtime spoof, WebRTC leak block, OffscreenCanvas/SVG patch.
