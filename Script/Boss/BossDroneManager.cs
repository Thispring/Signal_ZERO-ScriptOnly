using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Boss가 소환하는 Drone들을 관리하는 스크립트입니다.
/// 모든 Boss에 관련한 스크립트는 Boss를 앞에 붙여줍니다.
/// </summary>
/// 
/// <remarks>
/// Boss의 공격 패턴은 BossDrone을 무작위로 소환하고, 파괴된 드론의 수에 따라 공격을 진행합니다.
/// 드론의 체력은 보스 패턴 시퀀스 진행에 따라 증가합니다.
/// </remarks>
public class BossDroneManager : MonoBehaviour
{
    // 드론 파괴 이벤트 (HP<=0)
    public static event Action OnDroneDestroyed;

    [Header("Status")]
    private float baseHP = 50f;           // 기본 체력
    [SerializeField]
    private float HP;                      // 현재 체력
    [SerializeField]
    private float hpIncreasePerSequence = 10f; // 시퀀스당 증가하는 체력
    private float maxHP;                   // 최대 체력 (슬라이더용)

    [Header("UI")]
    [SerializeField]
    private Slider hpSlider;
    [SerializeField]
    private Vector3 sliderOffset = new Vector3(0, 2f, 0);

    [Header("Rendering")]
    [SerializeField]
    private Material objMaterial;
    private Color originalColor;
    [SerializeField]
    private GameObject explosionEffectPrefab;

    void Awake()
    {
        if (explosionEffectPrefab != null)
            explosionEffectPrefab.SetActive(false);

        if (objMaterial != null)
        {
            objMaterial = Instantiate(objMaterial);
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = objMaterial;
            }
        }
        objMaterial.color = Color.white;
        originalColor = objMaterial.color;
    }

    void Update()
    {
        if (hpSlider != null)
        {
            hpSlider.transform.position = Camera.main.WorldToScreenPoint(transform.position + sliderOffset);
            hpSlider.value = HP / maxHP; // 최대 체력 기준으로 비율 계산
        }
    }

    // 드론 초기화 메서드 (BossFSM에서 호출)
    public void InitializeWithSequence(int sequenceCount)
    {
        // 시퀀스에 따른 체력 증가 (예: 기본 50 + 시퀀스당 10)
        maxHP = baseHP + (hpIncreasePerSequence * sequenceCount);
        HP = maxHP;
    }

    // 플레이어의 공격 등에서 호출
    public void TakeDamage(float damage)
    {
        // 버스트 상태이거나, 보조 로봇이 공격 중이면, 버스트 게이지 충전 불가
        if (PlayerStatusManager.Instance.isBurstActive == false)
            PlayerStatusManager.Instance.burstCurrentGauge += 2; // 버스트 게이지 증가

        HP -= damage;
        StartCoroutine(FlashRed());
        if (HP <= 0)
        {
            try { OnDroneDestroyed?.Invoke(); } catch { }
            // 파괴 대신 비활성화
            // 이펙트를 위해 코루틴 사용
            StartCoroutine(ExplosionEffect());
        }
    }

    private IEnumerator ExplosionEffect()
    {
        if (explosionEffectPrefab != null)
        {
            explosionEffectPrefab.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            explosionEffectPrefab.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    private IEnumerator FlashRed()
    {
        if (objMaterial == null) yield break;
        objMaterial.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        objMaterial.color = originalColor;
    }
}
