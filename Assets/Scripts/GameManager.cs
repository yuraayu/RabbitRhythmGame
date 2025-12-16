using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体を統括するマネージャー
/// BPMに同期した4/4拍子でノーツを継続配置（フェーズなし）
/// </summary>
public class GameManager : MonoBehaviour
{
    // === 公開設定 ===
    [Header("ゲーム設定")]
    [Tooltip("再生する楽曲")]
    public AudioClip musicClip;
    
    [Tooltip("楽曲の BPM")]
    public float bpm = 120f;
    
    [Tooltip("1小節の拍数")]
    public int beatsPerMeasure = 4;

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

    // === Unityライフサイクル ===

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (!isGameActive) return;

        // プレイヤー入力検出（常に受け付ける）
        HandlePlayerInput();
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

        // 4/4拍子で自動ノーツ配置シーケンスを生成
        SetupBPMSyncedSequence();

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

        Debug.Log("[GameManager] ゲーム開始！BPM同期モードで稼働中です。");
    }

    /// <summary>
    /// BPMに同期した4/4拍子ノーツシーケンスを生成（無限ループ）
    /// 各拍にノーツを配置：1拍目, 2拍目, 3拍目...
    /// </summary>
    private void SetupBPMSyncedSequence()
    {
        List<float> timings = new List<float>();
        float beatDuration = GetBeatDuration();
        
        // 最大120秒間のノーツシーケンスを事前生成
        for (int beatIndex = 1; beatIndex <= 480; beatIndex++)  // 480拍 = 120秒@120BPM
        {
            timings.Add(beatIndex * beatDuration);
        }

        if (rhythmManager != null)
        {
            rhythmManager.SetTargetTimings(timings);
            Debug.Log($"[GameManager] BPM同期シーケンス設定: {timings.Count}個のノーツを生成");
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

    public float GetBeatDuration()
    {
        return 60f / bpm;
    }
}
