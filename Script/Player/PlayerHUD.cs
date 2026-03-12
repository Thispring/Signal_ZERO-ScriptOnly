using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Player정보를 출력하는 HUD에 관련된 스크립트 입니다.
/// 모든 Player에 관련한 스크립트는 Player을 앞에 붙여줍니다.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("WeaponName")]
    [SerializeField]    // Inspector 할당
    private TextMeshProUGUI textWeaponName;

    // 모든 요소 Inspector 할당
    [Header("CrossHair")]
    [SerializeField]
    private Image imageCrossHair;   // 조준점 이미지
    [SerializeField]
    private Sprite[] spritesCrossHair;  // 조준점에 사용되는 sprite 배열

    // 모든 요소 Inspector 할당
    [Header("Sniper HUD")]  // 'Sniper' 사격 UI
    [SerializeField]
    private Slider sniperChargeHUD; // 차지 Slider  
    [SerializeField]
    private Image sniperZoomHUD; // 스나이퍼 줌 UI

    // 모든 Image Inspector 할당
    [Header("Scope Blur")]
    [SerializeField]
    private Image scopeBlurImage; // 스나이퍼 조준 시 화면 블러 이미지
    [SerializeField]
    private Image burstScopeBlurImage; // 버스트 시, 화면 블러 불투명으로 고정
    [SerializeField, Range(0f, 1f)]
    private float scopeBlurMaxAlpha = 1f; // 차지 완료 시 목표 투명도

    // 모든 요소 Inspector 할당
    [Header("Ammo")]
    private WeaponBase weapon;
    [SerializeField]
    private TextMeshProUGUI textAmmo;   // 현재/최대 탄 수 출력 text
    [SerializeField]
    private TextMeshProUGUI textReload;   // 재장전 상태 출력 text

    // 모든 요소 Inspector 할당
    [Header("Game Info")]
    [SerializeField]
    private StageManager stageManager; // 스테이지 번호 참조
    [SerializeField]
    private TextMeshProUGUI textStage; // 스테이지 정보 출력 text
    [SerializeField]
    private PlayerStatusManager playerStatusManager; // 플레이어 상태 참조

    // 모든 요소 Inspector 할당
    [Header("HP")]
    private int previousHP; // 이전 체력을 저장하는 변수
    [SerializeField]
    private Slider playerHPSlider;
    [SerializeField]
    private TextMeshProUGUI textPlayerMaxHP; // 플레이어 최대 체력 정보 text
    [SerializeField]
    private Image lowHPImage;   // 저체력 경고 UI 이미지

    [Header("Coin")]
    [SerializeField]    // Inspector 할당
    private TextMeshProUGUI textCoin;

    // 모든 요소 Inspector 할당
    [Header("Burst")]
    [SerializeField]
    private Slider burstGaugeSlider; // 버스트 게이지 슬라이더
    [SerializeField]
    private Image burstReadyImage; // 버스트 사용 가능 표시 이미지
    // 무기 종류 별, 버스트 이미지를 다르게 표시하기 위해 배열 사용
    [SerializeField]
    private Sprite[] burstReadySprite;
    [SerializeField]
    private Sprite[] burstActiveWeaponSprite;

    [Header("DOTween")]
    // burstReadyImage 활성화를 Tween 애니메이션으로 연출
    private RectTransform burstReadyRectPos;
    private Vector2 burstReadyStartAnchoredPos;
    private bool burstReadyStartPosSet = false;
    private bool burstReadyAnimating = false;

    // 모든 요소 Inspector 할당
    // 어떤 무기를 선택했는지 표시하는 UI
    [Header("Weapon UI")]
    [SerializeField]
    private Sprite[] stateSelectedSprite;   // 무기 선택 상태에 따른 이미지
    [SerializeField]
    private Image[] weaponImages;
    [SerializeField]
    private TextMeshProUGUI[] textWeaponNumber;
    [SerializeField]
    private Image[] weaponIcons;
    [SerializeField]
    private Sprite[] weaponSprites;
    // 모든 무기 참조 (HUD에서 버스트 아이콘 반영용)
    private WeaponBase[] allWeapons;

    // 모든 요소 Inspector 할당    
    [Header("SupportBot")]
    [SerializeField]
    private SupportBotStatusManager supportBotStatusManager;
    [SerializeField]
    private SupportBotFSM supportBotFSM;
    [SerializeField]
    private Image supportBotImage;
    [SerializeField]
    private Sprite[] supportBotTypeSprite;
    [SerializeField]
    private Image supportBotReadyImage; // SupportBot 스킬 쿨타임 이미지
    // SupportBot 타입 변경 감지용 캐시
    private SupportBotType? prevSupportBotType = null;

    void Awake()
    {
        lowHPImage.gameObject.SetActive(false);

        Color color = supportBotImage.color;
        color.a = 255;
        supportBotImage.color = color; // SupportBot 이미지 투명도 255

        if (textReload != null) textReload.text = ""; // 재장전 상태 초기화 
        if (textPlayerMaxHP != null) textPlayerMaxHP.text = ""; // 최대 체력 텍스트 초기화

        // 스코프 블러 초기 알파 0 설정
        if (scopeBlurImage != null)
        {
            var c = scopeBlurImage.color;
            c.a = 0f;
            scopeBlurImage.color = c;
        }
        if (burstScopeBlurImage != null)
        {
            var c = burstScopeBlurImage.color;
            c.a = 0f;
            burstScopeBlurImage.color = c;
        }

        // burstReadyImage 활성화
        if (burstReadyImage != null)
        {
            burstReadyImage.gameObject.SetActive(true);
            burstReadyRectPos = burstReadyImage.rectTransform;
            // 시작 위치는 Inspector에서 설정되지 않았다면 현재 위치를 기본으로 사용
            burstReadyStartAnchoredPos = burstReadyRectPos.anchoredPosition;
            burstReadyStartPosSet = true;
        }
    }

    void Start()
    {
        SetBurstReadyStartPos(burstReadyStartAnchoredPos);
    }

    void Update()
    {
        UpdateAmmoHUD(weapon.CurrentAmmo, weapon.MaxAmmo);  // 실시간으로 총알 개수 업데이트
        UpdateReloadHint(); // 현재 탄약이 전체 탄약의 절반 이하일 때, 재장전 유도 UI 표시
        UpdateStageUI(); // 스테이지 UI 업데이트

        // 이전 체력과 현재 체력을 비교하여 UpdateHPHUD 호출
        int currentHP = playerStatusManager.CurrentHP; // PlayerStatusManager에서 현재 체력 가져오기

        if (previousHP != currentHP)
        {
            previousHP = currentHP; // 이전 체력을 현재 체력으로 갱신
            if (playerHPSlider != null)
            {
                playerHPSlider.maxValue = playerStatusManager.MaxHP;
                playerHPSlider.value = currentHP;
            }
        }

        // 최대 체력 증가 배수 텍스트 갱신 (increaseCount 기반)
        UpdatePlayerMaxHPText(playerStatusManager.increaseCount);
        UpdateLowHPImage();
        UpdateCoinHUD(playerStatusManager.coin);

        UpdateBurstWeaponIcons();
        UpdateBurstGauge(playerStatusManager.burstCurrentGauge, playerStatusManager.burstMaxGauge);
        UpdateBurstReadyImage(playerStatusManager.isBurstReady); // 버스트 준비 이미지 업데이트

        UpdateSupportBotReady();

        // SupportBot 타입 변경 시 스프라이트 갱신
        if (supportBotStatusManager != null)
        {
            var currentType = supportBotStatusManager.setting.botType;
            if (!prevSupportBotType.HasValue || prevSupportBotType.Value != currentType)
            {
                UpdateSupportBotTypeSprite(currentType);
                prevSupportBotType = currentType;
            }
        }        
    }

    #region Weapon Methods

    public void SetupAllWeapons(WeaponBase[] weapons)
    {
        allWeapons = weapons; // 무기 배열 보관
        for (int i = 0; i < weapons.Length; ++i)
        {
            weapons[i].onAmmoEvent.AddListener(UpdateAmmoHUD);
        }
        // 초기 무기 아이콘 상태 반영
        UpdateBurstWeaponIcons();
    }

    public void SwitchingWeapon(WeaponBase newWeapon)   // 무기 교체 
    {
        weapon = newWeapon;
        SetUpWeapon();

        int selectedIndex;
        switch (weapon.WeaponType)
        {
            case WeaponType.Single:
                imageCrossHair.sprite = spritesCrossHair[0];
                selectedIndex = 0;
                break;
            case WeaponType.Rapid:
                imageCrossHair.sprite = spritesCrossHair[1];
                selectedIndex = 1;
                break;
            case WeaponType.Sniper:
                imageCrossHair.sprite = spritesCrossHair[2];
                selectedIndex = 2;
                break;
            default:
                Debug.LogWarning("알 수 없는 무기 타입입니다. 기본 조준점을 설정합니다.");
                imageCrossHair.sprite = spritesCrossHair[0];
                selectedIndex = -1;
                break;
        }

        // weaponImages(3개), stateSelectedSprite(비선택/선택 2개) 기준으로 for문 단순화
        for (int i = 0; i < weaponImages.Length; i++)
        {
            if (i == selectedIndex && selectedIndex >= 0)
            {
                weaponImages[i].sprite = stateSelectedSprite[1]; // 선택

                // 선택된 무기 번호 텍스트 색상을 #03FFFF로 변경
                if (i < textWeaponNumber.Length && textWeaponNumber[i] != null)
                {
                    textWeaponNumber[i].color = new Color32(3, 255, 255, 255); // #03FFFF 색상
                }
            }
            else
            {
                weaponImages[i].sprite = stateSelectedSprite[0]; // 비선택

                // 선택되지 않은 무기 번호 텍스트 색상을 흰색으로 유지
                if (i < textWeaponNumber.Length && textWeaponNumber[i] != null)
                {
                    textWeaponNumber[i].color = Color.white; // 흰색
                }
            }
        }

        // 무기 교체 직후 재장전 힌트 즉시 갱신
        UpdateReloadHint();

        // 무기 교체 직후 버스트 아이콘도 즉시 반영
        UpdateBurstWeaponImages();
        UpdateBurstWeaponIcons();
    }

    private void SetUpWeapon()
    {
        textWeaponName.text = weapon.WeaponName.ToString(); // 텍스트 변경

        // 무기 아이콘(weaponIcons)을 버스트 상태에 맞게 갱신
        UpdateBurstWeaponIcons();

        // 무기 타입 확인하여 차지샷 UI 활성화/비활성화
        sniperChargeHUD.gameObject.SetActive(weapon.WeaponType == WeaponType.Sniper);
        // 무기 타입 확인하여 Sniper UI 활성화/비활성화
        sniperZoomHUD.gameObject.SetActive(weapon.WeaponName == WeaponName.Sniper);
    }

    private void UpdateAmmoHUD(int currentAmmo, int maxAmmo)    // 총알 정보를 UI에 표시하는 함수
    {
        textAmmo.text = $"{currentAmmo}/{maxAmmo}";
    }

    // 무기별 탄약이 절반 미만이면 재장전 힌트를 표시
    private void UpdateReloadHint()
    {
        if (textReload == null || weapon == null) return;

        int maxAmmo = weapon.MaxAmmo;
        int currentAmmo = weapon.CurrentAmmo;

        if (maxAmmo > 0 && currentAmmo < (maxAmmo / 2f))
        {
            textReload.text = "재장전: R";
        }
        else
        {
            textReload.text = "";
        }
    }

    #endregion

    #region Sniper HUD & Scope Blur Methods

    public void UpdateSniperChargingHUD(float chargeProgress, bool isChargeReady)
    {
        sniperChargeHUD.value = 1;  // Slider의 벨류값으로 저격 가능 상태 표시
        if (isChargeReady)
        {
            sniperChargeHUD.value = 1; // 차지 완료
        }
        else
        {
            sniperChargeHUD.value = chargeProgress; // 진행도 표시
        }
    }
    
    // 외부에서 차지 진행도(0~1)에 따라 알파를 갱신
    public void UpdateScopeBlurByCharge(float chargeProgress)
    {
        if (scopeBlurImage == null) return;
        float a = Mathf.Clamp01(chargeProgress) * scopeBlurMaxAlpha;
        var c = scopeBlurImage.color;
        c.a = a;
        scopeBlurImage.color = c;
    }

    public void ResetScopeBlur()
    {
        if (scopeBlurImage == null) return;
        var c = scopeBlurImage.color;
        c.a = 0f;
        scopeBlurImage.color = c;
    }

    // 버스트 중 블러를 강제로 최대치로 고정
    public void SetScopeBlurMax()
    {
        if (burstScopeBlurImage == null) return;
        var c = burstScopeBlurImage.color;
        c.a = 1f;
        burstScopeBlurImage.color = c;
    }

    // 버스트 전용: 즉시 0으로 끄기
    public void ResetBurstScopeBlur()
    {
        if (burstScopeBlurImage == null) return;
        var c = burstScopeBlurImage.color;
        c.a = 0f;
        burstScopeBlurImage.color = c;
    }

    // sniperZoomHUD 이미지를 0.25사이즈에서 1사이즈까지 점차 커지게하는 메서드
    // 초기값은 0.25f, 최종값은 1f, duration은 0.5초
    public void AnimateSniperZoomHUD()
    {
        if (sniperZoomHUD == null) return;
        // 코루틴 없이 즉시 1로 설정
        sniperZoomHUD.rectTransform.localScale = new Vector3(1f, 1f, 1f);
    }

    // 스나이퍼 줌 HUD 크기 초기화: 입력값을 0 또는 1로만 적용
    public void ResetSniperZoomHUD(float initialScale = 0f)
    {
        if (sniperZoomHUD == null) return;
        // 0 또는 1로 스냅(0.5 기준 이분)
        float snapped = (initialScale >= 0.5f) ? 1f : 0f;
        sniperZoomHUD.rectTransform.localScale = new Vector3(snapped, snapped, 1f);
    }

    #endregion

    #region Burst Methods

    private void UpdateBurstGauge(float current, float max)
    {
        if (burstGaugeSlider != null)
        {
            burstGaugeSlider.maxValue = max;
            burstGaugeSlider.value = current;
        }
    }

    private void UpdateBurstReadyImage(bool isReady)
    {
        if (burstReadyImage != null)
        {
            // 항상 활성화 상태 유지
            burstReadyImage.gameObject.SetActive(true);

            // isReady일 때 현재 무기 타입에 따라 스프라이트 교체
            if (isReady && weapon != null && burstReadySprite != null && burstReadySprite.Length >= 3)
            {
                int spriteIndex = 0; // 기본 Single
                switch (weapon.WeaponType)
                {
                    case WeaponType.Single:
                        spriteIndex = 0; break;
                    case WeaponType.Rapid:
                        spriteIndex = 1; break;
                    case WeaponType.Sniper:
                        spriteIndex = 2; break;
                    default:
                        spriteIndex = 0; break;
                }

                var sprite = burstReadySprite[spriteIndex];
                if (sprite != null)
                {
                    burstReadyImage.sprite = sprite;
                }
            }

            // isReady에 따라 별도 메서드로 애니메이션 제어
            if (isReady)
            {
                AnimateBurstReadyToTarget();
            }
            else
            {
                ReturnBurstReadyToStart();
            }
        }
    }

    // isBurstReady가 true일 때 타겟 위치로 이동 (중복 실행 방지)
    private void AnimateBurstReadyToTarget()
    {
        if (burstReadyRectPos == null || !burstReadyStartPosSet) return;
        if (burstReadyAnimating) return;

        // 목표 위치: X = 795, Y는 시작 Y 유지
        Vector2 target = new Vector2(795f, burstReadyStartAnchoredPos.y);

        // 이미 목표에 거의 도달한 경우 바로 리턴
        if (Vector2.Distance(burstReadyRectPos.anchoredPosition, target) < 1f) return;

        burstReadyAnimating = true;
        burstReadyRectPos.DOAnchorPos(target, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            burstReadyAnimating = false;
        });
    }

    // isBurstReady가 false일 때 원위치로 복귀
    private void ReturnBurstReadyToStart()
    {
        if (burstReadyRectPos == null || !burstReadyStartPosSet) return;
        if (burstReadyAnimating) return;

        // 이미 시작 위치에 거의 도달한 경우 바로 리턴
        if (Vector2.Distance(burstReadyRectPos.anchoredPosition, burstReadyStartAnchoredPos) < 1f) return;

        burstReadyAnimating = true;
        burstReadyRectPos.DOAnchorPos(burstReadyStartAnchoredPos, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            burstReadyAnimating = false;
        });
    }

    // 버스트 준비 이미지의 초기 시작 좌표를 설정할 수 있는 메서드
    private void SetBurstReadyStartPos(Vector2 anchoredPos)
    {
        if (burstReadyRectPos == null && burstReadyImage != null)
            burstReadyRectPos = burstReadyImage.rectTransform;

        if (burstReadyRectPos != null)
        {
            burstReadyStartAnchoredPos = anchoredPos;
            burstReadyRectPos.anchoredPosition = anchoredPos;
            burstReadyStartPosSet = true;
        }
    }

    // 각 Weapon 별 isBurst가 true면 해당 무기 아이콘을 burstActiveWeaponSprite로 교체
    private void UpdateBurstWeaponImages()
    {
        if (weaponImages == null || weaponImages.Length < 3) return;

        // 현재 선택 인덱스 계산(선택/비선택 기본 아이콘 복원에 사용)
        int selectedIndex = -1;
        if (weapon != null)
        {
            switch (weapon.WeaponType)
            {
                case WeaponType.Single: selectedIndex = 0; break;
                case WeaponType.Rapid: selectedIndex = 1; break;
                case WeaponType.Sniper: selectedIndex = 2; break;
            }
        }

        // 기본 아이콘(선택/비선택)으로 먼저 세팅
        for (int i = 0; i < weaponImages.Length; i++)
        {
            if (i == selectedIndex && selectedIndex >= 0)
            {
                if (stateSelectedSprite != null && stateSelectedSprite.Length > 1)
                    weaponImages[i].sprite = stateSelectedSprite[1]; // 선택 아이콘
            }
            else
            {
                if (stateSelectedSprite != null && stateSelectedSprite.Length > 0)
                    weaponImages[i].sprite = stateSelectedSprite[0]; // 비선택 아이콘
            }
        }
    }

    // weaponIcons를 weaponSprites로 초기화 후, 각 무기가 버스트 중이면 해당 인덱스를 burstActiveWeaponSprite로 교체
    private void UpdateBurstWeaponIcons()
    {
        if (weaponIcons == null || weaponSprites == null) return;
        if (weaponIcons.Length < 3 || weaponSprites.Length < 3) return;

        // 1) 기본 아이콘으로 초기화
        for (int i = 0; i < 3; i++)
        {
            var baseSprite = weaponSprites[i];
            if (weaponIcons[i] != null && baseSprite != null)
            {
                weaponIcons[i].sprite = baseSprite;
            }
        }

        // 2) 버스트 상태인 무기만 덮어쓰기
        if (burstActiveWeaponSprite == null || burstActiveWeaponSprite.Length < 3) return;

        bool singleBurst = false, rapidBurst = false, sniperBurst = false;

        if (allWeapons != null && allWeapons.Length > 0)
        {
            foreach (var w in allWeapons)
            {
                if (w == null || !w.IsBurst) continue;
                switch (w.WeaponType)
                {
                    case WeaponType.Single: singleBurst = true; break;
                    case WeaponType.Rapid: rapidBurst = true; break;
                    case WeaponType.Sniper: sniperBurst = true; break;
                }
            }
        }
        else if (weapon != null && weapon.IsBurst)
        {
            // allWeapons가 없다면 현재 무기만 기준으로 처리
            switch (weapon.WeaponType)
            {
                case WeaponType.Single: singleBurst = true; break;
                case WeaponType.Rapid: rapidBurst = true; break;
                case WeaponType.Sniper: sniperBurst = true; break;
            }
        }

        if (singleBurst && weaponIcons[0] != null && burstActiveWeaponSprite[0] != null)
            weaponIcons[0].sprite = burstActiveWeaponSprite[0];
        if (rapidBurst && weaponIcons[1] != null && burstActiveWeaponSprite[1] != null)
            weaponIcons[1].sprite = burstActiveWeaponSprite[1];
        if (sniperBurst && weaponIcons[2] != null && burstActiveWeaponSprite[2] != null)
            weaponIcons[2].sprite = burstActiveWeaponSprite[2];
    }

    #endregion

    #region Player Methods

    private void UpdateCoinHUD(int currentCoin)
    {
        textCoin.text = $"Coin: {currentCoin}"; // 코인 UI 업데이트
    }

    // 최대 체력 증가 배수를 표시: increaseCount >= 2 일 때 "x{increaseCount}"
    private void UpdatePlayerMaxHPText(int increaseCount)
    {
        if (textPlayerMaxHP == null) return;
        if (increaseCount >= 2)
        {
            textPlayerMaxHP.text = $"MaxHP: {increaseCount}00";
        }
        else
        {
            textPlayerMaxHP.text = $"MaxHP: 100";
        }
    }

    private void UpdateLowHPImage()
    {
        int currentHP = playerStatusManager.CurrentHP; // PlayerStatusManager에서 현재 체력 가져오기
        int maxHP = playerStatusManager.MaxHP;         // PlayerStatusManager에서 최대 체력 가져오기

        if (previousHP != currentHP || playerHPSlider.maxValue != maxHP)
        {
            previousHP = currentHP; // 이전 체력을 현재 체력으로 갱신
            if (playerHPSlider != null)
            {
                playerHPSlider.maxValue = maxHP;
                playerHPSlider.value = currentHP;
            }
        }

        // 전체 체력의 30% 미만일 때, Low HP 이미지 활성화/비활성화
        if (maxHP > 0 && ((float)currentHP / maxHP) < 0.3f)
        {
            if (lowHPImage != null && !lowHPImage.gameObject.activeSelf)
                lowHPImage.gameObject.SetActive(true);
        }
        else
        {
            if (lowHPImage != null && lowHPImage.gameObject.activeSelf)
                lowHPImage.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Other Methods (Stage & SupportBot)

    private void UpdateStageUI() // 스테이지 정보 UI 업데이트
    {
        textStage.text = $"stage: " + stageManager.stageNumber.ToString();
    }

    // SupportBot 타입을 스프라이트 인덱스로 매핑하고 이미지 교체
    // Guard[0], Shield[1], Heal[2]
    private void UpdateSupportBotTypeSprite(SupportBotType type)
    {
        int spriteIndex = 0; // 기본값: Guard
        switch (type)
        {
            case SupportBotType.Guard:
                spriteIndex = 0; break;
            case SupportBotType.Shield:
                spriteIndex = 1; break;
            case SupportBotType.Heal:
                spriteIndex = 2; break;
        }

        if (supportBotTypeSprite != null && spriteIndex >= 0 && spriteIndex < supportBotTypeSprite.Length)
        {
            var sprite = supportBotTypeSprite[spriteIndex];
            if (sprite != null && supportBotImage != null)
            {
                supportBotImage.sprite = sprite;
            }
        }
    }

    private void UpdateSupportBotReady()
    {
        // 보조로봇 쿨타임 업데이트
        if (supportBotStatusManager != null)
        {
            // 보조로봇 쿨타임 가져오기
            float supportBotCooldown = supportBotStatusManager.setting.coolTime;
            // 최대 쿨타임 설정
            float supportBotMaxCooldown = 10;
            if (supportBotCooldown <= 0 && supportBotFSM.IsSupporting == false)
            {
                supportBotReadyImage.fillAmount = 1f; // 쿨타임 완료 상태
                var c = (Color32)supportBotImage.color;
                c.a = 255; // 쿨타임 완료 시 불투명도 255
                supportBotImage.color = c;
            }
            else if (supportBotFSM.IsSupporting)
            {
                supportBotReadyImage.fillAmount = 1f - (supportBotCooldown / supportBotMaxCooldown); // 쿨타임 진행 상태
                var c = (Color32)supportBotImage.color;
                c.a = 255; // 쿨타임 완료 시 불투명도 255
                supportBotImage.color = c;
            }
            else
            {
                supportBotReadyImage.fillAmount = 1f - (supportBotCooldown / supportBotMaxCooldown); // 쿨타임 진행 상태
                var c = (Color32)supportBotImage.color;
                c.a = 150; // 진행 중일 때 투명도 150
                supportBotImage.color = c;
            }
        }
        else
        {
            supportBotReadyImage.fillAmount = 0f;
        }
    }

    #endregion
}
