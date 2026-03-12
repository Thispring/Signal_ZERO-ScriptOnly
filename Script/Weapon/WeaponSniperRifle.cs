using UnityEngine;
using System.Collections;

/// <summary>
/// 게임에 존재하는 특수 돌격 소총의 '저격'(Sniper)사격에 대한 스크립트 입니다.
/// 모든 무기에 관련한 스크립트는 Weapon을 앞에 붙여줍니다.
/// WeaponBase를 상속받아 기본적인 무기 기능을 구현합니다.
/// </summary>
public class WeaponSniperRifle : WeaponBase
{
    [Header("References")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private PlayerHUD playerHUD;    // UI활성화를 위해 참조
    [SerializeField]    // Inspector에서 끌어서 사용
    private EMP emp;    // sniperRifle로 적에게 데미지를 입히면 EMP의 쿨타임이 감소되는 로직을 위한 참조

    [Header("Effects")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private GameObject flashEffect;  // 총구 이펙트

    [Header("Audio")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private AudioClip audioRapid;   // 연발 공격 사운드
    [SerializeField]    // Inspector에서 끌어서 사용
    private AudioClip audioClipReload;  // 재장전 사운드

    [Header("Values")]
    private float chargeProgress = 0f; // Sniper 차지 진행도 변수, HUD의 Slider UI에 사용

    private WeaponSwitchSystem switchSystem;

    void Awake()
    {
        base.Setup();   // WeaponBase의 Setup() 메소드 호출

        mainCamera = Camera.main; // 메인 카메라 캐싱
        audioSource = GetComponent<AudioSource>();  // AudioSource 가져오기

        weaponSetting.currentAmmo = weaponSetting.maxAmmo;  // 처음 탄 수는 최대로 설정
        SetCurrentDamage(70);  // WeaponBase의 SetCurrentDamage 함수를 통해 Sniper 타입 초기 데미지 설정, 괄호 안에 숫자가 초기 값

        switchSystem = GetComponent<WeaponSwitchSystem>();
    }

    void OnEnable()
    {
        shotTextEffect.SetActive(false);    // 사격 텍스트 이펙트 비활성화
        flashEffect.SetActive(false);   // 총구 이펙트 비활성화
        onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);   // 무기가 활성화될 때 해당 무기의 탄 수 정보를 갱신한다
    }

    void Update()
    {
        // 탄 수가 0이 되면 자동재장전
        if (weaponSetting.currentAmmo == 0)
        {
            StartReload();
            return;
        }

        // 차징 중이고, 차징 준비가 되지 않았으며, 공격 주기(Time.time - 이전 공격시간이 공격속도보다 클 때)가 지났을 때 발동
        if (isCharging && !isChargeReady && (Time.time - lastAttackTime > weaponSetting.attackRate))
        {
            // 차지 진행도 계산, 0.5초 동안 0에서 1로 증가
            chargeProgress = Mathf.Clamp01((Time.time - chargeStartTime) / 0.5f);
            // 차지 진행도에 따라 HUD 업데이트   
            playerHUD.UpdateSniperChargingHUD(chargeProgress, isChargeReady);
            // 차지 진행도에 맞춰 스코프 블러 업데이트
            if (!IsBurst) playerHUD.UpdateScopeBlurByCharge(chargeProgress);
            if (IsBurst) playerHUD.SetScopeBlurMax(); // 버스트 상태에서는 블러를 최대치로 고정
            // 차지 준비 완료 시간(0.5초)이 지났다면 차지 준비 완료 상태로 변경
            if (Time.time - chargeStartTime >= 0.5f)
            {
                isChargeReady = true; // 차지 준비 완료 상태로 변경하여, 차지 중지
                playerHUD.UpdateSniperChargingHUD(1f, isChargeReady); // 차지가 끝난 UI를 재갱신
                if (!IsBurst) playerHUD.UpdateScopeBlurByCharge(1f); // 블러도 최대치로
            }
        }
    }

    public override void StartWeaponAction(int type = 0)
    {
        // 재장전 중일 때는 무기 액션(=사격 등)을 할 수 없다
        if (isReload == true) return;

        // 다른 무기로 변경 시 바로 사격 중지
        if (switchSystem.weaponSwitchNumber != 2) return;

        // 만약 재장전 중일 때는 엄폐 상태가 해제되지 않음
        PlayerStatusManager playerStatusManager = PlayerStatusManager.Instance;
        playerStatusManager.isHiding = false; // 사격 시 엄폐 상태 해제

        // 버스트 상태일 때는 다른 공격으로 전환
        // 마우스 왼쪽 클릭 (공격 시작)
        if (type == 0 && !IsBurst)
        {
            if (!isCharging && (Time.time - lastAttackTime > weaponSetting.attackRate))
            {
                // Sniper는 반동 애니메이션 재생을 위해 isAiming을 따로 사용
                animator.SetAnimationBool("isAiming", true);
                // 처음 마우스를 눌렀을 때 차지 시작
                isCharging = true;
                isChargeReady = false;
                chargeStartTime = Time.time;
            }
        }
        else
        {
            OnAttack();
            ResetCharge();
        }
    }

    private void ResetCharge()  // 차지 초기화
    {
        // 버스트 상태에서는 DelayTime 코루틴을 실행하지 않음
        if (!IsBurst)
            StartCoroutine(DelayTime());    // 자연스러운 애니메이션을 위한 DelayTime 코루틴 호출
        // 차지 관련 변수 초기화
        isCharging = false;
        isChargeReady = false;
        chargeStartTime = 0f;
    }

    public override void StopWeaponAction(int type = 0)
    {
        // 마우스 왼쪽 클릭 (공격, 차지 게이지, HUD 초기화)
        if (type == 0 && chargeStartTime > 0)
        {
            OnAttack();
            ResetCharge();
            playerHUD.UpdateSniperChargingHUD(0, isChargeReady);
            // 버스트 중에는 블러 유지
            if (!PlayerStatusManager.Instance.isBurstActive)
                playerHUD.ResetScopeBlur();
        }
    }

    public override void StartReload()
    {
        weaponSetting.isReloading = true; // 재장전 상태 설정

        // 현재 차지중이면 재장전 불가능
        if (isCharging == true) return;
        // 현재 재장전 중이면 재장전 불가능
        if (isReload == true) return;
        // 탄약이 다 차있으면 재장전 불가능
        if (weaponSetting.currentAmmo == weaponSetting.maxAmmo) return;

        // 무기 액션 도중에 'R'키를 눌러 재장전을 시도하면 무기 액션 종료 후 재장전
        ResetCharge();  // 차지 초기화
        playerHUD.UpdateSniperChargingHUD(0, isChargeReady);  // 차지 HUD 초기화
        playerHUD.ResetScopeBlur();
        StopCoroutine("OnAttack");

        StartCoroutine("OnReload");
    }

    private IEnumerator OnReload()  // 재장전 코루틴
    {
        PlayerStatusManager playerStatusManager = PlayerStatusManager.Instance;
        playerStatusManager.isHiding = true; // 재장전 중에는 엄폐 상태 유지

        isReload = true;

        // 재장전 애니메이션, 사운드 재생
        animator.OnReload();
        PlaySound(audioClipReload);

        while (true)
        {
            // 사운드가 재생중이 아니면, 재장전 사운드 재생이 종료되었다는 뜻
            if (audioSource.isPlaying == false)
            {
                // 재장전 변수 초기화
                weaponSetting.isReloading = false; // 재장전 상태 해제                
                isReload = false;

                // 현재 탄 수를 최대로 설정하고, 바뀐 탄 수 정보를 Text UI에 업데이트
                weaponSetting.currentAmmo = weaponSetting.maxAmmo;
                onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

                yield break;
            }

            yield return null;
        }
    }

    public void OnAttack()
    {
        if (Time.time - lastAttackTime > weaponSetting.attackRate)
        {
            // 공격주기가 되어야 공격할 수 있도록 하기 위해 현재 시간 저장
            lastAttackTime = Time.time;

            // 탄 수가 없으면 공격 불가능
            if (weaponSetting.currentAmmo <= 0)
            {
                return;
            }
            // 공격 시 currentAmmo 1 감소
            weaponSetting.currentAmmo--;
            // 현재 탄수 정보를 갱신
            onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

            // 무기 애니메이션 재생
            animator.SetAnimationBool("isAimShot", true);

            // 이펙트 재생
            StartCoroutine("OnflashEffect");
            StartCoroutine("OnTextEffect");
            // 발사 사운드 재생
            PlaySound(audioRapid);

            // 광선을 발사해 원하는 위치 공격
            Vector3 mouseScreenPos = Input.mousePosition;
            if (Shoot(mouseScreenPos, out RaycastHit hit)) // Shoot 호출
            {
                //Debug.Log($"Hit at {hit.point}");
            }
        }
    }

    public override bool Shoot(Vector3 mouseScreenPos, out RaycastHit hit)
    {
        Ray ray = GetMouseRay(mouseScreenPos);

        float sphereRadius = 0.1f; // 판정 반경
        float maxDistance = weaponSetting.attackDistance;

        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan, 3f);

        // SphereCast로 가장 먼저 맞은 적 하나만 감지
        if (Physics.SphereCast(ray.origin, sphereRadius, ray.direction, out hit, maxDistance))
        {
            // tutorialEnemy와 enemy의 컴포넌트를 가져와 구분
            EnemyStatusManager enemy = hit.collider.GetComponent<EnemyStatusManager>();
            EnemyMissile enemyMissile = hit.collider.GetComponent<EnemyMissile>();
            BossMissile bossMissile = hit.collider.GetComponent<BossMissile>();
            BossStatusManager bossStatus = hit.collider.GetComponent<BossStatusManager>();
            BossDroneManager bossDroneManager = hit.collider.GetComponent<BossDroneManager>();

            // 모두 없으면 false 반환
            if (enemy == null && enemyMissile == null && bossMissile == null && bossStatus == null && bossDroneManager == null)
            {
                return false;
            }

            // 데미지는 설정된 sniperDamge에 chargeProgress의 진행도를 곱하여 계산
            float damage = weaponSetting.sniperDamage * (1 + chargeProgress);

            // 태그와 컴포넌트 일치 여부 확인 및 데미지 처리
            if (hit.collider.CompareTag("EnemySmall") && enemy != null)
            {
                enemy.TakeDamage(damage);
                //Debug.Log($"sniperRifle로 EnemySmall에게 데미지 적용 완료 {damage}");
            }
            else if (hit.collider.CompareTag("EnemyMedium") && enemy != null)
            {
                enemy.TakeDamage(damage);
                //Debug.Log($"sniperRifle로 EnemyMedium에게 데미지 적용 완료 {damage}");
            }
            else if (hit.collider.CompareTag("EnemyBig") && enemy != null)
            {
                damage += 200;  // Sniper -> Big 타입 추가 데미지
                enemy.criticalHit++; // 추가 코인을 위한 criticalHit 증가
                enemy.TakeDamage(damage);
                //Debug.Log($"sniperRifle로 EnemyBig에게 추가 데미지 적용 완료 {damage}");
            }
            else if (hit.collider.CompareTag("EnemyMissile") && enemyMissile != null)
            {
                enemyMissile.TakeDamage(damage);
                //Debug.Log($"sniperRifle로 EnemyMissile에게 데미지 적용 완료 {damage}");
            }
            else if (hit.collider.CompareTag("BossMissile") && bossMissile != null)
            {
                bossMissile.TakeDamage(damage);
                //Debug.Log($"sniperRifle로 BossMissile에게 데미지 적용 완료 {damage}");
            }
            else if (hit.collider.CompareTag("Boss") && bossStatus != null)
            {
                damage += 100;  // 보스에게 추가 데미지
                bossStatus.TakeDamage(damage);
                //Debug.Log($"sniperRifle로 Boss에게 데미지 적용 완료 {damage}");
            }
            else if (hit.collider.CompareTag("BossDrone") && bossDroneManager != null)
            {
                bossDroneManager.TakeDamage(damage);
            }
            else
            {
                // 태그와 컴포넌트가 일치하지 않음
                return false;
            }

            return true;
        }

        hit = default;
        return false;
    }

    private IEnumerator DelayTime()
    {
        // 애니메이션이 자연스럽게 재생되도록 잠시 대기
        yield return new WaitForSeconds(0.5f);
        animator.SetAnimationBool("isAiming", false);
        animator.SetAnimationBool("isAimShot", false);
    }

    private IEnumerator OnflashEffect()
    {
        flashEffect.SetActive(true);    // 총구 이펙트 오브젝트 활성화

        yield return new WaitForSeconds(weaponSetting.attackRate * 0.3f);   // 무기의 공격속도보다 빠르게 설정

        flashEffect.SetActive(false);   // 총구 이펙트 오브젝트 비활성화
    }

    private IEnumerator OnTextEffect()  // 사격 시 텍스트 이펙트 코루틴
    {
        shotTextEffect.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        shotTextEffect.SetActive(false);
    }

    /// 아래는 버스트 기능 함수로, 무기별로 다르게 구현합니다.
    /// Sniper Weapon의 Burst는 EMP 발동과 풀 차지 데미지 유지 입니다.
    /// 버스트 발동 시 Shield 타입을 바로 처치할 수 있게 1000 데미지를 주는 로직 추가
    public override void WeaponBurst()
    {
        weaponSetting.isBurst = true; // 버스트 기능 활성화
        if (!IsBurst) return;

        StartCoroutine(StartBurst()); // 버스트 시작 코루틴 호출
        BurstCutScene.Instance.PlayBurstCutScene(); // 버스트 컷씬 재생
    }

    private IEnumerator StartBurst()
    {
        BurstCutScene.Instance.ShowBurstActiveImage(true); // 버스트 활성화 이미지 표시
        chargeProgress = 1f;    // 버스트 상태에서는 차지 진행도를 1로 고정
        playerHUD.UpdateSniperChargingHUD(chargeProgress, isChargeReady); // 버스트 상태 UI 업데이트
                                                                          // 버스트 시작 시 블러 최대 고정(새 전용 함수)
        playerHUD.SetScopeBlurMax();
        emp.EMPAction((int)weaponSetting.sniperBurstTime); // EMP의 기절 시간을 버스트 타임으로 설정

        // 버스트 시작 시, Shield 타입(EnemyStatusManager + EnemyShieldManager 동시 보유)에게만 즉시 1000 피해 적용
        try
        {
            var enemies = FindObjectsByType<EnemyStatusManager>(FindObjectsSortMode.None);
            int affected = 0;
            foreach (var esm in enemies)
            {
                if (esm == null) continue;
                var shieldMgr = esm.GetComponent<EnemyShieldManager>();
                if (shieldMgr != null)
                {
                    esm.TakeDamage(1000f);
                    affected++;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"StartBurst damage apply failed: {ex.Message}");
        }

        int tempCurrentAmmo = weaponSetting.currentAmmo; // 현재 탄약 수를 임시로 저장
        int tempMaxAmmo = weaponSetting.maxAmmo; // 최대 탄약 수를 임시로 저장
        float tempDamage = weaponSetting.sniperDamage; // 현재 데미지를 임시로 저장

        weaponSetting.currentAmmo = weaponSetting.burstCurrentAmmo; // 현재 탄약 수를 버스트 상태의 현재 탄약 수로 설정
        weaponSetting.maxAmmo = weaponSetting.burstMaxAmmo; // 최대 탄약 수를 버스트 상태의 최대 탄약 수로 설정
        weaponSetting.sniperDamage += weaponSetting.sniperBurstDamage; // 현재 데미지를 버스트 상태의 데미지로 설정

        // 버스트 지속 시간, 1초마다 1씩 감소하며 isBurstActive가 해제되면 즉시 종료
        float t = weaponSetting.sniperBurstTime;
        float tick = 0f;
        while (PlayerStatusManager.Instance.isBurstActive)
        {
            tick += Time.deltaTime;
            // 매 프레임 블러 최대 유지 함수 호출
            playerHUD.SetScopeBlurMax();
            if (tick >= 1f)
            {
                t -= 1f;
                tick -= 1f;
                if (t <= 0f) break; // 0이 되자마자 종료
            }
            yield return null; // 프레임 단위 대기(중간에 플래그 변경을 즉시 반영)
        }

        chargeProgress = 0f; // 차지 진행도 초기화
        playerHUD.UpdateSniperChargingHUD(chargeProgress, isChargeReady); // 버스트 상태 UI 업데이트
        playerHUD.ResetBurstScopeBlur();

        weaponSetting.currentAmmo = tempCurrentAmmo; // 원래의 현재 탄약 수로 복원
        weaponSetting.maxAmmo = tempMaxAmmo; // 원래의 최대 탄약 수로 복원
        weaponSetting.sniperDamage = tempDamage; // 원래의 현재 데미지로 복원

        weaponSetting.isBurst = false; // 버스트 기능 비활성화

        BurstCutScene.Instance.ShowBurstActiveImage(false); // 버스트 활성화 이미지 비활성화
        PlayerStatusManager.Instance.isBurstActive = false; // 플레이어의 버스트 상태 비활성화
    }
}
