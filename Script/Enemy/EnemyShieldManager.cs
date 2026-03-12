using UnityEngine;
using System;

/// <summary>
/// Shield 타입 Enemy를 관리하는 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
///
/// <remarks> 
/// 해당 스크립트를 가진 Enemy가 소환 시, 모든 Normal 타입 Enemy가 무적이 됩니다.
/// Normal 타입의 무적은 Layer를 Ignore Raycast로 변경하고, EnemyShield 에서 shieldActiveCount을 증가시켜 무적 상태로 설정합니다.
/// </remarks> 
public class EnemyShieldManager : MonoBehaviour
{
    [Header("Layer")]
    private const string ignoreRaycastLayer = "Ignore Raycast";
    private const string enemyLayer = "Enemy";

    // 이벤트 선언
    public static event Action<bool> OnEnemyShieldToggle;
    public static bool IsShieldActive { get; private set; } = false;

    void Update()
    {
        EnemyShield[] shields = FindObjectsByType<EnemyShield>(FindObjectsSortMode.None);
        foreach (var shield in shields)
        {
            // 자신만 레이어 변경
            shield.gameObject.layer = LayerMask.NameToLayer(ignoreRaycastLayer);
            shield.HandleShieldActiveCount(1);
        }
    }

    void OnEnable()
    {
        // 이벤트 호출(활성화)
        OnEnemyShieldToggle?.Invoke(true);
        IsShieldActive = true;
    }

    void OnDisable()
    {
        EnemyShield[] shields = FindObjectsByType<EnemyShield>(FindObjectsSortMode.None);
        foreach (var shield in shields)
        {
            // 자신만 레이어 원복
            shield.gameObject.layer = LayerMask.NameToLayer(enemyLayer);
            shield.HandleShieldActiveCount(0);
        }

        // 이벤트 호출(비활성화)
        OnEnemyShieldToggle?.Invoke(false);
        IsShieldActive = false;
    }
}
