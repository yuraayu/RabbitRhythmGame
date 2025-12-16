using UnityEngine;

/// <summary>
/// メトロノーム機能
/// ゲームのリズムが正確に BPM に合っているかを確認するための補助ツール
/// </summary>
public class MetronomeManager : MonoBehaviour
{
    [Header("BPM設定")]
    [Tooltip("メトロノームの BPM")]
    public float bpm = 120f;
    
    [Tooltip("1小節の拍数")]
    public int beatsPerMeasure = 4;

    [Header("メトロノーム音")]
    [Tooltip("通常の拍音")]
    public AudioClip beatSound;
    
    [Tooltip("小節頭の拍音（より大きい音）")]
    public AudioClip measureHeadSound;

    [Header("コントロール")]
    [Tooltip("メトロノームを有効にするか")]
    public bool isMetronomeEnabled = true;

    [Tooltip("メトロノーム開始タイミングのオフセット（秒、負値で早める、正値で遅らせる）")]
    public float startTimeOffset = 0f;

    [Header("フェーズ切り替え同期")]
    [Tooltip("フェーズが切り替わるまでの小節数（例：2なら2小節ごとにフェーズ切り替え）")]
    public int phaseSwitchMeasureInterval = 1;

    [Header("ゲーム連携")]
    [Tooltip("ゲームマネージャー（現在のフェーズを取得するため）")]
    public GameManager gameManager;

    [Tooltip("効果音マネージャー（フェーズ別拍音を再生するため）")]
    public SoundManager soundManager;

    // === プライベート変数 ===
    private AudioSource audioSource;
    private float beatDuration;  // 1拍の時間（秒）
    private float nextBeatTime = 0f;  // 次の拍の時刻
    private int currentBeat = 0;  // 現在の拍（0～beatsPerMeasure-1）
    private int currentMeasure = 0;  // 現在の小節
    private bool isMetronomeStarted = false;
    private float metronomeStartTime = 0f;

    void Start()
    {
        // AudioSource の取得または作成
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;

        // ビート間隔を計算
        CalculateBeatDuration();

        Debug.Log($"[MetronomeManager] メトロノーム初期化: BPM={bpm}, ビート間隔={beatDuration:F3}秒");
    }

    void Update()
    {
        if (!isMetronomeEnabled) return;

        if (!isMetronomeStarted)
        {
            return;
        }

        // 次のビートのタイミングをチェック
        float elapsedTime = Time.time - metronomeStartTime;
        
        if (elapsedTime >= nextBeatTime)
        {
            PlayBeat();
            currentBeat++;
            
            if (currentBeat >= beatsPerMeasure)
            {
                currentBeat = 0;
                currentMeasure++;
            }
            
            nextBeatTime += beatDuration;
        }
    }

    /// <summary>
    /// メトロノームを開始する
    /// </summary>
    /// <param name="offsetSeconds">開始タイミングのオフセット（秒）。負の値で早期開始、正の値で遅延開始</param>
    public void StartMetronome(float offsetSeconds = 0f)
    {
        if (isMetronomeStarted)
        {
            Debug.LogWarning("[MetronomeManager] メトロノームは既に開始しています");
            return;
        }

        isMetronomeStarted = true;
        metronomeStartTime = Time.time - offsetSeconds;  // オフセットを反映
        nextBeatTime = 0f;
        currentBeat = 0;
        currentMeasure = 0;

        Debug.Log($"[MetronomeManager] メトロノーム開始（オフセット: {offsetSeconds:F3}秒）");
    }

    /// <summary>
    /// メトロノームを停止する
    /// </summary>
    public void StopMetronome()
    {
        isMetronomeStarted = false;
        Debug.Log("[MetronomeManager] メトロノーム停止");
    }

    /// <summary>
    /// 拍を再生する
    /// </summary>
    private void PlayBeat()
    {
        AudioClip clip = (currentBeat == 0) ? measureHeadSound : GetBeatSoundForPhase();
        
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            Debug.Log($"[メトロノーム] 小節 {currentMeasure + 1}, 拍 {currentBeat + 1} ({(currentBeat == 0 ? "強" : "弱")})");
        }
        else
        {
            Debug.LogWarning($"[MetronomeManager] ビート音が設定されていません (小節頭={currentBeat == 0})");
        }
    }

    /// <summary>
    /// 現在のゲームフェーズに応じた拍音を取得
    /// </summary>
    private AudioClip GetBeatSoundForPhase()
    {
        if (gameManager == null)
        {
            // gameManager が未設定の場合はログ出力（初回のみ）
            return null;
        }

        GameManager.GamePhase currentPhase = gameManager.GetCurrentPhase();
        bool isPlayerPhase = (currentPhase == GameManager.GamePhase.Player);

        // SoundManager 経由で、フェーズに応じた拍音を取得
        if (soundManager != null)
        {
            // メトロノーム小節頭以外の拍の場合
            if (currentBeat != 0)
            {
                return isPlayerPhase ? soundManager.playerPhaseBeatSound : soundManager.samplePhaseBeatSound;
            }
        }

        return null;
    }

    /// <summary>
    /// 1拍の時間を計算する
    /// </summary>
    private void CalculateBeatDuration()
    {
        beatDuration = (60f / bpm);  // 秒
    }

    /// <summary>
    /// BPM を変更する
    /// </summary>
    public void SetBPM(float newBPM)
    {
        bpm = newBPM;
        CalculateBeatDuration();
        Debug.Log($"[MetronomeManager] BPM を {bpm} に変更しました（ビート間隔={beatDuration:F3}秒）");
    }

    /// <summary>
    /// 現在の状態を取得
    /// </summary>
    public (int measure, int beat) GetCurrentPosition()
    {
        return (currentMeasure, currentBeat);
    }

    /// <summary>
    /// 現在の小節番号を取得（ゲーム開始からの累積小節数）
    /// </summary>
    public int GetCurrentMeasure()
    {
        return currentMeasure;
    }

    /// <summary>
    /// 現在の拍を取得（0～beatsPerMeasure-1）
    /// </summary>
    public int GetCurrentBeat()
    {
        return currentBeat;
    }

    /// <summary>
    /// 現在がメトロノーム開始からの経過時間（秒）
    /// </summary>
    public float GetElapsedTime()
    {
        if (!isMetronomeStarted) return 0f;
        return Time.time - metronomeStartTime;
    }

    /// <summary>
    /// メトロノームが実行中かどうか
    /// </summary>
    public bool IsRunning()
    {
        return isMetronomeStarted;
    }
}
