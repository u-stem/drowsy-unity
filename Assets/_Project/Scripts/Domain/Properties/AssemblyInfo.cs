using System.Runtime.CompilerServices;

// post-Phase2 アルゴリズム最適化レビュー Top-2(2026-05-16)で `Drowsy.Domain` に
// `InternalsVisibleTo` を追加した際、Unity Editor の Auto-refresh で新規 AssemblyInfo.cs を
// csproj に取り込ませる時間を待たず、`GameState.cs` 冒頭に直接 `[assembly: InternalsVisibleTo(...)]`
// を書く暫定措置を採用していた。
//
// 本ファイル(2026-05-17 / #5 post-Phase2 残対応)で本来位置の Properties/AssemblyInfo.cs に
// 集約する。GameState.cs 冒頭の暫定 attribute は Unity Editor が本ファイルを csproj に取り込んだ
// 後に削除する(同一 attribute の重複は警告のみで動作影響なし)。
//
// Domain は Pure C# / noEngineReferences:true を維持し、Application 側は Ports & Adapters の
// 依存方向に従う(Application → Domain の internal アクセスのみ許可)。
[assembly: InternalsVisibleTo("Drowsy.Application")]
[assembly: InternalsVisibleTo("Drowsy.Domain.Tests")]
[assembly: InternalsVisibleTo("Drowsy.Application.Tests")]
