using UnityEngine;

/// <summary>
/// 人参（ノーツ）のアニメーション制御
/// 出現、消滅、判定フィードバックアニメーションを管理
/// </summary>
public class CarrotAnimationController : MonoBehaviour
{
    [Header("アニメーション設定")]
    [Tooltip("出現アニメーションの持続時間（秒）")]
    public float appearAnimationDuration = 0.2f;
    
    [Tooltip("消滅アニメーションの持続時間（秒）")]
    public float disappearAnimationDuration = 0.3f;
    
    [Tooltip("判定フィードバックアニメーションの時間（秒）")]
    public float feedbackAnimationDuration = 0.2f;

    [Header("スケール設定")]
    [Tooltip("出現時の初期スケール")]
    public float appearStartScale = 0.5f;
    
    [Tooltip("消滅時の最終スケール")]
    public float disappearEndScale = 0.1f;

    [Header("参照")]
    [Tooltip("SpriteRenderer コンポーネント")]
    public SpriteRenderer spriteRenderer;

    // === プライベート変数 ===
    private Vector3 originalScale;
    private Color originalColor;
    private float animationTimer = 0f;
    private AnimationState currentAnimationState = AnimationState.Idle;

    private enum AnimationState
    {
        Idle,           // 待機状態
        Appearing,      // 出現中
        Disappearing,   // 消滅中
        Feedback        // 判定フィードバック
    }

    // === Unityライフサイクル ===

    void Start()
    {
        if (spriteRenderer != null)
        {
            originalScale = transform.localScale;
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        UpdateAnimation();
    }

    // === アニメーション管理 ===

    private void UpdateAnimation()
    {
        if (animationTimer > 0f)
        {
            animationTimer -= Time.deltaTime;

            switch (currentAnimationState)
            {
                case AnimationState.Appearing:
                    UpdateAppearAnimation();
                    break;

                case AnimationState.Disappearing:
                    UpdateDisappearAnimation();
                    break;

                case AnimationState.Feedback:
                    UpdateFeedbackAnimation();
                    break;
            }
        }
    }

    private void UpdateAppearAnimation()
    {
        // スケール: 小さいから元のサイズへ
        float progress = 1f - (animationTimer / appearAnimationDuration);
        float scale = Mathf.Lerp(appearStartScale, 1f, progress);
        transform.localScale = originalScale * scale;

        // アルファ: 透明から不透明へ
        Color color = originalColor;
        color.a = Mathf.Lerp(0f, 1f, progress);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }

        if (animationTimer <= 0f)
        {
            currentAnimationState = AnimationState.Idle;
            transform.localScale = originalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    private void UpdateDisappearAnimation()
    {
        // スケール: 元のサイズから小さくへ
        float progress = 1f - (animationTimer / disappearAnimationDuration);
        float scale = Mathf.Lerp(1f, disappearEndScale, progress);
        transform.localScale = originalScale * scale;

        // アルファ: 不透明から透明へ
        Color color = originalColor;
        color.a = Mathf.Lerp(1f, 0f, progress);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }

        if (animationTimer <= 0f)
        {
            currentAnimationState = AnimationState.Idle;
            Destroy(gameObject);
        }
    }

    private void UpdateFeedbackAnimation()
    {
        // フィードバック: スケールを一度大きくしてから戻す
        float progress = 1f - (animationTimer / feedbackAnimationDuration);
        float scale = Mathf.Sin(progress * Mathf.PI) * 0.2f + 1f;
        transform.localScale = originalScale * scale;

        if (animationTimer <= 0f)
        {
            currentAnimationState = AnimationState.Idle;
            transform.localScale = originalScale;
        }
    }

    // === 外部から呼び出すメソッド ===

    /// <summary>
    /// 出現アニメーションを再生
    /// </summary>
    public void PlayAppearAnimation()
    {
        currentAnimationState = AnimationState.Appearing;
        animationTimer = appearAnimationDuration;
        transform.localScale = originalScale * appearStartScale;

        Color color = originalColor;
        color.a = 0f;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }

        Debug.Log("[CarrotAnimationController] 出現アニメーション開始");
    }

    /// <summary>
    /// 消滅アニメーションを再生
    /// </summary>
    public void PlayDisappearAnimation()
    {
        currentAnimationState = AnimationState.Disappearing;
        animationTimer = disappearAnimationDuration;
        transform.localScale = originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        Debug.Log("[CarrotAnimationController] 消滅アニメーション開始");
    }

    /// <summary>
    /// 判定フィードバックアニメーションを再生
    /// </summary>
    public void PlayFeedbackAnimation()
    {
        currentAnimationState = AnimationState.Feedback;
        animationTimer = feedbackAnimationDuration;

        Debug.Log("[CarrotAnimationController] フィードバックアニメーション開始");
    }
}
