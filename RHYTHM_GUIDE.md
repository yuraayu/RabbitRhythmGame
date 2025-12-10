# ゲーム実装ガイド - BPM とシーケンス設定

## 現在の実装内容

### ✅ 完了した機能

1. **簡易的な BPM ベースのリズム設定**
   - `GameManager` で BPM と拍数を設定可能
   - BPM から自動的に拍の時間を計算

2. **ノーツの横位置ずらし**
   - 複数のノーツを横に並べて配置
   - `noteSpacingX` で間隔を調整可能

3. **お手本→プレイフェーズ効果音**
   - お手本フェーズ：ノーツが置かれるたびに「ポンッ」音
   - プレイフェーズ開始：「Play!」音
   - プレイフェーズ：判定ごとに異なる音

4. **フェーズ別ノーツ管理**
   - お手本フェーズ：ノーツを配置（判定なし）
   - プレイフェーズ：プレイヤー入力を受け付け、成功したら消去

---

## 使用方法

### 1. GameManager の設定

**Inspector で以下を設定：**

```
ゲーム設定:
├── Music Clip: BGM オーディオクリップ
├── BPM: 120（曲のテンポ）
├── Beats Per Measure: 4（1小節の拍数）
├── Listen Phase Duration: 3.0（お手本フェーズ秒数）
├── Play Phase Duration: 3.0（プレイフェーズ秒数）
└── Result Phase Duration: 1.0（結果フェーズ秒数）
```

### 2. BPM ベースのシーケンス生成（C# コード例）

**GameManager の `listenSequenceTimings` を設定する方法：**

```csharp
// 方法1：手動で時間を指定
gameManager.listenSequenceTimings = new List<float>
{
    0f,      // 0秒目
    0.5f,    // 0.5秒目
    1.0f,    // 1.0秒目
};

// 方法2：BPM から自動生成（推奨）
// 「1拍、1拍、2拍」のパターン
int[] beatPattern = { 1, 1, 2 };
gameManager.listenSequenceTimings = gameManager.GenerateSimpleSequence(beatPattern);

// BPM 120 の場合：
// - 1拍 = 0.5 秒
// - 2拍 = 1.0 秒
// タイミング：0秒、0.5秒、1.0秒
```

### 3. RhythmManager の設定

**Inspector で以下を設定：**

```
表示設定:
├── Note Position X Base: 0（X座標の基準位置）
├── Note Spacing X: 1.5（ノーツ間隔）
├── Note Position Y: -2.0（Y座標）
└── Note Lifetime: 2.0（ノーツ自動破棄時間）
```

---

## 効果音設定

### SoundManager の設定

**Inspector で以下の効果音を割り当て：**

1. **Carrot Placed SE**: ノーツ配置音（「ポンッ」）
2. **Play Phase SE**: プレイフェーズ開始音
3. **Perfect SE**: Perfect 判定音（高音）
4. **Good SE**: Good 判定音（中音）
5. **Bad SE**: Bad 判定音（低音）
6. **Miss SE**: Miss 判定音

**フリー効果音サイト：**
- [効果音ラボ](https://soundeffect-lab.info/)
- [フリー効果音](https://freepd.com/effects)
- [Pixabay](https://pixabay.com/sound-effects/)

---

## ゲームフロー（新）

```
┌──────────────────────┐
│  Listen Phase        │
│  (お手本フェーズ)     │
├──────────────────────┤
│ 1. ノーツを配置      │
│ 2. 「ポンッ」音      │
│ 3. プレイヤー入力 ✗  │
└──────────────────────┘
          ↓（3秒後）
┌──────────────────────┐
│  Play Phase          │
│  (プレイフェーズ)     │
├──────────────────────┤
│ 1. 「Play!」音      │
│ 2. プレイヤー入力 ✓  │
│ 3. Perfect/Good/Bad  │
│    音が鳴る          │
│ 4. ノーツ消去        │
└──────────────────────┘
          ↓（3秒後）
┌──────────────────────┐
│  Result Phase        │
│  (結果フェーズ)       │
│ スコア表示           │
└──────────────────────┘
          ↓（1秒後）
     次のラウンド
```

---

## シーケンス設定の例

### 例1：シンプルな 4 拍

```csharp
// BPM 120 の場合、各拍は 0.5 秒間隔
int[] pattern = { 1, 1, 1, 1 };  // 4 拍均等
// タイミング: 0s, 0.5s, 1.0s, 1.5s
```

### 例2：シンコペーション（複合拍）

```csharp
int[] pattern = { 1, 2, 1 };  // 1拍、2拍、1拍
// タイミング: 0s, 0.5s, 1.5s
```

### 例3：複雑なリズム

```csharp
int[] pattern = { 2, 1, 1, 2, 1 };  // 5つのノーツ
// タイミング: 0s, 1.0s, 1.5s, 2.0s, 3.0s
```

---

## 重要なポイント

### ✅ プレイフェーズ中のみ入力受け付け

```csharp
// NoteJudgeController.OnPlayerTap() 内で確認
if (gameManager != null && gameManager.GetCurrentPhase() != GameManager.GamePhase.Play)
{
    return;  // お手本フェーズでは入力を無視
}
```

### ✅ お手本フェーズではノーツは消えない

```csharp
// RhythmManager の Destroy() は noteLifetime 後
// プレイフェーズで入力すると即座に消去
```

### ✅ Miss 判定はプレイフェーズ中のみ

```csharp
// CheckMissedNotes() で Playフェーズをチェック
```

---

## 次のステップ

1. **複数ラウンド対応**
   - 現在は同じシーケンスが繰り返される
   - 難易度別シーケンスの実装

2. **MIDI ファイル統合**
   - DryWetMidi ライブラリの追加
   - DAW で作成した楽曲の自動解析

3. **複数レーン対応**
   - 左右のキー入力
   - 複雑なリズムパターン

4. **ゲームオーバー・スコア画面**
   - 最終スコア表示
   - ハイスコア記録

---

**最終更新:** 2025年11月19日
