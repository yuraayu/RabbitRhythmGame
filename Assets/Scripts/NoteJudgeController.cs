using UnityEngine;
using System.Collections.Generic;

// ノーツの判定処理とスコアリングを管理するメインクラス
public class NoteJudgeController : MonoBehaviour
{
    // === 公開設定 ===
    [Header("判定設定（時間ベース）")]
    [Tooltip("Perfect判定となる時間差（秒）")]
    public float perfectTimingWindow = 0.1f;
    [Tooltip("Good判定となる時間差（秒）")]
    public float goodTimingWindow = 0.2f;
    [Tooltip("Bad判定となる時間差（秒）")]
    public float badTimingWindow = 0.3f;

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
    private AudioSource audioSource;  // 音楽再生時間取得用

    // 現在、判定ラインに最も近い位置にあるノーツを追跡するためのリスト
    // 実際のゲームではレーンごとに管理が必要
    private List<GameObject> activeNotes = new List<GameObject>(); 

    // ゲーム開始時や初期化時に呼び出す
    void Start()
    {
        // GameManager から AudioSource を取得
        if (gameManager != null)
        {
            audioSource = gameManager.GetComponent<AudioSource>();
        }
        
        if (audioSource == null)
        {
            Debug.LogError("[NoteJudgeController] AudioSource が見つかりません");
        }
        
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
        // プレイヤーフェーズ中のみ Miss チェックを行う
        if (gameManager != null && gameManager.GetCurrentPhase() != GameManager.GamePhase.Player)
        {
            return;
        }

        float currentTime = (audioSource != null && audioSource.isPlaying) ? audioSource.time : 0f;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            GameObject note = activeNotes[i];
            
            // nullチェック：削除されたオブジェクトをリストから削除
            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }
            
            // ノーツの目標時刻を取得
            NoteData noteData = note.GetComponent<NoteData>();
            if (noteData == null) continue;
            
            // タイミング差分を計算
            float timingDifference = currentTime - noteData.targetTime;
            
            // ノーツがまだ判定ウィンドウの前の場合は判定しない
            if (timingDifference < -perfectTimingWindow)
            {
                continue;
            }
            
            // ノーツの目標時刻を通り過ぎた場合（Miss判定ウィンドウを超過）
            if (timingDifference > badTimingWindow)
            {
                Debug.Log($"[NoteJudgeController] Miss! ノーツ目標時刻: {noteData.targetTime:F3}秒, 現在時刻: {currentTime:F3}秒, 差分: {timingDifference:F3}秒");
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
        // プレイヤーフェーズ中のみ入力を受け付ける
        if (gameManager != null && gameManager.GetCurrentPhase() != GameManager.GamePhase.Player)
        {
            return;
        }

        if (activeNotes.Count == 0) return;

        // 判定ラインに最も近いノーツを取得（今回はリストの先頭と仮定）
        GameObject closestNote = activeNotes[0];
        
        // 現在の音楽再生時間とノーツの目標時刻の差分を計測
        float currentTime = (audioSource != null && audioSource.isPlaying) ? audioSource.time : 0f;
        NoteData noteData = closestNote.GetComponent<NoteData>();
        float timingDifference = currentTime - (noteData != null ? noteData.targetTime : 0f);
        
        // 判定処理を実行し、ノーツを破棄
        string result = CheckHitTiming(timingDifference);
        ProcessJudgment(result);
        
        // ノーツに判定済みフラグを設定
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

    // 時間差に基づいて判定を返す（秒単位）
    public string CheckHitTiming(float timingDifference)
    {
        float absDiff = Mathf.Abs(timingDifference);
        
        if (absDiff <= perfectTimingWindow)
        {
            return "Perfect";
        }
        else if (absDiff <= goodTimingWindow)
        {
            return "Good";
        }
        else if (absDiff <= badTimingWindow)
        {
            return "Bad";
        }
        else
        {
            return "Miss";
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
    /// 判定されなかったノーツをフェードアウトして消す
    /// お手本フェーズ移行時に呼び出す
    /// </summary>
    public void FadeOutAndRemoveUnjudgedNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            GameObject note = activeNotes[i];
            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            // NoteDataから判定状態を確認
            NoteData noteData = note.GetComponent<NoteData>();
            if (noteData != null && !noteData.isJudged)
            {
                // ノーツに落下・フェードアウトアニメーションを開始
                StartCoroutine(AnimateNoteDropAndFade(note));
                activeNotes.RemoveAt(i);
            }
        }
        
        Debug.Log($"[NoteJudgeController] 未判定のノーツをフェードアウト中");
    }

    /// <summary>
    /// ノーツが下に落下してフェードアウトするアニメーション
    /// </summary>
    private System.Collections.IEnumerator AnimateNoteDropAndFade(GameObject note)
    {
        if (note == null) yield break;

        CanvasGroup canvasGroup = note.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = note.AddComponent<CanvasGroup>();
        }

        float duration = fadeOutDuration;
        float elapsedTime = 0f;
        Vector3 startPosition = note.transform.position;
        Vector3 endPosition = startPosition + Vector3.down * 3f;  // 下に3単位分落下

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 落下
            note.transform.position = Vector3.Lerp(startPosition, endPosition, t);

            // フェードアウト
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        // クリーンアップ
        Destroy(note);
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