using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보조 로봇의 상태를 관리하는 스크립트입니다.
/// 모든 보조 로봇에 관련한 스크립트는 SupportBot을 앞에 붙여줍니다.
/// </summary>
public class SupportBotStatusManager : MonoBehaviour
{
    // 보조로봇 싱글톤
    public static SupportBotStatusManager Instance { get; private set; }

    [Header("References")]
    public SupportBotSetting setting; // 보조 로봇 설정 참조
    public StageManager stageManager; // StageManager 참조
    private SupportBotFSM botFSM; // SupportBot 상태 머신
    private SupportBotEffectManager effectManager; // 이펙트 매니저 참조
    private SupportBotAudioManager audioManager; // 오디오 매니저 참조

    [Header("Values")]
    private float currentHP; // 현재 체력

    [Header("Flags")]
    public bool isDead = false; // 보조 로봇이 죽었는지 여부를 확인하는 플래그
    public bool isPaused = false; // 보조 로봇의 정지 상태를 확인하는 플래그
    public bool isTutorialActive = false; // 튜토리얼 활성화 여부

    [Header("UI")]
    [SerializeField]
    private Slider healthSlider; // 체력 슬라이더
    public Vector3 sliderOffset = new Vector3(0, 2, 0); // 슬라이더 위치 오프셋

    void Awake()
    {
        currentHP = setting.HP; // 초기 체력 설정

        botFSM = GetComponent<SupportBotFSM>(); // SupportBotFSM 컴포넌트 참조
        effectManager = GetComponent<SupportBotEffectManager>(); // SupportBotEffectManager 컴포넌트 참조
        audioManager = GetComponent<SupportBotAudioManager>(); // SupportBotAudioManager 컴포넌트 참조

        // 싱글톤 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 현재 오브젝트 삭제
            return;
        }
    }

    void Start()
    {
        // 스테이지가 시작되면 보조 로봇 활성화
        // 로봇이 파괴되어도 스테이지 시작 시 활성화 
        if (stageManager.isStart)
        {
            gameObject.SetActive(true); // 스테이지가 시작되면 보조 로봇 활성화
            currentHP = setting.HP; // 체력 초기화
            isDead = false; // 죽음 상태 초기화
        }
    }

    void Update()
    {
        // 체력 슬라이더 위치 업데이트
        if (healthSlider != null)
        {
            healthSlider.transform.position = Camera.main.WorldToScreenPoint(transform.position + sliderOffset);
            healthSlider.value = currentHP / setting.HP; // 체력 비율로 설정
        }

        if (!isTutorialActive)
        {
            // 튜토리얼 스테이지에서는 보조 로봇 Prefab 비활성화
            effectManager.ToggleSupportBot(false);
        }
        else
        {
            effectManager.ToggleSupportBot(true);
        }
    }

    public void TakeDamage(float damage)
    {
        if (setting.botType != SupportBotType.Guard && botFSM.IsGuarding == false) return;

        if (isDead) return; // 이미 죽은 상태라면 데미지 처리하지 않음

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, setting.HP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        effectManager.ToggleDestroyEffect(true);    // 파괴 이펙트 활성화
        audioManager.PlayDeathSound();
        botFSM.DestroySupportBotGuard();
        if (isDead) return; // 이미 죽은 상태라면 Die()를 실행하지 않음

        isDead = true; // 보조 로봇이 죽었음을 표시
        gameObject.SetActive(false); // 보조 로봇 비활성화
    }

    // Repair를 public void 함수로 변경하여, Stage 시작 시 호출할 수 있도록 합니다.
    public void Repair()
    {
        if (isDead)
            effectManager.ToggleDestroyEffect(false); // 파괴 이펙트 비활성화

        // 비활성화된 상태라면 강제로 활성화
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        currentHP = setting.HP; // 체력 초기화
        isDead = false; // 죽음 상태 초기화
    }

    // ShopManager에 사용할 업그레이드 메서드들 (개별 함수로 분리)
    public void UpgradeGuard(int gradeHPValue, int guardTimeValue)
    {
        setting.guardTime += guardTimeValue;
        setting.HP += gradeHPValue * 10;
        setting.guardLevel += 1; // 업그레이드 레벨 증가
    }

    public void UpgradeShieldTime(int shieldTimeValue)
    {
        setting.shieldTime += shieldTimeValue;
        setting.shieldLevel += 1; // 업그레이드 레벨 증가;
    }

    public void UpgradeHealAmount(int healAmountValue, int healTimeValue)
    {
        setting.healAmount += healAmountValue;
        setting.healTime += healTimeValue;
        setting.healLevel += 1; // 업그레이드 레벨 증가
    }
}
