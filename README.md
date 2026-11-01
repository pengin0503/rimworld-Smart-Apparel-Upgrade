# Smart Apparel Upgrade

[日本語](#日本語) | [English](#english)

## 日本語
RimWorld 1.7向けの服装アップグレード支援MOD。同じ種類・素材で、品質と耐久度がより高い服を探し、服装ポリシーとアクセス可否を確認します。設定画面に交換候補数と除外比較の件数を表示します。

**状態: 開発版。コンパイルとゲーム内動作は未検証です。** 自動着替えは初期設定で無効です。有効化した場合も、基本的に待機中の入植者にだけバニラの着替えJobを指示します。強制着用やロック中の服には介入しません。他の衣服制御MODとの自動運用は未検証です。

### ビルド
.NET 10 SDKと同じバージョンのRimWorld 1.7 DLLが必要です。 `dotnet build Source/SmartApparelUpgrade.csproj -c Release /p:RimWorldManaged=/path/to/RimWorld` 。ゲームDLLは同梱しません。

## English
A RimWorld 1.7 apparel-upgrade utility. Finds clothing with the same type and material but better quality and condition while respecting apparel policies and accessibility. Settings display recommendations and rejected-comparison counts.

**Status: Development version; compilation and gameplay are not yet verified.** Automatic wearing is OFF by default. When enabled, the mod only orders non-working pawns to use vanilla wear jobs. It avoids forced and locked apparel. Compatibility with other automatic outfit-control mods is unverified.

### Build
.NET 10 SDK and matching RimWorld 1.7 DLLs are required. Run `dotnet build Source/SmartApparelUpgrade.csproj -c Release /p:RimWorldManaged=/path/to/RimWorld`. No game DLLs are distributed.
