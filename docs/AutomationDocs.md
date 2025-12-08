# JumpTask Automation - Technical Doc

## Plan (rút gọn)
- Xác định tác vụ: xem video, mở URL, đánh giá app, thao tác UI (tap/swipe/text).
- Thiết lập nền tảng: .NET 8 + ADB, logger, scheduler, storage log.
- Phát triển module: `ADBController`, `UIAutomatorHelper`, `TaskExecutor`, `TaskSchedulerService`, `DatabaseManager`, `NotificationManager`.
- Tích hợp và thử nghiệm: chạy queue demo, kiểm tra kết nối ADB/device, bổ sung tác vụ thật.
- Mở rộng: UI quản lý tác vụ (WPF/WinForms), thông báo email/SMS/webhook, lưu trữ SQLite/Firebase.

## Requirements
- OS: Windows 10+.
- SDK: .NET 8 SDK.
- Công cụ: Android platform-tools (adb) với USB debugging bật và thiết bị đã authorized.
- Env: `ADB_PATH` (tùy chọn nếu adb không có trong PATH).
- Phụ trợ tùy chọn: SQLite/Firebase nếu cần dữ liệu và báo cáo phong phú hơn NDJSON.

## Design / Architecture
- Entry: `src/JumpTaskAutomation/Program.cs` khởi tạo logger → ADB → helpers → executor → database → notifier → scheduler; enqueue các job demo.
- Automation layer:
  - `ADBController`: chạy adb (tap, swipe, input text, launch app, open URL, Play Store, back).
  - `UIAutomatorHelper`: dump UI hierarchy, hỗ trợ map tọa độ/selector.
  - `TaskExecutor`: tác vụ mức cao (watch video, open web, stub review flow).
- Backend layer:
  - `TaskSchedulerService`: queue bằng channel, xử lý tuần tự, đo thời gian, log + notify.
  - `DatabaseManager`: lưu NDJSON theo dòng tại `data/automation-log.ndjson`.
  - `NotificationManager`: hook thông báo (hiện tại console).
- Logging: `ConsoleAutomationLogger`.
- Mở rộng: thêm WPF/WinForms UI trong `src/JumpTaskAutomation/Frontend`.

## Coding Standards
- C#: .NET 8, nullable enabled.
- Đặt tên: PascalCase cho class/file; camelCase cho biến/hàm; mỗi file ≤ 500 dòng, tách theo chức năng/object.
- Thư mục:
  - `src/JumpTaskAutomation/Automation/` (ADBController, TaskExecutor, UIAutomatorHelper, …)
  - `src/JumpTaskAutomation/Backend/` (TaskSchedulerService, DatabaseManager, NotificationManager, models)
  - `src/JumpTaskAutomation/Logging/` (logger)
  - `src/JumpTaskAutomation/Frontend/` (UI dự phòng)
- Commit message: `feat: ...`, `fix: ...`, `chore: ...`, mỗi commit cho một thay đổi rõ ràng.
- Bình luận code tối thiểu, chỉ khi logic không tự giải thích; tránh hardcode tọa độ, dùng dump để map theo thiết bị.

## Roadmap gợi ý
- Hoàn thiện flow đánh giá app: map tọa độ từ UI dump, nhập đánh giá, gửi.
- Chuyển lưu trữ sang SQLite/Firebase; thêm dashboard báo cáo.
- Thay console notify bằng email/SMS/webhook.
- Tạo UI quản lý queue (WPF/WinForms) cho người dùng không cần CLI.
