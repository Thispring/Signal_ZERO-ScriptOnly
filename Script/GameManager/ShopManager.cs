using UnityEngine;
using UnityEngine.UI;
using System; // Action 사용을 위해 추가

/// <summary>
/// 플레이어가 코인을 통해 능력치를 성장 할 수 있는 상점에 관한 스크립트입니다.
/// </summary>
///
/// <remarks> 
/// 각 메서드들은 버튼을 통해 연결하여 사용합니다.
/// 기능별로 region을 사용하여 관리합니다.
/// </remarks>
public class ShopManager : MonoBehaviour
{
    public event Action OnResumeButtonClicked; // 상점의 '계속하기' 버튼 클릭 시 호출될 이벤트

    #region  References 
    // 모든 References는 Inspector에서 끌어서 사용
    [Header("References")]
    [SerializeField]
    private StageManager stageManager; // 스테이지 매니저 참조
    [SerializeField]
    private PlayerStatusManager playerStatusManager; // 플레이어 상태 매니저 참조
    [SerializeField]
    private WeaponSingleRifle singleWeapon;
    [SerializeField]
    private WeaponRapidRifle rapidWeapon;
    [SerializeField]
    private WeaponSniperRifle sniperWeapon;
    [SerializeField]
    private ShopHUD shopHUD; // 상점 UI 참조
    #endregion

    #region Count Value
    [Header("Count")]   // 강화 버튼을 누른 횟수 카운트
    private int singlePowerUpCount = 1;
    private int rapidPowerUpCount = 1;
    private int sniperPowerUpCount = 1;
    private int healCount = 1;
    private int increaseMaxHPCount = 1;
    private int singleBurstPowerUpCount = 1;
    private int rapidBurstPowerUpCount = 1;
    private int sniperBurstPowerUpCount = 1;
    private int supportBotShieldUpgradeCount = 1;
    private int supportBotHealUpgradeCount = 1;
    private int supportBotGuardUpgradeCount = 1;
    #endregion

    #region Level Value
    [Header("Level")]    // 능력치 강화 레벨
    public int singlePowerUpLevel = 1;
    public int rapidPowerUpLevel = 1;
    public int sniperPowerUpLevel = 1;
    public int healLevel = 1;
    public int increaseMaxHPLevel = 1;
    public int singleBurstPowerUpLevel = 1;
    public int rapidBurstPowerUpLevel = 1;
    public int sniperBurstPowerUpLevel = 1;
    public int supportBotShieldUpgradeLevel = 1;
    public int supportBotHealUpgradeLevel = 1;
    public int supportBotGuardUpgradeLevel = 1;
    #endregion

    #region Power Up Value
    [Header("Power Up Value")]    // 강화 관련 수치
    // 무기 공격력 수치
    public int singlePowerUp = 4;
    public int rapidPowerUp = 2;
    public int sniperPowerUp = 10;
    // 버스트 시 무기 공격력 수치
    public int singleBurstPowerUp = 20;
    public int rapidBurstPowerUp = 5;
    public int sniperBurstPowerUp = 15;
    // 보조로봇 강화 수치
    public int guardBotPowerUp = 15; // SupportBot Guard 타입 업그레이드 수치 -> 체력 증가
    public int healBotPowerUp = 3; // SupportBot Heal 타입 업그레이드 수치 -> 회복량 증가
    #endregion

    #region Power Up Time Value
    [Header("Power Up Time Value")]    // 버스트 지속시간
    public int singleBurstTime = 1;
    public int rapidBurstTime = 1;
    public int sniperBurstTime = 1;
    // SupportBot 지속시간 업그레이드 수치
    public int guardBotTime = 1;
    public int shieldBotTime = 1;
    public int healBotTime = 1;
    #endregion

    #region Temp Level Value
    [Header("Temp Level")]  // 강화 되돌리기를 고려한 임시 레벨
    private int tempSinglePowerUpLevel = 1;
    private int tempSniperPowerUpLevel = 1;
    private int tempRapidPowerUpLevel = 1;
    private int tempHealLevel = 1;
    private int tempIncreaseMaxHPLevel = 1;
    private int tempSingleBurstPowerUpLevel = 1;
    private int tempRapidBurstPowerUpLevel = 1;
    private int tempSniperBurstPowerUpLevel = 1;
    private int tempSupportBotShieldUpgradeLevel = 1;
    private int tempSupportBotHealUpgradeLevel = 1;
    private int tempSupportBotGuardUpgradeLevel = 1;
    #endregion

    #region Temp Level Getters
    // Temp 레벨 값들을 외부에서 접근할 수 있는 Getter 메서드들
    public int GetTempSinglePowerUpLevel() { return tempSinglePowerUpLevel; }
    public int GetTempRapidPowerUpLevel() { return tempRapidPowerUpLevel; }
    public int GetTempSniperPowerUpLevel() { return tempSniperPowerUpLevel; }
    public int GetTempHealLevel() { return tempHealLevel; }
    public int GetTempIncreaseMaxHPLevel() { return tempIncreaseMaxHPLevel; }
    public int GetTempSingleBurstPowerUpLevel() { return tempSingleBurstPowerUpLevel; }
    public int GetTempRapidBurstPowerUpLevel() { return tempRapidBurstPowerUpLevel; }
    public int GetTempSniperBurstPowerUpLevel() { return tempSniperBurstPowerUpLevel; }
    public int GetTempSupportBotShieldUpgradeLevel() { return tempSupportBotShieldUpgradeLevel; }
    public int GetTempSupportBotHealUpgradeLevel() { return tempSupportBotHealUpgradeLevel; }
    public int GetTempSupportBotGuardUpgradeLevel() { return tempSupportBotGuardUpgradeLevel; }
    #endregion

    [Header("Coin")]    // 코인 관련 수치
    public int coinsToPay = 10; // 지불할 코인 수
    private int currrentCoin; // 현재 코인 수
    
    [Header("Health Calculation")] // 체력 계산 관련 전역 변수
    private int tempMaxHPIncreasePerLevel = 50; // increaseMaxHPCount당 최대체력 증가량
    private int actualHealAmount = 0;   // (최대 체력 - 현재 체력)을 1coin당 체력1을 회복하기 까지 필요한 변수

    [Header("Heal Application Control")] // 회복 적용 제어 변수
    private bool hasHealBeenPurchased = false; // Heal 버튼을 눌렀는지 여부 (최대체력 증가와 상관없이)

    // 모든 Sound 요소 Inspector에서 할당
    [Header("Sound")]
    [SerializeField]
    private AudioClip shopButtonClickClip;
    [SerializeField]
    private AudioClip warningButtonClickClip;
    [SerializeField]
    private AudioClip buttonClickClip;
    [SerializeField]
    private AudioSource buttonAudioSource;

    // UI 요소 Inspector에서 할당
    [Header("UI References")]
    [SerializeField]
    private GameObject shopCanvas; // 인스펙터에서 상점 캔버스 할당

    // 상점 인터랙션(버튼 클릭 등) 켜고 끄기, 튜토리얼에서 사용
    public void SetInteractable(bool interactable)
    {
        if (shopCanvas == null) return;

        // CanvasGroup이 없으면 하위 Button 컴포넌트들을 찾아 토글
        var buttons = shopCanvas.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            b.interactable = interactable;
        }

        // 추가로, Resume 버튼을 별도로 제어하고 싶으면 해당 버튼을 참조해 토글하세요.
    }

    void Awake()
    {
        sniperPowerUpLevel = 1;
        rapidPowerUpLevel = 1;
        singlePowerUpLevel = 1;

        shopHUD = GetComponent<ShopHUD>();
        if (shopHUD != null)
        {
            shopHUD.ClearUpgradeInfo(); // 초기 info 텍스트 설정
        }
    }

    // 현재 코인과 누적 구매 회복량을 ShopHUD 등에서 표시하기 위한 Getter
    public int GetCurrentCoin()
    {
        // PlayerStatusManager가 존재하면 그 값을 우선 사용
        return playerStatusManager != null ? playerStatusManager.coin : currrentCoin;
    }

    public int GetPurchasedHealAmount()
    {
        return actualHealAmount;
    }

    // 상점 기준의 현재 최대 체력 (임시 최대체력 증가분 포함)
    public int GetInShopMaxHP()
    {
        if (playerStatusManager == null) return 0;
        return playerStatusManager.MaxHP + (tempMaxHPIncreasePerLevel * (increaseMaxHPCount - 1));
    }

    // 현재 체력 Getter (UI 표시용)
    public int GetPlayerCurrentHP()
    {
        return playerStatusManager != null ? playerStatusManager.CurrentHP : 0;
    }

    // 이미 구매한 회복치(actualHealAmount)를 고려한 남은 필요 회복량
    public int GetRemainingNeededHeal()
    {
        if (playerStatusManager == null) return 0;
        int currentMaxHP = GetInShopMaxHP();
        int currentHP = playerStatusManager.CurrentHP;
        int needToFull = Mathf.Max(0, currentMaxHP - currentHP);
        int remainingNeeded = Mathf.Max(0, needToFull - actualHealAmount);
        return remainingNeeded;
    }

    public void saveCoin(int coin)
    {
        // coin은 이전 stage 코인
        currrentCoin = coin;
    }

    public void openShop()
    {
        tempSniperPowerUpLevel = sniperPowerUpLevel;
        tempRapidPowerUpLevel = rapidPowerUpLevel;
        tempSinglePowerUpLevel = singlePowerUpLevel;
        tempHealLevel = healLevel;
        tempIncreaseMaxHPLevel = increaseMaxHPLevel;
        tempSingleBurstPowerUpLevel = singleBurstPowerUpLevel;
        tempRapidBurstPowerUpLevel = rapidBurstPowerUpLevel;
        tempSniperBurstPowerUpLevel = sniperBurstPowerUpLevel;

        tempSupportBotShieldUpgradeLevel = supportBotShieldUpgradeLevel;
        tempSupportBotHealUpgradeLevel = supportBotHealUpgradeLevel;
        tempSupportBotGuardUpgradeLevel = supportBotGuardUpgradeLevel;

        // 상점 열 때 레벨 텍스트 강제 업데이트
        if (shopHUD != null)
        {
            shopHUD.ForceUpdateLevelTexts();
            shopHUD.ClearUpgradeInfo();
        }
    }

    // 튜토리얼에서 호출할 상점 열기 함수
    public void OpenShopForTutorial()
    {
        if (shopCanvas != null) shopCanvas.SetActive(true);
        Time.timeScale = 0; // 튜토리얼 중에는 시간을 직접 정지
        Cursor.visible = true;
        openShop(); // 기존의 상점 초기화 로직 재사용
    }

    // 튜토리얼 단계에서 상점이 열려있는지 확인할 수 있는 public 메서드
    public bool IsShopOpen()
    {
        return shopCanvas != null && shopCanvas.activeInHierarchy;
    }

    #region Weapon Upgrade
    public void SinglePowerUp()
    {
        int cost = coinsToPay * (tempSinglePowerUpLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            singlePowerUpCount++;
            tempSinglePowerUpLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void SinglePowerDown()
    {
        if (singlePowerUpCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempSinglePowerUpLevel;
            playerStatusManager.coin += refund;
            singlePowerUpCount--;
            tempSinglePowerUpLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }

    public void RapidPowerUp()
    {
        int cost = coinsToPay * (tempRapidPowerUpLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            rapidPowerUpCount++;
            tempRapidPowerUpLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void RapidPowerDown()
    {
        if (rapidPowerUpCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempRapidPowerUpLevel;
            playerStatusManager.coin += refund;
            rapidPowerUpCount--;
            tempRapidPowerUpLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }

    public void SniperPowerUp()
    {
        int cost = coinsToPay * (tempSniperPowerUpLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            sniperPowerUpCount++;
            tempSniperPowerUpLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void SniperPowerDown()
    {
        if (sniperPowerUpCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempSniperPowerUpLevel;
            playerStatusManager.coin += refund;
            sniperPowerUpCount--;
            tempSniperPowerUpLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }
    #endregion

    #region Burst Weapon Upgrade
    public void SingleBurstPowerUp()
    {
        int cost = coinsToPay * (tempSingleBurstPowerUpLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            singleBurstPowerUpCount++;
            tempSingleBurstPowerUpLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void SingleBurstPowerDown()
    {
        if (singleBurstPowerUpCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempSingleBurstPowerUpLevel;
            playerStatusManager.coin += refund;
            singleBurstPowerUpCount--;
            tempSingleBurstPowerUpLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }

    public void RapidBurstPowerUp()
    {
        int cost = coinsToPay * (tempRapidBurstPowerUpLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            rapidBurstPowerUpCount++;
            tempRapidBurstPowerUpLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void RapidBurstPowerDown()
    {
        if (rapidBurstPowerUpCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempRapidBurstPowerUpLevel;
            playerStatusManager.coin += refund;
            rapidBurstPowerUpCount--;
            tempRapidBurstPowerUpLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }

    public void SniperBurstPowerUp()
    {
        int cost = coinsToPay * (tempSniperBurstPowerUpLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            sniperBurstPowerUpCount++;
            tempSniperBurstPowerUpLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void SniperBurstPowerDown()
    {
        if (sniperBurstPowerUpCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempSniperBurstPowerUpLevel;
            playerStatusManager.coin += refund;
            sniperBurstPowerUpCount--;
            tempSniperBurstPowerUpLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }
    #endregion

    #region SupportBot Upgrade
    // Shield 타입 SupportBot 업그레이드
    public void SupportBotShieldUpgrade()
    {
        int cost = coinsToPay * (tempSupportBotShieldUpgradeLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            supportBotShieldUpgradeCount++;
            tempSupportBotShieldUpgradeLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void SupportBotShieldUpgradeDown()
    {
        if (supportBotShieldUpgradeCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempSupportBotShieldUpgradeLevel;
            playerStatusManager.coin += refund;
            supportBotShieldUpgradeCount--;
            tempSupportBotShieldUpgradeLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }

    // Heal 타입 SupportBot 업그레이드
    public void SupportBotHealUpgrade()
    {
        int cost = coinsToPay * (tempSupportBotHealUpgradeLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            supportBotHealUpgradeCount++;
            tempSupportBotHealUpgradeLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void SupportBotHealUpgradeDown()
    {
        if (supportBotHealUpgradeCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempSupportBotHealUpgradeLevel;
            playerStatusManager.coin += refund;
            supportBotHealUpgradeCount--;
            tempSupportBotHealUpgradeLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }

    // Guard 타입 SupportBot 업그레이드
    public void SupportBotGuardUpgrade()
    {
        int cost = coinsToPay * (tempSupportBotGuardUpgradeLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            supportBotGuardUpgradeCount++;
            tempSupportBotGuardUpgradeLevel++;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void SupportBotGuardUpgradeDown()
    {
        if (supportBotGuardUpgradeCount > 1)
        {
            PlayButtonClickSound();
            int refund = coinsToPay * tempSupportBotGuardUpgradeLevel;
            playerStatusManager.coin += refund;
            supportBotGuardUpgradeCount--;
            tempSupportBotGuardUpgradeLevel--;
            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }

    #endregion

    #region Health
    public void Heal()
    {
        // 현재 최대 체력 계산 (increaseMaxHPCount 고려)
        int currentMaxHP = playerStatusManager.MaxHP + (tempMaxHPIncreasePerLevel * (increaseMaxHPCount - 1));
        int currentHP = playerStatusManager.CurrentHP;

        // 이미 구매한 회복치(actualHealAmount)를 고려한 남은 필요 회복량 계산
        int needToFull = Mathf.Max(0, currentMaxHP - currentHP);
        int remainingNeeded = Mathf.Max(0, needToFull - actualHealAmount);

        // 더 이상 살 필요가 없으면 종료
        if (remainingNeeded <= 0)
        {
            PlayWarningButtonSound();
            shopHUD?.ShowWarningMaxHeal();
            return;
        }

        // 보유 코인 체크
        if (playerStatusManager.coin <= 0)
        {
            PlayWarningButtonSound();
            shopHUD?.ShowWarningCoin();
            return;
        }

        // 이번에 구매할 회복량(=지불 코인) 결정: 남은 필요량과 보유 코인 중 작은 값
        int purchase = Mathf.Min(remainingNeeded, playerStatusManager.coin);

        PlayShopButtonSound();

        // 코인 지불 및 누적 회복량 갱신
        playerStatusManager.coin -= purchase;
        actualHealAmount += purchase; // 누적 구매

        // UI 및 추적용 카운터 동기화
        healCount += purchase;
        tempHealLevel += purchase;

        // 플래그 갱신
        hasHealBeenPurchased = true;

        shopHUD?.UpdateLevelTexts();
        shopHUD?.RefreshCurrentUpgradeInfo();
    }

    public void HealDown()
    {
        // 누적 구매한 회복이 없다면 경고
        if (actualHealAmount <= 0 && healCount <= 1)
        {
            PlayWarningButtonSound();
            shopHUD?.ShowWarningNoUpgrade();
            return;
        }

        PlayButtonClickSound();

        // 구매한 회복 전액 환불
        int refund = actualHealAmount;
        if (refund > 0)
        {
            playerStatusManager.coin += refund;
        }

        // 상태 초기화
        actualHealAmount = 0;

        // UI 카운터 초기화 (레벨/카운트 시스템 유지 시)
        healCount = 1;
        tempHealLevel = 1;

        hasHealBeenPurchased = false;

        shopHUD?.UpdateLevelTexts();
        shopHUD?.RefreshCurrentUpgradeInfo();
    }

    public void IncreaseMaxHP()
    {
        int cost = coinsToPay * (tempIncreaseMaxHPLevel + 1);
        if (playerStatusManager.coin >= cost)
        {
            // 임시 변수를 활용한 회복 제한 체크
            int maxHPIncrease = 50;
            int healAmount = 50;
            int currentHP = playerStatusManager.CurrentHP;

            // 상점에서의 임시 최대 체력 계산
            int baseMaxHP = playerStatusManager.MaxHP; // 현재 실제 최대 체력
            int currentTempIncrease = (tempIncreaseMaxHPLevel - increaseMaxHPLevel) * 50; // 현재 임시 증가량
            int newTempIncrease = currentTempIncrease + maxHPIncrease; // 새로운 임시 증가량
            int newTempMaxHP = baseMaxHP + newTempIncrease; // 증가 후 임시 최대 체력

            // 회복 후 체력이 새로운 임시 최대 체력을 초과하는지 체크
            if (currentHP + healAmount > newTempMaxHP)
            {
                PlayWarningButtonSound();
                if (shopHUD != null)
                {
                    shopHUD.ShowWarningNoUpgrade(); // 회복 제한 경고 메시지
                }
                return;
            }

            PlayShopButtonSound();
            playerStatusManager.coin -= cost;
            increaseMaxHPCount++;
            tempIncreaseMaxHPLevel++;

            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningCoin();
        }
    }

    public void IncreaseMaxHPDown()
    {
        if (increaseMaxHPCount > 1)
        {
            // 되돌리기 전 회복 제한 체크 (가상 최종 체력 = 현재체력 + 구매한 회복)
            int simulatedFinalHP = playerStatusManager.CurrentHP + actualHealAmount;

            // 상점에서의 임시 최대 체력 계산 (되돌린 후)
            int baseMaxHP = playerStatusManager.MaxHP; // 현재 실제 최대 체력
            int newTempIncrease = (tempIncreaseMaxHPLevel - 1 - increaseMaxHPLevel) * 50; // 되돌린 후 임시 증가량
            int newTempMaxHP = baseMaxHP + newTempIncrease; // 되돌린 후 임시 최대 체력

            // 가상 최종 체력이 되돌린 후 임시 최대 체력을 초과하는지 체크
            if (simulatedFinalHP > newTempMaxHP)
            {
                PlayWarningButtonSound();
                if (shopHUD != null)
                {
                    shopHUD.ShowWarningReturnMaxHP(); // 되돌리기 제한 경고 메시지
                }
                return;
            }

            PlayButtonClickSound();
            int refund = coinsToPay * tempIncreaseMaxHPLevel;
            playerStatusManager.coin += refund;
            increaseMaxHPCount--;
            tempIncreaseMaxHPLevel--;

            if (shopHUD != null)
            {
                shopHUD.UpdateLevelTexts();
                shopHUD.RefreshCurrentUpgradeInfo(); // 현재 선택된 업그레이드 정보 갱신
            }
        }
        else
        {
            PlayWarningButtonSound();
            shopHUD.ShowWarningNoUpgrade();
        }
    }
    #endregion

    #region Sound
    // 버튼 클릭 사운드 재생 함수
    private void PlayShopButtonSound()
    {
        if (shopButtonClickClip != null && buttonAudioSource != null)
        {
            buttonAudioSource.PlayOneShot(shopButtonClickClip);
        }
    }

    private void PlayWarningButtonSound()
    {
        if (warningButtonClickClip != null && buttonAudioSource != null)
        {
            buttonAudioSource.PlayOneShot(warningButtonClickClip);
        }
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickClip != null && buttonAudioSource != null)
        {
            buttonAudioSource.PlayOneShot(buttonClickClip);
        }
    }
    #endregion

    // 보조로봇 타입 선택 버튼
    // SupportBot Setting의 Type enum 데이터를 가져와, 버튼의 매개변수로 전달
    // Attack(0), Heal(1), Shield(2), QuickReload(3), Guard(4)
    public void SelectSupportBot(int botType)
    {
        // SupportBotStatusManager가 null인지 확인
        if (SupportBotStatusManager.Instance?.setting == null)
        {
            Debug.LogError("SupportBotStatusManager가 초기화되지 않았습니다!");
            PlayWarningButtonSound();
            return;
        }

        // 유효한 botType 범위 확인 (0: Attack, 1: Heal, 2: Shield, 3: QuickReload, 4: Guard)
        if (botType < 0 || botType > 4)
        {
            Debug.LogError($"유효하지 않은 SupportBot 타입: {botType}. 0(Attack), 1(Heal), 2(Shield), 3(QuickReload), 4(Guard)만 지원됩니다.");
            PlayWarningButtonSound();
            return;
        }

        // int를 enum으로 변환
        SupportBotType selectedType = (SupportBotType)botType;
        SupportBotType currentType = SupportBotStatusManager.Instance.setting.botType;

        // 현재 타입과 선택한 타입이 같은지 확인
        if (selectedType == currentType)
        {
            // 같은 타입이면 경고 디버그 실행
            PlayWarningButtonSound();

            if (shopHUD != null)
            {
                shopHUD.ShowWarningSameType(); // 같은 타입 경고 함수로 변경
            }
        }
        else
        {
            // SupportBot 타입 변경
            SupportBotStatusManager.Instance.setting.botType = selectedType;

            PlayButtonClickSound();

            if (shopHUD != null)
            {
                shopHUD.RefreshCurrentUpgradeInfo();
                shopHUD.ShowBotTypeChanged(botType); // 타입 변경 알림
            }
        }
    }

    // botType enum을 한글 이름으로 변환하는 헬퍼 함수
    private string GetBotTypeName(SupportBotType botType)
    {
        return botType switch
        {
            SupportBotType.Heal => "회복",
            SupportBotType.Shield => "보호막",
            SupportBotType.Guard => "가드",
            _ => "알 수 없음"
        };
    }

    public void ResumeGame()
    {
        if (shopCanvas != null) shopCanvas.SetActive(false);

        // '계속하기' 버튼이 눌렸음을 이벤트를 통해 알립니다.
        OnResumeButtonClicked?.Invoke();

        // 무기 업그레이드 적용
        for (int i = 2; i <= singlePowerUpCount; i++)
        {
            singleWeapon.IncreaseDamage(singlePowerUp);
        }

        for (int i = 2; i <= rapidPowerUpCount; i++)
        {
            rapidWeapon.IncreaseDamage(rapidPowerUp);
        }

        for (int i = 2; i <= sniperPowerUpCount; i++)
        {
            sniperWeapon.IncreaseDamage(sniperPowerUp);
        }

        // 버스트 업그레이드 적용
        for (int i = 2; i <= singleBurstPowerUpCount; i++)
        {
            singleWeapon.IncreaseBurstDamage(singleBurstPowerUp);
            singleWeapon.IncreaseBurstTime(singleBurstTime);
        }

        for (int i = 2; i <= rapidBurstPowerUpCount; i++)
        {
            rapidWeapon.IncreaseBurstDamage(rapidBurstPowerUp);
            rapidWeapon.IncreaseBurstTime(rapidBurstTime);
        }

        for (int i = 2; i <= sniperBurstPowerUpCount; i++)
        {
            sniperWeapon.IncreaseBurstDamage(sniperBurstPowerUp);
            sniperWeapon.IncreaseBurstTime(sniperBurstTime);
        }

        // 체력 관련 업그레이드 적용 (새로운 환불 로직 반영)
        // increaseMaxHPCount - 1 방식으로 통일
        int totalMaxHPIncrease = (increaseMaxHPCount - 1) * 50;
        if (totalMaxHPIncrease > 0)
        {
            PlayerStatusManager.Instance.IncreaseMaxHP(totalMaxHPIncrease, increaseMaxHPCount);
        }

        // actualHealAmount를 사용한 회복 적용 (bool 변수로 제어)
        if (hasHealBeenPurchased && actualHealAmount > 0)
        {
            // 최종 최대체력 계산: 실제 적용 후의 현재 MaxHP 사용
            int finalMaxHP = PlayerStatusManager.Instance.MaxHP;
            int currentHP = PlayerStatusManager.Instance.CurrentHP;

            // 회복 후 최대체력을 초과하지 않도록 제한
            int safeHealAmount = Mathf.Min(actualHealAmount, finalMaxHP - currentHP);

            if (safeHealAmount > 0)
            {
                PlayerStatusManager.Instance.Heal(safeHealAmount);
            }
            else
            {
                //Debug.Log($"ResumeGame: 이미 최대체력에 도달하여 회복 불필요 (현재: {currentHP}, 최대: {finalMaxHP})");
            }
        }
        else
        {
            //Debug.Log($"ResumeGame: Heal 버튼을 누르지 않았거나 actualHealAmount가 0이므로 회복 적용 안함 (hasHealBeenPurchased: {hasHealBeenPurchased}, actualHealAmount: {actualHealAmount})");
        }

        // SupportBot 업그레이드 적용 (개별 함수 사용)
        if (SupportBotStatusManager.Instance != null)
        {
            // Shield 타입 SupportBot 업그레이드
            for (int i = 2; i <= supportBotShieldUpgradeCount; i++)
            {
                SupportBotStatusManager.Instance.UpgradeShieldTime(shieldBotTime);
            }

            // Heal 타입 SupportBot 업그레이드
            for (int i = 2; i <= supportBotHealUpgradeCount; i++)
            {
                SupportBotStatusManager.Instance.UpgradeHealAmount(healBotPowerUp, healBotTime);
            }

            // Guard 타입 SupportBot 업그레이드
            for (int i = 2; i <= supportBotGuardUpgradeCount; i++)
            {
                SupportBotStatusManager.Instance.UpgradeGuard(guardBotPowerUp, guardBotTime);
            }
        }

        // 임시 레벨을 실제 레벨에 반영
        sniperPowerUpLevel = tempSniperPowerUpLevel;
        rapidPowerUpLevel = tempRapidPowerUpLevel;
        singlePowerUpLevel = tempSinglePowerUpLevel;
        healLevel = tempHealLevel;
        increaseMaxHPLevel = tempIncreaseMaxHPLevel;
        singleBurstPowerUpLevel = tempSingleBurstPowerUpLevel;
        rapidBurstPowerUpLevel = tempRapidBurstPowerUpLevel;
        sniperBurstPowerUpLevel = tempSniperBurstPowerUpLevel;

        supportBotShieldUpgradeLevel = tempSupportBotShieldUpgradeLevel;
        supportBotHealUpgradeLevel = tempSupportBotHealUpgradeLevel;
        supportBotGuardUpgradeLevel = tempSupportBotGuardUpgradeLevel;

        // 카운트 초기화
        singlePowerUpCount = 1;
        rapidPowerUpCount = 1;
        sniperPowerUpCount = 1;
        healCount = 1;
        increaseMaxHPCount = 1;
        singleBurstPowerUpCount = 1;
        rapidBurstPowerUpCount = 1;
        sniperBurstPowerUpCount = 1;

        supportBotShieldUpgradeCount = 1;
        supportBotHealUpgradeCount = 1;
        supportBotGuardUpgradeCount = 1;

        // Heal 버튼 사용 여부 초기화
        hasHealBeenPurchased = false; // Heal 버튼 사용 여부 초기화
        actualHealAmount = 0; // actualHealAmount 초기화

        // 상점 닫을 때 게임 재개
        PauseManager.RemovePause();
        if (stageManager.stageNumber == 0)
        {
            // 튜토리얼 스테이지에서만 발동
        }
        else
        {
            // 일반 스테이지에서 발동
            stageManager.stageNumber += 1;
        }

        stageManager.ResumeGame();
    }
}
