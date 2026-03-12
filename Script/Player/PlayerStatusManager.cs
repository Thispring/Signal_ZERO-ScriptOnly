using UnityEngine;
using System; // System.Action을 사용하기 위해 추가
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Player의 상태를 관리하는 스크립트 입니다.
/// 모든 Player에 관련한 스크립트는 Player을 앞에 붙여줍니다.
/// </summary>
[System.Serializable]
public class HPEvent : UnityEngine.Events.UnityEvent<int, int> { } // 체력 이벤트를 위한 클래스
public class PlayerStatusManager : MonoBehaviour
{
    public static PlayerStatusManager Instance { get; private set; } // 싱글톤 인스턴스

    // 플레이어의 쉴드가 외부 요인에 의해 파괴되었을 때 호출되는 이벤트입니다.
    public static event Action OnShieldBroken;
    // 플레이어가 사망했을 때 호출되는 이벤트입니다.
    public static event Action OnPlayerDied;
    [HideInInspector]
    public HPEvent onHPEvent = new HPEvent(); // 체력 이벤트, 체력이 바뀔 때 마다 외부에 있는 메소드 자동 호출 할 이벤트

    [Header("Player HP")]
    [SerializeField]
    private int maxHP = 100;
    private int currentHP; // 현재 체력
    public int increaseCount = 1;   // 최대 체력 증가를 위한 변수

    public int MaxHP => maxHP;  // 최대 체력 Getter 추가
    public int CurrentHP => currentHP; // 현재 체력 Getter 추가

    [Header("Coins")]
    public int coin; // 코인 수

    [Header("Flags")]
    public bool isHiding = true; // 플레이어의 엄폐 여부, false = 비엄폐, true = 엄폐
    public bool isShot = false; // 플레이어가 총을 쏘는지 여부
    public bool isShieldActive = false; // 플레이어의 보호막 활성화 여부
    public bool isHealActive = false; // 플레이어의 힐 활성화 여부

    [Header("HitEffect")]
    [SerializeField]
    private Image hitImage;
    [SerializeField]
    private GameObject[] hitEffects;
    public float effectRenderTime = 2f;
    private Coroutine hitEffectCoroutine; // 코루틴 핸들 추가
    private float hitEffectEndTime = 0f; // 가장 마지막 호출된 hitEffect 종료 시간

    [Header("Burst Variables")]
    public float burstCurrentGauge = 0f; // 현재 버스트 게이지
    public float burstMaxGauge = 100f; // 최대 버스트 게이지
    public bool isBurstReady = false; // 버스트 준비 상태
    public bool isBurstActive = false; // 버스트 활성화 여부

    // 튜토리얼에서 해당 변수 상태로, 버스트 게이지를 99 이상으로 넘지 못하게 막고, 죽지 않게 설정합니다.
    [Header("Tutorial")]
    public bool isTutorialBurstAction = true; // 튜토리얼 버스트 상태 여부
    public bool isTutorialDamageAction = true; // 튜토리얼 데미지 상태 여부

    void Awake()
    {
        isTutorialBurstAction = true;
        isTutorialDamageAction = true;

        isHiding = true; // 초기 엄폐 상태
        isBurstReady = false; // 버스트 준비 상태 초기화
        increaseCount = 1;
        currentHP = maxHP; // 현재 체력을 최대 체력으로 초기화

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        // Sprite의 투명도 초기화
        if (hitImage != null)
        {
            Color color = hitImage.color;
            color.a = 0; // 투명도 0으로 설정
            hitImage.color = color;
        }

        if (hitEffects != null)
        {
            for (int i = 0; i < hitEffects.Length; i++)
            {
                if (hitEffects[i] != null)
                {
                    hitEffects[i].SetActive(false); // 모든 이펙트 비활성화
                }
            }
        }
    }

    void Update()
    {
        // HitEffect실행 후 2초가 지나면 자동으로 투명도 0으로 설정
        if (hitImage.color.a != 0)
        {
            effectRenderTime -= Time.deltaTime;
            if (effectRenderTime <= 0)
            {
                Color color = hitImage.color;
                color.a = 0; // 투명도 0으로 설정
                hitImage.color = color;
            }
        }
        else
        {
            effectRenderTime = 2f;
        }

        if (isTutorialBurstAction == true) // 튜토리얼 상태면, 게이지가 99 이상으로 못넘게 설정
        {
            if (burstCurrentGauge >= 99f)
            {
                burstCurrentGauge = 99f;
            }
        }

        if (burstCurrentGauge >= burstMaxGauge)
        {
            isBurstReady = true; // 버스트 준비 상태
        }
        else
        {
            isBurstReady = false; // 버스트 준비 상태 비활성화
        }
    }

    // 체력 감소 함수
    public bool DecreaseHP(int damage)
    {
        int previousHP = currentHP;

        currentHP = currentHP - damage > 0 ? currentHP - damage : 0;

        onHPEvent.Invoke(previousHP, currentHP);

        // 튜토리얼 상태면, 체력이 10 미만으로 떨어지지 않게 설정
        if (isTutorialDamageAction)
        {
            if (currentHP < 10)
                currentHP = 10;
        }
        
        if (currentHP == 0)
        {
            return true;
        }

        return false;
    }

    // 쉴드 파괴 이벤트를 외부에서 호출하기 위한 public 함수
    public void BreakShield()
    {
        if (isShieldActive)
        {
            OnShieldBroken?.Invoke();
        }
    }

    // 데미지 호출 및, 타격 이펙트 호출 함수
    public void TakeDamage(int damage)
    {
        if (isHiding) damage /= 2; // 엄폐 상태면 데미지 반감

        if (isShieldActive) return; // 보호막이 활성화되어 있으면 데미지 무시

        bool isDie = DecreaseHP(damage);
        if (isDie == true)
        {
            OnPlayerDied?.Invoke(); // 플레이어 사망 이벤트 호출
        }
        else
        {
            if (hitEffectCoroutine != null)
            {
                StopCoroutine(hitEffectCoroutine);
            }
            hitEffectCoroutine = StartCoroutine(ShowHitEffect());
        }
    }

    // 타격 받았을 때 이펙트
    private IEnumerator ShowHitEffect()
    {
        if (hitImage != null && hitEffects != null)
        {
            hitEffects[0].SetActive(true);

            // 투명도 1로 설정
            Color color = hitImage.color;
            color.a = 1f;
            hitImage.color = color;

            yield return new WaitForSeconds(0.5f); // 0.5초 유지

            // 페이드 아웃
            float fadeDuration = 0.5f;
            float elapsedTime = 0f;

            for (int i = 0; i < hitEffects.Length; i++)
            {
                if (hitEffects[i] != null)
                {
                    hitEffects[i].SetActive(false); // 모든 이펙트 비활성화
                }
            }

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                color.a = Mathf.Lerp(0.5f, 0f, elapsedTime / fadeDuration);
                hitImage.color = color;
                yield return null;
            }

            if (Time.time >= hitEffectEndTime)
            {
                color.a = 0;
                hitImage.color = color;
            }
        }
        else
        {
            Debug.LogError("hitImage가 null입니다.");
        }
    }

    public void Heal(int healAmount)
    {
        int previousHP = currentHP;
        currentHP = Mathf.Min(currentHP + healAmount, maxHP); // 최대 체력 제한
        onHPEvent.Invoke(previousHP, currentHP); // 체력 이벤트 호출
    }

    public void IncreaseMaxHP(int amount, int count)
    {
        maxHP += amount; // 최대 체력 증가
        increaseCount += count - 1;
        // UI에 반영하기 위해 실제 count보다 -1 연산
    }

    // 회복 시간동안, 매개변수로 받은 회복량을 1초마다 회복하는 코루틴
    public IEnumerator HealPlayer(int healAmount, float healDuration)
    {
        // 회복 시간과 회복량을 함수 지역변수로 선언
        float elapsedTime = 0f;
        int totalHealed = 0;

        SupportBotStatusManager supportBot = SupportBotStatusManager.Instance;
        // elapsedTime가 healDuration보다 작을 때까지 반복
        while (elapsedTime < healDuration)
        {
            if (supportBot.isPaused) yield break; // 일시정지 상태면 즉시 종료

            int previousHP = currentHP;
            currentHP = Mathf.Min(currentHP + healAmount, maxHP); // 최대 체력 제한
            totalHealed += currentHP - previousHP;

            onHPEvent.Invoke(previousHP, currentHP);    // HUD 체력 이벤트 호출

            yield return new WaitForSeconds(1.0f); // 정확히 1초 대기
            elapsedTime += 1.0f;    // elapsedTime에 1을 증가시켜, 1초 경과
        }
    }
}
