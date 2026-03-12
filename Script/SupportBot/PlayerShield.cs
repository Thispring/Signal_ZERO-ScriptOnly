using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerShield : MonoBehaviour
{
    [SerializeField]
    private PlayerStatusManager playerStatus;

    void Awake()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        // "EnemyMissile" 또는 "BossMissile" 태그와 충돌했는지 확인
        if (other.CompareTag("EnemyMissile") || other.CompareTag("BossMissile"))
        {
            // 쉴드가 활성화 상태일 때만 파괴 로직 실행
            if (playerStatus != null && playerStatus.isShieldActive)
            {
                // PlayerStatusManager를 통해 쉴드 파괴 이벤트 호출
                playerStatus.BreakShield();

                Debug.Log("PlayerShield가 미사일과 충돌하여 쉴드가 파괴되었습니다.");
            }
        }
    }
}
