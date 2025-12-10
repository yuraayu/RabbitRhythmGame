using UnityEngine;

/// <summary>
/// ウサギのアニメーション制御
/// 待機状態、かじる、悲しい表情などのアニメーション状態を管理する
/// </summary>
public class RabbitAnimationController : MonoBehaviour
{
    [Header("アニメーション設定")]
    [Tooltip("かじるアニメーションの持続時間（秒）")]
    public float biteAnimationDuration = 0.3f;
    
    [Tooltip("かじるアニメーションのスケール倍率")]
    public float biteScale = 1.1f;
    
    [Tooltip("悲しい表情の持続時間（秒）")]
    public float sadFaceDuration = 0.5f;

    [Header("参照")]
    [Tooltip("ウサギのメインボディSpriteRenderer")]
    public SpriteRenderer rabbitBody;
    
    [Tooltip("ウサギの顔のSpriteRenderer（表情切り替え用）")]
    public SpriteRenderer rabbitFace;

    [Header("スプライト")]
    [Tooltip("通常の顔スプライト")]
    public Sprite normalFaceSprite;
    
    [Tooltip("幸せな顔スプライト（かじる時）")]
    public Sprite happyFaceSprite;
    
    [Tooltip("悲しい顔スプライト（ミス時）")]
    public Sprite sadFaceSprite;

    // === プライベート変数 ===
    private Vector3 originalScale;
    private float animationTimer = 0f;
    private AnimationState currentAnimationState = AnimationState.Idle;

    private enum AnimationState
    {
        Idle,           // 待機状態
        Biting,         // かじる
        SadExpression   // 悲しい表情
    }

    // === Unityライフサイクル ===

    void Start()
    {
        if (rabbitBody != null)
        {
            originalScale = rabbitBody.transform.localScale;
        }

        SetFaceExpression(normalFaceSprite);
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
                case AnimationState.Biting:
                    UpdateBiteAnimation();
                    break;

                case AnimationState.SadExpression:
                    if (animationTimer <= 0f)
                    {
                        ResetToIdle();
                    }
                    break;
            }
        }
    }

    private void UpdateBiteAnimation()
    {
        // かじるアニメーション：スケールを大きくしてから戻す
        float progress = 1f - (animationTimer / biteAnimationDuration);
        float scale = Mathf.Lerp(biteScale, 1f, progress);

        if (rabbitBody != null)
        {
            rabbitBody.transform.localScale = originalScale * scale;
        }

        if (animationTimer <= 0f)
        {
            ResetToIdle();
        }
    }

    private void ResetToIdle()
    {
        currentAnimationState = AnimationState.Idle;
        animationTimer = 0f;

        if (rabbitBody != null)
        {
            rabbitBody.transform.localScale = originalScale;
        }

        SetFaceExpression(normalFaceSprite);
    }

    // === 表情設定 ===

    private void SetFaceExpression(Sprite faceSprite)
    {
        if (rabbitFace != null && faceSprite != null)
        {
            rabbitFace.sprite = faceSprite;
        }
    }

    // === 外部から呼び出すメソッド ===

    /// <summary>
    /// かじるアニメーションを再生
    /// </summary>
    public void PlayBiteAnimation()
    {
        currentAnimationState = AnimationState.Biting;
        animationTimer = biteAnimationDuration;
        SetFaceExpression(happyFaceSprite);

        Debug.Log("[RabbitAnimationController] かじるアニメーション再生");
    }

    /// <summary>
    /// 悲しい表情アニメーションを再生
    /// </summary>
    public void PlaySadFaceAnimation()
    {
        currentAnimationState = AnimationState.SadExpression;
        animationTimer = sadFaceDuration;
        SetFaceExpression(sadFaceSprite);

        Debug.Log("[RabbitAnimationController] 悲しい表情アニメーション再生");
    }

    /// <summary>
    /// 待機状態に戻す
    /// </summary>
    public void SetIdleState()
    {
        ResetToIdle();
    }
}
