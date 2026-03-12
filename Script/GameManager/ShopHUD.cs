using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

// 업그레이드 종류를 정의하는 Enum
public enum UpgradeType
{
    SingleWeapon,
    RapidWeapon,
    SniperWeapon,
    SingleBurst,
    RapidBurst,
    SniperBurst,
    SupportBotGuard,
    SupportBotShield,
    SupportBotHeal,
    Heal,
    IncreaseMaxHP,
}

/// <summary>
/// 상점과 관련된 UI를 관리하는 스크립트입니다.
/// </summary>
public class ShopHUD : MonoBehaviour
{
    /// 업그레이드 종류를 구별하기 위한 카테고리
    /// Weapon, Burst, SupportBot, Health로 구분중
    [Header("Category")]
    [SerializeField] private Image[] categoryImage; // 카테고리 이미지

    [Header("Level Text")]
    [SerializeField] private TextMeshProUGUI singleUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI rapidUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI sniperUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI singleBurstUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI rapidBurstUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI sniperBurstUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI supportBotShieldLevelText;
    [SerializeField] private TextMeshProUGUI supportBotHealLevelText;
    [SerializeField] private TextMeshProUGUI supportBotGuardLevelText;
    [SerializeField] private TextMeshProUGUI healLevelText;
    [SerializeField] private TextMeshProUGUI increaseMaxHPLevelText;

    [Header("Info Text")]
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI botTypeText;

    // Select Button Images (상점 UI의 버튼 이미지를 Inspector에서 3개 할당)
    [Header("Select Button Images")]
    [SerializeField] private Image[] selectButtonImage = new Image[3];
    // 버튼 상태별 스프라이트: [0] = 비선택, [1] = 선택
    [Header("Select Button State Sprites")]
    [SerializeField] private Sprite[] selectButtonStateSprites = new Sprite[2];

    [Header("References")]
    // ShopManager 참조
    private ShopManager shopManager;
    [SerializeField]
    private SupportBotStatusManager supportBotStatusManager;

    // 현재 선택된 업그레이드 타입 저장
    private UpgradeType? currentSelectedUpgrade = null;

    // botTypeText 표시 시간 변수
    private float botTypeTextDisplayTime = 2.0f;

    void Awake()
    {
        selectButtonImage[0].sprite = selectButtonStateSprites[1];
        infoText.text = "";
        botTypeText.text = "";
        shopManager = GetComponent<ShopManager>();
    }

    void Start()
    {
        // 게임 시작 시 초기 레벨 텍스트 설정
        InitializeLevelTexts();
    }

    void Update()
    {
        SelectSupportBotButtonImage();
    }

    private void SelectSupportBotButtonImage()
    {
        switch (supportBotStatusManager.setting.botType)
        {
            case SupportBotType.Guard:
                selectButtonImage[0].sprite = selectButtonStateSprites[1];
                selectButtonImage[1].sprite = selectButtonStateSprites[0];
                selectButtonImage[2].sprite = selectButtonStateSprites[0];
                break;
            case SupportBotType.Shield:
                selectButtonImage[1].sprite = selectButtonStateSprites[1];
                selectButtonImage[0].sprite = selectButtonStateSprites[0];
                selectButtonImage[2].sprite = selectButtonStateSprites[0];
                break;
            case SupportBotType.Heal:
                selectButtonImage[2].sprite = selectButtonStateSprites[1];
                selectButtonImage[0].sprite = selectButtonStateSprites[0];
                selectButtonImage[1].sprite = selectButtonStateSprites[0];
                break;
        }
    }

    // 초기 레벨 텍스트 설정 (모든 레벨을 1로 초기화)
    private void InitializeLevelTexts()
    {
        // 무기 레벨 텍스트 초기화
        if (singleUpgradeLevelText != null)
            singleUpgradeLevelText.text = "Lv: 1";
        if (rapidUpgradeLevelText != null)
            rapidUpgradeLevelText.text = "Lv: 1";
        if (sniperUpgradeLevelText != null)
            sniperUpgradeLevelText.text = "Lv: 1";

        // 버스트 데미지 레벨 텍스트 초기화
        if (singleBurstUpgradeLevelText != null)
            singleBurstUpgradeLevelText.text = "Lv: 1";
        if (rapidBurstUpgradeLevelText != null)
            rapidBurstUpgradeLevelText.text = "Lv: 1";
        if (sniperBurstUpgradeLevelText != null)
            sniperBurstUpgradeLevelText.text = "Lv: 1";

        // SupportBot 레벨 텍스트 초기화
        if (supportBotShieldLevelText != null)
            supportBotShieldLevelText.text = "Lv: 1";
        if (supportBotHealLevelText != null)
            supportBotHealLevelText.text = "Lv: 1";
        if (supportBotGuardLevelText != null)
            supportBotGuardLevelText.text = "Lv: 1";

        // Health 레벨 텍스트 초기화
        if (healLevelText != null)
            healLevelText.text = "회복량: 1";
        if (increaseMaxHPLevelText != null)
            increaseMaxHPLevelText.text = "Lv: 1";
    }

    public void ActiveCatergory(int categoryIndex)
    {
        // 모든 카테고리 이미지 비활성화
        for (int i = 0; i < categoryImage.Length; i++)
        {
            categoryImage[i].gameObject.SetActive(false);
        }

        // 선택된 카테고리 이미지만 활성화
        if (categoryIndex >= 0 && categoryIndex < categoryImage.Length)
        {
            categoryImage[categoryIndex].gameObject.SetActive(true);
        }
    }

    // 레벨 텍스트 업데이트 함수들
    public void UpdateLevelTexts()
    {
        // shopManager가 null이면 우선 초기화된 값으로 설정
        if (shopManager == null)
        {
            shopManager = GetComponent<ShopManager>();
            if (shopManager == null)
            {
                InitializeLevelTexts(); // shopManager를 찾을 수 없으면 기본값으로 초기화
                return;
            }
        }

        // 무기 레벨 텍스트 업데이트 (temp 변수 사용)
        if (singleUpgradeLevelText != null)
            singleUpgradeLevelText.text = $"Lv: {shopManager.GetTempSinglePowerUpLevel()}";
        if (rapidUpgradeLevelText != null)
            rapidUpgradeLevelText.text = $"Lv: {shopManager.GetTempRapidPowerUpLevel()}";
        if (sniperUpgradeLevelText != null)
            sniperUpgradeLevelText.text = $"Lv: {shopManager.GetTempSniperPowerUpLevel()}";

        // 버스트 데미지 레벨 텍스트 업데이트 (temp 변수 사용)
        if (singleBurstUpgradeLevelText != null)
            singleBurstUpgradeLevelText.text = $"Lv: {shopManager.GetTempSingleBurstPowerUpLevel()}";
        if (rapidBurstUpgradeLevelText != null)
            rapidBurstUpgradeLevelText.text = $"Lv: {shopManager.GetTempRapidBurstPowerUpLevel()}";
        if (sniperBurstUpgradeLevelText != null)
            sniperBurstUpgradeLevelText.text = $"Lv: {shopManager.GetTempSniperBurstPowerUpLevel()}";

        // SupportBot 레벨 텍스트 업데이트 (temp 변수 사용)
        if (supportBotShieldLevelText != null)
            supportBotShieldLevelText.text = $"Lv: {shopManager.GetTempSupportBotShieldUpgradeLevel()}";
        if (supportBotHealLevelText != null)
            supportBotHealLevelText.text = $"Lv: {shopManager.GetTempSupportBotHealUpgradeLevel()}";
        if (supportBotGuardLevelText != null)
            supportBotGuardLevelText.text = $"Lv: {shopManager.GetTempSupportBotGuardUpgradeLevel()}";

        // Health 레벨 텍스트 업데이트 (temp 변수 사용)
        if (healLevelText != null)
            healLevelText.text = $"회복량: {shopManager.GetTempHealLevel()}";
        if (increaseMaxHPLevelText != null)
            increaseMaxHPLevelText.text = $"Lv: {shopManager.GetTempIncreaseMaxHPLevel()}";
    }

    // 업그레이드 정보 표시 함수들
    public void ShowUpgradeInfo(int upgradeTypeIndex)
    {
        UpgradeType upgradeType = (UpgradeType)upgradeTypeIndex;
        ShowUpgradeInfo(upgradeType);
    }

    public void ShowUpgradeInfo(UpgradeType upgradeType)
    {
        if (infoText == null || shopManager == null) return;

        // 현재 선택된 업그레이드 타입 저장
        currentSelectedUpgrade = upgradeType;

        string upgradeInfo = "";

        switch (upgradeType)
        {
            case UpgradeType.SingleWeapon:
                upgradeInfo = GetWeaponUpgradeInfo("Single", shopManager.singlePowerUp,
                                              shopManager.GetTempSinglePowerUpLevel(), shopManager.coinsToPay);
                break;

            case UpgradeType.RapidWeapon:
                upgradeInfo = GetWeaponUpgradeInfo("Rapid", shopManager.rapidPowerUp,
                                              shopManager.GetTempRapidPowerUpLevel(), shopManager.coinsToPay);
                break;

            case UpgradeType.SniperWeapon:
                upgradeInfo = GetWeaponUpgradeInfo("Sniper", shopManager.sniperPowerUp,
                                              shopManager.GetTempSniperPowerUpLevel(), shopManager.coinsToPay);
                break;

            case UpgradeType.SingleBurst:
                upgradeInfo = GetBurstUpgradeInfo("Single 버스트 업그레이드", shopManager.singleBurstPowerUp,
                                             shopManager.GetTempSingleBurstPowerUpLevel(), shopManager.singleBurstTime);
                break;

            case UpgradeType.RapidBurst:
                upgradeInfo = GetBurstUpgradeInfo("Rapid 버스트 업그레이드", shopManager.rapidBurstPowerUp,
                                             shopManager.GetTempRapidBurstPowerUpLevel(), shopManager.rapidBurstTime);
                break;

            case UpgradeType.SniperBurst:
                upgradeInfo = GetBurstUpgradeInfo("Sniper 버스트 업그레이드", shopManager.sniperBurstPowerUp,
                                             shopManager.GetTempSniperBurstPowerUpLevel(), shopManager.sniperBurstTime);
                break;

            case UpgradeType.SupportBotGuard:
                // Guard 업그레이드는 가드 체력(내구)과 지속시간을 늘립니다. ShopManager의 guardBotPowerUp, guardBotTime 값을 사용합니다.
                upgradeInfo = GetSupportBotGuardInfo("M.I.N.G.O 가드 타입 업그레이드", shopManager.guardBotPowerUp, shopManager.guardBotTime,
                                                  shopManager.GetTempSupportBotGuardUpgradeLevel(), shopManager.coinsToPay);
                break;

            case UpgradeType.SupportBotShield:
                // Shield 업그레이드는 지속시간을 늘립니다. ShopManager의 shieldBotTime 값을 사용합니다.
                upgradeInfo = GetSupportBotShieldInfo("M.I.N.G.O 쉴드 타입 업그레이드", shopManager.shieldBotTime,
                                                  shopManager.GetTempSupportBotShieldUpgradeLevel(), shopManager.coinsToPay);
                break;

            case UpgradeType.SupportBotHeal:
                // Heal 업그레이드는 회복량과 회복 주기를 늘립니다. ShopManager의 healBotPowerUp, healBotTime 값을 사용합니다.
                upgradeInfo = GetSupportBotHealInfo("M.I.N.G.O 회복 타입 업그레이드", shopManager.healBotPowerUp, shopManager.healBotTime,
                                                  shopManager.GetTempSupportBotHealUpgradeLevel(), shopManager.coinsToPay);
                break;

            case UpgradeType.Heal:
                upgradeInfo = GetHealInfo();
                break;

            case UpgradeType.IncreaseMaxHP:
                upgradeInfo = GetHealthUpgradeInfo("최대 체력 증가", 50, shopManager.GetTempIncreaseMaxHPLevel(), shopManager.coinsToPay);
                break;
        }

        infoText.text = upgradeInfo;
    }

    private string GetWeaponUpgradeInfo(string weaponName, int powerUpValue, int currentLevel, int coinCost)
    {
        // Calculate displayed damage/increase and scaled cost. Show next level (currentLevel + 1) and cost scaled by level.
        int damage = powerUpValue * currentLevel;
        int cost = coinCost * (currentLevel + 1);
        return $"<color=#0769f3>{weaponName} 모드 업그레이드</color>\n\n" +
               $"Lv: <color=#0769f3>{currentLevel}</color>\n" +
               $"{weaponName} 모드의 공격력이 <color=#0769f3>+{damage}</color> 증가합니다.\n" +
               $"비용: <color=#0769f3>{cost}</color> 코인";
    }

    private string GetBurstUpgradeInfo(string burstName, int powerUpValue, int currentLevel, float burstTime)
    {
        // Compute displayed damage/increase and scaled cost using shopManager base cost. Show next level.
        int damage = powerUpValue * currentLevel;
        float time = burstTime * currentLevel;
        int cost = shopManager.coinsToPay * (currentLevel + 1);
        return $"<color=#0769f3>{burstName}</color>\n" +
               $"Lv: <color=#0769f3>{currentLevel}</color>\n" +
               $"비용: <color=#0769f3>{cost}</color>\n\n" +
               $"버스트의 공격력이 <color=#0769f3>+{damage}</color>\n" +
               $"지속 시간이 <color=#0769f3>+{time}</color> 증가합니다.";
    }

    private string GetHealthUpgradeInfo(string upgradeName, int upgradeAmount, int currentLevel, int coinCost)
    {
        // Highlight only parameter values
        int amount = upgradeAmount * currentLevel;
        int cost = coinCost * (currentLevel + 1);
        return $"<color=#0769f3>{upgradeName}</color>\n" +
               $"Lv: <color=#0769f3>{currentLevel}</color>\n" +
               $"비용: <color=#0769f3>{cost}</color>\n\n" +
               $"최대 체력이 <color=#0769f3>+{amount}</color> 증가합니다.";
    }

    // Heal 전용 정보: per spec
    private string GetHealInfo()
    {
        int coins = Mathf.Max(0, shopManager.GetCurrentCoin());
        int inShopMaxHP = shopManager.GetInShopMaxHP();
        int currentHP = shopManager.GetPlayerCurrentHP();
        int remainingNeeded = shopManager.GetRemainingNeededHeal();
        int healableNow = Mathf.Min(remainingNeeded, coins);
        return "<color=#0769f3>플레이어 체력회복\n</color>" +
               "보유 코인: <color=#0769f3>" + coins + "</color>\n\n" +
               "코인 <color=#0769f3>1</color>개당 체력 <color=#0769f3>1</color>을 회복합니다.\n" +
               "현재 체력: <color=#0769f3>" + currentHP + "</color> / <color=#0769f3>" + inShopMaxHP + "</color>\n" +
               "남은 필요 회복량: <color=#0769f3>" + remainingNeeded + "</color>\n";
    }

    private string GetSupportBotShieldInfo(string botName, int shieldTimeValue, int currentLevel, int coinCost)
    {
        // Highlight only parameter values
        int increase = shieldTimeValue * currentLevel;
        int cost = coinCost * (currentLevel + 1);
        return $"<color=#0769f3>{botName}</color>\n" +
               $"Lv: <color=#0769f3>{currentLevel}</color>\n" +
               $"비용: <color=#0769f3>{cost}</color>\n\n" +
               $"가드 지속 시간이 <color=#0769f3>+{increase}</color> 증가합니다.";
    }

    private string GetSupportBotHealInfo(string botName, int healAmountValue, int healTimeValue, int currentLevel, int coinCost)
    {
        // Highlight only parameter values
        int amount = healAmountValue * currentLevel;
        int timeIncrease = healTimeValue * currentLevel;
        int cost = coinCost * (currentLevel + 1);
        return $"<color=#0769f3>{botName}</color>\n" +
               $"Lv: <color=#0769f3>{currentLevel}</color>\n" +
               $"비용: <color=#0769f3>{cost}</color>\n\n" +
               $"M.I.N.G.O의 회복량이 <color=#0769f3>+{amount}</color>\n" +
               $"회복 지속 시간이 <color=#0769f3>+{timeIncrease}</color> 증가합니다.";
    }

    private string GetSupportBotGuardInfo(string botName, int guardPowerValue, int guardTimeValue, int currentLevel, int coinCost)
    {
        // Highlight only parameter values
        int power = guardPowerValue * currentLevel;
        int timeInc = guardTimeValue * currentLevel;
        int cost = coinCost * (currentLevel + 1);
        return $"<color=#0769f3>{botName}</color>\n" +
               $"Lv: <color=#0769f3>{currentLevel}</color>\n" +
               $"비용: <color=#0769f3>{cost}</color>\n\n" +
               $"M.I.N.G.O의 체력이 <color=#0769f3>+{power}</color>\n" +
               $"가드 지속 시간이 <color=#0769f3>+{timeInc}</color> 증가합니다.";
    }

    // 현재 선택된 업그레이드 정보를 자동으로 갱신하는 함수
    public void RefreshCurrentUpgradeInfo()
    {
        if (currentSelectedUpgrade.HasValue)
        {
            ShowUpgradeInfo(currentSelectedUpgrade.Value);
        }
    }

    // 강제로 레벨 텍스트를 업데이트하는 함수 (상점이 열릴 때 확실히 업데이트)
    public void ForceUpdateLevelTexts()
    {
        // shopManager 다시 찾기
        if (shopManager == null)
            shopManager = GetComponent<ShopManager>();

        // 즉시 업데이트
        UpdateLevelTexts();

        // 한 프레임 후에 다시 업데이트
        StartCoroutine(DelayedForceUpdate());
    }

    private System.Collections.IEnumerator DelayedForceUpdate()
    {
        yield return new WaitForEndOfFrame();
        UpdateLevelTexts();
    }

    // 정보 초기화
    public void ClearUpgradeInfo()
    {
        if (infoText != null)
        {
            infoText.text = "업그레이드 항목을 선택하세요";
        }
    }

    public void ShowWarningCoin()
    {
        StartCoroutine(WarningCoin());
    }

    private IEnumerator WarningCoin()
    {
        infoText.text = "코인이 부족합니다.";
        yield return new WaitForSecondsRealtime(0.5f);  // 상점 열였을 때, Time.timeScale이 0이므로 Realtime 사용
        infoText.text = "";
    }

    public void ShowWarningNoUpgrade()
    {
        StartCoroutine(WarningNoUpgrade());
    }

    private IEnumerator WarningNoUpgrade()
    {
        infoText.text = "더 이상 되돌릴 수 없습니다.";
        yield return new WaitForSecondsRealtime(0.5f);
        infoText.text = "";
    }

    public void ShowWarningMaxHeal()
    {
        StartCoroutine(WarningMaxHeal());
    }

    private IEnumerator WarningMaxHeal()
    {
        infoText.text = "체력이 모두 회복되었습니다.";
        yield return new WaitForSecondsRealtime(0.5f);
        infoText.text = "";
    }

    public void ShowWarningReturnMaxHP()
    {
        StartCoroutine(WarningReturnMaxHP());
    }

    private IEnumerator WarningReturnMaxHP()
    {
        infoText.text = "체력회복을 취소해야 되돌릴 수 있습니다.";
        yield return new WaitForSecondsRealtime(0.5f);
        infoText.text = "";
    }

    public void ShowWarningSameType()
    {
        StartCoroutine(WarningSameType());
    }

    private IEnumerator WarningSameType()
    {
        botTypeText.text = "이미 같은 타입입니다.";
        yield return new WaitForSecondsRealtime(0.5f);
        botTypeText.text = "";
    }

    // SupportBot 타입 변경 알림 함수
    public void ShowBotTypeChanged(int botType)
    {
        if (botTypeText == null) return;

        // 표시 시간을 2초로 초기화
        botTypeTextDisplayTime = 2.0f;

        string botTypeName = "";
        switch (botType)
        {
            case 0:
                botTypeName = "자동공격";
                break;
            case 1:
                botTypeName = "보호막";
                break;
            case 2:
                botTypeName = "회복";
                break;
            case 3:
                botTypeName = "급속 장전";
                break;
            case 4:
                botTypeName = "가드";
                break;
            default:
                botTypeName = "알 수 없음";
                break;
        }

        botTypeText.text = $"선택된 보조로봇 타입: {botTypeName}";

        // 코루틴을 사용해 설정된 시간 후 텍스트 초기화
        StartCoroutine(ClearBotTypeText());
    }

    private IEnumerator ClearBotTypeText()
    {
        yield return new WaitForSecondsRealtime(botTypeTextDisplayTime); // 전역변수 사용 (상점에서는 timeScale이 0이므로 Realtime 사용)
        if (botTypeText != null)
        {
            botTypeText.text = "";
        }
    }
}
