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
        }

        Debug.Log("[GameManager] ゲーム開始！お手本フェーズから始まります。");
        OnSamplePhaseStart();
    }

    /// <summary>
    /// BPMに同期した4/4拍子ノーツシーケンスを生成（無限ループ）
    /// メトロノームのオフセットを反映
    /// </summary>
    private void SetupBPMSyncedSequence()
    {
        List<float> timings = new List<float>();
        float beatDuration = GetBeatDuration();
        
        // メトロノームのオフセットを考慮
        // （負のオフセットで早期開始、正のオフセットで遅延開始）
        float baseTime = -metronomeOffsetSeconds;
        
        // 最大120秒間のノーツシーケンスを事前生成
        for (int beatIndex = 1; beatIndex <= 480; beatIndex++)  // 480拍 = 120秒@120BPM
        {
            timings.Add(baseTime + beatIndex * beatDuration);
        }

        if (rhythmManager != null)
        {
            rhythmManager.SetTargetTimings(timings);
            Debug.Log($"[GameManager] BPM同期シーケンス設定: {timings.Count}個のノーツを生成（オフセット: {metronomeOffsetSeconds:F3}秒）");
        }
    }

    // === フェーズ更新 ===

    private void UpdatePhase()
    {
        float elapsedTime = Time.time - phaseStartTime;
        float phaseDuration = GetPhaseDuration(currentPhase);

        if (elapsedTime >= phaseDuration)
        {
            // フェーズ切り替え
            if (currentPhase == GamePhase.Player)
            {
                TransitionToPhase(GamePhase.Sample);
            }
            else
            {
                TransitionToPhase(GamePhase.Player);
            }
        }
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
        Debug.Log($"[GameManager] ラウンド {roundCount} - お手本フェーズ開始");
        
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
        Debug.Log("[GameManager] プレイヤーフェーズ開始 - 入力受付中");
        
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
}
