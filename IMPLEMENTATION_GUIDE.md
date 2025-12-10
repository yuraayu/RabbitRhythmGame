# ゲーム実装進捗ドキュメント

## 完了したステップ

### ✅ ステップ1：ゲームフロー実装（GameManager）

**作成ファイル:** `GameManager.cs`

**機能:**
- ゲームのステート管理（Listen/Play/Result フェーズ）
- フェーズの自動切り替え時間管理
- 各フェーズの継続時間設定
  - `listenPhaseDuration`: お手本フェーズの時間（秒）
  - `playPhaseDuration`: プレイフェーズの時間（秒）
  - `resultPhaseDuration`: 結果フェーズの時間（秒）
- ラウンド管理
- プレイヤー入力の検出（スペースキー）

**設定方法:**
1. ゲームシーンに空のGameObjectを作成
2. 名前を「GameManager」に変更
3. `GameManager.cs`スクリプトをアタッチ
4. Inspectorで以下を設定：
   - **Music Clip**: BGMオーディオクリップ
   - **Listen Phase Duration**: 3.0 (秒)
   - **Play Phase Duration**: 3.0 (秒)
   - **Result Phase Duration**: 1.0 (秒)
   - **Note Judge Controller**: NoteJudgeControllerをドラッグ&ドロップ
   - **Rhythm Manager**: RhythmManagerをドラッグ&ドロップ

---

### ✅ ステップ2：UI実装（UIController）

**作成ファイル:** `UIController.cs`

**表示要素:**
- **スコア表示** (`scoreText`): 現在のスコア
- **コンボ表示** (`comboText`): 現在のコンボ数（コンボ50以上で大きく表示）
- **判定結果表示** (`judgmentResultText`): Perfect/Good/Bad/Miss（色分け表示）
- **フェーズ表示** (`phaseText`): Listen!/Play!/Result

**判定結果の色:**
- **Perfect**: 黄色
- **Good**: 緑
- **Bad**: 赤
- **Miss**: ピンク
- **Listen**: シアン
- **Play**: 緑
- **Result**: 黄色

**設定方法:**
1. Canvasを作成（なければ）
2. 上記のテキスト要素をCanvasの子として作成
3. UIControllerスクリプトを新しいGameObjectにアタッチ
4. Inspectorで以下を設定：
   - **Score Text**: スコア表示テキスト
   - **Combo Text**: コンボ表示テキスト
   - **Judgment Result Text**: 判定結果テキスト
   - **Phase Text**: フェーズ表示テキスト
   - **Game Manager**: GameManagerをドラッグ&ドロップ
   - **Note Judge Controller**: NoteJudgeControllerをドラッグ&ドロップ

---

### ✅ ステップ3：ビジュアルアニメーション実装

#### 3-1. ウサギアニメーション（RabbitAnimationController）

**作成ファイル:** `RabbitAnimationController.cs`

**アニメーション:**
- **かじるアニメーション**: スケール1.1倍、0.3秒間
- **悲しい表情**: 0.5秒間表示
- **待機状態**: 通常の表情

**スプライト要件:**
- `normalFaceSprite`: 通常の顔
- `happyFaceSprite`: 幸せそうな顔（かじる時）
- `sadFaceSprite`: 悲しい顔（ミス時）

**設定方法:**
1. ウサギのGameObjectにスクリプトをアタッチ
2. `rabbitBody`: ウサギのメインSpriteRenderer
3. `rabbitFace`: 顔のSpriteRenderer（表情切り替え用）
4. 各スプライトをInspectorで設定

#### 3-2. 人参アニメーション（CarrotAnimationController）

**作成ファイル:** `CarrotAnimationController.cs`

**アニメーション:**
- **出現**: スケール0.5→1.0、透明度0→1、0.2秒
- **消滅**: スケール1.0→0.1、透明度1→0、0.3秒
- **判定フィードバック**: スケールが上下に振動、0.2秒

**設定方法:**
1. 人参のPrefabにスクリプトをアタッチ
2. `spriteRenderer`: 人参のSpriteRenderer

---

## 修正・拡張されたスクリプト

### NoteJudgeController.cs
- UIControllerへの判定結果通知機能を追加
- RabbitAnimationControllerへのアニメーション再生指示機能を追加
- ゲッター関数を追加（スコア、コンボ取得）
- Nullチェック機能を強化

### RhythmManager.cs
- GameManagerからのシーケンス設定メソッドを追加
- ノーツクリア機能を追加

---

## 次のステップ（実装予定）

- [ ] 効果音の実装（SE）
- [ ] ゲームオーバー画面
- [ ] スコア画面
- [ ] 難易度選択
- [ ] 複数シーンの管理
- [ ] セーブ/ロード機能
- [ ] ハイスコア管理

---

## シーン構成（推奨）

```
Canvas
├── ScoreText (Score: 0)
├── ComboText (Combo: 0)
├── JudgmentResultText (空初期状態)
└── PhaseText (Listen!)

GameplayArea
├── Rabbit (ウサギ) → RabbitAnimationController
│   ├── RabbitBody (SpriteRenderer)
│   └── RabbitFace (SpriteRenderer)
├── Table (机)
└── NotesContainer (ノーツ親)

GameManager (AudioSource, GameManager script)
UIManager (UIController script)
AudioManager (オーディオ管理用)
```

---

## デバッグ情報

各スクリプトは Debug.Log を使用しており、Console から以下の情報が確認できます：

- ゲームフェーズの遷移
- スコア、コンボの更新
- 判定結果
- アニメーション状態の変更
- ノーツの生成・破棄

---

## 既知の制限事項

1. 現在、複数レーン非対応（単一レーンのみ）
2. 複数ラウンドの自動継続は未実装（企画では複数ラウンドが想定）
3. ポーズ機能がない
4. リスタート機能がない

---

**最終更新:** 2025年11月19日
**実装者:** AI Assistant
