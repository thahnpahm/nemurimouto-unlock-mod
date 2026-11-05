# Nemurimouto unlock mod

![18+](https://img.shields.io/badge/18%2B-NSFW-red)

> [!WARNING]
> **Nội dung 18+.** Nemurimouto là game dành cho người lớn; mod này mở khóa toàn bộ cảnh trong gallery. Chỉ dùng nếu bạn đủ 18 tuổi (hoặc tuổi trưởng thành theo luật nơi bạn sống).
>
> Mod không chính thức, không liên quan tới nhà phát triển. Hãy sao lưu save trước khi dùng; tự chịu rủi ro.

Patcher cho `Assembly-CSharp.dll` của Nemurimouto (Unity 6000.3.18f1, bản `win_b`), viết bằng [Mono.Cecil](https://github.com/jbevain/cecil). Repo không chứa file nào của game: patcher sửa trực tiếp bản game của người chơi.

Chi tiết phân tích (cấu trúc game, file save, công thức, lý do chọn từng điểm patch): [docs/REA-NOTES.md](docs/REA-NOTES.md).

## Mod thay đổi gì
| Hàm gốc | Sau khi patch |
|---|---|
| `GalleryPlaceholderButton.IsUnlockedFor` | Gallery mở hết, xem lại được mọi cảnh |
| `TalentState.get_Earned` | Luôn có 999 điểm talent và học được |
| `ShopStockRules.Unlocked` | Shop hiện cả món cần talent |
| `GameManager.get_Money` | Tiền luôn là 99.999.999 |
| `RelationshipStatLevels.Evaluate` | Love/Lust luôn đầy tới ngưỡng, sự kiện kế tiếp luôn sẵn sàng |
| `SettingsManager.Activate` | Tự kích hoạt mã `FROG123` của nhà phát triển |

## Dùng
Chép `NemurimoutoMod.exe` vào thư mục có `Nemurimouto.exe` rồi nhấp đúp. Lần đầu chạy, exe sao lưu bản gốc thành `Nemurimouto_Data\Managed\Assembly-CSharp.dll.bak`; các lần sau luôn patch lại từ bản `.bak` đó.

Gỡ mod: xóa `Assembly-CSharp.dll`, đổi `Assembly-CSharp.dll.bak` về tên cũ.

Nếu game cập nhật làm code thay đổi, patcher báo `Not found: ...` và không ghi file.

## Build
```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```
Hoặc patch thủ công: `dotnet run -- <Assembly-CSharp.dll gốc> <output.dll>`
