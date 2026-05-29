# ロードマップ / Phase 進捗

Phase の現在地を記録する。各 Phase / マイルストーンの設計詳細は対応 ADR を Single Source of Truth とする。

## 現在地

- **Phase 1**(Domain 拡張): 完結(ADR-0002)
- **Phase 2**(DrowZzz 本命実装): 完結(2026-05-16 = M5-PR8、ADR-0005)
- **Phase 3**(N>2 拡張 / 本格 UI / 世界観統合 / Networking 等): 未着手(ロードマップ ADR の起票後に着手判断)

## 現況

- WebGL Build: Result Success(検証手順は [`architecture/webgl-il2cpp-verification.md`](architecture/webgl-il2cpp-verification.md))
- EditMode テスト: 全緑
- Domain C0 カバレッジ: 100%(ADR-0018)
- ADR: 27 件(索引は [`adr/README.md`](adr/README.md))
- カード: No.00〜No.20(継続追加中)

## Phase 2 マイルストーン

| マイルストーン | 内容 | 詳細 ADR |
| ---- | ---- | ---- |
| M1 | ターン進行 + カードプレイ骨格 | ADR-0006 |
| M2 | カード効果 | ADR-0007〜ADR-0009 |
| M3 | 勝利条件 + ゲームメカニクス拡張 | ADR-0010 / ADR-0011 |
| M4 | 永続化 + ScriptableObject 化 + ユーザー設定 | ADR-0012 |
| M5 | Bootstrap + Presentation | ADR-0016 |

- Phase 2 完結後もカード(No.16〜No.20)を継続追加中。
- 各カードは EARS + Gherkin で記録し、設計判断を伴う場合に ADR を起票する(No.18〜20 は ADR-0023〜ADR-0025)。
