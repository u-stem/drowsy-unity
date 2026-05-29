using System.Runtime.CompilerServices;

// テスト用 asmdef からの internal アクセスを許可する。
// CardEntryAsset / AttributeEntry に test 用 internal ctor を用意し、
// Drowsy.Infrastructure.Tests から `new CardEntryAsset(...)` でインスタンスを直接構築可能にする
// (`[SerializeField] private` field 経由のテストインスタンス構築を避けるため)。
[assembly: InternalsVisibleTo("Drowsy.Infrastructure.Tests")]
