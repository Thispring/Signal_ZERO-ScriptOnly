using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SearchEnemyManager는 적을 검색하고 관리하는 스크립트입니다.
/// </summary>
/// 
/// <remarks>
/// 보조로봇의 공격 및 방어 기능을 위해 사용합니다.
/// Enemy 소환 이벤트를 List에 등록하고 SupportBotFSM에서 호출합니다.
/// </remarks>
public class SearchEnemyManager : MonoBehaviour
{
    // 소환되는 Enemy에게 해당 클래스를 참조해야 하기 때문에 싱글톤으로 구현
    public static SearchEnemyManager Instance { get; private set; } 
    private List<EnemyStatusManager> enemyList = new List<EnemyStatusManager>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void OnEnable()
    {
        // 이벤트 구독
        EnemyStatusManager.OnEnemySpawned += OnEnemySpawn;
        EnemyStatusManager.OnEnemyDestroyed += OnEnemyDestroy;
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        EnemyStatusManager.OnEnemySpawned -= OnEnemySpawn;
        EnemyStatusManager.OnEnemyDestroyed -= OnEnemyDestroy;
    }

    void Update()
    {
        // 주기적으로 null 체크 및 정리
        CleanupDestroyedEnemies();
    }

    // 적이 소환되면 리스트에 추가
    private void OnEnemySpawn(EnemyStatusManager enemy)
    {
        enemyList.Add(enemy);
    }

    // 적이 파괴되면 리스트에서 제거
    private void OnEnemyDestroy(EnemyStatusManager enemy)
    {
        enemyList.Remove(enemy);
    }

    // 가장 가까운 적을 찾는 함수 (public으로 변경)
    // 플레이어와 가장 가까운 Enemy를 반환합니다.
    public EnemyStatusManager FindNearestEnemy(Vector3 position)
    {
        EnemyStatusManager nearestEnemy = null;
        // 현재까지의 최단거리를 저장
        float nearestDistanceSqr = Mathf.Infinity; // 제곱 거리로 최적화

        foreach (var enemy in enemyList)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue; // 비활성화된 적은 무시

            float distanceSqr = Vector3.SqrMagnitude(position - enemy.transform.position);
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy; // 가장 가까운 적 반환
    }

    // Shield 대상(EnemyShieldManager)을 찾기
    // 플레이어와 가장 가까운 적의 Transform을 반환합니다.
    public Transform FindNearestShieldEnemy(Vector3 position)
    {
        Transform nearestTransform = null;
        float nearestDistanceSqr = Mathf.Infinity;

        // EnemyShield 검색
        foreach (var enemy in enemyList)
        {
            // 현재 활성화된 적만 검색
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            if (enemy.GetComponent<EnemyShieldManager>() == null) continue; // Shield 없는 적 스킵

            float distanceSqr = Vector3.SqrMagnitude(position - enemy.transform.position);
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestTransform = enemy.transform;
            }
        }

        return nearestTransform;
    }

    // 파괴된 적들(비활성화 상태) 정리
    private void CleanupDestroyedEnemies()
    {
        for (int i = enemyList.Count - 1; i >= 0; i--)
        {
            if (enemyList[i] == null || !enemyList[i].gameObject.activeInHierarchy)
            {
                enemyList.RemoveAt(i);
            }
        }
    }
}
