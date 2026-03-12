using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼 단계에서 적들의 소환을 관리하는 스크립트 입니다.
/// </summary>
public class TutorialStep_SpawnEnemies : TutorialStep
{
    [Header("References")]
    [SerializeField] 
    private EnemyMemoryPool enemyMemoryPool;
    [SerializeField] 
    private ShopManager shopManager; // 적을 모두 처치하고 상점을 열기 위해 참조 추가

    [Header("Enemy Spawn Settings")]
    [SerializeField]
    private GameObject[] enemyPrefabs;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    [SerializeField] 
    private int numberOfEnemiesToSpawn = 3;
    [SerializeField] 
    private Transform target;

    [Header("Completion Conditions")]
    [SerializeField] 
    private bool isSpawnComplete = false;
    [SerializeField] 
    private bool waitForAllEnemiesKilled = false; // true면 모든 적이 죽어야 단계가 완료됩니다.
    [SerializeField] 
    private bool openShopOnCompletion = false; // true고, waitForAllEnemiesKilled도 true면, 모든 적 처치 후 상점을 엽니다.
    private int enemiesKilled = 0;

    private bool isFinished = false;
    public override bool IsFinished => isFinished;

    public override void Enter()
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            isFinished = true;
            return;
        }

        PauseManager.RemovePause();
        isFinished = false;
        enemiesKilled = 0;
        spawnedEnemies.Clear();

        if (enemyMemoryPool == null)
        {
            Debug.LogError("EnemyMemoryPool이 할당되지 않았습니다.");
            isFinished = true;
            return;
        }

        StartCoroutine(WaitFirstSpawnTime());

        // 만약 적 처치를 기다릴 필요가 없다면, 소환 즉시 완료 처리합니다.
        if (!waitForAllEnemiesKilled)
        {
            isFinished = true;
        }
    }

    public override void Execute(TutorialController controller)
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            controller.SetNextTutorial();
            return;
        }

        // 모든 적이 죽었는지 확인합니다.
        if (waitForAllEnemiesKilled && spawnedEnemies.Count > 0 && enemiesKilled >= spawnedEnemies.Count)
        {
            // 상점 열기 옵션이 켜져있으면 상점을 엽니다.
            if (openShopOnCompletion)
            {
                if (shopManager != null)
                {
                    shopManager.OpenShopForTutorial(); // 튜토리얼용 상점 열기 함수 호출
                }
                else
                {
                    Debug.LogError("ShopManager가 할당되지 않았습니다. 상점을 열 수 없습니다.");
                }
            }

            isFinished = true; // 단계를 완료합니다.
        }

        if (isSpawnComplete)
        {
            controller.RegisterSpawnedEnemies(spawnedEnemies);
            // 모든 적이 소환되었으면 단계를 완료합니다.
            controller.SetNextTutorial();
        }
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("소환할 Enemy Prefab이 할당되지 않았습니다.");
            isFinished = true;
            return;
        }

        // 선택할 프리팹 인덱스 목록 생성 (중복 없이)
        List<int> spawnIndices = new List<int>();

        // 요청이 3개 이상이고 enemyPrefabs에 최소 3개가 있다면
        // 명시적으로 enemyPrefabs[0], [1], [2]를 소환 리스트에 추가
        if (numberOfEnemiesToSpawn >= 3 && enemyPrefabs.Length >= 3)
        {
            spawnIndices.Add(0);
            spawnIndices.Add(1);
            spawnIndices.Add(2);
        }

        // 실제 소환 루프
        for (int idx = 0; idx < spawnIndices.Count; idx++)
        {
            int prefabIndex = spawnIndices[idx];
            GameObject prefabToSpawn = enemyPrefabs[prefabIndex];
            if (prefabToSpawn == null) continue;

            // EnemyMemoryPool을 사용해 랜덤 위치을 가져옵니다.
            SpawnInfo finalSpawnPosition = enemyMemoryPool.GetRandomSpawnPositionInFirstWorld(prefabToSpawn);

            Vector3 adjustedPosition = finalSpawnPosition.Position;
            Bounds spawnBounds = finalSpawnPosition.Bounds;

            // 튜토리얼 적을 소환합니다.
            GameObject spawnedEnemy = enemyMemoryPool.SpawnTutorialEnemy(prefabToSpawn, adjustedPosition);

            if (spawnedEnemy != null)
            {
                spawnedEnemies.Add(spawnedEnemy);

                // EnemyFSM을 안전하게 찾아 설정합니다.
                EnemyFSM enemyFSM = null;
                if (!spawnedEnemy.TryGetComponent<EnemyFSM>(out enemyFSM))
                {
                    enemyFSM = spawnedEnemy.GetComponent<EnemyFSM>();
                }

                if (enemyFSM != null)
                {
                    enemyFSM.Setup(target);
                    enemyFSM.SetSpawnArea(spawnBounds);
                }
                else
                {
                    Debug.LogWarning($"{spawnedEnemy.name}에 EnemyFSM 컴포넌트를 찾을 수 없습니다.");
                }

                // 적 처치를 기다려야 한다면, 해당 적의 OnEnemyDied 이벤트에 구독합니다.
                if (waitForAllEnemiesKilled)
                {
                    EnemyStatusManager status = spawnedEnemy.GetComponent<EnemyStatusManager>();
                    if (status != null)
                    {
                        status.OnEnemyDied += HandleEnemyKilled;
                    }
                }
            }
        }

        // 소환 완료 후 타임아웃 코루틴은 한 번만 시작
        StartCoroutine(WaitSpawnTime());
    }

    private void HandleEnemyKilled(GameObject deadEnemy)
    {
        enemiesKilled++;

        // 메모리 누수 방지를 위해 이벤트 구독을 해제합니다.
        EnemyStatusManager status = deadEnemy.GetComponentInChildren<EnemyStatusManager>();
        if (status != null)
        {
            status.OnEnemyDied -= HandleEnemyKilled;
        }
    }

    public override void Skip()
    {
        base.Skip();
        // 스폰된 적들 정리
        CleanupSpawnedEnemies();
    }

    public override void Exit()
    {
        // 씬 전환 등으로 인해 단계가 강제 종료될 경우를 대비해, 남아있는 모든 이벤트 구독을 해제합니다.
        CleanupSpawnedEnemies();
    }

    private void CleanupSpawnedEnemies()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                EnemyStatusManager status = enemy.GetComponentInChildren<EnemyStatusManager>();
                if (status != null)
                {
                    status.OnEnemyDied -= HandleEnemyKilled;
                }
                // 스킵 시 적 비활성화
                if (isSkipped)
                {
                    enemy.SetActive(false);
                }
            }
        }
    }

    private IEnumerator WaitSpawnTime()
    {
        yield return new WaitForSeconds(3f);
        isSpawnComplete = true;
    }

    private IEnumerator WaitFirstSpawnTime()
    {
        yield return new WaitForSeconds(1f);
        SpawnEnemies();
    }
}
