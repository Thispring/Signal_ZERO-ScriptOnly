using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy의 소환을 관리하기 위한 메모리 풀 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>

// 위치 반환용 구조체 선언
public struct SpawnInfo
{
    public Vector3 Position;
    public Bounds Bounds;
}

public class EnemyMemoryPool : MonoBehaviour
{
    public static Action onSpawnTileAction;
    public static EnemyMemoryPool Instance { get; private set; }

    [Header("References")]
    private MemoryPool spawnPointMemoryPool;
    private MemoryPool enemyPool;
    [SerializeField]
    private StageManager stageManager;
    [SerializeField]
    private BossStatusManager bossStatusManager;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemySpawnPointPrefab;
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Enemy Array")]
    [SerializeField] private GameObject[] enemyPrefabArray;

    [Header("Values")]
    public float enemySpawnTime = 6f;
    public float enemySpawnLatency = 1f;
    public float enemyTileLatency = 3f;
    private int numberOfEnemySpawnedAtOnce = 1;
    private int currentNumber = 0;
    public int killScore = 0;
    public int maximumNumber;

    [Header("Flags")]
    public bool isClearStage = false;
    public bool isStageCheck = false;
    private bool isSpawningPaused = false;
    private bool hasPausedForMiddleBoss = false;
    public bool isMiddleBossClear = false;
    public bool isFinalBossClear = false;
    private bool hasPausedForFinalBoss = false;
    public bool isMiddleBossFirstSpawn = false;
    public bool isFinalBossFirstSpawn = false;

    [Header("Target")]
    // enemy에게 공격대상으로 전달할 타겟, 투사체를 공격에 이용하므로 위치만 전달
    [SerializeField]
    private Transform target;

    [Header("Position")]
    private Vector3[] spawnPositions;

    [Header("Spawn Area")]
    [Tooltip("0=RightHigh,1=LeftHigh,2=MediumLow,3=BigLow,4=RightStart,5=LeftStart,6=ShieldHigh,7=ShieldMedium,8=ShieldBig")]
    public GameObject[] firstWorldSpawnArea;
    private Bounds[] firstWorldSpawnBounds;
    public GameObject[] middleBossWorldSpawnArea;
    private Bounds[] middleBossWorldSpawnBounds;
    public GameObject[] finalBossWorldSpawnArea;
    private Bounds[] finalBossWorldSpawnBounds;
    private Bounds[] activeWorldBounds;

    // 크기나 타입별 스폰 위치 결정을 위한 인덱스
    [Header("Spawn Pos Index")]
    private const int IDX_RIGHT_HIGH = 0;
    private const int IDX_LEFT_HIGH = 1;
    private const int IDX_MEDIUM_LOW = 2;
    private const int IDX_BIG_LOW = 3;
    private const int IDX_RIGHT_START = 4;
    private const int IDX_LEFT_START = 5;
    private const int IDX_SHIELD_HIGH = 6;
    private const int IDX_SHIELD_MEDIUM = 7;
    private const int IDX_SHIELD_BIG = 8;

    void Awake()
    {
        // 월드1의 Bounds 준비
        if (firstWorldSpawnArea == null || firstWorldSpawnArea.Length < 8)
        {
            Debug.LogError("firstWorldSpawnArea 설정 오류(길이 8 미만)");
            firstWorldSpawnBounds = new Bounds[0];
        }
        else
        {
            firstWorldSpawnBounds = new Bounds[firstWorldSpawnArea.Length];
            for (int i = 0; i < firstWorldSpawnArea.Length; i++)
            {
                var go = firstWorldSpawnArea[i];
                if (go == null) { Debug.LogError($"firstWorldSpawnArea[{i}]가 비어 있습니다."); continue; }
                var col = go.GetComponent<Collider>();
                if (col == null) { Debug.LogError($"firstWorldSpawnArea[{i}]에 Collider가 없습니다: {go.name}"); continue; }
                firstWorldSpawnBounds[i] = col.bounds;
            }
        }

        // 중간 보스 월드의 Bounds 준비 
        if (middleBossWorldSpawnArea != null && middleBossWorldSpawnArea.Length >= 9)
        {
            middleBossWorldSpawnBounds = new Bounds[middleBossWorldSpawnArea.Length];
            for (int i = 0; i < middleBossWorldSpawnArea.Length; i++)
            {
                var go = middleBossWorldSpawnArea[i];
                if (go == null) { Debug.LogError($"middleBossWorldSpawnArea[{i}]가 비어 있습니다."); continue; }
                var col = go.GetComponent<Collider>();
                if (col == null) { Debug.LogError($"middleBossWorldSpawnArea[{i}]에 Collider가 없습니다: {go.name}"); continue; }
                middleBossWorldSpawnBounds[i] = col.bounds;
            }
        }

        // 최종 보스 월드의 Bounds 준비 
        if (finalBossWorldSpawnArea != null && finalBossWorldSpawnArea.Length > 0)
        {
            finalBossWorldSpawnBounds = new Bounds[finalBossWorldSpawnArea.Length];
            for (int i = 0; i < finalBossWorldSpawnArea.Length; i++)
            {
                var go = finalBossWorldSpawnArea[i];
                if (go == null) { Debug.LogError($"finalBossWorldSpawnArea[{i}]가 비어 있습니다."); continue; }
                var col = go.GetComponent<Collider>();
                if (col == null) { Debug.LogError($"finalBossWorldSpawnArea[{i}]에 Collider가 없습니다: {go.name}"); continue; }
                finalBossWorldSpawnBounds[i] = col.bounds;
            }
        }

        spawnPointMemoryPool = new MemoryPool(enemySpawnPointPrefab);
        enemyPool = new MemoryPool(enemyPrefabs);
        enemyPool.SetAllPoolObjectsParent(transform);

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        enemyPrefabArray = new GameObject[maximumNumber];
        spawnPositions = new Vector3[maximumNumber];

        onSpawnTileAction += () => StartCoroutine(SpawnTile());

        EnemyStatusManager.OnEnemyDestroyed += OnEnemyDestroyed;
    }

    void OnDestroy()
    {
        onSpawnTileAction = null;

        EnemyStatusManager.OnEnemyDestroyed -= OnEnemyDestroyed;
    }

    void Start()
    {
        //StageTextFade.Instance.StartTextFade("", 2f);
        //onSpawnTileAction?.Invoke();
    }

    // TEST: 튜토리얼 테스트 Setup
    public void Setup()
    {
        StageTextFade.Instance.StartTextFade("1", 2f);
        onSpawnTileAction?.Invoke();
    }

    // 적이 파괴될 때 호출되는 메서드
    private void OnEnemyDestroyed(EnemyStatusManager enemyStatusManager)
    {
        killScore++; // 스코어 증가
    }

    void Update()
    {
        // 스테이지 클리어 조건 -> 소환된 적을 모두 처치해야함
        // 중간, 최종 보스에서는 추가조건 구현
        if (!isStageCheck)
        {
            bool cleared = false;
            if (!stageManager.isMiddleBoss && !stageManager.isFinalBoss)
            {
                cleared = killScore == maximumNumber;
            }
            else if (stageManager.isMiddleBoss && !stageManager.isFinalBoss)
            {
                if (bossStatusManager.hasTriggeredHalfHealthEvent == true)
                {
                    ResumeSpawning();
                }

                cleared = isMiddleBossClear && killScore == maximumNumber;

                if (killScore == maximumNumber)
                {
                    bossStatusManager.hasTriggeredHalfHealthEvent = false;
                }
            }
            else if (stageManager.isFinalBoss)
            {
                if (bossStatusManager.hasTriggeredHalfHealthEvent == true)
                {
                    ResumeSpawning();
                }

                // 최종보스는 보스만 잡아서 클리어 되게 killScore == maximumNumber 조건 제거
                cleared = isFinalBossClear;
            }

            if (cleared)
            {
                StartCoroutine(PauseAfterDelay());
            }
        }
    }

    // 다음 스테이지로 넘어갈 때마다 호출
    // 스테이지에 등장할 적의 수 등을 조절
    private IEnumerator PauseAfterDelay()
    {
        isStageCheck = true;
        killScore = 0;
        stageManager.PauseGame();

        switch (stageManager.stageNumber)
        {
            case 1: maximumNumber = 6; break;
            case 2: maximumNumber = 8; break;
            case 3:
                maximumNumber = 10;
                numberOfEnemySpawnedAtOnce = 2; break;
            case 4:
                maximumNumber = 11;
                numberOfEnemySpawnedAtOnce = 2; break;
            case 5:
                maximumNumber = 10;
                numberOfEnemySpawnedAtOnce = 2; break;
            case 6:
                maximumNumber = 12;
                numberOfEnemySpawnedAtOnce = 1;
                break;
            case 7: maximumNumber = 13; break;
            case 8: maximumNumber = 14; break;
            case 9: maximumNumber = 15; break;
            case 10: maximumNumber = 10; break;
            default: break;
        }

        yield return new WaitForSeconds(1f);
    }

    // 중간 보스용 스폰 정지 메서드
    public void ResumeSpawning()
    {
        isSpawningPaused = false;
    }
    public void PauseSpawning()
    {
        isSpawningPaused = true;
        if (stageManager.isMiddleBoss)
            hasPausedForMiddleBoss = true;
        if (stageManager.isFinalBoss)
            hasPausedForFinalBoss = true;
    }

    // enemy 배열에서 랜덤으로 선택해, 타입에 맞는 위치에다가 적이 소환됨을
    // 표시하는 타일을 생성하는 코루틴
    private IEnumerator SpawnTile()
    {
        // 초기화
        isStageCheck = false;
        isSpawningPaused = false;
        float originalSpawnTime = enemySpawnTime;
        float originalTileLatency = enemyTileLatency;

        yield return new WaitForSeconds(enemyTileLatency);

        // 배열 초기화 및 구성
        enemyPrefabArray = new GameObject[maximumNumber];
        spawnPositions = new Vector3[maximumNumber];

        int[] weights = BuildWeightsForStage(stageManager.stageNumber);
        for (int i = 0; i < maximumNumber; i++)
        {
            int pick = PickIndexByWeight(weights);

            // Shield 타입 제외 (최종보스에서만)
            if (stageManager.isFinalBoss)
            {
                int safety = 0;
                while ((pick == 8 || pick == 9 || pick == 10) && safety < 20)
                {
                    pick = PickIndexByWeight(weights);
                    safety++;
                }
                if (safety >= 20)
                {
                    // 1~7 사이의 랜덤 값으로 pick을 강제 설정
                    pick = UnityEngine.Random.Range(1, 8); // 1 이상 8 미만 → 1~7
                }
            }

            if (pick < 0 || enemyPrefabs == null || pick >= enemyPrefabs.Length || enemyPrefabs[pick] == null)
            {
                pick = GetFirstValidPrefabIndex();

                if (pick == -1)
                {
                    Debug.LogWarning("소환 가능한 Enemy Prefab이 없습니다.");
                    break;
                }
            }
            enemyPrefabArray[i] = enemyPrefabs[pick];
        }

        currentNumber = 0;

        // 중간보스 시작 시 30초 대기 후 소환 시작
        if (stageManager.isMiddleBoss && !isMiddleBossFirstSpawn)
        {
            isMiddleBossFirstSpawn = true;
            yield return new WaitForSeconds(30f);
            isMiddleBossFirstSpawn = false;
        }

        // 최종보스 시작 시 30초 대기 후 소환 시작
        if (stageManager.isFinalBoss && !isFinalBossFirstSpawn)
        {
            isFinalBossFirstSpawn = true;
            yield return new WaitForSeconds(30f);
            isFinalBossFirstSpawn = false;
        }

        while (stageManager.isStart)
        {
            // 중간보스 진행 중 절반쯤에서 중지 (기존 로직 유지)
            if (stageManager.isMiddleBoss && !hasPausedForMiddleBoss && !isSpawningPaused && currentNumber >= maximumNumber / 2)
            {
                isSpawningPaused = true;
                hasPausedForMiddleBoss = true;
                Debug.Log("보스전 중간 페이즈, 적 소환을 일시 중지합니다.");
            }

            // 최종보스 진행 중 절반쯤에서 중지 (기존 로직 유지)
            if (stageManager.isFinalBoss && !hasPausedForFinalBoss && !isSpawningPaused && currentNumber >= maximumNumber / 2)
            {
                isSpawningPaused = true;
                hasPausedForFinalBoss = true;
                Debug.Log("최종보스전 중간 페이즈, 적 소환을 일시 중지합니다.");
            }

            if (isSpawningPaused)
            {
                yield return null;
                continue;
            }

            int spawnCount = Mathf.Min(numberOfEnemySpawnedAtOnce, maximumNumber - currentNumber);
            for (int i = 0; i < spawnCount; i++)
            {
                GameObject point = spawnPointMemoryPool.ActivatePoolItem();
                if (point == null) continue;

                // 스폰 영역 선택
                activeWorldBounds = firstWorldSpawnBounds;
                if (stageManager.stageNumber == 10 && finalBossWorldSpawnBounds != null)
                    activeWorldBounds = finalBossWorldSpawnBounds;
                else if (stageManager.stageNumber >= 5 && middleBossWorldSpawnBounds != null)
                    activeWorldBounds = middleBossWorldSpawnBounds;

                int randomSide = UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;
                int prefabIndexForThisSpawn = enemyPrefabs != null ? Array.IndexOf(enemyPrefabs, enemyPrefabArray[currentNumber]) : -1;
                bool isShield = prefabIndexForThisSpawn >= 8 && prefabIndexForThisSpawn <= 10;
                EnemySize sizeForThisSpawn = GetEnemySizeForPrefab(enemyPrefabArray[currentNumber]);

                int spawnBoundsIndex;
                if (isShield)
                {
                    spawnBoundsIndex = (sizeForThisSpawn == EnemySize.Small) ? IDX_SHIELD_HIGH : (sizeForThisSpawn == EnemySize.Medium ? IDX_SHIELD_MEDIUM : IDX_SHIELD_BIG);
                }
                else
                {
                    spawnBoundsIndex = (sizeForThisSpawn == EnemySize.Small) ? (randomSide == 1 ? IDX_RIGHT_HIGH : IDX_LEFT_HIGH) : (sizeForThisSpawn == EnemySize.Medium ? IDX_MEDIUM_LOW : IDX_BIG_LOW);
                }

                Bounds spawnBounds = activeWorldBounds != null && activeWorldBounds.Length > 0
                    ? activeWorldBounds[Mathf.Clamp(spawnBoundsIndex, 0, activeWorldBounds.Length - 1)]
                    : default;

                Vector3 newPosition;
                bool validPosition;
                int attemptCount = 0;
                int maxAttempts = 50;
                float minDistance = 3.0f;
                do
                {
                    if (spawnBoundsIndex == IDX_MEDIUM_LOW || spawnBoundsIndex == IDX_BIG_LOW || spawnBoundsIndex == IDX_SHIELD_HIGH || spawnBoundsIndex == IDX_SHIELD_MEDIUM || spawnBoundsIndex == IDX_SHIELD_BIG)
                    {
                        if (isShield)
                        {
                            newPosition = new Vector3(
                                UnityEngine.Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                                spawnBounds.center.y,
                                spawnBounds.min.z
                            );
                        }
                        else
                        {
                            newPosition = new Vector3(
                                UnityEngine.Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                                spawnBounds.center.y,
                                UnityEngine.Random.Range(spawnBounds.min.z, spawnBounds.max.z)
                            );
                        }
                    }
                    else
                    {
                        newPosition = new Vector3(
                            UnityEngine.Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                            UnityEngine.Random.Range(spawnBounds.min.y, spawnBounds.max.y),
                            UnityEngine.Random.Range(spawnBounds.min.z, spawnBounds.max.z)
                        );
                    }

                    validPosition = true;
                    for (int j = 0; j < currentNumber; j++)
                    {
                        if (Vector3.Distance(spawnPositions[j], newPosition) < minDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                    attemptCount++;
                    if (attemptCount >= maxAttempts)
                    {
                        Debug.LogWarning("적 스폰 위치가 반복 충돌로 강제로 배정됨.");
                        break;
                    }
                } while (!validPosition);

                point.transform.position = newPosition;
                spawnPositions[currentNumber] = newPosition;
                StartCoroutine(SpawnEnemy(point, currentNumber, newPosition, randomSide, spawnBounds));
                currentNumber++;
            }

            if (currentNumber >= maximumNumber)
                break;

            yield return new WaitForSeconds(enemySpawnTime);
        }

        enemySpawnTime = originalSpawnTime;
        enemyTileLatency = originalTileLatency;
    }

    private static int PickIndexByWeight(int[] weights)
    {
        if (weights == null || weights.Length == 0) return -1;
        int total = 0;
        for (int i = 0; i < weights.Length; i++) total += Mathf.Max(0, weights[i]);
        if (total <= 0) return -1;
        int r = UnityEngine.Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            acc += Mathf.Max(0, weights[i]);
            if (r < acc) return i;
        }
        return weights.Length - 1;
    }

    private int[] BuildWeightsForStage(int stage)
    {
        int n = enemyPrefabs != null ? enemyPrefabs.Length : 0;
        var w = new int[n];
        if (n < 11) return w;
        if (stage <= 1)
        { w[0] = 60; w[1] = 30; w[2] = 10; }
        else if (stage == 2)
        { w[0] = 45; w[1] = 25; w[2] = 10; w[3] = 12; w[4] = 6; w[5] = 2; }
        else if (stage == 3)
        { w[0] = 38; w[1] = 22; w[2] = 8; w[3] = 12; w[4] = 6; w[5] = 2; w[6] = 8; w[7] = 4; }
        else if (stage >= 4 && stage <= 5)
        { w[0] = 35; w[1] = 20; w[2] = 7; w[3] = 10; w[4] = 5; w[5] = 2; w[6] = 8; w[7] = 4; w[8] = 6; w[9] = 2; w[10] = 1; }
        else if (stage >= 6 && stage <= 7)
        { w[0] = 30; w[1] = 20; w[2] = 8; w[3] = 10; w[4] = 6; w[5] = 3; w[6] = 9; w[7] = 5; w[8] = 7; w[9] = 3; w[10] = 2; }
        else
        { w[0] = 26; w[1] = 18; w[2] = 10; w[3] = 10; w[4] = 7; w[5] = 4; w[6] = 9; w[7] = 6; w[8] = 8; w[9] = 4; w[10] = 2; }
        if (stage < 2) { w[3] = 0; w[4] = 0; w[5] = 0; }
        if (stage < 3) { w[6] = 0; w[7] = 0; }
        if (stage < 4) { w[8] = 0; w[9] = 0; w[10] = 0; }
        for (int i = 0; i < n; i++) if (enemyPrefabs[i] == null) w[i] = 0;
        return w;
    }

    private int GetFirstValidPrefabIndex()
    {
        if (enemyPrefabs == null) return -1;
        for (int i = 0; i < enemyPrefabs.Length; i++) if (enemyPrefabs[i] != null) return i;
        return -1;
    }

    private EnemySize GetEnemySizeForPrefab(GameObject prefab)
    {
        if (prefab == null || enemyPrefabs == null) return EnemySize.Small;
        int idx = Array.IndexOf(enemyPrefabs, prefab);
        if (idx < 0) return EnemySize.Small;
        if (idx == 0 || idx == 3 || idx == 8 || idx == 11) return EnemySize.Small;
        if (idx == 1 || idx == 4 || idx == 6 || idx == 9 || idx == 12) return EnemySize.Medium;
        return EnemySize.Big;
    }
    
    // 소환 타일 위치에 실제 Enemy를 소환하는 코루틴
    private IEnumerator SpawnEnemy(GameObject point, int index, Vector3 spawnPosition, int randomSide, Bounds spawnBounds)
    {
        yield return new WaitForSeconds(enemySpawnLatency);

        GameObject prefabToSpawn = enemyPrefabArray[index];
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"배열의 인덱스 {index}에 프리팹이 없습니다.");
            yield break;
        }

        int prefabIndex = System.Array.IndexOf(enemyPrefabs, prefabToSpawn);
        if (prefabIndex < 0)
        {
            Debug.LogError($"Prefab {prefabToSpawn.name} is not in the enemyPrefabs list.");
            yield break;
        }

        GameObject item = enemyPool.ActivatePoolItem(prefabIndex);
        if (item == null)
        {
            Debug.LogError($"Failed to activate enemy from pool for prefab index {prefabIndex}");
            yield break;
        }

        item.transform.position = point.transform.position;
        item.transform.rotation = Quaternion.identity;

        EnemyFSM enemyFSM = item.GetComponentInChildren<EnemyFSM>();
        EnemyStatusManager enemyStatusManager = item.GetComponentInChildren<EnemyStatusManager>();
        EnemySmallSpawn smallSpawn = item.GetComponentInChildren<EnemySmallSpawn>();
        if (smallSpawn != null)
        {
            int startIndex = (randomSide == 1) ? IDX_RIGHT_START : IDX_LEFT_START;
            Bounds startBounds = (firstWorldSpawnBounds != null && startIndex >= 0 && startIndex < firstWorldSpawnBounds.Length)
                ? firstWorldSpawnBounds[startIndex]
                : default;
            smallSpawn.StartMovement(spawnPosition, randomSide, startBounds);
        }

        // stage별 Enemy 공격력, 체력 설정
        if (enemyStatusManager != null)
        {
            switch (stageManager.stageNumber)
            {
                case <= 3:
                    enemyStatusManager.SetEnemyHPForStage(stageManager.stageNumber);
                    break;
                case >= 4 and <= 7:
                    numberOfEnemySpawnedAtOnce = 2;
                    enemyStatusManager.SetEnemyHPForStage(stageManager.stageNumber);
                    enemyStatusManager.SetEnemyDamageForStage(stageManager.stageNumber);
                    break;
                case >= 8 and <= 9:
                    numberOfEnemySpawnedAtOnce = 3;
                    enemyStatusManager.SetEnemyHPForStage(stageManager.stageNumber * 2);
                    enemyStatusManager.SetEnemyDamageForStage(stageManager.stageNumber);
                    break;
                case 10:
                    numberOfEnemySpawnedAtOnce = 1;
                    enemyStatusManager.SetEnemyHPForStage(stageManager.stageNumber * 4);
                    enemyStatusManager.SetEnemyDamageForStage(stageManager.stageNumber * 2);
                    break;
                default:
                    break;
            }
            enemyFSM.SetSpawnArea(spawnBounds);
            enemyFSM.Setup(target);
        }
        else
        {
            Debug.LogError("EnemyFSM 컴포넌트를 찾을 수 없습니다.");
        }

        enemyPrefabArray[index] = null;
        spawnPointMemoryPool.DeactivatePoolItem(point);
    }

    /// <summary>
    /// firstWorldSpawnBounds 내에서 랜덤 스폰 위치를 반환합니다. 튜토리얼 등에서 사용됩니다.
    /// </summary>
    /// <param name="enemyPrefab">소환할 적 프리팹 (크기 및 타입 확인용)</param>
    /// <returns>계산된 스폰 위치</returns>
    public SpawnInfo GetRandomSpawnPositionInFirstWorld(GameObject enemyPrefab)
    {
        // prefab 인덱스 확인
        int prefabIndex = enemyPrefabs != null ? Array.IndexOf(enemyPrefabs, enemyPrefab) : -1;
        bool isTutorialPrefab = prefabIndex >= 12 && prefabIndex <= 14;

        // 기존 로직 기준으로 크기 판정
        EnemySize enemySize = GetEnemySizeForPrefab(enemyPrefab);

        // 튜토리얼 전용 프리팹이면 강제 크기 지정 (Inspector에 12=small,13=medium,14=big)
        if (isTutorialPrefab)
        {
            if (prefabIndex == 11) enemySize = EnemySize.Small;
            else if (prefabIndex == 12) enemySize = EnemySize.Medium;
            else if (prefabIndex == 13) enemySize = EnemySize.Big;
        }

        // SpawnTile과 동일한 바운드 인덱스 결정 로직 (isShield 사용 제거)
        int randomSide = UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;
        int spawnBoundsIndex;
        if (enemySize == EnemySize.Small)
        {
            spawnBoundsIndex = (randomSide == 1) ? IDX_RIGHT_HIGH : IDX_LEFT_HIGH;
        }
        else if (enemySize == EnemySize.Medium)
        {
            spawnBoundsIndex = IDX_MEDIUM_LOW;
        }
        else // Big
        {
            spawnBoundsIndex = IDX_BIG_LOW;
        }

        // 바운드 취득
        Bounds spawnBounds = (firstWorldSpawnBounds != null && spawnBoundsIndex >= 0 && spawnBoundsIndex < firstWorldSpawnBounds.Length)
            ? firstWorldSpawnBounds[spawnBoundsIndex]
            : default;

        if (spawnBounds.size == Vector3.zero)
        {
            Debug.LogWarning("유효한 스폰 바운드를 찾을 수 없어 (0,0,0) 반환");
            return new SpawnInfo { Position = Vector3.zero, Bounds = spawnBounds };
        }

        // 바운드 내에서 충돌 회피하며 랜덤 위치 계산 (SpawnTile과 일치하도록 분기)
        Vector3 newPosition;
        if (spawnBoundsIndex == IDX_MEDIUM_LOW || spawnBoundsIndex == IDX_BIG_LOW)
        {
            // 중/대형 영역은 y는 center 고정, x/z는 범위 내 랜덤
            newPosition = new Vector3(
                UnityEngine.Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                spawnBounds.center.y,
                UnityEngine.Random.Range(spawnBounds.min.z, spawnBounds.max.z)
            );
        }
        else
        {
            // Right/Left High 등은 x,y,z 모두 범위 내 랜덤
            newPosition = new Vector3(
                UnityEngine.Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                UnityEngine.Random.Range(spawnBounds.min.y, spawnBounds.max.y),
                UnityEngine.Random.Range(spawnBounds.min.z, spawnBounds.max.z)
            );
        }

        return new SpawnInfo { Position = newPosition, Bounds = spawnBounds };
    }

    /// <summary>
    /// 튜토리얼용으로 특정 적을 지정된 위치에 소환합니다.
    /// </summary>
    /// <param name="enemyPrefab">소환할 적 프리팹</param>
    /// <param name="spawnPosition">소환할 위치</param>
    /// <returns>소환된 적 게임오브젝트</returns>
    public GameObject SpawnTutorialEnemy(GameObject enemyPrefab, Vector3 spawnPosition)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("튜토리얼 적 소환 실패: 전달된 enemyPrefab이 null입니다.");
            return null;
        }

        // enemyPrefabs 배열에서 전달된 프리팹의 인덱스를 찾습니다.
        int prefabIndex = System.Array.IndexOf(enemyPrefabs, enemyPrefab);
        if (prefabIndex < 0)
        {
            Debug.LogError($"소환하려는 프리팹 '{enemyPrefab.name}'이(가) EnemyMemoryPool의 enemyPrefabs 목록에 없습니다.");
            return null;
        }

        // 메모리 풀에서 적을 활성화합니다.
        GameObject enemy = enemyPool.ActivatePoolItem(prefabIndex);
        if (enemy == null)
        {
            Debug.LogError($"메모리 풀에서 '{enemyPrefab.name}' 프리팹에 해당하는 적을 활성화하는데 실패했습니다.");
            return null;
        }

        // 위치와 회전 값을 설정합니다.
        enemy.transform.position = spawnPosition;
        enemy.transform.rotation = Quaternion.identity;

        return enemy;
    }
}
