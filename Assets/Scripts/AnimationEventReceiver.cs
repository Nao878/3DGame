using UnityEngine;

/// <summary>
/// Starter Assetsのアニメーションイベントを受け取るためのダミーレシーバー
/// VRoidモデルにアタッチして警告を解消する
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("デバッグログを出力する")]
    public bool showDebugLog = false;

    [Header("オーディオ（オプション）")]
    [Tooltip("足音用のオーディオクリップ（空でも可）")]
    public AudioClip[] footstepClips;
    
    [Tooltip("着地音用のオーディオクリップ（空でも可）")]
    public AudioClip landingClip;
    
    [Range(0, 1)]
    public float volume = 0.5f;

    /// <summary>
    /// 足音イベント（Starter Assetsのアニメーションから呼ばれる）
    /// </summary>
    public void OnFootstep(AnimationEvent animationEvent)
    {
        // AnimationEvent.animatorClipInfo.weight が0.5以上のときのみ有効
        if (animationEvent.animatorClipInfo.weight < 0.5f) return;

        if (showDebugLog)
        {
            Debug.Log("[AnimationEventReceiver] OnFootstep");
        }

        // オーディオクリップが設定されていれば再生
        if (footstepClips != null && footstepClips.Length > 0)
        {
            var clip = footstepClips[Random.Range(0, footstepClips.Length)];
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position, volume);
            }
        }
    }

    /// <summary>
    /// 着地イベント（Starter Assetsのアニメーションから呼ばれる）
    /// </summary>
    public void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight < 0.5f) return;

        if (showDebugLog)
        {
            Debug.Log("[AnimationEventReceiver] OnLand");
        }

        // オーディオクリップが設定されていれば再生
        if (landingClip != null)
        {
            AudioSource.PlayClipAtPoint(landingClip, transform.position, volume);
        }
    }
}
