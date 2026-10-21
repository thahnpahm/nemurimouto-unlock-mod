# Ghi chép phân tích Nemurimouto (REA)

Kết quả phân tích tĩnh `Assembly-CSharp.dll` bằng REA (`inspect_managed_artifact`, `inspect_managed_members`) và đọc IL bằng Mono.Cecil. Mọi điều dưới đây là suy ra từ metadata/IL, **không** phải quan sát lúc chạy, trừ phần "Đã chạy thử".

## Định danh bản đã phân tích
| Mục | Giá trị |
|---|---|
| Game | Nemurimouto, nhà phát triển `pachipoi` (`Nemurimouto_Data/app.info`) |
| Gói | `win_b.zip` (392.651.029 byte, 697 entry) |
| Engine | Unity `6000.3.18f1 (5ebeb53e4c07)`, scripting backend Mono |
| `Assembly-CSharp.dll` gốc | SHA-256 `f96edca50d9043089c2af20f0b2f74b80bc198be6ef92584ef7b21c452a8ac22`, 1.014.784 byte |
| MVID | `eb2cc6b5-a70a-4418-ac5a-2401c4f1d815` |
| Bản đã patch (v2) | SHA-256 `c7c177b756c329c8d5a9e0ef65a466f7bea3d4f0d37f34d723b24f4cc2d6429c` |
| Quy mô | 781 TypeDef, 5314 MethodDef, 4445 Field, 2804 user string |

Nếu hash `Assembly-CSharp.dll` khác hash gốc ở trên thì game đã cập nhật: các ghi chép này có thể không còn đúng.

## Kiến trúc
- Code game: namespace `Nemurimouto.*` (Work, Talents, Stats, Items, Effects, Ending, Loading, Results, UI, Routines) và các class không namespace: `GameManager` (219 method, trung tâm), `DialogueManager`, `DailyRoutine*`, `HScene*`, `DataPersistenceManager`.
- Thư viện: Live2D Cubism, Ink + QuantumDialogue (hội thoại), I2 + Lean Localization, DOTween, Addressables (bundle trong `StreamingAssets/aa`), URP.
- Scene: `MainMenu`, `GameBootstrap`, `Prologue`, `Intro`, `DailyRoutineTest` (hub chính), `HScene01`–`07`, `ZHBocchi`.

## Trạng thái nhân vật (enum)
- `MayusLocation`: None, Livingroom, MayusRoom, Kitchen, Door, Outside
- `MayusState`: None, Moving, PerformingRoutine, Event
- `MayusMoodState`: None, Giddy, Awake
- `RelationshipStat`: None, Affection (=1, love), Heat (=2, lust)
- `WorkOutcome`: None, Clean, Rework, Critical

## File save
- Vị trí: `%USERPROFILE%\AppData\LocalLow\pachipoi\Nemurimouto\<profileId>\<fileName>`; 10 ô thủ công (`sav00`…) + `autosave`.
- Định dạng: JSON từ `JsonUtility` của `GameData`.
- Mã hóa (`FileDataHandler.EncryptDecrypt`): XOR từng ký tự với `"word"[i % 4]`, chỉ khi `useEncryption` bật.
- `Decode` chấp nhận JSON thô nếu chuỗi bắt đầu `{`, kết thúc `}`, có `"lastUpdated"` và `"day"`.
- Ghi an toàn: `.tmp`, đọc lại kiểm tra, `File.Replace`, giữ `.bak`.
- Trường `GameData`: `lastUpdated`, `timePlayedInSeconds`, `playerName`, `currentRoutineGuid`, `day`, `timeSlot`, `endingCompleted`, `endlessMode`, `love`, `lust`, `giddyHeat`, `health`, `money`, `supplyShopDay`, `shopStock`, `itemUpgrades`, `work`, `talents`, `temporaryEffects`, `triggers`, `intTriggers`, `readChats`, `ver`, `moodState`, `currentItems`, `currentShop`.

## Cơ chế và hằng số
- **Kết thúc**: `EndingRules.LastStoryDay` trả về 30. `IsDue` đúng khi `day > 30` và chưa xong ending/endless. Sau ending có thể vào endless mode (không còn sự kiện mới).
- **Tâm trạng** (`MoodRules.GainPenaltyPercent`): mood ≥ 70 → 0% (CALM); 50–70 → 15% (POOR); < 50 → 30% (BAD) trừ vào điểm nhận được.
- **Love/Lust** (`RelationshipStatLevels`): tối đa `Maximum = 5680`, 10 mốc (`Thresholds`, mảng 10 phần tử). `Evaluate(total, completedEvents)` kẹp tổng vào khoảng `[Thresholds[n-1], Thresholds[n]]` với n = số sự kiện đã xong, nên chỉ số dừng ở ngưỡng cho tới khi sự kiện của level diễn ra. `AwaitingEvent` = `CurrentPoints >= RequiredPoints` và chưa max.
- Cờ hoàn thành sự kiện: `event` + `Love`/`Desire` + số 2 chữ số + `Done` (vd. `eventDesire02Done`).
- **Talent**: điểm = `Earned - Spent`. `Earned` = level (giới hạn bởi `MaximumLevel`) + `legacyBonusPoints`. Tier n cần đã tiêu `(n-1)*5` điểm (`TalentDefinition.get_UnlockCost`). Light Touch II/III cần cấp trước. **`TalentState.Normalize` dựng lại `ranks` và cắt bớt cho vừa `Earned`**, và `TryLearn` gọi `Normalize` trước khi học.
- **Tiền**: chỉ chặn ở `int.MaxValue` (`PerformWorkTurn`, `TryPurchaseOffer`).
- **Công việc** (`WorkRules`/`WorkBalance`): lượt làm tốn motivation (mặc định 100, Deep Reserves 150). Tỉ lệ Clean mặc định 80%, Reliable Worker 90%. Bỏ việc bị trừ reputation.
- **Gallery** (`GalleryPlaceholderButton.IsUnlockedFor`): mở khi `GameManager.ReadChatGuids` chứa GUID của `ChatData` hoặc `ChatData.IsCompleted`. GUID nằm trong asset, nên sửa save không đủ để mở gallery.
- **Hiệu ứng xấu**: `lonely`, `hungry`, `stubbed-toe` (`NegativeEffectRules`).

## Mã kích hoạt và kiểm duyệt (`SettingsManager`)
- `Activate(code)`: nếu `code.ToUpper() == "FROG123"` thì `isBotched = isNotRelated = isFullyCon = false`, lưu PlayerPrefs `"Code"`, `isActivated = true`, gọi `OnActivate`.
- `Start()`: `Activate(PlayerPrefs.GetString("Code"))`; nếu `isSteam` thì `isChristian = true` + trigger `isChristian`; nếu `isSteam && isChristian` thì `isCensored = true` và bật `censorRenderer`. Mã không tắt kiểm duyệt của bản Steam.
- `DRRequirements` có các cờ `notChirstianRequired`, `notBotchedRequired`, `notFullyConRequired`, `relatedRequired` dùng để lọc routine.
- `SteamBegging`: hội thoại nhắc wishlist trên Steam, cờ `steamReminderShown`/`steamReminderPending`.

## Điểm patch (mod v2)
| Phương thức | IL gốc (tóm tắt) | Patch |
|---|---|---|
| `GalleryPlaceholderButton.IsUnlockedFor(GameManager)` | kiểm tra `ReadChatGuids`/`IsCompleted` | `ldc.i4 1; ret` |
| `TalentState.get_Earned()` | level + legacyBonusPoints | `ldc.i4 999; ret` |
| `ShopStockRules.Unlocked(ItemSO, TalentState)` | kiểm tra talent | `ldc.i4 1; ret` |
| `GameManager.get_Money()` | `ldfld <Money>k__BackingField` | `ldc.i4 99999999; ret` |
| `RelationshipStatLevels.Evaluate(float, int)` | `ldarg.0` trước `call ClampTotal` (IL_003e) | thay bằng `ldc.r4 5680` |
| `SettingsManager.Activate(string)` | `ldarg.1` ở IL_0000 | thay bằng `ldstr "FROG123"` |

Bài học: patch `get_Points` (bản v1) **không** hoạt động, vì `Normalize` cắt ranks theo `Earned`. Phải patch `get_Earned`.

## Đã chạy thử
- Game nạp bản patch, khởi tạo `GameManager`/`SettingsManager`, không có exception trong `Player.log` (`%USERPROFILE%\AppData\LocalLow\pachipoi\Nemurimouto\Player.log`). Lần nạp assembly đầu tiên mất khoảng 26 giây khi game nằm trong OneDrive.
- Chưa kiểm chứng trong lúc chơi: sự kiện tự kích hoạt khi love/lust đầy, mua đồ khi tiền cố định.

## Chưa làm
- Chưa giải các mảng hằng số tĩnh (`Thresholds`, `WorkBalance.Weights`, `Milestones`).
- Chưa giải nén asset bundle (hình, âm thanh, script Ink, GUID của `ChatData`).
- Chưa biết giá trị `isSteam` được serialize trong scene của bản này.

## Đọc lại IL nhanh, không cần REA
Dùng Mono.Cecil (vd. một console app 10 dòng) để in `MethodDefinition.Body.Instructions` của các phương thức trên. Evidence ID của REA trong phiên phân tích (`ev_924f0fee…` artifact, `ev_685fc1fc…` members) chỉ tồn tại trong phiên đó.
