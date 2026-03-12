using UnityEngine;
using System.Collections;

/// <summary>
/// 보조 로봇의 행동을 관리하는 스크립트입니다.
/// 모든 보조 로봇에 관련한 스크립트는 SupportBot을 앞에 붙여줍니다.
/// </summary>
public class SupportBotFSM : MonoBehaviour
{
    // 가드 시스템 이벤트 (정적 이벤트)
    public static event System.Action<Transform> OnGuardActivated;
    public static event System.Action<Transform> OnGuardDeactivated;

    // 현재 가드 상태를 전역적으로 관리
    public static bool IsAnyGuardActive { get; private set; } = false;
    public static Transform CurrentGuardBot { get; private set; } = null;

    [Header("References")]
    private SupportBotStatusManager status;
    private SupportBotEffectManager effect;
    private SupportBotAudioManager audioManager;
    [SerializeField]    // Inspector 할당
    private PlayerStatusManager playerStatusManager; // 플레이어의 보호막, 회복 로직을 위한 참조

    [Header("Flags")]
    private bool isSupporting = false; // 보조 로봇이 지원 중인지 여부
    public bool IsSupporting => isSupporting;
    private bool isAttacking = false; // 보조 로봇이 공격 중인지 여부
    private bool isGuarding = false; // 보조 로봇이 가드 중인지 여부
    public bool IsGuarding => isGuarding;
    private bool isCooldownReady = false; // 쿨타임 완료 여부(false면 감소 중)

    [Header("Values")]
    private float initialCoolTime = 0f;  // 인스펙터에서 설정된 초기 쿨타임 값 저장
    // 스킬 지속시간을 저장하는 변수
    private float guardDuration;
    private float shieldDuration;
    private float healDuration;
    private float attackDuration;
    private Coroutine shieldCoroutine; // 쉴드 코루틴을 참조하기 위한 변수

    void Awake()
    {
        status = GetComponent<SupportBotStatusManager>();
        effect = GetComponent<SupportBotEffectManager>();
        audioManager = GetComponent<SupportBotAudioManager>();
    }

    void OnEnable()
    {
        // 쉴드 파괴 이벤트 구독
        PlayerStatusManager.OnShieldBroken += ForceDeactivateShield;
    }

    void OnDisable()
    {
        // 쉴드 파괴 이벤트 구독 해제 (메모리 누수 방지)
        PlayerStatusManager.OnShieldBroken -= ForceDeactivateShield;
    }
    void Start()
    {
        // 초기 쿨타임 값을 저장 (인스펙터에서 설정된 값)
        if (status != null)
        {
            initialCoolTime = status.setting.coolTime;
            isCooldownReady = false;
            status.setting.coolTime = 1f;   // 게임 시작 시 바로 사용 가능하도록 설정
        }
    }

    void Update()
    {
        if (status.isDead) return; // 보조 로봇이 죽었으면 아무 행동도 하지 않음
        if (status.isTutorialActive == false) return; // 튜토리얼에 맞게 작동하게 하도록 return 값 반환

        // 발동 조건에 쿨타임 조건 추가
        if (!isSupporting && status.setting.coolTime <= 0f && Input.GetKeyDown(KeyCode.D))
        {
            effect.ActivateSkillEffect();
            switch (status.setting.botType)
            {
                // NOTE: 업그레이드 레벨 변수 3이상은 자동 재장전 기능
                // 업그레이드 레벨 변수 6이상은 자동 공격 기능 추가
                case SupportBotType.Guard:
                    if (status.setting.guardLevel >= 6)
                    {
                        QuickReload();
                        StartCoroutine(AttackNearestEnemy());
                    }
                    else if (status.setting.guardLevel >= 3)
                    {
                        QuickReload();
                    }

                    ActivateGuard();
                    break;
                case SupportBotType.Shield:
                    if (status.setting.shieldLevel >= 6)
                    {
                        QuickReload();
                        StartCoroutine(AttackNearestEnemy());
                    }
                    else if (status.setting.shieldLevel >= 3)
                    {
                        QuickReload();
                    }

                    ActivatePlayerShield();
                    break;
                case SupportBotType.Heal:
                    if (status.setting.healLevel >= 6)
                    {
                        QuickReload();
                        StartCoroutine(AttackNearestEnemy());
                    }
                    else if (status.setting.healLevel >= 3)
                    {
                        QuickReload();
                    }

                    ActivatePlayerHeal();
                    break;
            }
        }

        // 쿨타임 감소: 쿨타임이 진행 중일 때 1초당 1씩 감소
        if (!isCooldownReady && !isSupporting)
        {
            status.setting.coolTime -= Time.deltaTime; // 쿨타임 감소
            if (status.setting.coolTime <= 0f)
            {
                status.setting.coolTime = 0f; // 0 미만으로 내려가지 않도록 예외 처리
                isCooldownReady = true;
            }
        }
        else if (isSupporting)
        {
            status.setting.coolTime = initialCoolTime; // 쿨타임 초기화
        }
    }

    #region Guard Functions

    private void ActivateGuard()
    {
        isSupporting = true; // 지원 중 상태로 변경
        isGuarding = true; // 가드 중 상태로 변경

        // 전역 가드 상태 설정
        IsAnyGuardActive = true;
        CurrentGuardBot = transform;

        // 기존 적들에게 이벤트 발생
        OnGuardActivated?.Invoke(transform);

        StartCoroutine(GuardCoroutine()); // 가드 코루틴 시작
    }

    private IEnumerator GuardCoroutine()
    {
        audioManager.PlaySkillActiveSound();
        // Guard 상태 유지 시간 설정
        guardDuration = status.setting.guardTime;
        float elapsed = 0f;

        while (elapsed < guardDuration)
        {
            if (status.isPaused || status.isDead)
            {
                // 일시정지 또는 Death 시 가드 비활성화 및 지원 종료
                DeactivateGuard();
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Guard 시간 종료
        DeactivateGuard();
    }

    private void DeactivateGuard()
    {
        // 전역 가드 상태 해제
        IsAnyGuardActive = false;
        CurrentGuardBot = null;

        // 모든 적들에게 가드 해제 이벤트 발생
        OnGuardDeactivated?.Invoke(transform);

        isSupporting = false; // 지원 종료 상태로 변경
        isGuarding = false; // 가드 종료 상태로 변경
        isCooldownReady = false; // 쿨타임 감소 시작
    }

    public void DestroySupportBotGuard()
    {
        // Death 시에도 가드 해제
        if (IsAnyGuardActive && CurrentGuardBot == transform)
        {
            DeactivateGuard();
        }
    }

    #endregion

    #region Shield Functions

    // 보조 로봇 보호막: 일정 시간 동안 플레이어 무적 상태 부여
    private void ActivatePlayerShield()
    {
        if (isSupporting || playerStatusManager.isShieldActive) return;

        audioManager.PlaySkillActiveSound();
        isSupporting = true; // 지원 중 상태로 변경

        // 보호막 활성화 로직
        playerStatusManager.isShieldActive = true;
        effect.ToggleShieldEffect(true);    // 보호막 이펙트 활성화
        shieldCoroutine = StartCoroutine(OnShield()); // 보호막 코루틴 시작 및 참조 저장
    }

    private IEnumerator OnShield()
    {
        shieldDuration = status.setting.shieldTime;
        yield return new WaitForSeconds(shieldDuration);

        // 코루틴이 정상적으로 완료되었을 때만 쿨타임 감소 시작
        isCooldownReady = false;
        DeactivateShield();
    }

    /// <summary>
    /// 쉴드를 비활성화하고 관련 상태를 정리합니다.
    /// </summary>
    private void DeactivateShield()
    {
        playerStatusManager.isShieldActive = false;
        isSupporting = false; // 지원 종료 상태로 변경
        effect.ToggleShieldEffect(false); // 보호막 이펙트 비활성화
        shieldCoroutine = null; // 코루틴 참조 초기화
    }

    /// <summary>
    /// 외부 이벤트에 의해 쉴드를 강제로 비활성화합니다.
    /// </summary>
    private void ForceDeactivateShield()
    {
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
            DeactivateShield();
            // 강제 종료 시에는 쿨타임이 즉시 돌도록 설정할 수 있습니다.
            // 또는 기존 쿨타임 정책을 유지할 수도 있습니다. 여기서는 쿨타임 감소를 시작합니다.
            isCooldownReady = false;
        }
    }

    #endregion

    #region Heal Functions

    private void ActivatePlayerHeal()
    {
        audioManager.PlaySkillActiveSound();
        effect.ToggleHealEffect(true); // 회복 이펙트 활성화
        isSupporting = true; // 지원 중 상태로 변경

        // 실제 플레이어 체력 회복 로직
        StartCoroutine(playerStatusManager.HealPlayer(status.setting.healAmount, status.setting.healTime));
        // 회복 시간, flag, effect 설정
        StartCoroutine(OnHeal());
    }

    private IEnumerator OnHeal()
    {
        float elapsed = 0f;
        healDuration = status.setting.healTime;

        while (elapsed < healDuration)
        {
            if (status.isPaused)
            {
                yield break; // 일시정지 신호가 오면 즉시 코루틴 종료
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        effect.ToggleHealEffect(false); // 회복 이펙트 비활성화  
        isSupporting = false; // 지원 종료 상태로 변경
        isCooldownReady = false; // 쿨타임 감소 시작
    }

    #endregion

    #region Attack Functions

    // 보조 로봇의 자동 공격
    private IEnumerator AttackNearestEnemy()
    {
        if (isAttacking) yield break; // 이미 공격 코루틴이 실행 중이면 중복 실행 방지
        effect.StartAutoAttackEffect(); // 자동 공격 이펙트 시작

        isAttacking = true; // 공격 중 상태로 변경

        attackDuration = status.setting.attackTime; // 전체 공격 지속 시간
        float elapsedTime = 0f; // 경과 시간

        while (elapsedTime < attackDuration)
        {
            audioManager.PlayAttackSound();
            if (status.isDead || status.isPaused)
            {
                isAttacking = false;
                yield break; // 사망 또는 일시정지 시 즉시 중단
            }

            // --- 공격 로직 시작 ---
            Transform shieldTarget = FindNearestShieldEnemy();
            if (shieldTarget != null)
            {
                // 일반 적 쉴드 대상
                var enemyStatus = shieldTarget.GetComponent<EnemyStatusManager>();
                if (enemyStatus == null)
                {
                    enemyStatus = shieldTarget.GetComponentInParent<EnemyStatusManager>();
                }

                if (enemyStatus != null)
                {
                    enemyStatus.SupportBotTakeDamage(status.setting.damage);
                }
                else
                {
                    Debug.LogWarning("Shield target found but no EnemyStatusManager on target or its parents.");
                }
            }
            else
            {
                // 2. Boss 우선 공격
                BossStatusManager bossStatus = FindFirstObjectByType<BossStatusManager>();
                if (bossStatus != null)
                {
                    bossStatus.TakeDamage(status.setting.damage);
                }
                else
                {
                    // 3. 일반 Enemy 공격
                    EnemyStatusManager nearestEnemy = FindNearestEnemy();
                    if (nearestEnemy != null)
                    {
                        nearestEnemy.SupportBotTakeDamage(status.setting.damage);
                    }
                }
            }
            // --- 공격 로직 종료 ---

            // 다음 공격까지 대기
            yield return new WaitForSeconds(status.setting.attackRate);
            elapsedTime += status.setting.attackRate; // 실제 대기한 시간만큼 경과 시간에 추가
        }

        effect.EndAutoAttackEffect(); // 자동 공격 이펙트 종료
        isAttacking = false; // 공격 종료 상태로 변경
    }

    private EnemyStatusManager FindNearestEnemy()
    {
        // SearchEnemyManager를 통해 적 찾기
        if (SearchEnemyManager.Instance != null)
        {
            return SearchEnemyManager.Instance.FindNearestEnemy(transform.position);
        }

        Debug.LogWarning("SearchEnemyManager instance not found!");
        return null;
    }

    private Transform FindNearestShieldEnemy()
    {
        // Shield 적 찾기
        if (SearchEnemyManager.Instance != null)
        {
            return SearchEnemyManager.Instance.FindNearestShieldEnemy(transform.position);
        }

        return null;
    }

    #endregion

    #region QuickReload Functions

    // 플레이어 탄약 급속 충전
    private void QuickReload()
    {
        // WeaponBase를 상속받는 무기 오브젝트를 찾아서 급속 재장전
        WeaponBase[] weapons = FindObjectsByType<WeaponBase>(FindObjectsSortMode.None);
        foreach (var weapon in weapons)
        {
            weapon.QuickReload();
            weapon.onAmmoEvent.Invoke(weapon.CurrentAmmo, weapon.MaxAmmo);
        }
    }

    #endregion
}
