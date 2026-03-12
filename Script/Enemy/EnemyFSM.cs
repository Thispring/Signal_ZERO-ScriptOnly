using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy의 행동 패턴을 관리하는 스크립트입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
/// 
/// <remarks>
/// EnemyBehaviorSystem에 구현되어 있는 행동 메서드를 상황에 맞게 호출합니다.
/// </remarks>
public class EnemyFSM : MonoBehaviour
{
    [Header("References")]
    // status는 Projectile에 정지 신호를 요청해야 할 상황이 있기에 public으로 설정
    // 현재 텔레포트 시 Projectile 정지에 사용 중
    public EnemyStatusManager status;
    private EnemyEffectController effectController;
    private EnemyBehaviorSystem behaviorSystem;

    // 공격 관련 변수
    [Header("Attack")]
    public Transform[] projectileSpawnPoint; // 발사체 발사 위치
    public GameObject projectile; // 발사체 프리팹
    public Transform target;   // Enemy가 공격할 타겟
    [SerializeField]
    private Transform originalTarget; // 기존 타겟 (기본은 플레이어)
    public bool isAttackSign = false; // 공격 신호를 위한 변수
    public Coroutine attackCoroutine; // 공격 코루틴 참조
    public int attackCount = 0; // 공격 횟수

    [Header("Missile")]
    public GameObject enemyMissilePrefab; // EnemyMissile 프리팹

    // 이동 관련 변수
    [Header("Move")]
    public bool isMoving = true; // 움직임 상태 플래그
    public Coroutine moveCoroutine; // Move 코루틴 참조
    public Transform enemySpawnArea; // Enemy가 소환된 영역
    public Bounds enemySpawnBounds; // Enemy가 소환된 영역의 Bounds 정보
    public bool isTeleporting = false; // 텔레포트 중인지 여부
    private bool canTeleport = false; // 텔레포트 가능 여부 (50% 확률)

    [Header("Tutorial")]
    // 튜토리얼 용, 적 대기 상태 변수
    public bool isTutorialSpawn = false;

    void Awake()
    {
        effectController = GetComponent<EnemyEffectController>();
        status = GetComponent<EnemyStatusManager>();
        behaviorSystem = GetComponent<EnemyBehaviorSystem>();

        effectController.StunEffect(false);

        if (TutorialController.Instance != null &&
        TutorialController.Instance.isTutorialClear == true)
        {
            isTutorialSpawn = true;
        }
    }

    void Start()
    {
        // Guard 시스템 이벤트 구독
        SupportBotFSM.OnGuardActivated += HandleGuardActivated;
        SupportBotFSM.OnGuardDeactivated += HandleGuardDeactivated;

        // 50% 확률로 텔레포트 가능 여부 결정
        canTeleport = Random.value > 0.5f;

        isAttackSign = false;
    }

    void OnEnable()
    {
        // EMP 이벤트 구독
        EMP.OnEmpActivated += HitEMP;
    }

    void OnDisable()
    {
        // EMP 이벤트 구독 해제 (메모리 누수 방지)
        EMP.OnEmpActivated -= HitEMP;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        SupportBotFSM.OnGuardActivated -= HandleGuardActivated;
        SupportBotFSM.OnGuardDeactivated -= HandleGuardDeactivated;
        EMP.OnEmpActivated -= HitEMP;
    }

    void Update()
    {
        // 3스테이지 이후, 체력이 절반이하, 텔레포트 가능 상태일 때 텔레포트 진행
        if (canTeleport && status.isHalfHP && !status.isDead && !isTeleporting
            && StageManager.Instance.stageNumber >= 3)
        {
            behaviorSystem.StartTeleportMove();
        }
        /*
        // TEST: 텔레포트 테스트
        if (status.isHalfHP && !status.isDead && !isTeleporting)
        {
            behaviorSystem.StartTeleportMove();
        }
        */
    }

    public void Setup(Transform target)
    {
        isTeleporting = false;
        this.target = target; // 타겟 설정
        originalTarget = target; // 원래 타겟 저장

        // 스폰 시 현재 가드 상태 체크 (가드 발동 후 소환된 적 처리)
        CheckCurrentGuardState();

        StartCoroutine(WaitIdleTime()); // Idle 애니메이션을 위한 상태 대기
        StartCoroutine(WaitFirstAttackTime());
    }

    private void CheckCurrentGuardState()
    {
        // 스폰 시점에 가드가 활성화되어 있는지 확인
        if (SupportBotFSM.IsAnyGuardActive && SupportBotFSM.CurrentGuardBot != null)
        {
            OnSupportBotGuard(SupportBotFSM.CurrentGuardBot);
        }
    }

    // EnemyMemoryPool을 통해 자신의 스폰 영역을 받아옴
    public void SetSpawnArea(Bounds spawnArea)
    {
        // Bounds 정보를 직접 저장
        enemySpawnBounds = spawnArea;

    }

    private IEnumerator WaitFirstAttackTime()
    {
        // Big 타입은 소환 애니메이션이 길기 때문에, 1.5초 동안 BoxCollider 비활성화 필요
        if (status.setting.enemySize == EnemySize.Big)
        {
            // 모든 BoxCollider를 비활성화
            BoxCollider[] boxColliders = GetComponents<BoxCollider>();
            foreach (BoxCollider boxCollider in boxColliders)
            {
                boxCollider.enabled = false; // 비활성화
            }
            yield return new WaitForSeconds(0.8f);
            foreach (BoxCollider boxCollider in boxColliders)
            {
                boxCollider.enabled = true; // 활성화
            }
        }

        // isTutorialSpawn이 true가 되거나, 튜토리얼이 완료될 때까지 대기
        yield return new WaitUntil(() =>
        {
            // 튜토리얼이 완료되었다면 isTutorialSpawn을 true로 설정
            if (TutorialController.Instance != null &&
                TutorialController.Instance.isTutorialClear == true)
            {
                isTutorialSpawn = true;
            }

            // isTutorialSpawn이 true가 되면 조건 만족
            return isTutorialSpawn;
        });

        yield return new WaitForSeconds(1f);
        behaviorSystem.StartAttack(); // BehaviorSystem을 통해 공격 시작 
    }

    // 첫 소환 시, Idle 애니메이션 유지를 위한 대기 코루틴
    private IEnumerator WaitIdleTime()
    {
        // Small 타입의 경우, 비행을 하며 소환되는 컨셉으로, 별개로 관리
        EnemySmallSpawn enemySmallSpawn = GetComponent<EnemySmallSpawn>();
        if (enemySmallSpawn != null)
        {
            // enemySmallSpawn.isMoving이 true인 동안 대기
            while (enemySmallSpawn.isMoving)
            {
                isAttackSign = true;
                yield return null; // 다음 프레임까지 대기
                isAttackSign = false;
            }
        }
        else
        {
            if (status.setting.enemySize == EnemySize.Big)
            {
                // 모든 BoxCollider를 비활성화
                BoxCollider[] boxColliders = GetComponents<BoxCollider>();
                foreach (BoxCollider boxCollider in boxColliders)
                {
                    boxCollider.enabled = false; // 비활성화
                }
            }
        }
    }

    // 이벤트 핸들러 함수들
    private void HandleGuardActivated(Transform guardBot)
    {
        OnSupportBotGuard(guardBot);
    }

    private void HandleGuardDeactivated(Transform guardBot)
    {
        OffSupportBotGuard(guardBot);
    }

    public void OnSupportBotGuard(Transform supportBot)
    {
        // 서포트 봇의 가드 사용 시 타겟을 서포트 봇으로 변경
        target = supportBot;
    }

    public void OffSupportBotGuard(Transform supportBot)
    {
        // 서포트 봇의 가드 사용이 해제되면 타겟을 기존 타겟(플레이어)으로 복원
        if (target == supportBot)
        {
            target = originalTarget;
        }
    }

    // 상태 이상 관련 메서드
    public void HitEMP(int stunTime)
    {
        StartCoroutine(Stun(stunTime));
    }

    // EMP를 맞으면 5초간 소환된 Enemy 정지
    private IEnumerator Stun(int stunTime)
    {
        effectController.StunEffect(true);

        int randomRateTime = Random.Range(4, 8); // 4초에서 8초 사이의 랜덤 시간

        // Enemy가 공격, 이동 중일 때 해당 행동을 멈추고, 일정 시간 후 재개
        if (isMoving)
        {
            behaviorSystem.StopMove();
            yield return new WaitForSeconds(stunTime);
            effectController.StunEffect(false);
            yield return new WaitForSeconds(randomRateTime); // 랜덤 시간 대기
            behaviorSystem.StartMove();
        }

        if (!isAttackSign)
        {
            behaviorSystem.StopAttack();
            effectController.isNextAttackWaitStop = true; // 공격 대기 이펙트 활성화
            yield return new WaitForSeconds(stunTime);
            effectController.StunEffect(false);
            effectController.isNextAttackWaitStop = false; // 공격 대기 이펙트 비활성화
            yield return new WaitForSeconds(randomRateTime); // 랜덤 시간 대기
            behaviorSystem.StartAttack();
        }

        // 만약 위 두 조건에 걸리지 않았다면 실행
        yield return new WaitForSeconds(stunTime);
        effectController.StunEffect(false);
    }
}
