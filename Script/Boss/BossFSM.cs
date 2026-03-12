using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Boss의 행동을 관리하는 스크립트입니다.
/// 모든 Boss에 관련한 스크립트는 Boss를 앞에 붙여줍니다.
/// </summary>
public class BossFSM : MonoBehaviour
{
    [Header("References")]
    private BossStatusManager status;
    [SerializeField]
    private BossBehaviorSystem behaviorSystem;

    [Header("Attack")]
    public Transform target;
    public GameObject bossProjectilePrefab;
    public Transform[] projectileSpawnPoint;
    public GameObject missilePrefab;
    public Transform[] missileSpawnPoint;
    public int missileSpawnNumber;

    [Header("Attack Loop State")]
    private float waitTimer;               // 남은 대기 시간(초)
    private bool isWaiting = true;         // 대기 중 여부
    private bool nextIsMissile = true;     // 다음 공격이 미사일인지 여부
    private bool isAttacking = false;      // 공격 수행 중 여부
    private int bossDroneMaxCountInternal = 0;  // 내부 최대 카운트 추적
    private int spawnCount = 0;          // 이번 사이클에 소환할 드론 수
    public int sequenceCount = 0;    // 보스 시퀀스 카운트 (BossBehaviorSystem에서 접근)
    // 이전 사이클에 소환된 드론 목록(다음 ResetWaitTimer에서 강제 비활성화 전용)
    private readonly List<GameObject> dronesSpawnedLastCycle = new List<GameObject>();

    [Header("BossDrone")]
    public GameObject bossDroneObject;
    private MemoryPool bossDronePool;
    public int bossDroneCount;
    public int bossDroneMaxCount;
    [SerializeField]
    private GameObject bossDroneSpawnArea;
    private Bounds bossDroneSpawnBound;

    [Header("UI")]
    public Slider waitAttackSlider;

    void Awake()
    {
        missileSpawnNumber = 0;
        status = GetComponent<BossStatusManager>(); // Boss 상태 매니저 컴포넌트 참조
        behaviorSystem = GetComponent<BossBehaviorSystem>(); // Boss 행동 시스템 컴포넌트 참조

        if (bossDroneSpawnArea != null)
            bossDroneSpawnBound = bossDroneSpawnArea.GetComponent<Collider>().bounds; // Boss Drone 소환 영역의 Bounds 설정

        if (bossDroneObject != null)
        {
            bossDronePool = new MemoryPool(bossDroneObject);
            for (int i = 0; i < bossDroneCount; i++)
            {
                GameObject obj = bossDronePool.ActivatePoolItem();
                obj.SetActive(false); // 생성 즉시 비활성화
            }
        }
    }

    void Start()
    {
        // 공격 종료 이벤트 구독
        if (behaviorSystem != null)
        {
            behaviorSystem.OnMissileAttackFinished += HandleMissileFinished;
            behaviorSystem.OnNormalAttackFinished += HandleNormalFinished;
        }
        // 드론 파괴 이벤트 구독
        BossDroneManager.OnDroneDestroyed += HandleDroneDestroyed;
        ResetWaitTimer();
    }

    void Update()
    {
        // 슬라이더는 0..1로 다음 공격까지의 진행률 표시
        if (waitAttackSlider != null && status != null && status.setting.patternDelay > 0f)
        {
            float duration = status.setting.patternDelay;
            float progress = Mathf.Clamp01(1f - (waitTimer / duration));
            waitAttackSlider.maxValue = 1f;
            waitAttackSlider.value = progress;
        }

        // 루틴: 대기 -> 공격 -> 대기
        if (isWaiting && !isAttacking && status.hasTriggeredHalfHealthEvent == false)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                isAttacking = true;
                // 공격 직전: 이번 사이클 소환 드론 강제 비활성화 (카운트 변경 없음)
                if (dronesSpawnedLastCycle.Count > 0)
                {
                    for (int i = 0; i < dronesSpawnedLastCycle.Count; i++)
                    {
                        var d = dronesSpawnedLastCycle[i];
                        if (d != null && d.activeInHierarchy)
                            d.SetActive(false);
                    }
                    // 이후 새 사이클에서 다시 채워지도록 즉시 비움
                    dronesSpawnedLastCycle.Clear();
                }
                // 드론 카운트 기준 공격 결정
                int currentCount = bossDroneCount;
                int maxCount = Mathf.Max(bossDroneMaxCount, bossDroneMaxCountInternal);
                if (currentCount <= 0)
                {
                    // 공격 스킵 후 타이머 리셋
                    isAttacking = false;
                    // Middle 보스만 패턴 토글
                    if (status != null && status.setting.bossType == BossType.middle)
                        nextIsMissile = !nextIsMissile;
                    ResetWaitTimer();
                    return;
                }

                bool isHalf = (currentCount < (maxCount + 1) / 2) && currentCount >= 1;
                if (behaviorSystem != null)
                    behaviorSystem.nextAttackIsHalf = isHalf;

                if (status != null && status.setting.bossType == BossType.final)
                {
                    // Final 보스는 미사일 공격만 구현
                    behaviorSystem?.MissileAttack();
                }
                else
                {
                    // Middle 보스는 번갈아 공격
                    if (nextIsMissile)
                        behaviorSystem?.MissileAttack();
                    else
                        behaviorSystem?.NormalAttack();
                }
            }
        }
    }

    private void ResetWaitTimer()
    {
        float baseTime = status != null ? status.setting.patternDelay : 2f;
        waitTimer = Mathf.Max(0f, baseTime);
        isWaiting = true;

        // 사이클 카운트 초기화: 이전 공격에서 남은 드론 카운트가 다음 공격에 영향 없도록
        bossDroneCount = 0;
        bossDroneMaxCount = 0;
        bossDroneMaxCountInternal = 0;

        // 다음 사이클용으로 목록 초기화 (강제 비활성화는 공격 직전에 수행)
        dronesSpawnedLastCycle.Clear();

        // 타이머 시작 시 드론 스폰 (한 사이클에 한 번)
        if (bossDronePool != null)
        {
            // 드론 소환 갯수 무작위로 결정
            if (status != null && status.setting.bossType == BossType.middle)
                spawnCount = Random.Range(2, 7); // 2~6
            else if (status != null && status.setting.bossType == BossType.final)
                spawnCount = Random.Range(4, 10); // 4~9

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject obj = bossDronePool.ActivatePoolItem();
                if (obj == null) continue;
                // 랜덤 위치 배치
                Vector3 pos = bossDroneSpawnBound.center;
                if (bossDroneSpawnArea != null)
                {
                    Bounds b = bossDroneSpawnBound;
                    pos = new Vector3(
                        Random.Range(b.min.x, b.max.x),
                        Random.Range(b.min.y, b.max.y),
                        Random.Range(b.min.z, b.max.z)
                    );
                }
                obj.transform.position = pos;

                // 드론에 현재 시퀀스 카운트 전달
                var droneManager = obj.GetComponent<BossDroneManager>();
                if (droneManager != null)
                {
                    droneManager.InitializeWithSequence(sequenceCount);
                }

                obj.SetActive(true);
                bossDroneCount += 1;
                bossDroneMaxCount = bossDroneCount;
                bossDroneMaxCountInternal = Mathf.Max(bossDroneMaxCountInternal, bossDroneCount);
                // 이번 사이클에 소환된 드론으로 기록
                dronesSpawnedLastCycle.Add(obj);
            }
        }
    }

    private void HandleMissileFinished()
    {
        isAttacking = false;
        nextIsMissile = false; // 다음 공격은 노멀
        sequenceCount++;
        ResetWaitTimer();
    }

    private void HandleNormalFinished()
    {
        isAttacking = false;
        nextIsMissile = true; // 다음 공격은 미사일
        sequenceCount++;
        ResetWaitTimer();
    }

    private void HandleDroneDestroyed()
    {
        bossDroneCount = Mathf.Max(0, bossDroneCount - 1);
    }

    void OnDestroy()
    {
        // 이벤트 해제
        if (behaviorSystem != null)
        {
            behaviorSystem.OnMissileAttackFinished -= HandleMissileFinished;
            behaviorSystem.OnNormalAttackFinished -= HandleNormalFinished;
        }
        BossDroneManager.OnDroneDestroyed -= HandleDroneDestroyed;
    }
}
