using UnityEngine;
using System.Collections.Generic;

// ゲームの進行、音楽再生、ノーツのタイミング管理を行う
public class RhythmManager : MonoBehaviour
{
    // === 公開設定 ===
    
    [Tooltip("生成するノーツのPrefab (人参)")]
    public GameObject notePrefab;
    
    [Tooltip("ノーツの判定を行うコントローラ（GameManagerにアタッチされている）")]
    public NoteJudgeController judgeController;

    [Tooltip("効果音マネージャー")]
    public SoundManager soundManager;

    [Tooltip("ゲームマネージャー（音楽再生時間を取得するため）")]
    public GameManager gameManager;

    // === プライベート変数 ===
    
    // 音楽再生用AudioSource
    private AudioSource audioSource;
    [Tooltip("お手本となるノーツ（人参を食べる）のタイミングリスト (秒)")]
    // 例: 1.0f, 1.5f, 2.0f, 3.5f など
    public List<float> targetTimings = new List<float>();

    [Header("表示設定")]
    [Tooltip("ノーツを配置するX座標の基準位置")]
    public float notePositionXBase = 0f;
    
    [Tooltip("ノーツを配置するときの横ずれ間隔")]
    public float noteSpacingX = 1.5f;
    
    [Tooltip("ノーツを配置するY座標 (テーブルの上)")]
    public float notePositionY = -2.0f;
    
    // ノーツが消えるまでの時間 (プレイヤーがタップできる時間+猶予)
    public float noteLifetime = 2.0f; 
    
    // === プライベート変数 ===
    
    // 次に処理するノーツのインデックス
    private int nextNoteIndex = 0;

    // ゲーム開始からの経過時間
    private float startTime;
    
    // 前フレームで処理したノーツのタイミング（重複処理防止）
    private float lastProcessedTiming = -1f;
    
    // === Unityライフサイクル ===

    void Start()
    {
        // GameManager から AudioSource を取得
        if (gameManager != null)
        {
            audioSource = gameManager.GetComponent<AudioSource>();
        }

        // GameManager が無い場合のフォールバック
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogError("[RhythmManager] AudioSource が見つかりません。GameManager に AudioSource をアタッチしてください。");
        }

        Debug.Log("RhythmManager が初期化されました。");
    }

    void Update()
    {
        // お手本フェーズ中のみノーツを配置
        if (gameManager != null && gameManager.GetCurrentPhase() != GameManager.GamePhase.Sample)
        {
            return;
        }

        // 音楽が再生中でない場合は処理しない
        if (audioSource == null || !audioSource.isPlaying) return;

        // 現在の音楽再生時間を取得
        float currentTime = audioSource.time;

        // 次のノーツのタイミングが現在の再生時間を過ぎていないかチェック
        // 1フレーム内に複数のノーツが生成されないように、1フレームに1ノーツだけ処理
        if (nextNoteIndex < targetTimings.Count && 
            targetTimings[nextNoteIndex] <= currentTime)
        {
            // ノーツの生成タイミングに達した
            SpawnNote(targetTimings[nextNoteIndex]);
            nextNoteIndex++;
            
            // デバッグ出力
            Debug.Log($"[RhythmManager] ノーツ #{nextNoteIndex} 生成: 目標時刻 {targetTimings[nextNoteIndex - 1]:F2}秒, 現在時刻 {currentTime:F2}秒");
        }
    }
    
    // === メソッド ===

    // ノーツ（人参）をテーブルに生成し、NoteJudgeControllerに登録する
    void SpawnNote(float targetTime)
    {
        // ノーツの横位置をずらす（何番目のノーツかで判定）
        float notePositionX = notePositionXBase + (nextNoteIndex * noteSpacingX);
        
        // ノーツを生成
        GameObject newNote = Instantiate(
            notePrefab, 
            new Vector3(notePositionX, notePositionY, 0), 
            Quaternion.identity);
            
        // プレイヤー入力待ち状態を示すためにノーツを少し大きく表示するなどのアニメーションを追加しても良い
        
        // 1. ノーツ自身に目標時間を保持させる（判定に必要）
        NoteData noteData = newNote.AddComponent<NoteData>();
        noteData.targetTime = targetTime;
        
        // 2. NoteJudgeControllerにノーツを登録する
        if (judgeController != null)
        {
            // NoteJudgeControllerは、このノーツと目標時間を使って判定を行います
            judgeController.AddNote(newNote);
        }

        // 3. 効果音を再生（ノーツが置かれた音）
        if (soundManager != null)
        {
            soundManager.PlayCarrotPlacedSound();
        }
        
        // 4. ノーツの自動削除は行わない
        // プレイフェーズで判定されるか、フェーズ終了時に消去される
    }

    // === GameManagerからのアクセス用メソッド ===

    /// <summary>
    /// 新しいお手本シーケンスを設定し、ノーツ生成を開始する
    /// </summary>
    public void SetTargetTimings(List<float> timings, int baseTimeOffset = 0)
    {
        targetTimings = new List<float>(timings);
        nextNoteIndex = 0;
        lastProcessedTiming = -1f;
        
        Debug.Log($"[RhythmManager] 新しいシーケンスを設定：ノーツ数 = {targetTimings.Count}, タイミング = {string.Join(", ", timings)}");
    }

    /// <summary>
    /// プレイヤーフェーズへの切り替え時に呼び出す
    /// ノーツ配置用インデックスをリセット（プレイヤーフェーズではノーツが配置されないようにするため）
    /// </summary>
    public void ResetNoteIndex()
    {
        nextNoteIndex = targetTimings.Count;  // インデックスを終端に移動
        Debug.Log("[RhythmManager] ノーツインデックスをリセット（プレイヤーフェーズ開始）");
    }

    /// <summary>
    /// すべてのアクティブなノーツをクリアする
    /// </summary>
    public void ClearAllNotes()
    {
        if (judgeController != null)
        {
            judgeController.ClearAllNotes();
        }
    }
}