using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy가 발사하는 총알 발사체에 대한 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
/// 
/// <remarks>
/// 중간 보스의 공격 중, 발사체 연속 공격이 있으므로 BossFSM도 참조할 수 있도록 구현했습니다.
/// pool 방식으로 재사용할 수 있도록 구현했습니다.
/// </remarks>
public class EnemyProjectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private EnemyFSM enemyFSM;
    [SerializeField]
    private BossFSM bossFSM;
    private MemoryPool originPool;

    [Header("Vector3")]
    private Vector3 originPos; // 발사체의 원래 위치
    private Vector3 targetPos; // 발사체가 날아갈 목표 위치

    [Header("Values")]
    private float travelTime = 1f; // 타겟까지 도달하는 데 걸리는 시간
    private float acceleration; // 가속도
    private float currentSpeed; // 현재 속도
    private int projectileDamage;   //  발사체 데미지 (EnemySetting에서 damage 가져오기)

    [Header("Flags")]
    private bool isMoving = false; // 발사체 이동 상태 플래그

    void Awake()
    {
        // Rigidbody 추가
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true; // 물리 계산 비활성화

        originPos = transform.position; // 발사체의 원래 위치 저장

        if (enemyFSM == null) enemyFSM = GetComponentInParent<EnemyFSM>();    // 부모 EnemyFSM 스크립트 참조
        gameObject.SetActive(false);
    }

    // 발사체용 EMP 이벤트 구독
    // EMP 발동 시, 발사체 즉시 정지
    void OnEnable()
    {
        EMP.OnEmpProjectileActivated += HitEMP;
    }
    void OnDisable()
    {
        EMP.OnEmpProjectileActivated -= HitEMP;
    }

    void FixedUpdate()
    {
        // 부모 오브젝트가 비활성화되었는지 확인
        if (enemyFSM == null && bossFSM == null)
        {
            Debug.LogWarning("부모 EnemyFSM 오브젝트가 비활성화되었습니다. 발사체를 반환합니다.");
            StartCoroutine(WaitAttack());
            return; // 더 이상 Update 로직을 실행하지 않음
        }

        // 순간이동 중이라면, 프로젝타일을 대기 상태로 전환
        if (enemyFSM != null && enemyFSM.status.isTeleportProtected)
        {
            StartCoroutine(WaitAttack());
        }

        if (isMoving && !gameObject.activeInHierarchy) return; // 이동 중이라면 로직 실행 안 함

        // 타겟 방향으로 회전
        transform.LookAt(targetPos);

        // 회전 보정: X축을 목표 방향으로 정렬 (90도 회전)
        transform.rotation *= Quaternion.Euler(0, 0, 0);

        // 타겟까지의 거리 계산
        float distanceToTarget = Vector3.Distance(transform.position, targetPos);

        // 가속도 계산: a = 2 * d / t^2
        acceleration = 2 * distanceToTarget / (travelTime * travelTime);

        // 속도 계산: v = v0 + at
        currentSpeed += acceleration * Time.fixedDeltaTime;

        // 이동 방향 계산 (targetPos - 현재 위치)
        Vector3 direction = (targetPos - transform.position).normalized;

        // 이동
        transform.position += direction * currentSpeed * Time.fixedDeltaTime;

        // 목표 위치에 도달했는지 확인
        if (distanceToTarget <= 0.1f)
        {
            StartCoroutine(WaitAttack()); // 공격 종료 처리
        }
    }

    public void Setup(Vector3 position, int damage)
    {
        projectileDamage = damage;
        targetPos = position;
        currentSpeed = 0f; // 초기 속도 초기화
        isMoving = true; // 이동 상태 활성화

        // 스테이지 종료 이벤트 구독
        // 스테이지가 종료되면 게임 내 남은 발사체를 모두 제거
        if (StageManager.Instance != null)
        {
            StageManager.Instance.onPauseGameEvent.RemoveListener(OnPauseGame);
            StageManager.Instance.onPauseGameEvent.AddListener(OnPauseGame);
        }
    }

    // 외부에서 FSM을 참조하기 위한 메서드
    public void SetOwner(EnemyFSM fsm)
    {
        enemyFSM = fsm;
    }
    // 보스 공격 시 BossFSM도 참조할 수 있도록 메서드 추가
    public void SetOwnerBoss(BossFSM fsm)
    {
        bossFSM = fsm;
    }

    public void SetPool(MemoryPool pool)
    {
        originPool = pool;
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isHit = false; // 의도한 충돌을 체크하기 위한 플래그

        if (other.CompareTag("SupportBot"))
        {
            other.GetComponent<SupportBotStatusManager>().TakeDamage(projectileDamage);
            isHit = true;
        }
        else if (other.CompareTag("Player"))
        {
            // NOTE: 기지 시스템을 제거했으므로, 엄폐 시 데미지 반감으로 변경
            // 데미지 처리는 PlayerStatusManager에서 처리
            other.GetComponent<PlayerStatusManager>().TakeDamage(projectileDamage);
            isHit = true;
        }

        if (isHit)
            StartCoroutine(WaitAttack());
    }

    public void HitEMP()
    {
        StartCoroutine(WaitAttack());
    }

    private IEnumerator WaitAttack()
    {
        isMoving = false; // 이동 상태 비활성화

        // enemyFSM의 공격 신호를 먼저 초기화
        // bossFSM은 공격 신호가 따로 없으므로 생략
        if (enemyFSM != null)
        {
            enemyFSM.isAttackSign = false; // 공격 신호 초기화
        }

        if (originPool != null)
        {
            // 풀에 반환 요청
            originPool.DeactivatePoolItem(gameObject);
        }
        else
        {
            // 풀을 사용하지 않는 경우 직접 비활성화
            if (gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
            }
        }

        transform.position = originPos;

        yield return null; // 다음 프레임까지 대기
    }

    private void OnPauseGame()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("EnemyProjectile이 비활성화된 상태에서 OnPauseGame 호출됨");
            return;
        }

        StartCoroutine(WaitAttack());
    }

    private void OnDestroy()
    {
        // StageManager가 제거되었다면 이벤트 구독 해제
        if (StageManager.Instance != null)
        {
            StageManager.Instance.onPauseGameEvent.RemoveListener(OnPauseGame);
        }
    }
}
