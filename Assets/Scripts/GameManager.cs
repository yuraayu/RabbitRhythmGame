using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体を統括するマネージャー
/// プレイヤーフェーズ→お手本フェーズで交互に切り替え
/// ノーツはメトロノームのオフセットを反映して配置
/// </summary>
public class GameManager : MonoBehaviour
{
    // === ゲームフェーズ定義 ===
    public enum GamePhase
    {
        Player,    // プレイヤーフェーズ（入力受け付け）
        Sample     // お手本フェーズ（ノーツ表示のみ）
    }

    // === 公開設定 ===
    [Header("ゲーム設定")]
    [Tooltip("再生する楽曲")]
    public AudioClip musicClip;
    
    [Tooltip("楽曲の BPM")]
    public float bpm = 120f;
    
    [Tooltip("1小節の拍数")]
    public int beatsPerMeasure = 4;

    [Header("フェーズ設定")]
    [Tooltip("プレイヤーフェーズの小節数")]
    public int playerPhaseMeasures = 1;
    
    [Tooltip("お手本フェーズの小節数")]
    public int samplePhaseMeasures = 1;

    [Header("コンポーネント参照")]
    [Tooltip("ノーツ判定用コントローラ")]
    public NoteJudgeController noteJudgeController;
    
    [Tooltip("ノーツ生成用マネージャー")]
    public RhythmManager rhythmManager;

    [Tooltip("効果音マネージャー")]
    public SoundManager soundManager;

    [Tooltip("メトロノーム（デバッグ用）")]
    public MetronomeManager metronomeManager;

    [Header("メトロノーム調整")]
    [Tooltip("メトロノーム開始のオフセット（秒）")]
    public float metronomeOffsetSeconds = 0f;

    // === プライベート変数 ===
    private AudioSource audioSource;
    private bool isGameActive = false;
    private GamePhase currentPhase = GamePhase.Sample;
    private float phaseStartTime = 0f;
    private int roundCount = 0;
    private int lastPhaseSwitchMeasure = -1;  // 最後にフェーズを切り替えた小節番号

    // === Unityライフサイクル ===

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (!isGameActive) return;

        // フェーズ更新
        UpdatePhase();

        // プレイヤーフェーズ中のみ入力を受け付ける
        if (currentPhase == GamePhase.Player)
        {
            HandlePlayerInput();
        }
    }

    // === 初期化 ===

    private void InitializeGame()
    {
        // AudioSourceの取得または作成
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = musicClip;

        // コンポーネント参照の確認
        if (noteJudgeController == null)
        {
            Debug.LogError("[GameManager] NoteJudgeControllerが設定されていません");
        }

        if (rhythmManager == null)
        {
            Debug.LogError("[GameManager] RhythmManagerが設定されていません");
        }

        // ゲーム開始
        StartGame();
    }

    // === ゲーム開始 ===

    private void StartGame()
    {
        isGameActive = true;
        roundCount = 0;
        currentPhase = GamePhase.Sample;
        phaseStartTime = Time.time;
        lastPhaseSwitchMeasure = -1;  // 初期化

        // 音楽再生開始
        if (musicClip != null)
        {
            audioSource.Play();
        }

        // メトロノーム開始
        if (metronomeManager != null && metronomeManager.isMetronomeEnabled)
        {
            metronomeManager.StartMetronome(metronomeOffsetSeconds);
            Debug.Log($"[GameManager] メトロノーム開始（オフセット: {metronomeOffsetSeconds:F3}秒）");
            Debug.Log($"[GameManager] フェーズ切り替え間隔: {metronomeManager.phaseSwitchMeasureInterval}小節");
        }

        Debug.Log("[GameManager] ゲーム開始！お手本フェーズから始まります。");
        OnSamplePhaseStart();
    }

    /// <summary>
    /// BPMに同期した4/4拍子ノーツシーケンスを生成
    /// 現在のお手本フェーズ期間中のみノーツを配置
    /// フェーズ開始時の音楽再生時間を基準にしたタイミングを生成
    /// </summary>
    private void SetupBPMSyncedSequence()
    {
        List<float> timings = new List<float>();
        float beatDuration = GetBeatDuration();
        
        // 現在のお手本フェーズの持続時間を計算（秒）
        float phaseDurationSeconds = GetPhaseDuration(GamePhase.Sample);
        
        // 現在の音楽再生時間を基準にする（このフェーズ開始時点の音楽時間）
        float phaseStartMusicTime = audioSource != null ? audioSource.time : 0f;
        
        // メトロノームのオフセットを考慮
        float baseTime = -metronomeOffsetSeconds;
        
        // フェーズ期間内のノーツのみを生成
        // タイミングは「フェーズ開始時の音楽時間 + オフセット」で計算
        int maxBeats = (int)Mathf.Ceil(phaseDurationSeconds / beatDuration);
        
        for (int beatIndex = 1; beatIndex <= maxBeats; beatIndex++)
        {
            float offsetTime = baseTime + beatIndex * beatDuration;
            if (offsetTime <= phaseDurationSeconds)  // フェーズ期間内のみ追加
            {
                float absoluteTime = phaseStartMusicTime + offsetTime;
                timings.Add(absoluteTime);
            }
        }

        if (rhythmManager != null)
        {
            rhythmManager.SetTargetTimings(timings);
            Debug.Log($"[GameManager] お手本フェーズ用シーケンス設定: {timings.Count}個のノーツを生成（フェーズ開始時間: {phaseStartMusicTime:F2}秒, フェーズ期間: {phaseDurationSeconds:F2}秒）");
        }
    }

    // === フェーズ更新 ===

    private void UpdatePhase()
    {
        // メトロノームが有効で開始している場合、メトロノームの小節頭でフェーズ切り替え
        if (metronomeManager != null && metronomeManager.isMetronomeEnabled)
        {
            int currentMeasure = metronomeManager.GetCurrentMeasure();
            int currentBeat = metronomeManager.GetCurrentBeat();

            // 小節頭（拍0）に到達したかつ、前回のフェーズ切り替えから十分な小節が経過
            if (currentBeat == 0 && currentMeasure != lastPhaseSwitchMeasure)
            {
                int measuresSinceLastSwitch = currentMeasure - lastPhaseSwitchMeasure;
                int targetInterval = GetPhaseSwitchInterval();

                if (measuresSinceLastSwitch >= targetInterval)
                {
                    TransitionToPhase(currentPhase == GamePhase.Player ? GamePhase.Sample : GamePhase.Player);
                    lastPhaseSwitchMeasure = currentMeasure;
                }
            }
        }
        else
        {
            // メトロノームが無効な場合は、従来の時間ベースのフェーズ更新
            float elapsedTime = Time.time - phaseStartTime;
            float phaseDuration = GetPhaseDuration(currentPhase);

            if (elapsedTime >= phaseDuration)
            {
                TransitionToPhase(currentPhase == GamePhase.Player ? GamePhase.Sample : GamePhase.Player);
            }
        }
    }

    /// <summary>
    /// フェーズ切り替え間隔（小節数）を取得
    /// </summary>
    private int GetPhaseSwitchInterval()
    {
        if (metronomeManager != null)
        {
            return metronomeManager.phaseSwitchMeasureInterval;
        }
        return playerPhaseMeasures;  // メトロノーム無効時はフォールバック
    }

    /// <summary>
    /// フェーズの持続時間を計算
    /// </summary>
    private float GetPhaseDuration(GamePhase phase)
    {
        int measures = (phase == GamePhase.Player) ? playerPhaseMeasures : samplePhaseMeasures;
        return (measures * beatsPerMeasure * 60f) / bpm;
    }

    /// <summary>
    /// フェーズを遷移
    /// </summary>
    private void TransitionToPhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        phaseStartTime = Time.time;

        if (newPhase == GamePhase.Sample)
        {
            OnSamplePhaseStart();
        }
        else
        {
            OnPlayerPhaseStart();
        }
    }

    // === フェーズ処理 ===

    private void OnSamplePhaseStart()
    {
        roundCount++;
        Debug.Log($"[GameManager] ラウンド {roundCount} - お手本フェーズ開始（小節: {metronomeManager?.GetCurrentMeasure() ?? -1}）");
        
        // プレイヤーフェーズで食べられなかったノーツをフェードアウト
        if (noteJudgeController != null)
        {
            noteJudgeController.FadeOutAndRemoveUnjudgedNotes();
        }
        
        // お手本フェーズ用のノーツを生成
        SetupBPMSyncedSequence();
    }

    private void OnPlayerPhaseStart()
    {
        Debug.Log($"[GameManager] プレイヤーフェーズ開始（小節: {metronomeManager?.GetCurrentMeasure() ?? -1}） - 入力受付中");
        
        // プレイヤーフェーズでは前のお手本フェーズのノーツを残したままにする
        // NoteJudgeControllerが入力を処理する
    }

    private void HandlePlayerInput()
    {
        // プレイフェーズ中にスペースキーが押された
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (noteJudgeController != null)
            {
                noteJudgeController.OnPlayerTap();
            }
        }
    }

    // === ゲーム終了 ===

    public void EndGame()
    {
        isGameActive = false;
        audioSource.Stop();
        Debug.Log("[GameManager] ゲーム終了");
    }

    // === 外部からのアクセス用メソッド ===

    public GamePhase GetCurrentPhase()
    {
        return currentPhase;
    }

    public float GetBeatDuration()
    {
        return 60f / bpm;
    }

    /// <summary>
    /// BPM を動的に変更する
    /// 次のお手本フェーズからノーツタイミングが新しい BPM で計算される
    /// </summary>
    public void SetBPM(float newBPM)
    {
        if (newBPM <= 0)
        {
            Debug.LogError("[GameManager] BPM は正の値である必要があります");
            return;
        }

        float oldBPM = bpm;
        bpm = newBPM;

        // メトロノーム側の BPM も更新
        if (metronomeManager != null)
        {
            metronomeManager.SetBPM(newBPM);
        }

        Debug.Log($"[GameManager] BPM を {oldBPM} から {newBPM} に変更しました");
    }
}
