using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ゲームUIを管理するコントローラ
/// スコア、コンボ、判定結果、フェーズ表示を担当する
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("UI要素参照")]
    [Tooltip("スコア表示テキスト")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("コンボ表示テキスト")]
    public TextMeshProUGUI comboText;
    
    [Tooltip("判定結果表示テキスト（Perfect/Good/Bad/Miss）")]
    public TextMeshProUGUI judgmentResultText;
    
    [Tooltip("フェーズ表示テキスト（Listen/Play）")]
    public TextMeshProUGUI phaseText;

    [Header("参照")]
    [Tooltip("ゲームマネージャー")]
    public GameManager gameManager;
    
    [Tooltip("ノート判定コントローラ")]
    public NoteJudgeController noteJudgeController;

    [Header("UI表示設定")]
    [Tooltip("判定結果の表示時間（秒）")]
    public float judgmentDisplayDuration = 1.0f;
    
    [Tooltip("コンボ表示のフォントサイズ")]
    public int comboFontSize = 40;
    
    [Tooltip("通常時のフォントサイズ")]
    public int normalFontSize = 30;

    // === プライベート変数 ===
    private float judgmentDisplayTimer = 0f;
    private bool isDisplayingJudgment = false;

    // === Unityライフサイクル ===

    void Start()
    {
        InitializeUI();
    }

    void Update()
    {
        // UI更新
        UpdateScoreDisplay();
        UpdateComboDisplay();
        UpdatePhaseDisplay();
        UpdateJudgmentDisplay();
    }

    // === 初期化 ===

    private void InitializeUI()
    {
        if (scoreText == null)
            Debug.LogWarning("[UIController] scoreTextが設定されていません");
        if (comboText == null)
            Debug.LogWarning("[UIController] comboTextが設定されていません");
        if (judgmentResultText == null)
            Debug.LogWarning("[UIController] judgmentResultTextが設定されていません");
        if (phaseText == null)
            Debug.LogWarning("[UIController] phaseTextが設定されていません");

        // 初期表示をクリア
        if (judgmentResultText != null)
            judgmentResultText.text = "";
    }

    // === UI更新メソッド ===

    private void UpdateScoreDisplay()
    {
        if (scoreText != null && noteJudgeController != null)
        {
            int score = noteJudgeController.GetCurrentScore();
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateComboDisplay()
    {
        if (comboText != null && noteJudgeController != null)
        {
            int combo = noteJudgeController.GetCurrentCombo();
            
            if (combo > 0)
            {
                comboText.text = $"Combo: {combo}";
                
                // コンボ数が多い場合はフォントを大きくする
                if (combo > 50)
                {
                    comboText.fontSize = comboFontSize + 10;
                }
                else
                {
                    comboText.fontSize = comboFontSize;
                }
            }
            else
            {
                comboText.text = "Combo: 0";
                comboText.fontSize = normalFontSize;
            }
        }
    }

    private void UpdatePhaseDisplay()
    {
        if (phaseText != null && gameManager != null)
        {
            GameManager.GamePhase currentPhase = gameManager.GetCurrentPhase();
            
            switch (currentPhase)
            {
                case GameManager.GamePhase.Listen:
                    phaseText.text = "Listen!";
                    phaseText.color = Color.cyan;
                    break;

                case GameManager.GamePhase.Play:
                    phaseText.text = "Play!";
                    phaseText.color = Color.green;
                    break;

                case GameManager.GamePhase.Result:
                    phaseText.text = "Result";
                    phaseText.color = Color.yellow;
                    break;
            }
        }
    }

    private void UpdateJudgmentDisplay()
    {
        if (!isDisplayingJudgment) return;

        judgmentDisplayTimer -= Time.deltaTime;

        if (judgmentDisplayTimer <= 0f)
        {
            isDisplayingJudgment = false;
            if (judgmentResultText != null)
                judgmentResultText.text = "";
        }
    }

    // === 判定結果表示（外部から呼び出し） ===

    /// <summary>
    /// 判定結果を画面に表示する
    /// </summary>
    public void DisplayJudgmentResult(string judgmentText)
    {
        if (judgmentResultText == null) return;

        judgmentResultText.text = judgmentText;
        isDisplayingJudgment = true;
        judgmentDisplayTimer = judgmentDisplayDuration;

        // 判定に応じた色を設定
        switch (judgmentText)
        {
            case "Perfect":
                judgmentResultText.color = Color.yellow;
                break;
            case "Good":
                judgmentResultText.color = Color.green;
                break;
            case "Bad":
                judgmentResultText.color = Color.red;
                break;
            case "Miss":
                judgmentResultText.color = new Color(1f, 0f, 0.5f); // ピンク
                break;
        }

        Debug.Log($"[UIController] 判定表示: {judgmentText}");
    }

    // === 画面フェーズ遷移エフェクト ===

    /// <summary>
    /// フェーズ変更時のアニメーション
    /// </summary>
    public void PlayPhaseTransitionAnimation(GameManager.GamePhase newPhase)
    {
        // TODO: フェーズ遷移時のフラッシュやフェードアウト等のアニメーション
        Debug.Log($"[UIController] フェーズ遷移アニメーション: {newPhase}");
    }
}
