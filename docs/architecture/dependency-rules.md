# アーキテクチャ依存ルール詳細

CLAUDE.md §5 の補足。各レイヤの責任と依存関係の詳細。

---

## 1. レイヤ構造

```
Bootstrap (DI 登録)
   ↓ depends on
Infrastructure (永続化・I/O)   Presentation (UI / View)
   ↓                              ↓
Application (UseCase)
   ↓
Domain (純粋ロジック)
```

外側のレイヤは内側を参照できる。逆方向の参照は不可。

## 2. 各レイヤの責任

### 2.1 Domain

**役割**: ゲームの純粋ルール。エンティティ・値オブジェクト・ルール判定。

**含めて良いもの**:
- 値オブジェクト(`CardId`, `PlayerId`)
- エンティティ(`Card`, `Player`)
- 集合(`Pile`, `Hand`)
- 状態(`GameState`, `TurnState`、すべて immutable)
- ルール interface(`IGameRule`)
- アクション interface(`IGameAction`)
- ユーティリティ interface(`IRandomSource`)
- 純 C# 実装(`XorShiftRandom`)

**含めてはいけないもの**:
- `using UnityEngine`(asmdef `noEngineReferences: true` で物理保証)
- `MonoBehaviour` 派生
- `ScriptableObject` 派生
- 永続化(ファイル I/O / PlayerPrefs)
- 外部 API 呼び出し
- DI コンテナ参照

**依存先**: なし

### 2.2 Application

**役割**: ユースケース。Domain の操作を組み合わせた業務手続き。

**含めて良いもの**:
- UseCase クラス(`StartGameUseCase`, `PlayCardUseCase`)
- リポジトリ interface(Domain 永続化の抽象、実装は Infrastructure)
- DTO(Domain と Presentation の境界)
- async/await(`UniTask`)

**含めてはいけないもの**:
- `MonoBehaviour`
- `ScriptableObject`
- Unity 固有 API(`Resources.Load` 等)
- 永続化具象実装

**依存先**: Domain

### 2.3 Infrastructure

**役割**: Application が定義したインターフェースを実装。永続化・外部 I/O。

**含めて良いもの**:
- Repository 実装(`ScriptableObjectCardRepository`)
- Save / Load(JSON / PlayerPrefs)
- 外部 API クライアント

**含めてはいけないもの**:
- Application の具象 UseCase クラスを直接呼び出す処理(インターフェース経由のみ)
- Domain ルールの再実装(Domain を呼ぶ)

**依存先**: Domain, Application(インターフェースのみ)

### 2.4 Presentation

**役割**: UI / View。プレイヤーが見る・触れる部分。

**含めて良いもの**:
- View MonoBehaviour
- Presenter(MVP の P)
- UI コントローラ
- アニメーション制御
- R3 を使ったリアクティブ UI 更新

**含めてはいけないもの**:
- ゲームルール判定(Domain に委譲)
- 直接の永続化(Application 経由)

**依存先**: Domain(参照可)、Application(主)

### 2.5 Bootstrap

**役割**: DI コンテナ(VContainer)へのサービス登録。アプリケーションのエントリポイント。

**含めて良いもの**:
- `LifetimeScope` 派生クラス(`AppLifetimeScope`, `GameLifetimeScope`)
- DI 登録 builder
- Scene エントリポイントの MonoBehaviour

**依存先**: 全レイヤ

## 3. 違反例と是正

### 違反例 1: Domain から UnityEngine 参照

```csharp
// Domain/Cards/Card.cs (NG)
using UnityEngine;  // ← asmdef で禁止されているのでコンパイルエラー
```

是正: `Vector2` 等の Unity 型は `Drowsy.Domain.Math.Vector2` として独自定義するか、Application 以上で扱う。

### 違反例 2: Infrastructure から Application 具象 UseCase を直接呼ぶ

```csharp
// Infrastructure/SaveSystem.cs (NG)
public class SaveSystem
{
    private readonly StartGameUseCase _useCase; // ← 具象に依存
}
```

是正: Application で `IGameSaver` interface を定義し、Infrastructure はそれを実装する。

### 違反例 3: Presentation で Domain の immutable 状態を破壊

```csharp
// Presentation/Views/HandView.cs (NG)
hand.Cards.Add(newCard);  // ← Hand は immutable
```

是正: Application 層の `PlayCardUseCase` 等を呼び出し、新 GameState を取得して View を更新。

## 4. 機械検知

| 違反 | 検知方法 | レイヤ |
| ---- | ---- | ---- |
| Domain で `using UnityEngine` | asmdef `noEngineReferences: true` でコンパイルエラー | 物理保証 |
| 逆方向の参照 | asmdef `references` の依存グラフで Unity が拒否 | 物理保証 |
| Application 具象 UseCase を Infrastructure から直接呼ぶ | カスタム Roslyn Analyzer | Phase 1 以降で実装 |
| autoReferenced=false の徹底 | asmdef Inspector で目視 + lefthook の簡易検知 | Phase 0 |
