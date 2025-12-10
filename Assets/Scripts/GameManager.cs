using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体を統括するマネージャー
/// ゲームのステート管理（Listen/Play/Result）、フェーズ切り替え、全体的な進行管理を行う
/// </summary>
public class GameManager : MonoBehaviour
{
    // === ゲームステート定義 ===
    public enum GamePhase
    {
        Listen,    // お手本フェーズ（リスニング）
        Play,      // プレイフェーズ（プレイヤー入力）
        Result     // 結果フェーズ
    }

    // === 公開設定 ===
    [Header("ゲーム設定")]
    [Tooltip("再生する楽曲")]
    public AudioClip musicClip;
    
    [Tooltip("楽曲の BPM")]
    public float bpm = 120f;
    
    [Tooltip("1小節の拍数")]
    public int beatsPerMeasure = 4;
    
    [Tooltip("お手本フェーズの小節数")]
    public int listenPhaseMeasures = 1;
    
    [Tooltip("プレイフェーズの小節数")]
    public int playPhaseMeasures = 1;
    
    [Tooltip("結果フェーズの秒数（ノーツフェードアウト用）")]
    public float resultPhaseDuration = 0.5f;

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
    [Tooltip("メトロノーム開始のオフセット（秒）。負の値で早期開始、正の値で遅延開始")]
    public float metronomeOffsetSeconds = 0f;

    [Header("お手本シーケンス設定")]
    [Tooltip("1ラウンドのお手本ノーツのタイミング（秒）")]
    public List<float> listenSequenceTimings = new List<float>();
    
    // === プライベート変数 ===
    private GamePhase currentPhase = GamePhase.Listen;
    private float phaseStartTime = 0f;
    private float musicStartTime = 0f;
    private float musicStartTimestamp = 0f;  // 音楽が再生を開始した時刻（Unityの Time.time）
    private AudioSource audioSource;
    
    private int currentRound = 0;
    private bool isGameActive = false;

    // === Unityライフサイクル ===

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (!isGameActive) return;

        // 各フェーズの時間管理
        UpdatePhase();

        // プレイフェーズでのプレイヤー入力検出
        if (currentPhase == GamePhase.Play)
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
        currentRound = 0;
        currentPhase = GamePhase.Listen;
        phaseStartTime = Time.time;
        musicStartTimestamp = Time.time;  // 音楽開始時刻を記録

        // デフォルトシーケンスの設定（空の場合）
        if (listenSequenceTimings.Count == 0)
        {
            SetupDefaultSequence();
        }

        // 音楽再生開始
        if (musicClip != null)
        {
            audioSource.Play();
        }

        // メトロノーム開始（デバッグ用）
        if (metronomeManager != null && metronomeManager.isMetronomeEnabled)
        {
            metronomeManager.StartMetronome(metronomeOffsetSeconds);
            Debug.Log($"[GameManager] メトロノーム開始（オフセット: {metronomeOffsetSeconds:F3}秒）");
        }

        Debug.Log("[GameManager] ゲーム開始！最初のお手本フェーズに入ります。");
        OnPhaseChanged(GamePhase.Listen);
    }

    /// <summary>
    /// デフォルトシーケンスを設定（トントントン）
    /// 120BPM, 4拍子の場合、各拍は0.5秒
    /// ノーツ: 0.5, 1.0, 1.5秒（3つのノーツ、4拍目は「ハイ！」）
    /// </summary>
    private void SetupDefaultSequence()
    {
        float beatDuration = GetBeatDuration();
        listenSequenceTimings = new List<float>
        {
            beatDuration * 1,  // 1拍目 (0.5s)
            beatDuration * 2,  // 2拍目 (1.0s)
            beatDuration * 3   // 3拍目 (1.5s)
            // 4拍目は「ハイ！」という合図音 - ノーツは置かない
        };
        Debug.Log($"[GameManager] デフォルトシーケンス設定: ビート間隔 = {beatDuration:F3}秒, タイミング = [{string.Join(", ", listenSequenceTimings.ConvertAll(x => x.ToString("F2")))}]");
    }

    // === フェーズ更新 ===

    private void UpdatePhase()
    {
        float elapsedTime = Time.time - phaseStartTime;

        switch (currentPhase)
        {
            case GamePhase.Listen:
                if (elapsedTime >= GetPhaseDuration(listenPhaseMeasures))
                {
                    TransitionToPhase(GamePhase.Play);
                }
                break;

            case GamePhase.Play:
                if (elapsedTime >= GetPhaseDuration(playPhaseMeasures))
                {
                    TransitionToPhase(GamePhase.Result);
                }
                break;

            case GamePhase.Result:
                if (elapsedTime >= resultPhaseDuration)
                {
                    // 次のラウンドへ
                    currentRound++;
                    TransitionToPhase(GamePhase.Listen);
                }
                break;
        }
    }
    
    /// <summary>
    /// 小節数からフェーズ持続時間（秒）を計算
    /// </summary>
    private float GetPhaseDuration(int measures)
    {
        return (measures * beatsPerMeasure * 60f) / bpm;
    }

    // === フェーズ遷移 ===

    private void TransitionToPhase(GamePhase nextPhase)
    {
        currentPhase = nextPhase;
        phaseStartTime = Time.time;

        OnPhaseChanged(nextPhase);

        switch (nextPhase)
        {
            case GamePhase.Listen:
                OnListenPhaseStart();
                break;

            case GamePhase.Play:
                OnPlayPhaseStart();
                break;

            case GamePhase.Result:
                OnResultPhaseStart();
                break;
        }
    }

    // === 各フェーズの処理 ===

    private void OnPhaseChanged(GamePhase phase)
    {
        Debug.Log($"[GameManager] フェーズ変更: {phase}");
    }

    private void OnListenPhaseStart()
    {
        // お手本フェーズ開始
        // - ノーツを生成（listenSequenceTimingsに基づいて）
        // - UIに「Listen!」表示
        // - プレイヤー入力を受け付けない

        Debug.Log($"[GameManager] ラウンド {currentRound + 1} - お手本フェーズ開始");
        
        // 現在の音楽再生時刻からのオフセット付きシーケンスを生成
        if (rhythmManager != null)
        {
            float currentMusicTime = audioSource.time;
            rhythmManager.SetTargetTimings(listenSequenceTimings, (int)Mathf.Floor(currentMusicTime));
            Debug.Log($"[GameManager] お手本フェーズ: 現在の音楽時刻 = {currentMusicTime:F2}秒");
        }
    }

    private void OnPlayPhaseStart()
    {
        // プレイフェーズ開始
        // - プレイヤー入力を受け付ける
        // - UIに「Play!」表示
        // - 合図音「ハイ！」を再生

        Debug.Log("[GameManager] プレイフェーズ開始 - プレイヤー入力受付中");

        // 合図音を再生
        if (soundManager != null)
        {
            soundManager.PlayListenPhaseEndSound();
        }
    }

    private void OnResultPhaseStart()
    {
        // 結果フェーズ開始
        // - スコア、コンボ、判定結果を表示
        // - 次のラウンドの準備

        int score = noteJudgeController != null ? noteJudgeController.GetCurrentScore() : 0;
        int combo = noteJudgeController != null ? noteJudgeController.GetCurrentCombo() : 0;

        Debug.Log($"[GameManager] 結果: スコア {score}, コンボ {combo}");

        // プレイフェーズで判定されなかったノーツをフェードアウト
        if (noteJudgeController != null)
        {
            noteJudgeController.FadeOutUnjudgedNotes();
        }
    }

    // === プレイヤー入力処理 ===

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

    public float GetPhaseElapsedTime()
    {
        return Time.time - phaseStartTime;
    }

    public int GetCurrentRound()
    {
        return currentRound;
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    // === BPM ユーティリティ ===

    /// <summary>
    /// BPM から 1 拍の時間（秒）を計算
    /// </summary>
    public float GetBeatDuration()
    {
        return 60f / bpm;
    }

    /// <summary>
    /// 指定された拍数分の時間を計算
    /// 例：beat = 2 なら、2拍分の時間を返す
    /// </summary>
    public float GetTimingForBeats(int beatCount)
    {
        return GetBeatDuration() * beatCount;
    }

    /// <summary>
    /// 簡易的なシーケンスを生成する
    /// 例：GenerateSimpleSequence(new[] { 1, 1, 2 }) → 1拍、1拍、2拍のタイミング
    /// </summary>
    public List<float> GenerateSimpleSequence(int[] beatPattern)
    {
        List<float> timings = new List<float>();
        float currentTime = 0f;
        float beatDuration = GetBeatDuration();

        foreach (int beat in beatPattern)
        {
            timings.Add(currentTime);
            currentTime += beatDuration * beat;
        }

        return timings;
    }
}
