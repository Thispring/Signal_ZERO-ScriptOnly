using UnityEngine;

/// <summary>
/// Enemy의 무적 상태를 표시를 담당하는 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
public class EnemyShield : MonoBehaviour
{
    [SerializeField]
    private GameObject shieldPrefab;    // 무적 상태를 시각적으로 보여주는 프리팹

    public int shieldActiveCount = 0; // 쉴드 활성화 카운트, 1 이상이면 무적 상태

    void Awake()
    {
        if (shieldPrefab != null) shieldPrefab.SetActive(false);

        shieldActiveCount = 0; 
    }

    void Update()
    {
        if (shieldActiveCount > 0)
        {
            if (shieldPrefab != null)
                shieldPrefab.SetActive(true);
        }
        else
        {
            if (shieldPrefab != null)
                shieldPrefab.SetActive(false);
        }
    }

    void OnEnable()
    {
        EnemyShieldManager.OnEnemyShieldToggle += HandleShieldToggle;

        // 현재 Shield 상태를 직접 체크해서 쉴드 상태 맞추기
        HandleShieldToggle(EnemyShieldManager.IsShieldActive);
    }

    void OnDisable()
    {
        EnemyShieldManager.OnEnemyShieldToggle -= HandleShieldToggle;
    }

    private void HandleShieldToggle(bool isActive)
    {
        Debug.Log($"HandleShieldToggle 호출됨: {isActive}");
    }

    public void HandleShieldActiveCount(int count)
    {
        shieldActiveCount = count;
        Debug.Log($"HandleShieldActiveCount 호출됨: {shieldActiveCount}");
    }
}
