# Lời Thề Quần Đảo (Oath of the Isles)

Game 2D chiến thuật phòng thủ (RTS-lite + Tower Defense) cho Android, xây dựng theo tài liệu
`Oath_of_the_Isles_Game_Design_Document.docx`. Toàn bộ giao diện và nội dung bằng tiếng Việt.

- **Unity** 6000.6.0f1 · URP 2D · Input System (cảm ứng) · màn hình ngang
- **Đồ họa chủ đạo:** Tiny Swords (Pixel Frog)
- **Quái vật bổ sung:** 2D Characters – Casual Monsters (Layer Lab), Old Style Cartoon Mobs 1 Pack (Devils Pig),
  Greek/Fantasy Enemies (Potion Junkies)
- **Font:** Inter SemiBold (SIL Open Font License) — hỗ trợ đầy đủ dấu tiếng Việt

## Chạy thử nhanh

1. Mở project bằng Unity 6000.6.0f1.
2. Menu **Oath → 1. Thiết lập toàn bộ (Setup All)** (chỉ cần khi dữ liệu/scene chưa có hoặc muốn tạo lại).
3. Mở `Assets/Scenes/01_MainMenu.unity` và nhấn Play.

## Build Android

- Menu **Oath → 3. Build APK Android** → tạo `Builds/LoiTheQuanDao.apk` (ARM64, IL2CPP, minSdk 26).
- Hoặc **File → Build Profiles → Android → Build** (danh sách scene đã được cấu hình sẵn).

## Nội dung game

| Màn | Tên | Cơ chế chính |
|---|---|---|
| 1 | Ngọn Lửa Đầu Tiên | Hướng dẫn: xây tháp, tuyển quân, điều quân, 3 đợt |
| 2 | Cây Cầu Gãy | Lối hẹp, ưu tiên Thương binh, Vua Chất Nhờn |
| 3 | Những Hòn Đảo Bị Lãng Quên | Hai hướng tấn công, bảo vệ dân làng, sửa chữa |
| 4 | Cuộc Vây Hãm Kinh Đô | Ba hướng, nâng cấp tháp, quái thú Hy Lạp, Sư Tử Nemea |
| 5 | Ánh Sáng Thuộc Về Tất Cả | Trận cuối: Tướng Veyr và Chimera, đoạn kết chiến dịch |

- **Chế độ Sinh tồn** (menu chính): đợt địch vô tận tự sinh, mạnh dần, thủ lĩnh mỗi 5 đợt, lưu kỷ lục.
- **Quân Lam:** Thợ, Kiếm sĩ, Thương binh (làm chậm địch), Cung thủ, Tu sĩ (hồi máu).
- **Công trình:** Lâu đài (mục tiêu), Doanh trại, Nhà ở (+quân số), Nhà dân làng và 5 loại tháp (mỗi loại 3 cấp):

| Tháp | Đặc tính | Mở khóa |
|---|---|---|
| Tháp Cung | Bắn nhanh từng mục tiêu, bắn được quái bay | Màn 1 |
| Tháp Bắn Đá | Ném đá tầm xa, sát thương lan, không bắn quái bay | Màn 1 |
| Tháp Bi Sắt Gai | Xuyên giáp, làm chậm | Màn 2 |
| Tháp Hỏa Ngục | Nổ lan + thiêu đốt theo thời gian | Màn 3 |
| Tháp Búa Địa Chấn | Nện quanh tháp: sát thương + choáng, không đánh quái bay | Màn 4 |

  Tháp có thể nâng cấp, đổi ưu tiên mục tiêu (Đầu tiên / Mạnh nhất / Gần nhất) và bán lại (hoàn 60%).
  Hình ảnh tháp đá lấy từ gói **Stone Tower Game Assets** (`Assets/Stone Tower`).
- **Kinh tế:** Gỗ (xây/sửa) và Vàng (tuyển quân/nâng cấp) do Thợ khai thác, thưởng hạ địch, thưởng gọi đợt sớm.
- **Địch:** quân Cờ Đỏ (Tiny Swords màu đỏ) + 30 loại quái từ 3 gói quái vật, gồm các thủ lĩnh.
- **Điều khiển cảm ứng:** chạm chọn/ra lệnh, kéo để di chuyển bản đồ, chụm hai ngón để phóng to, nút Back của Android = tạm dừng.
- **Lưu tiến trình** (PlayerPrefs): màn đã mở khóa, số sao, thời gian tốt nhất, cài đặt âm thanh/rung.

## Cấu trúc mã (Assets/Scripts)

| Thư mục | Nội dung |
|---|---|
| `Core/` | GameManager, SaveManager, SceneLoader, AudioManager (âm thanh tổng hợp), GameArt, World, LevelBootstrap |
| `Data/` | LevelConfig (ScriptableObject), UnitCatalog (thông số đơn vị/cân bằng), TowerCatalog (5 loại tháp × 3 cấp), LevelMaps (bản đồ ký tự) |
| `Map/` | MapData (phân tích bản đồ), NavGrid (A*), MapBuilder (Tilemap tự nối viền) |
| `Units/` | Unit (UnitController), EnemyAI (waypoint), Pawn, Health, UnitVisual, Projectile, FX |
| `Buildings/` | Building, TowerController, BuildPlot, ResourceNode |
| `Systems/` | ResourceManager, WaveManager, BuildingManager, SelectionManager, TouchInputController, CameraController |
| `UI/` | HUD, menu chính, chọn màn, hội thoại cốt truyện, hướng dẫn, tạm dừng, kết quả |
| `Editor/` | OathSetup (tự sinh scene/dữ liệu/cấu hình Android), LevelDesigns (cốt truyện + đợt địch), OathBridge |
| `Dev/` | DevHarness — công cụ kiểm thử tự động, chỉ chạy trong Editor |

Dữ liệu màn chơi nằm ở `Assets/ScriptableObjects/Levels/Level01..05.asset` và có thể chỉnh trực tiếp trong Inspector
(menu **Oath → 2** sẽ ghi đè bằng nội dung trong `LevelDesigns.cs`).

## Ghi chú giấy phép

Tên game, cốt truyện và nhân vật (Kael, Mira, Veyr, quần đảo Elaria) là nội dung sáng tạo của đồ án.
Các gói asset được dùng theo điều khoản của tác giả; không phân phối lại riêng lẻ các gói asset.
