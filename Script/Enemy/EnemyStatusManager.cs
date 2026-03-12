using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System; // Action 사용을 위해 추가

/// <summary>
/// Enemy의 상태를 관리하는 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
public class EnemyStatusManager : MonoBehaviour
{
    public event Action<GameObject> OnEnemyDied; // 적 사망 시 호출될 이벤트, 튜토리얼에서 사용
    public static event UnityAction<EnemyStatusManager> OnEnemySpawned; // 소환 이벤트
    public static event UnityAction<EnemyStatusManager> OnEnemyDestroyed; // 파괴 이벤트

    [Header("References")]
    public EnemySetting setting; // 설정 참조, Inspector에서 값 지정
    private EnemyAnimatorController animatorController;   // EnemyAnimatorController 참조   
    private EnemyEffectController effectController; // EnemyEffectController 참조
    private EnemyAudioController audioController; // EnemyAudioController 참조

    [Header("Values")]
    private float currentHP; // 현재 체력
    public int criticalHit = 0;    // enemy 치명타 변수, 보너스 코인 지급에 사용

    [Header("Flags")]
    public bool isDead = false; // 적이 죽었는지 여부를 확인하는 플래그
    public bool isHalfHP = false; // 체력이 절반 이하인지 여부를 확인하는 플래그
    public bool isTeleportProtected = false; // 텔레포트 보호 상태 플래그

    [Header("UI")]
    [SerializeField]
    private Slider healthSlider; // 체력 슬라이더
    public Vector3 sliderOffset = new Vector3(0, 2, 0); // 슬라이더 위치 오프셋

    void Awake()
    {
        animatorController = GetComponent<EnemyAnimatorController>();
        effectController = GetComponent<EnemyEffectController>();
        audioController = GetComponent<EnemyAudioController>();
    }

    void OnEnable()
    {
        isDead = false;
        isHalfHP = false;
        isTeleportProtected = false;
        criticalHit = 0;
        currentHP = setting.HP;

        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(true);
            healthSlider.maxValue = setting.HP;
            healthSlider.value = currentHP;
        }

        if (setting.enemySize == EnemySize.Small)
            audioController.PlayIdleSound();
        else
            audioController.PlaySpawnSound();
            
        effectController.BonusCoinTextEffect(false);
        effectController.DeathEffect(false);

        OnEnemySpawned?.Invoke(this);
    }

    void Update()
    {
        // 슬라이더를 에너미 상단에 고정
        if (healthSlider != null && !isTeleportProtected)
        {
            healthSlider.gameObject.SetActive(true);
            healthSlider.transform.position = Camera.main.WorldToScreenPoint(transform.position + sliderOffset);
            healthSlider.value = currentHP;
        }
        else
        {
            healthSlider.gameObject.SetActive(false);
        }

        if (currentHP < setting.HP / 2 && !isHalfHP)
        {   
            // 체력이 절반 이하로 떨어졌을 때, 텔레포트 조건 활성화
            // 텔레포트 로직은 BehaviorSystem에 정의되어 있음
            isHalfHP = true;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return; // 이미 죽은 상태라면 데미지 처리하지 않음

        if (isTeleportProtected) return; // 텔레포트 보호 상태라면 데미지 무시  

        effectController.OnHitEffect();
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, setting.HP);

        // 버스트 상태이거나, 보조 로봇이 공격 중이면, 게이지 충전 불가
        if (PlayerStatusManager.Instance.isBurstActive == false)
            PlayerStatusManager.Instance.burstCurrentGauge += setting.gaugeReward; // 버스트 게이지 증가

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // SupportBot이 공격할 때, 함수를 개별로 호출하여, 게이지 충전 불가능하게 설계
    public void SupportBotTakeDamage(float damage)
    {
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
        if (isDead) return; // 이미 죽은 상태라면 Die()를 실행하지 않음

        isDead = true; // 적이 죽었음을 표시

        // OnEnemyDied 이벤트를 호출하여 자신의 죽음을 알립니다.
        // Die()가 호출되는 시점이 실제 죽는 시점이므로 여기에 배치합니다.
        OnEnemyDied?.Invoke(transform.parent.gameObject); // 부모 오브젝트(실제 프리팹 인스턴스)를 전달

        StartCoroutine(DeathAni());
    }

    private IEnumerator DeathAni()
    {
        animatorController.isDeath();
        effectController.DeathEffect(true); 
        audioController.PlayDeathSound();
        
        // Player의 coin 증가
        if (PlayerStatusManager.Instance != null)
        {
            PlayerStatusManager.Instance.coin += setting.dropCoin; // 코인 증가
        }
        else
        {
            Debug.LogError("PlayerStatusManager 싱글톤 인스턴스를 찾을 수 없습니다.");
        }

        // 상성 데미지를 더 받게 되는 경우 코인을 추가로 지급
        // criticalHit > 0 조건으로 상성 공격이 한 번이라도 성공했는지 확인
        if (criticalHit > 0)
        {
            PlayerStatusManager.Instance.coin += setting.bonusCoin;
            effectController.BonusCoinTextEffect(true);
        }

        yield return new WaitForSeconds(1f);
        OnEnemyDestroyed?.Invoke(this); // 적이 파괴되었음을 알리는 이벤트 호출
        gameObject.SetActive(false); // 적 비활성화
    }

    // 매개변수로 스테이지 번호를 받아 enemy의 체력을 설정하는 함수 
    public void SetEnemyHPForStage(int stageNum)
    {
        // 스테이지 1에서는 기본 HP를 그대로 사용
        if (stageNum <= 1)
        {
            currentHP = setting.HP;
            // healthSlider maxValue 업데이트
            if (healthSlider != null)
            {
                healthSlider.maxValue = setting.HP;
            }
            return;
        }

        // 체력 증가 계수 계산
        float multiplier = 1.0f;
        switch (setting.enemySize)
        {
            case EnemySize.Small:
                multiplier = 1.0f + (stageNum - 1) * 0.15f;
                break;
            case EnemySize.Medium:
                multiplier = 1.0f + (stageNum - 1) * 0.18f;
                break;
            case EnemySize.Big:
                multiplier = 1.0f + (stageNum - 1) * 0.20f;
                break;
        }

        // 기본 HP에 계산된 배율을 곱하여 최종 HP 설정
        setting.HP *= multiplier;
        currentHP = setting.HP; // 현재 체력도 함께 갱신

        // healthSlider의 maxValue도 함께 업데이트
        if (healthSlider != null)
        {
            healthSlider.maxValue = setting.HP;
            healthSlider.value = currentHP;
        }
    }

    // 매개변수로 스테이지 번호를 받아 enemy의 데미지를 설정하는 함수
    public void SetEnemyDamageForStage(int stageNum)
    {
        if (stageNum <= 1) return; // 스테이지 1에서는 기본 데미지 사용

        // 데미지 증가 계수: Small(10%), Medium(12%), Big(15%)
        float multiplier = 1.0f;
        switch (setting.enemySize)
        {
            case EnemySize.Small:
                multiplier = 1.0f + (stageNum - 1) * 0.10f;
                break;
            case EnemySize.Medium:
                multiplier = 1.0f + (stageNum - 1) * 0.12f;
                break;
            case EnemySize.Big:
                multiplier = 1.0f + (stageNum - 1) * 0.15f;
                break;
        }

        // 기본 데미지에 계산된 배율을 곱하여 최종 데미지 설정
        setting.damage = (int)(setting.damage * multiplier);
    }
}
