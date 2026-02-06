using UnityEngine;

/// <summary>
/// カーソルロック制御
/// ESCキーでカーソルのロック/アンロックを切り替え
/// </summary>
public class CursorController : MonoBehaviour
{
    [Header("設定")]
    public KeyCode unlockKey = KeyCode.Escape;
    public bool startLocked = true;

    private bool isCursorLocked = true;

    void Start()
    {
        if (startLocked)
        {
            LockCursor();
        }
        else
        {
            UnlockCursor();
        }
    }

    void Update()
    {
        // ESCキーでトグル
        if (Input.GetKeyDown(unlockKey))
        {
            if (isCursorLocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }

        // マウスクリックで再ロック（ゲーム画面をクリックした時）
        if (!isCursorLocked && Input.GetMouseButtonDown(0))
        {
            // UIをクリックした場合は除外（必要に応じて調整）
            LockCursor();
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isCursorLocked = true;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isCursorLocked = false;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // アプリケーションがフォーカスを失ったらカーソルを解放
        if (!hasFocus)
        {
            UnlockCursor();
        }
    }
}
