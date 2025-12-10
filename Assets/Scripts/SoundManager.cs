using UnityEngine;

/// <summary>
/// ゲーム全体の効果音を管理するマネージャー
/// ノーツ配置音、判定音、フェーズ遷移音を担当する
/// </summary>
public class SoundManager : MonoBehaviour
{
    [Header("効果音クリップ")]
    [Tooltip("ノーツ（人参）が置かれる音")]
    public AudioClip carrotPlacedSE;
    
    [Tooltip("プレイフェーズ開始時の音")]
    public AudioClip playPhaseSE;
    
    [Tooltip("お手本フェーズ終了時の合図音（ハイ！）")]
    public AudioClip listenPhaseEndSE;
    
    [Tooltip("ウサギがかじる音（Perfect 用）")]
    public AudioClip bitePerfectSE;
    
    [Tooltip("ウサギがかじる音（Good 用）")]
    public AudioClip biteGoodSE;
    
    [Tooltip("ウサギがかじる音（Bad 用）")]
    public AudioClip biteBadSE;
    
    [Tooltip("Perfect 判定時の音")]
    public AudioClip perfectSE;
    
    [Tooltip("Good 判定時の音")]
    public AudioClip goodSE;
    
    [Tooltip("Bad 判定時の音")]
    public AudioClip badSE;
    
    [Tooltip("Miss 判定時の音")]
    public AudioClip missSE;

    [Header("音量設定")]
    [Tooltip("効果音の全体音量（0～1）")]
    [Range(0f, 1f)]
    public float masterVolume = 0.8f;

    // === プライベート変数 ===
    private AudioSource audioSource;

    // === Unityライフサイクル ===

    void Start()
    {
        // AudioSource を取得または作成
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    // === 効果音再生メソッド ===

    /// <summary>
    /// ノーツ配置音を再生
    /// </summary>
    public void PlayCarrotPlacedSound()
    {
        PlaySound(carrotPlacedSE);
    }

    /// <summary>
    /// プレイフェーズ開始音を再生
    /// </summary>
    public void PlayPlayPhaseSound()
    {
        PlaySound(playPhaseSE);
    }

    /// <summary>
    /// お手本フェーズ終了時の合図音（ハイ！）を再生
    /// </summary>
    public void PlayListenPhaseEndSound()
    {
        PlaySound(listenPhaseEndSE);
    }

    /// <summary>
    /// 判定結果に応じた音を再生
    /// </summary>
    public void PlayJudgmentSound(string judgment)
    {
        AudioClip clip = null;

        switch (judgment)
        {
            case "Perfect":
                clip = perfectSE;
                break;
            case "Good":
                clip = goodSE;
                break;
            case "Bad":
                clip = badSE;
                break;
            case "Miss":
                clip = missSE;
                break;
        }

        PlaySound(clip);
    }

    /// <summary>
    /// かじる音を再生（判定結果に応じた異なる音）
    /// </summary>
    public void PlayBiteSound(string judgment)
    {
        AudioClip clip = null;

        switch (judgment)
        {
            case "Perfect":
                clip = bitePerfectSE;
                break;
            case "Good":
                clip = biteGoodSE;
                break;
            case "Bad":
                clip = biteBadSE;
                break;
        }

        if (clip != null)
        {
            PlaySound(clip);
        }
    }

    /// <summary>
    /// 効果音を再生する（内部メソッド）
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SoundManager] 再生しようとした効果音がnullです");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("[SoundManager] AudioSource が見つかりません");
            return;
        }

        audioSource.PlayOneShot(clip, masterVolume);
    }
}
