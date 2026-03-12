using UnityEngine;
using System.Collections;

/// <summary>
/// Boss의 구체적인 행동 로직을 관리하는 스크립트입니다.
/// 모든 Boss에 관련한 스크립트는 Boss를 앞에 붙여줍니다.
/// </summary>
public class BossBehaviorSystem : MonoBehaviour
{
    // 공격 종료 신호 이벤트
    public event System.Action OnMissileAttackFinished;
    public event System.Action OnNormalAttackFinished;
    
    [Header("References")]
    private BossStatusManager status; 
    private BossFSM bossFSM; 
    private BossEffectController effect;
    private BossAnimatorController animator; 
    // 보스가 사용하는 발사체 Pool
    private MemoryPool missilePool;
    private MemoryPool projectilePool;
    
    [Header("Prefab")]
    [SerializeField]
    private GameObject[] bossMissile = new GameObject[20];
    [SerializeField]
    private GameObject[] bossProjectiles = new GameObject[20];

    [Header("Attack State")]
    // 다음 공격을 절반 수치로 수행할지 여부 (FSM에서 설정)
    [HideInInspector]
    public bool nextAttackIsHalf = false;

    [Header("Teleport")]
    private Vector3 originalPosition; // 초기 위치
    private int lastTeleportDirection = 0; // 마지막 텔레포트 방향 (-1: 왼쪽, 1: 오른쪽)
    private readonly Vector3 teleportEffectOffset = new Vector3(0f, 0.001625f, -0.00225f);

    [Header("Layer")]
    private const string ignoreRaycastLayer = "Ignore Raycast";
    private const string bossLayer = "Boss";
    // 레이어 변경 추적을 위한 리스트
    private System.Collections.Generic.List<GameObject> changedLayerObjects = new System.Collections.Generic.List<GameObject>();

    // 보스 억제/보호 상태
    [Header("Boss Protection/Suppress")]
    private bool isBossSuppressed = false;       // 공격 중지 + 모델 숨김
    private bool suppressionRoutineStarted = false;

    void Awake()
    {
        status = GetComponent<BossStatusManager>();
        bossFSM = GetComponent<BossFSM>();
        effect = GetComponent<BossEffectController>();
        animator = GetComponent<BossAnimatorController>();

        originalPosition = transform.position; // 초기 위치 저장

        // fireCount 기반으로 미사일 배열 크기 결정
        int missilePoolSize = Mathf.Max(1, status != null ? status.setting.fireCount : 1);
        bossMissile = new GameObject[missilePoolSize];

        int projectilePoolSize = Mathf.Max(1, status != null ? status.setting.fireCount : 1); // 공격당 1발 가정
        bossProjectiles = new GameObject[projectilePoolSize];

        // Memory Pool를 이용한 미사일 발사체 초기화
        if (bossFSM.missilePrefab != null)
        {
            missilePool = new MemoryPool(bossFSM.missilePrefab);
            missilePool.SetAllPoolObjectsParent(this.transform);
            for (int i = 0; i < bossMissile.Length; i++)
            {
                bossMissile[i] = missilePool.ActivatePoolItem();
                if (bossMissile[i] != null)
                    bossMissile[i].SetActive(false); // 비활성화 상태로 초기화
            }
        }

        // Memory Pool를 이용한 총알 발사체 초기화
        if (bossFSM.bossProjectilePrefab != null)
        {
            projectilePool = new MemoryPool(bossFSM.bossProjectilePrefab);
            projectilePool.SetAllPoolObjectsParent(this.transform);
            for (int i = 0; i < bossProjectiles.Length; i++)
            {
                bossProjectiles[i] = projectilePool.ActivatePoolItem();
                if (bossProjectiles[i] != null)
                {
                    // BossFSM 참조 주입
                    var ep = bossProjectiles[i].GetComponent<EnemyProjectile>();
                    if (ep != null)
                    {
                        ep.SetOwnerBoss(bossFSM);
                    }
                    bossProjectiles[i].SetActive(false); // 비활성화 상태로 초기화
                }
            }
        }
    }

    void Update()
    {
        // 중간보스일때만 텔레포트 발동
        if (status.setting.bossType == BossType.middle)
            HandleTeleportation();

        // 중간보스가 절반 페이즈에 진입하면 억제 상태로 전환
        if (!suppressionRoutineStarted && status != null && status.setting.bossType == BossType.middle && status.setting.bossPhase == BossPhase.halfHP)
        {
            suppressionRoutineStarted = true;
            StartBossSuppression();
        }
    }

    #region Teleport
    private void HandleTeleportation()
    {
        if (status == null || status.IsDead || status.MaxHP <= 0f)
        {
            return;
        }

        float hpRatio = status.CurrentHP / status.MaxHP;

        // 체력 비율 임계값 기반 텔레포트 트리거
        // 한 프레임에 하나만 처리되도록 높은 임계값부터 검사합니다.
        if (hpRatio <= 0.9f && status.CanTeleportAt(0.9f))
        {
            StartCoroutine(TeleportSequence());
        }
        else if (hpRatio <= 0.7f && status.CanTeleportAt(0.7f))
        {
            StartCoroutine(TeleportSequence());
        }
        else if (hpRatio <= 0.6f && status.CanTeleportAt(0.6f))
        {
            StartCoroutine(TeleportToOriginalPosition());
        }
        else if (hpRatio <= 0.4f && status.CanTeleportAt(0.4f))
        {
            StartCoroutine(TeleportSequence());
        }
        else if (hpRatio <= 0.2f && status.CanTeleportAt(0.2f))
        {
            StartCoroutine(TeleportSequence());
        }
        else if (hpRatio <= 0.1f && status.CanTeleportAt(0.1f))
        {
            StartCoroutine(TeleportToOriginalPosition());
        }
    }

    // 보스 억제 시작: 공격 중지, 데미지 면역, 모델 숨김, 스폰 일시정지 후 모든 적 정리 대기
    // 억제 상태는 보스의 체력이 절반 이하로 진입할 때 한 번만 시작됩니다. 
    private void StartBossSuppression()
    {
        isBossSuppressed = true;
        if (status != null) status.isTeleportProtected = true; // 데미지 무시 연동
        if (effect != null) effect.SetModelActive(false);

        // 스폰 일시정지 (이미 소환된 적만 남기고 추가 소환 중단)
        if (EnemyMemoryPool.Instance != null)
        {
            EnemyMemoryPool.Instance.PauseSpawning();
            StartCoroutine(WaitUntilAllEnemiesClearedThenResumeBoss());
        }
        else
        {
            // 인스턴스가 늦게 초기화되는 경우를 대비
            StartCoroutine(WaitForEnemyPoolThenPauseAndWait());
        }
    }

    private IEnumerator WaitForEnemyPoolThenPauseAndWait()
    {
        while (EnemyMemoryPool.Instance == null)
            yield return null;
        EnemyMemoryPool.Instance.PauseSpawning();
        yield return StartCoroutine(WaitUntilAllEnemiesClearedThenResumeBoss());
    }

    // 억제 상태 후 모든 스폰된 적이 제거되면 보스 재개(모델 표시/데미지 허용)
    private IEnumerator WaitUntilAllEnemiesClearedThenResumeBoss()
    {
        var pool = EnemyMemoryPool.Instance;
        if (pool == null) yield break;
        // 현재 웨이브의 적 전부 처치될 때까지 대기
        yield return new WaitUntil(() => pool.killScore >= pool.maximumNumber);

        // 보스전 재개: 모델 표시 및 데미지 허용
        isBossSuppressed = false;
        if (status != null) status.isTeleportProtected = false;
        if (effect != null) effect.SetModelActive(true);
        // 스폰은 계속 일시정지 상태로 두어 보스전에 집중 (필요 시 ResumeSpawning 호출 가능)
    }

    private IEnumerator TeleportSequence()
    {
        status.isTeleportProtected = true;
        effect.SetModelActive(false);
        effect.ActivateTeleportEffect(transform.position + teleportEffectOffset, 0);

        yield return new WaitForSeconds(0.8f);

        int direction = Random.Range(0, 2) == 0 ? -1 : 1;
        if (direction == lastTeleportDirection)
        {
            direction *= -1; // 이전과 같은 방향이면 반대로
        }
        lastTeleportDirection = direction;

        transform.position += new Vector3(50 * direction, 0, 0);

        effect.ActivateTeleportEffect(transform.position + teleportEffectOffset, 1);
        effect.SetModelActive(true);
        status.isTeleportProtected = false;
    }

    private IEnumerator TeleportToOriginalPosition()
    {
        status.isTeleportProtected = true;
        effect.SetModelActive(false);
        effect.ActivateTeleportEffect(transform.position + teleportEffectOffset, 0);

        yield return new WaitForSeconds(0.8f);

        transform.position = originalPosition;

        effect.ActivateTeleportEffect(transform.position + teleportEffectOffset, 1);
        effect.SetModelActive(true);
        status.isTeleportProtected = false;
    }
    #endregion
    
    #region Attack Methods
    // Boss의 공격을 관리하는 함수
    // 정수형 배열을 이용해 공격패턴을 정의합니다.
    public void NormalAttack()
    {
        if (isBossSuppressed) return; // 억제 중에는 공격 금지
        if (bossFSM.bossProjectilePrefab != null)
        {
            bool useHalf = nextAttackIsHalf;
            nextAttackIsHalf = false; // 1회성 플래그
            StartCoroutine(SpawnProjectiles(useHalf));
        }
    }

    private IEnumerator SpawnProjectiles(bool isHalf = false)
    {
        animator.SetBool("isAttack", true);
        SetBossLayerRecursively(gameObject, LayerMask.NameToLayer(ignoreRaycastLayer), true);
        // 스폰 포인트 유효성 체크
        if (bossFSM.projectileSpawnPoint == null || bossFSM.projectileSpawnPoint.Length == 0)
        {
            Debug.LogWarning("BossBehaviorSystem: projectileSpawnPoint가 비어 있습니다.");
            yield break;
        }

        int spawnPointCount = bossFSM.projectileSpawnPoint.Length;

        int effectiveCount = status.setting.fireCount;
        int effectiveDamage = status.setting.damage;
        if (isHalf)
        {
            effectiveCount = Mathf.Max(1, effectiveCount / 2);
            effectiveDamage = Mathf.Max(1, effectiveDamage / 2);
        }

        // final 보스일 때 드론 처치 비율에 따라 데미지 배수 적용
        if (status.setting.bossType == BossType.final)
        {
            // bossDroneCount, bossDroneMaxCount는 BossFSM에서 관리
            int droneCount = 0;
            int droneMaxCount = 0;
            if (bossFSM != null)
            {
                droneCount = bossFSM.bossDroneCount;
                droneMaxCount = bossFSM.bossDroneMaxCount > 0 ? bossFSM.bossDroneMaxCount : 1;
            }
            float ratio = (float)droneCount / droneMaxCount;
            if (ratio > 0.5f)
            {
                effectiveDamage *= 4;
            }
            else
            {
                effectiveDamage *= 2;
            }
        }

        // 바깥 루프: 설정된 공격 횟수만큼 반복 (필요 시 절반)
        for (int volley = 0; volley < effectiveCount; volley++)
        {
            int failedSpawnsThisVolley = 0; // 이번 볼리에서 풀 고갈로 실패한 횟수
            // 안쪽 루프: 스폰 포인트 개수만큼 각 위치/회전으로 발사
            for (int sp = 0; sp < spawnPointCount; sp++)
            {
                // 사용 가능한 비활성 발사체 찾기
                GameObject pooledProjectile = null;
                for (int k = 0; k < bossProjectiles.Length; k++)
                {
                    if (bossProjectiles[k] != null && !bossProjectiles[k].activeInHierarchy)
                    {
                        pooledProjectile = bossProjectiles[k];
                        break;
                    }
                }

                // 풀 고갈: 이번 스폰 포인트는 건너뛰고 실패 카운트 증가
                if (pooledProjectile == null)
                {
                    failedSpawnsThisVolley++;
                    continue;
                }

                Transform spawnT = bossFSM.projectileSpawnPoint[sp];
                if (spawnT == null)
                {
                    continue;
                }

                // 위치/회전 설정
                pooledProjectile.transform.position = spawnT.position;
                pooledProjectile.transform.rotation = spawnT.rotation;

                // 타겟 ‘위치’ 전달
                var projectileComponent = pooledProjectile.GetComponent<EnemyProjectile>();
                if (projectileComponent != null)
                {
                    Vector3 targetWorldPos = (bossFSM.target != null)
                        ? bossFSM.target.position
                        : spawnT.position + spawnT.forward * 20f; // 폴백

                    projectileComponent.Setup(targetWorldPos, effectiveDamage);
                }
                else
                {
                    Debug.LogError("EnemyProjectile 컴포넌트를 찾을 수 없습니다.");
                }

                pooledProjectile.SetActive(true);
            }

            // 이번 볼리에서 모든 스폰 포인트가 실패했다면(= k 시도 결과가 spawnPointCount에 도달), 루프 종료
            if (failedSpawnsThisVolley >= spawnPointCount)
                break;

            // 볼리 간 간격
            yield return new WaitForSeconds(status.setting.attackRate);
        }

        SetBossLayerRecursively(gameObject, LayerMask.NameToLayer(bossLayer), false);
        animator.SetBool("isAttack", false);
        // 일반 공격 종료 신호 전송
        OnNormalAttackFinished?.Invoke();
    }

    public void MissileAttack()
    {
        // Boss의 미사일 공격 로직을 구현합니다.
        if (isBossSuppressed) return; // 억제 중에는 공격 금지
        if (bossFSM.missilePrefab != null)
        {
            bool useHalf = nextAttackIsHalf;
            nextAttackIsHalf = false; // 1회성 플래그
            StartCoroutine(SpawnMissile(useHalf));
        }
    }

    private IEnumerator SpawnMissile(bool isHalf = false)
    {
        animator.SetBool("isAttack", true);
        changedLayerObjects.Clear(); // 리스트 초기화
        // 미사일 공격 시작 - 무적 상태 활성화
        SetBossLayerRecursively(gameObject, LayerMask.NameToLayer(ignoreRaycastLayer), true);

        int effectiveCount = bossMissile.Length;
        int effectiveDamage = status.setting.missileDamage > 0 ? status.setting.missileDamage : status.setting.damage;
        if (isHalf)
        {
            effectiveCount = Mathf.Max(1, effectiveCount / 2);
            effectiveDamage = Mathf.Max(1, effectiveDamage / 2);
        }

        // final 보스일 때 드론 처치 비율에 따라 데미지 배수 적용
        if (status.setting.bossType == BossType.final)
        {
            int droneCount = 0;
            int droneMaxCount = 0;
            if (bossFSM != null)
            {
                droneCount = bossFSM.bossDroneCount;
                droneMaxCount = bossFSM.bossDroneMaxCount > 0 ? bossFSM.bossDroneMaxCount : 1;
            }
            float ratio = (float)droneCount / droneMaxCount;
            if (ratio > 0.5f)
            {
                effectiveDamage *= 4;
            }
            else
            {
                effectiveDamage *= 2;
            }
        }

        for (int i = 0; i < effectiveCount; i++)
        {
            GameObject pooledMissile = bossMissile[i];
            if (pooledMissile != null && !pooledMissile.activeInHierarchy)
            {
                BossMissile bossMissile = pooledMissile.GetComponent<BossMissile>();
                if (bossMissile != null && status.setting.bossType == BossType.middle)
                {
                    // 미사일 발사 위치 설정
                    bossMissile.transform.position = bossFSM.missileSpawnPoint[bossFSM.missileSpawnNumber].position;
                    bossMissile.transform.rotation = bossFSM.missileSpawnPoint[bossFSM.missileSpawnNumber].rotation;

                    // sequenceCount 전달하여 체력 증가
                    int currentSequence = bossFSM != null ? bossFSM.sequenceCount : 0;
                    bossMissile.Setup(bossFSM.target.position, effectiveDamage, status.setting.missileHP, currentSequence);
                }
                else if (bossMissile != null && status.setting.bossType == BossType.final)
                {
                    // 미사일 발사 위치 설정
                    bossMissile.transform.position = bossFSM.missileSpawnPoint[bossFSM.missileSpawnNumber].position;
                    bossMissile.transform.rotation = bossFSM.missileSpawnPoint[bossFSM.missileSpawnNumber].rotation;

                    // sequenceCount 전달하여 체력 증가
                    int currentSequence = bossFSM != null ? bossFSM.sequenceCount : 0;
                    bossMissile.Setup(bossFSM.target.position, effectiveDamage, status.setting.missileHP, currentSequence);

                    // missileSpawnNumber를 0 -> 1 -> 0 순으로 토글
                    bossFSM.missileSpawnNumber = 1 - (bossFSM.missileSpawnNumber & 1);
                }

                pooledMissile.SetActive(true);

            }
            yield return new WaitForSeconds(0.5f); // 0.5초 간격으로 연속 발사
        }

        yield return new WaitForSeconds(2f);    // 미사일이 모두 날라오는 시간 고려
        // 미사일 공격 완료 - 무적 상태 해제 (변경된 오브젝트만 복구)
        SetBossLayerRecursively(gameObject, LayerMask.NameToLayer(bossLayer), false);

        // 미사일공격이 끝나면 다음 공격의 스폰위치를 변경
        // 안전(값이 0/1이 아닐 수도 있을 때)
        bossFSM.missileSpawnNumber = 1 - (bossFSM.missileSpawnNumber & 1);

        animator.SetBool("isAttack", false);
        // 미사일 공격 종료 신호 전송
        OnMissileAttackFinished?.Invoke();
    }
    #endregion

    // 부모와 자식 모두 레이어 변경 (Boss 레이어 오브젝트만 추적하여 변경)
    private void SetBossLayerRecursively(GameObject obj, int newLayer, bool isChangingToBossLayer)
    {
        if (obj == null) return;

        if (isChangingToBossLayer)
        {
            // Boss → Ignore Raycast로 변경할 때: Boss 레이어인 오브젝트만 변경하고 추적
            if (obj.layer == LayerMask.NameToLayer(bossLayer))
            {
                obj.layer = newLayer;
                changedLayerObjects.Add(obj); // 변경된 오브젝트 추적
            }
        }
        else
        {
            // Ignore Raycast → Boss로 복구할 때: 추적된 오브젝트만 복구
            if (changedLayerObjects.Contains(obj))
            {
                obj.layer = newLayer;
            }
        }

        foreach (Transform child in obj.transform)
        {
            SetBossLayerRecursively(child.gameObject, newLayer, isChangingToBossLayer);
        }
    }
}
