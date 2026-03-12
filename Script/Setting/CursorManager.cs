using UnityEngine;

/// <summary>
/// 게임의 커서 가시성을 관리하는 스크립트입니다.
/// </summary>
/// 
/// <remarks>
/// Scene의 빈 오브젝트에 컴포넌트를 추가하거나, 외부 함수 호출을 통해 사용합니다.
/// </remarks>
public class CursorManager : MonoBehaviour
{
    // 게임 시작 시 기본 커서 상태를 설정합니다.
    void Start()
    {
        // 예시: 게임 플레이 중에는 커서를 숨깁니다.
        SetCursorVisibility(true);
    }

    /// <summary>
    /// 커서의 가시성을 설정합니다.
    /// 이 함수는 static이므로 다른 어떤 스크립트에서도 참조 없이 호출할 수 있습니다.
    /// </summary>
    /// <param name="isVisible">커서를 보이게 하려면 true, 숨기려면 false로 설정합니다.</param>
    public static void SetCursorVisibility(bool isVisible)
    {
        // 현재 상태와 다를 때만 변경하여 불필요한 호출을 한 번 더 방지할 수 있습니다.
        if (Cursor.visible != isVisible)
        {
            Cursor.visible = isVisible;
        }
    }
}
