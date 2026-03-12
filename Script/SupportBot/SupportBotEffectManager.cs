using UnityEngine;
using DG.Tweening;  // idle 애니메이션을 위한 DOTween 사용

/// <summary>
/// 보조 로봇의 시각 효과를 관리하는 스크립트입니다. 
/// 모든 보조 로봇에 관련한 스크립트는 SupportBot을 앞에 붙여줍니다.
/// </summary>
public class SupportBotEffectManager : MonoBehaviour
{
    [Header("Reference")]
    private SupportBotStatusManager status;

    [Header("Material")]    // 보조 로봇 타입별 색상 변경용 변수
    [SerializeField]    // Inspector 할당
    private GameObject supBotPrefab;    // 보조 로봇 프리팹
    [SerializeField]    // Inspector 할당
    private Material[] botTypeMaterials; // 타입별 머티리얼 배열

    [Header("Idle Animation Values")]
    /// <summary>
    ///  idle 애니메이션은 제자리에서 상하로 움직이는 단순한 트윈 애니메이션입니다.
    /// </summary>
    private Tween bobTween;               // 트윈 애니메이션 참조용 변수
    private float idleAmplitude = 0.5f;   // 상하 이동량
    private float idleDuration = 1f;      // 편도 시간(초)
    private float baseY;        // 시작 위치의 Y 좌표(로컬)

    [Header("Skill Animation Values")]
    /// <summary>
    /// 스킬 애니메이션은 제자리에서 고개를 좌우로 회전하는 트윈 애니메이션입니다.
    /// </summary>
    private float skillRotateStepDuration = 0.5f; // 각 회전 스텝 시간(짧을수록 빠름)
    private float skillSequenceSpeed = 2f;        // 시퀀스 전체 배속(크면 빠름)

    // 모든 Effect GameObject Inspector 할당
    [Header("Skill Effect Prefabs")]
    [SerializeField]
    private GameObject shieldEffect;
    [SerializeField]
    private GameObject healEffect;
    [SerializeField]
    private GameObject destroyEffect;

    // 자동 공격 시 컬러 변경
    [Header("Auto Attack Color")]
    [SerializeField]
    private Material supBotMaterial;
    private Color originalColor;
    [SerializeField]
    private Color attackColor = Color.red;
    private bool isAttacking = false;   // 자동 공격 중 여부

    void Awake()
    {
        status = GetComponent<SupportBotStatusManager>();
        baseY = transform.localPosition.y;

        shieldEffect.SetActive(false);
        healEffect.SetActive(false);
        destroyEffect.SetActive(false);

        // Renderer 가져오기 및 Material Instance 생성
        var renderer = supBotPrefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            // .material 접근으로 자동 인스턴스 생성 및 참조 저장
            supBotMaterial = renderer.material;
            originalColor = supBotMaterial.GetColor("_Color");
        }
        else
        {
            Debug.LogError("supBotPrefab에서 Renderer를 찾을 수 없습니다!");
        }
    }

    void OnEnable()
    {
        StartBob();
    }

    void OnDisable()
    {
        if (bobTween != null)
        {
            bobTween.Kill();    // Kill 함수로 트윈 애니메이션을 중단 및 제거
            bobTween = null;
        }
        ResetToBasePosition(); // 위치와 회전 초기화
    }

    void Update()
    {
        // 자동 공격 중에는 TypeColor 실행 안 함
        if (isAttacking) return;
        
        // 타입이 바뀌었는지 바로 확인하기 위해 Update에서 체크
        // 성능 이슈가 있다면 상태가 바뀌는 시점에 호출하는 방식으로 변경
        TypeColor();
    }

    // 위아래 루프 트윈 시작(이미 있으면 재생만)
    private void StartBob()
    {
        // DOTween 함수를 이용해 idle 애니메이션 생성 및 재생
        // 연출 변경은 DOTween 함수의 매개변수를 수정
        if (bobTween == null)
        {
            bobTween = transform
                .DOLocalMoveY(baseY + idleAmplitude, idleDuration)
                .SetEase(Ease.InOutSine)    // 자연스러운 움직임
                .SetLoops(-1, LoopType.Yoyo)    // 왕복 운동을 무한 반복
                .SetUpdate(false)   // timeScale 영향을 받도록 설정
                .SetAutoKill(false); // 재사용 관련
        }

        if (!bobTween.IsPlaying()) bobTween.Play();
    }

    private void ResetToBasePosition()
    {
        Vector3 resetPos = transform.localPosition;
        resetPos.y = baseY;
        transform.localPosition = resetPos;

        // 스킬 애니메이션으로 인한 회전도 초기화
        transform.localRotation = Quaternion.identity;
    }

    private void TypeColor()
    {
        if (supBotPrefab == null || botTypeMaterials == null) return;
        
        var renderer = supBotPrefab.GetComponent<Renderer>();
        if (renderer == null) return;

        Material targetMaterial = null;
        
        switch (status.setting.botType)
        {
            case SupportBotType.Guard:
                targetMaterial = botTypeMaterials.Length > 0 ? botTypeMaterials[0] : null;
                break;
            case SupportBotType.Shield:
                targetMaterial = botTypeMaterials.Length > 1 ? botTypeMaterials[1] : null;
                break;
            case SupportBotType.Heal:
                targetMaterial = botTypeMaterials.Length > 2 ? botTypeMaterials[2] : null;
                break;
        }

        // 머테리얼 교체 시 참조 업데이트
        if (targetMaterial != null && renderer.sharedMaterial != targetMaterial)
        {
            renderer.material = targetMaterial;
            supBotMaterial = renderer.material; // 새 인스턴스 참조 저장
            originalColor = supBotMaterial.GetColor("_Color"); // Standard 셰이더의 _Color 프로퍼티에서 가져오기
        }
    }

    // 스킬 발동 애니메이션 시퀀스
    public void ActivateSkillEffect()
    {
        var seq = DOTween.Sequence();  // 빈 시퀀스 생성
        seq.timeScale = Mathf.Max(0.0001f, skillSequenceSpeed); // 전체 시퀀스 속도 조절

        seq.Append(transform.DORotate(new Vector3(0, 30, 0), skillRotateStepDuration).SetEase(Ease.InOutSine))  // 1단계: 30도 회전
           .Append(transform.DORotate(new Vector3(0, -30, 0), skillRotateStepDuration).SetEase(Ease.InOutSine)) // 2단계: -30도 회전
           .SetLoops(2, LoopType.Yoyo); // 전체 시퀀스를 2번 왕복 반복
    }

    // 스킬 이펙트 토글
    public void ToggleShieldEffect(bool isOn)
    {
        shieldEffect.SetActive(isOn);
    }

    public void ToggleHealEffect(bool isOn)
    {
        healEffect.SetActive(isOn);
    }

    public void ToggleDestroyEffect(bool isOn)
    {
        destroyEffect.SetActive(isOn);
    }

    public void ToggleSupportBot(bool isOn)
    {
        supBotPrefab.SetActive(isOn);
    }

    public void StartAutoAttackEffect()
    {
        if (supBotMaterial != null)
        {
            isAttacking = true; // 공격 상태로 전환 (TypeColor 중단)
            supBotMaterial.SetColor("_Color", attackColor);
        }
    }

    public void EndAutoAttackEffect()
    {
        if (supBotMaterial != null)
        {
            isAttacking = false; // 공격 종료
            supBotMaterial.SetColor("_Color", originalColor);
            
            // TypeColor 즉시 실행하여 타입별 머테리얼 복원
            TypeColor();
        }
    }
}
