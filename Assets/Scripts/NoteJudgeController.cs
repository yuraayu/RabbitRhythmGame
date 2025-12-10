using UnityEngine;
using System.Collections.Generic;

// ノーツの判定処理とスコアリングを管理するメインクラス
public class NoteJudgeController : MonoBehaviour
{
    // === 公開設定 ===
    [Header("判定設定")]
    [Tooltip("判定ラインのY座標。画面下の固定位置。")]
    public float judgmentLineY = -4.0f; 
    [Tooltip("Perfect判定となる距離の許容範囲")]
    public float perfectRange = 0.1f;
    [Tooltip("Good判定となる距離の許容範囲")]
    public float goodRange = 0.2f;

    [Header("スコアリング設定")]
    public int perfectScore = 300;
    public int goodScore = 150;
    public int badScore = 50;

    [Header("UI参照")]
    [Tooltip("UIコントローラ（判定結果表示用）")]
    public UIController uiController;

    [Header("ビジュアル参照")]
    [Tooltip("ウサギのアニメーション制御")]
    public RabbitAnimationController rabbitAnimationController;

    [Header("効果音参照")]
    [Tooltip("効果音マネージャー")]
    public SoundManager soundManager;

    [Header("ゲーム参照")]
    [Tooltip("ゲームマネージャー")]
    public GameManager gameManager;

    [Header("フェードアウト設定")]
    [Tooltip("フェードアウトアニメーションの時間（秒）")]
    public float fadeOutDuration = 0.5f;

    // === プライベート変数 ===
    private int currentScore = 0;
    private int currentCombo = 0;

    // 現在、判定ラインに最も近い位置にあるノーツを追跡するためのリスト
    // 実際のゲームではレーンごとに管理が必要
    private List<GameObject> activeNotes = new List<GameObject>(); 

    // ゲーム開始時や初期化時に呼び出す
    void Start()
    {
        Debug.Log("NoteJudgeControllerが初期化されました。");
    }

    // 毎フレーム実行される更新処理
    void Update()
    {
        // ノーツをリストに追加・削除するロジックは別途必要 (例: NoteSpawnerから受け取る)
        // ここでは、リスト内のノーツのY座標をチェックする
        
        // Miss判定の処理 (ノーツが判定ラインを通り過ぎた場合)
        CheckMissedNotes();

        // (デバッグ用) スコアとコンボの表示
        // 実際のゲームではUIテキストに反映させる
        // Debug.Log($"Score: {currentScore}, Combo: {currentCombo}");
    }

    // Miss判定をチェックする
    private void CheckMissedNotes()
    {
        // プレイフェーズ中のみ Miss チェックを行う
        if (gameManager != null && gameManager.GetCurrentPhase() != GameManager.GamePhase.Play)
        {
            return;
        }

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            GameObject note = activeNotes[i];
            
            // nullチェック：削除されたオブジェクトをリストから削除
            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }
            
            // ノーツが判定ラインから許容範囲を超えて遠ざかった（ミス）
            if (note.transform.position.y < judgmentLineY - goodRange) 
            {
                Debug.Log("Miss!");
                ProcessJudgment("Miss");
                
                // ノーツを破棄
                Destroy(note);
                activeNotes.RemoveAt(i);
            }
        }
    }

    // プレイヤーのタップ入力時に呼び出されるメソッド
    // 例: Input.GetKeyDown(KeyCode.Space)などで呼び出す
    public void OnPlayerTap()
    {
        // プレイフェーズ中のみ入力を受け付ける
        if (gameManager != null && gameManager.GetCurrentPhase() != GameManager.GamePhase.Play)
        {
            return;
        }

        if (activeNotes.Count == 0) return;

        // 判定ラインに最も近いノーツを取得（今回はリストの先頭と仮定）
        GameObject closestNote = activeNotes[0];
        
        // 判定ラインとの距離を計測
        float distance = Mathf.Abs(closestNote.transform.position.y - judgmentLineY);
        
        // 判定処理を実行し、ノーツを破棄
        string result = CheckHit(distance);
        ProcessJudgment(result);
        
        // ノーツに判定済みフラグを設定
        NoteData noteData = closestNote.GetComponent<NoteData>();
        if (noteData != null)
        {
            noteData.isJudged = true;
        }
        
        // ノーツのアニメーション
        CarrotAnimationController carrotAnim = closestNote.GetComponent<CarrotAnimationController>();
        if (carrotAnim != null)
        {
            carrotAnim.PlayFeedbackAnimation();
        }
        
        // ノーツをリストから削除（判定が完了したため）
        Destroy(closestNote, 0.3f); // アニメーション終了後に破棄
        activeNotes.RemoveAt(0);
    }

    // 距離に基づいて判定を返す
    public string CheckHit(float distance)
    {
        if (distance <= perfectRange)
        {
            return "Perfect";
        }
        else if (distance <= goodRange)
        {
            return "Good";
        }
        else // goodRangeを超えているが、Miss判定エリアにまだ入っていない場合
        {
            // この範囲は「Bad」として扱う
            return "Bad";
        }
    }

    // 判定結果に基づいてスコアとコンボを更新する
    private void ProcessJudgment(string judgment)
    {
        switch (judgment)
        {
            case "Perfect":
                currentScore += perfectScore + (currentCombo / 10 * 50); // コンボボーナス仮定
                currentCombo++;
                PlaySuccessAnimation();
                break;
            case "Good":
                currentScore += goodScore;
                currentCombo++;
                PlaySuccessAnimation();
                break;
            case "Bad":
                currentScore += badScore;
                currentCombo++; // Badでもコンボ継続（企画書に基づく）
                PlaySuccessAnimation();
                break;
            case "Miss":
                currentCombo = 0; // Missでコンボリセット
                PlayMissAnimation();
                break;
        }
        
        Debug.Log($"Judgment: {judgment}, Current Combo: {currentCombo}, Current Score: {currentScore}");

        // UIに判定結果を表示
        if (uiController != null)
        {
            uiController.DisplayJudgmentResult(judgment);
        }

        // 効果音を再生
        if (soundManager != null)
        {
            // 判定音を再生
            soundManager.PlayJudgmentSound(judgment);
            
            // かじる音も再生（Miss 以外）
            if (judgment != "Miss")
            {
                soundManager.PlayBiteSound(judgment);
            }
        }

        // TODO: フィーバータイム突入判定 (例: currentCombo % 100 == 0)
    }

    // === アニメーションヘルパー ===

    private void PlaySuccessAnimation()
    {
        if (rabbitAnimationController != null)
        {
            rabbitAnimationController.PlayBiteAnimation();
        }
    }

    private void PlayMissAnimation()
    {
        if (rabbitAnimationController != null)
        {
            rabbitAnimationController.PlaySadFaceAnimation();
        }
    }

    // === プレイフェーズ終了時の処理 ===

    /// <summary>
    /// プレイフェーズ終了時に、判定されなかったノーツをフェードアウトで消す
    /// </summary>
    public void FadeOutUnjudgedNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            GameObject note = activeNotes[i];
            
            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            // 判定済みなら何もしない
            NoteData noteData = note.GetComponent<NoteData>();
            if (noteData != null && noteData.isJudged)
            {
                continue;
            }

            // フェードアウトアニメーション開始
            CarrotAnimationController carrotAnim = note.GetComponent<CarrotAnimationController>();
            if (carrotAnim != null)
            {
                carrotAnim.PlayDisappearAnimation();
            }

            // フェードアウト時間後に破棄
            Destroy(note, fadeOutDuration);
            activeNotes.RemoveAt(i);
        }

        Debug.Log("[NoteJudgeController] 未判定ノーツをフェードアウト開始");
    }

    // 外部からノーツを追加するためのメソッド
    public void AddNote(GameObject note)
    {
        activeNotes.Add(note);
        // ノーツの落下スクリプト（NoteMoverなど）も別途必要
    }

    // === 外部からのアクセス用メソッド ===

    /// <summary>
    /// 現在のスコアを取得する
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// 現在のコンボ数を取得する
    /// </summary>
    public int GetCurrentCombo()
    {
        return currentCombo;
    }

    /// <summary>
    /// スコアとコンボをリセットする
    /// </summary>
    public void ResetScoreAndCombo()
    {
        currentScore = 0;
        currentCombo = 0;
        Debug.Log("[NoteJudgeController] スコアとコンボをリセットしました");
    }

    /// <summary>
    /// すべてのアクティブなノーツをクリアする
    /// </summary>
    public void ClearAllNotes()
    {
        foreach (GameObject note in activeNotes)
        {
            if (note != null)
            {
                Destroy(note);
            }
        }
        activeNotes.Clear();
        Debug.Log("[NoteJudgeController] すべてのノーツをクリアしました");
    }
}