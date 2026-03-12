using UnityEngine;
using System;

/// <summary>
/// Boss의 소환을 관리하는 스크립트입니다.
/// 모든 Boss에 관련한 스크립트는 Boss을 앞에 붙여줍니다.
/// </summary>
public class BossSpawnManager : MonoBehaviour
{
    // Boss 소환 요청 이벤트 (BossStatusManager가 구독하여 ActiveBoss 실행)
    public static event Action OnBossSpawnRequested;

    [Header("References")]
    [SerializeField]
    private StageManager stageManager;  // 소환 신호 전달을 위한 참조
    [SerializeField]    // Inspector로 끌어서 할당
    private BossCutScene bossCutScene; 

    [Header("Boss Prefabs")]
    [SerializeField]
    private GameObject middleBossPrefab; // 중간 보스 프리팹
    [SerializeField]
    private GameObject finalBossPrefab; // 최종 보스 프리팹

    [Header("Spawn Point")]
    private Transform spawnPoint; // 보스 소환 위치(별도 할당 없으면 오브젝트 현재 위치 사용)

    [Header("Flags")]
    private bool middleBossSpawned = false;
    private bool finalBossSpawned = false;

    // 내부에서 보스를 구분하기 위한 열거형
    private enum BossKind { Middle, Final }

    void Awake()
    {
        // 기본 스폰 포인트가 없으면 자신의 트랜스폼 사용
        if (spawnPoint == null) spawnPoint = this.transform;
    }

    void OnEnable()
    {
        // 컷신 종료 훅
        bossCutScene.OnMiddleBossCutsceneEnd += HandleMiddleBossCutsceneEnd;
        bossCutScene.OnFinalBossCutsceneEnd += HandleFinalBossCutsceneEnd;

        // TEST:
        // StageManager의 보스 시작 이벤트
        //StageManager.OnMiddleBossStageStart += HandleMiddleBossStageStart_FromStageManager;
        //StageManager.OnFinalBossStageStart += HandleFinalBossStageStart_FromStageManager;
    }

    void OnDisable()
    {
        // 컷신 종료 훅 해제
        bossCutScene.OnMiddleBossCutsceneEnd -= HandleMiddleBossCutsceneEnd;
        bossCutScene.OnFinalBossCutsceneEnd -= HandleFinalBossCutsceneEnd;

        // TEST:
        // StageManager 테스트 트리거 해제
        //StageManager.OnMiddleBossStageStart -= HandleMiddleBossStageStart_FromStageManager;
        //StageManager.OnFinalBossStageStart -= HandleFinalBossStageStart_FromStageManager;
    }

    private void HandleMiddleBossCutsceneEnd()
    {
        // 컷신 종료 시에도 스테이지/플래그 기반 가드 적용
        TrySpawnBossIfEligible(BossKind.Middle);
    }

    private void HandleFinalBossCutsceneEnd()
    {
        // 컷신 종료 시에도 스테이지/플래그 기반 가드 적용
        TrySpawnBossIfEligible(BossKind.Final);
    }

    /* TEST:
    // StageManager에서 바로 보스 시작 신호가 올 때(테스트 용), 상태에 맞는 보스를 즉시 소환 (분리)
    public void HandleMiddleBossStageStart_FromStageManager()
    {
        // StageManager 테스트/직접 트리거도 동일한 가드 경로 사용
        TrySpawnBossIfEligible(BossKind.Middle);
    }

    public void HandleFinalBossStageStart_FromStageManager()
    {
        // StageManager 테스트/직접 트리거도 동일한 가드 경로 사용
        TrySpawnBossIfEligible(BossKind.Final);
    }
    */

    private void SpawnBoss(GameObject bossPrefab)
    {
        if (bossPrefab == null)
        {
            Debug.LogError("BossSpawnManager: Boss 프리팹이 설정되지 않았습니다.");
            return;
        }

        // 프리팹 인스턴스 생성 (Awake 시 BossStatusManager가 이벤트를 구독함)
        var pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        var rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        GameObject bossInstance = Instantiate(bossPrefab, pos, rot);

        // 인스턴스가 생성되고 Awake가 호출되어 구독한 뒤 이벤트 발행 → ActiveBoss 실행
        OnBossSpawnRequested?.Invoke();
    }

    private StageManager GetStageManager()
    {
        // 우선 싱글톤 사용, 없으면 Inspector에서 참조된 fallback 사용
        return StageManager.Instance != null ? StageManager.Instance : stageManager;
    }

    private bool IsEligibleToSpawn(BossKind kind)
    {
        var sm = GetStageManager();
        if (sm == null)
        {
            Debug.LogWarning("[BossSpawnManager] StageManager가 없습니다. 안전을 위해 보스 소환을 건너뜁니다.");
            return false;
        }

        switch (kind)
        {
            case BossKind.Middle:
                return sm.isMiddleBoss && sm.stageNumber == 5 && !middleBossSpawned;
            case BossKind.Final:
                return sm.isFinalBoss && sm.stageNumber == 10 && !finalBossSpawned;
            default:
                return false;
        }
    }

    private void TrySpawnBossIfEligible(BossKind kind)
    {
        if (!IsEligibleToSpawn(kind))
        {
            return;
        }

        switch (kind)
        {
            case BossKind.Middle:
                if (middleBossSpawned) return; // 중복 소환 방지
                SpawnBoss(middleBossPrefab);
                middleBossSpawned = true;
                break;
            case BossKind.Final:
                if (finalBossSpawned) return; // 중복 소환 방지
                SpawnBoss(finalBossPrefab);
                finalBossSpawned = true;
                break;
        }
    }
}
