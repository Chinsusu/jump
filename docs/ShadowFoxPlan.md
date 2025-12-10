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
1) Profile Management: ✅ create/clone/import/export/delete, ✅ random fingerprint (Basic/Advanced/Ultra), edit thu cong, tag/group/search/bulk.
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
| 1 | ✅ Setup project, WPF shell, MVVM base, DI, SQLite | 5-7 ngay | ✅ UI chinh chay duoc |
| 2 | ✅ Profile CRUD + Fingerprint Generator (Ultra) + Import/Export | 10-14 ngay | ✅ Tao profile fake sau, import/export JSON |
| 3 | CefSharp integration + Spoofing JS injection | 10-12 ngay | Browser mo, spoof fingerprint |
| 4 | Proxy manager + per-profile proxy | 7-10 ngay | Proxy chay, checker |
| 5 | Personality & Behavior Engine | 12-16 ngay | Hanh vi giong nguoi |
| 6 | Automation recorder + scheduler | 10-14 ngay | Record/chay task |
| 7 | Polish, bulk actions, import/export, encrypt DB | 10 ngay | Ban 1 on dinh |

## 8. Spoofing JS injection (tom tat)
- File builder: `SpoofingScriptBuilder` tao string JS ~800 dong, inject vao moi page.
- Patch: canvas noise/hash, WebGL unmasked vendor/renderer override, AudioContext hash spoof, fonts override (defineProperty), navigator.* override, chrome.runtime spoof, WebRTC leak block, OffscreenCanvas/SVG patch.

## 9. Execution checklist (thuc thi tung phan)
- Foundation: tao solution, cai DI, logging (Serilog), config options, EF Core migrations (SQLite), seed data.
- Core domain: Models (Profile/Fingerprint/Proxy/Personality), Result<T>, validation; ProfileService (CRUD, clone, import/export), ProxyService (add/checker/bind), FingerprintGenerator (Basic/Advanced/Ultra presets).
- Infrastructure: AppDbContext + repositories; SpoofingScriptBuilder (JS), CefInitializer (cache dir per profile, command line switches); Proxy engine (Titanium.Web.Proxy wrapper); config storage (JSON + SQLite).
- UI (WPF/MVVM): Main shell + navigation, Profile list/detail, Proxy list/detail, Fingerprint editor, Browser launcher window, settings; commands bound to services; theming + data templates.
- Behavior engine: Personality presets, rule-based executor (JSON), script host (Lua.NET or Roslyn), timers/scheduler per profile, mouse/typing/scroll simulators.
- Automation: script recorder (mouse/keyboard), task scheduler UI, queue runner (parallel with limits), log viewer.
- QA/telemetry: unit tests (xUnit) cho generator/services; integration tests for DB; manual test matrix for spoofing (canvas/WebGL/WebRTC/fonts/timezone/UA); performance check (profile startup time, memory per profile).

## 10. Sprint roadmap (de xuat 8 sprint, ~2 tuan/sprint)
- Sprint 1: Setup solution, DI, Serilog, config, SQLite + EF Core migrations, base models, Result<T>, basic Profile CRUD (no UI), unit tests.
- Sprint 2: WPF shell + MVVM base, navigation, Profile list/detail UI, Proxy list UI (stub), theming, basic validation binding.
- Sprint 3: FingerprintGenerator (Basic/Advanced/Ultra) + UI editor; persist fingerprints; export/import profile (JSON).
- Sprint 4: CefSharp integration, profile-isolated browser launch (cache dir per profile), inject SpoofingScriptBuilder (canvas/WebGL/AudioContext/navigator/chrome.runtime/WebRTC), basic extension loading.
- Sprint 5: Proxy engine (HTTP/SOCKS5), checker, bind per profile, rotate policy (time-based), UI for proxy binding/check status.
- Sprint 6: Behavior engine: Personality model + presets, rule-based executor (JSON), mouse/typing/scroll simulation, per-profile schedule; log behavior events.
- Sprint 7: Automation: script recorder (mouse/keyboard), task scheduler per profile, runner with concurrency cap, UI to manage tasks; cookie import/export.
- Sprint 8: Hardening & polish: bulk actions, group/tag filters, DB encryption option, extension management UX, performance tuning, spoofing regression tests, release packaging.

## 11. Issue backlog (granular tasks)
- Foundation
  - Create solution + projects (Core/Infrastructure/UI/Automation/tests), wire DI container, Serilog config, options pattern.
  - Add EF Core + SQLite; create AppDbContext, initial migration, seed sample profiles/proxies.
  - Implement Result<T>, guard utilities, validation helpers.
- Profile & Fingerprint
  - Define domain models (Profile/Fingerprint/Proxy/Personality) + DTOs.
  - Implement IProfileRepository (EF) + ProfileService (CRUD, clone, import/export JSON).
  - Implement FingerprintGenerator with presets (Basic/Advanced/Ultra) + tests for distribution bounds.
  - Add JSON import/export for fingerprint sets; validation rules.
- Proxy
  - Implement IProxyRepository + ProxyService (add/update/delete).
  - Build ProxyChecker (HTTP/SOCKS5) with timeout/retries; record latency/anonymity/country.
  - Proxy rotation policy (time-based/request-based) and binding per profile.
- Behavior Engine
  - Define Personality model + presets; schedule parser (WorkHours JSON).
  - Implement rule-based executor (JSON) for mouse/typing/scroll delays.
  - Script host (Lua.NET or Roslyn) with sandbox; expose API (mouseMove, typeText, wait, click).
  - Simulators: mouse Bezier path generator, typing speed/typo injector, scroll pattern.
- Automation
  - Recorder: capture mouse/keyboard events; export to script format.
  - Scheduler: per-profile task queue, concurrency limiter, retry policy.
  - Runner UI: start/stop/pause tasks, progress + log view.
- CefSharp & Spoofing
  - CefInitializer: cache dir per profile, command-line switches (disable-features, proxy).
  - SpoofingScriptBuilder: compose JS (canvas/WebGL/AudioContext/navigator/chrome.runtime/WebRTC/fonts).
  - Inject scripts on creation; verify via test pages; add extension loading per profile.
- UI (WPF/MVVM)
  - Shell + navigation; Profile list (filter/search/tag); Profile detail editor.
  - Proxy list/detail with status; checker UI; bind proxy to profile.
  - Fingerprint editor (presets + manual overrides) with validation hints.
  - Browser window launcher per profile (open/close, status indicator).
  - Behavior/Automation tabs: schedule editor, task list, recorder controls.
- Testing/QA
  - Unit tests: services (Profile/Fingerprint/Proxy), generators, behavior rules.
  - Integration: EF Core (SQLite) migrations + repositories; proxy checker with mock server.
  - Spoofing verification scripts (automated checks for canvas/WebGL/WebRTC/timezone/UA).
  - Performance checks: profile launch time, memory per profile window, proxy checker throughput.
