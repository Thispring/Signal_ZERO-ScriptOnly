using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Boss의 상태를 관리하는 스크립트입니다.
/// 모든 Boss에 관련한 스크립트는 Boss를 앞에 붙여줍니다.
/// </summary>
public class BossStatusManager : MonoBehaviour
{
    // 중간/최종 보스 소환 이벤트 분리
    public static event System.Action OnMiddleBossSpawnRequested;
    public static event System.Action OnFinalBossSpawnRequested;

    [Header("References")]
    public BossSetting setting; // Inspector에서 타입(중간/최종)에 맞게 수치 값 설정
    private BossEffectController effect;
    private BossAnimatorController animator;
    [SerializeField]    // Inspector에서 끌어서 사용
    private EnemyMemoryPool enemyMemoryPool; // 보스 전투 중 Enemy 소환을 위한 참조
    [SerializeField]    // Inspector에서 끌어서 사용
    private StageManager stageManager; // 중간/최종 보스 조건 확인을 위한 참조

    [Header("Values")]
    private float currentHP; // 현재 Boss 체력 
    private float maxHP; // 최대 Boss 체력 
    // 외부에서 HP를 읽기 위한 Getter
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    [Header("Flags")]
    private bool isDead = false; // Boss가 죽었는지 여부를 확인하는 플래그
    public bool IsDead => isDead;
    public bool isTeleportProtected = false; // 텔레포트 중 보호 상태
    public bool hasTriggeredHalfHealthEvent = false; // 체력 절반 이벤트 트리거 여부

    // 텔레포트 실행 여부 추적 플래그, 변수명 뒤 숫자는 체력 퍼센트
    private bool hasTeleportedAt90 = false;
    private bool hasTeleportedAt70 = false;
    private bool hasTeleportedAt60 = false;
    private bool hasTeleportedAt40 = false;
    private bool hasTeleportedAt20 = false;
    private bool hasTeleportedAt10 = false;

    [Header("UI")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private Slider healthSlider; // Boss 체력 바 UI

    void Awake()
    {
        if (setting.HP >= 0)
            setting.bossPhase = BossPhase.fullHP;   // 초기 페이즈 설정

        effect = GetComponent<BossEffectController>(); // Boss 이펙트 컨트롤러 컴포넌트 참조
        animator = GetComponent<BossAnimatorController>(); // BossAnimatorController 컴포넌트 참조

        // BossSpawnManager에서 보스 소환 요청 이벤트를 받아 등록
        BossSpawnManager.OnBossSpawnRequested += OnBossSpawnRequestedHandler;

        gameObject.SetActive(false); // 소환전 비활성화
    }

    void Update()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHP;
            healthSlider.value = currentHP;
        }

        if (setting.HP >= 0)
        {
            // Boss 체력에 따라 페이즈 변경
            // 현재 체력이 최대 체력의 절반 이하일 때 페이즈 변경
            if (currentHP <= maxHP / 2)
                setting.bossPhase = BossPhase.halfHP;
            else
                setting.bossPhase = BossPhase.fullHP;
        }
    }

    void OnDestroy()
    {
        BossSpawnManager.OnBossSpawnRequested -= OnBossSpawnRequestedHandler; // 이벤트 구독 해제
    }

    // Boss 타입에 따라 활성화 구분
    private void OnBossSpawnRequestedHandler()
    {
        if (stageManager.isMiddleBoss && setting.bossType == BossType.middle)
        {
            ActiveMiddleBoss();
        }
        else if (stageManager.isFinalBoss && setting.bossType == BossType.final)
        {
            ActiveFinalBoss();
        }
    }

    // 중간 보스 활성화
    private void ActiveMiddleBoss()
    {
        gameObject.SetActive(true);
        currentHP = setting.HP;
        maxHP = currentHP;
        isDead = false;
        hasTriggeredHalfHealthEvent = false;
        hasTeleportedAt90 = false;
        hasTeleportedAt70 = false;
        hasTeleportedAt60 = false;
        hasTeleportedAt40 = false;
        hasTeleportedAt20 = false;
        hasTeleportedAt10 = false;
        OnMiddleBossSpawnRequested?.Invoke();
    }

    // 최종 보스 활성화
    private void ActiveFinalBoss()
    {
        gameObject.SetActive(true);
        currentHP = setting.HP;
        maxHP = currentHP;
        isDead = false;
        hasTriggeredHalfHealthEvent = false;
        OnFinalBossSpawnRequested?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        if (isDead || isTeleportProtected) return; // 죽었거나 텔레포트 중이면 데미지 무시

        currentHP -= damage; // 데미지 적용
        effect.OnHitEffect(); // 히트 이펙트 실행

        // 버스트 상태에서 공격 시 게이지 충전 불가
        // 필요할 시 보조로봇 공격 게이지 충전 방지 로직도 여기에 추가
        if (PlayerStatusManager.Instance.isBurstActive == false)
            PlayerStatusManager.Instance.burstCurrentGauge += 3; // 버스트 게이지 증가

        // 보스 체력이 절반 이하이고, 이벤트가 아직 호출되지 않았다면 스폰 일시정지
        // 중간 보스에만 임시 적용
        if (setting.bossType == BossType.middle && !hasTriggeredHalfHealthEvent && currentHP <= maxHP / 2)
        {
            if (EnemyMemoryPool.Instance != null && !EnemyMemoryPool.Instance.isMiddleBossFirstSpawn)
            {
                EnemyMemoryPool.Instance.ResumeSpawning();
            }

            if (setting.bossType == BossType.middle)
            {
                hasTriggeredHalfHealthEvent = true;
            }
        }

        if (currentHP <= 0)
        {
            currentHP = 0;
            StartCoroutine(DeathAni());
        }
    }

    private IEnumerator DeathAni()
    {
        effect.SetDestoryEffectActive(true);
        animator.isDeath(); // 죽음 애니메이션 재생
        yield return new WaitForSeconds(2f); // 2초 대기
        effect.SetDestoryEffectActive(false);

        Die(); // 죽음 처리
    }

    private void Die()
    {
        isDead = true; // 죽음 상태 설정

        // 모든 활성화된 BossDroneManager 비활성화
        BossDroneManager[] droneManagers = FindObjectsByType<BossDroneManager>(FindObjectsSortMode.None);
        foreach (BossDroneManager droneManager in droneManagers)
        {
            if (droneManager.gameObject.activeInHierarchy)
            {
                droneManager.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(false); // Boss 비활성화
        stageManager.isMiddleBoss = false; // 중간 보스 상태 초기화

        // 타입에 따라 EnemyMemoryPool에 클리어 신호 전달
        if (enemyMemoryPool != null)
        {
            if (setting.bossType == BossType.middle)
            {
                enemyMemoryPool.isMiddleBossClear = true; // 중간보스 클리어
            }
            else if (setting.bossType == BossType.final)
            {
                enemyMemoryPool.isFinalBossClear = true; // 최종보스 클리어
            }
        }
    }

    // 체력에 비례한 텔레포트 가능 여부 확인 함수
    public bool CanTeleportAt(float hpPercentage)
    {
        if (setting.bossType != BossType.middle) return false; // 현재 중간 보스만 텔레포트 가능

        // 텔레포트가 진행 중일 때는 새로운 텔레포트를 시작할 수 없습니다.
        if (isTeleportProtected) return false;

        // 부동소수점 반올림 이슈 방지: 0.9f * 100이 89.999로 떨어지는 경우를 대비해 반올림 사용
        int percentKey = Mathf.RoundToInt(hpPercentage * 100f);
        switch (percentKey)
        {
            case 90:
                if (!hasTeleportedAt90) { hasTeleportedAt90 = true; return true; }
                break;
            case 70:
                if (!hasTeleportedAt70) { hasTeleportedAt70 = true; return true; }
                break;
            case 60:
                if (!hasTeleportedAt60) { hasTeleportedAt60 = true; return true; }
                break;
            case 40:
                if (!hasTeleportedAt40) { hasTeleportedAt40 = true; return true; }
                break;
            case 20:
                if (!hasTeleportedAt20) { hasTeleportedAt20 = true; return true; }
                break;
            case 10:
                if (!hasTeleportedAt10) { hasTeleportedAt10 = true; return true; }
                break;
        }
        return false;
    }
}
