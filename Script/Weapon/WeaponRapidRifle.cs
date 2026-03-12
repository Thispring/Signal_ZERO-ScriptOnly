using UnityEngine;
using System.Collections;

/// <summary>
/// 게임에 존재하는 특수 돌격 소총의 '연발'(Rapid)사격에 대한 스크립트 입니다.
/// 모든 무기에 관련한 스크립트는 Weapon을 앞에 붙여줍니다.
/// WeaponBase를 상속받아 기본적인 무기 기능을 구현합니다.
/// </summary>
public class WeaponRapidRifle : WeaponBase
{
    [Header("Effects")]
    [SerializeField]
    private GameObject flashEffect;  // 총구 이펙트

    [Header("Audio")]
    [SerializeField]
    private AudioClip audioRapid;   // 연발 공격 사운드
    [SerializeField]
    private AudioClip audioClipReload;  // 재장전 사운드

    private WeaponSwitchSystem switchSystem;

    void Awake()
    {
        base.Setup();   // WeaponBase의 Setup() 메소드 호출

        mainCamera = Camera.main; // 메인 카메라 캐싱
        audioSource = GetComponent<AudioSource>();  // AudioSource 가져오기

        weaponSetting.currentAmmo = weaponSetting.maxAmmo;  // 처음 탄 수는 최대로 설정
        // WeaponBase의 SetCurrentDamage 함수를 통해 Rapid 타입 초기 데미지 설정, 괄호 안에 숫자가 초기 값
        SetCurrentDamage(8);

        switchSystem = GetComponent<WeaponSwitchSystem>();
    }

    void OnEnable()
    {
        shotTextEffect.SetActive(false);    // 사격 텍스트 이펙트 비활성화
        flashEffect.SetActive(false);   // 총구 이펙트 비활성화
        onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);   // 무기가 활성화될 때 해당 무기의 탄 수 정보를 갱신한다
    }

    public override void StartWeaponAction(int type = 0)
    {
        // 재장전 중일 때는 무기 액션(=사격 등)을 할 수 없다
        if (isReload == true) return;

        // 다른 무기로 변경 시 바로 사격 중지
        if (switchSystem.weaponSwitchNumber != 1) return;
        // 마우스 왼쪽 클릭 (공격 시작)
        if (type == 0)
        {
            // 연속 공격이 설정되어 있으면, OnAttackLoop 코루틴을 시작
            if (weaponSetting.isAutomaticAttack == true)
            {
                StartCoroutine("OnAttackLoop");
            }
        }
        else
        {
            OnAttack();
        }
    }

    public override void StopWeaponAction(int type = 0)
    {
        // 마우스 왼쪽 클릭 (공격 종료)
        if (type == 0)
        {
            if (animator != null)
            {
                animator.SetAnimationBool("isShot", false);
            }
            else
            {
                Debug.LogWarning("Animator가 null 상태입니다.");
            }

            StopCoroutine("OnAttackLoop");
        }
    }

    public override void StartReload()
    {
        // 현재 재장전 중이면 재장전 불가능
        if (isReload == true) return;
        // 탄약이 다 차있으면 재장전 불가능
        if (weaponSetting.currentAmmo == weaponSetting.maxAmmo) return;
        // 무기 액션 도중에 'R'키를 눌러 재장전을 시도하면 무기 액션 종료 후 재장전
        StopWeaponAction();

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
                isReload = false;

                // 현재 탄 수를 최대로 설정하고, 바뀐 탄 수 정보를 Text UI에 업데이트
                weaponSetting.currentAmmo = weaponSetting.maxAmmo;
                onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

                // 재장전 완료 후 마우스가 눌려있으면 즉시 발사
                if (Input.GetMouseButton(0) && weaponSetting.weaponName == WeaponName.Rapid)
                {
                    playerStatusManager.isHiding = false;   // 재사격 시 엄폐 상태 해제 
                    StartCoroutine(DelayedAttack()); // 한 프레임 지연으로 중복 방지
                }
                yield break;
            }

            yield return null;
        }
    }

    // 한 프레임 지연 후 공격 (중복 방지)
    private IEnumerator DelayedAttack()
    {
        yield return null; // 다음 프레임까지 대기
        if (Input.GetMouseButton(0))
        {
            StartWeaponAction();
        }
    }

    private IEnumerator OnAttackLoop()  // 연발 설정 시 공격 코루틴
    {
        while (true)
        {
            OnAttack();

            yield return new WaitForSeconds(weaponSetting.attackRate); // 공격 속도에 맞춰 실행
        }
    }

    public void OnAttack()
    {
        // 탄약이 0이 되면 자동 재장전
        if (weaponSetting.currentAmmo == 0)
        {
            StartReload();
            return;
        }

        if (Time.time - lastAttackTime > weaponSetting.attackRate)
        {
            // 공격주기가 되어야 공격할 수 있도록 하기 위해 현재 시간 저장
            lastAttackTime = Time.deltaTime;

            // 탄 수가 없으면 공격 불가능
            if (weaponSetting.currentAmmo <= 0)
            {
                return;
            }
            // 공격 시 currentAmmo 1 감소
            weaponSetting.currentAmmo--;
            onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);
            // 무기 애니메이션 재생
            animator.SetAnimationBool("isShot", true);
            // 총구 이펙트 재생
            StartCoroutine("OnflashEffect");
            StartCoroutine("OnTextEffect");
            PlaySound(audioRapid);

            // 광선을 발사해 원하는 위치 공격
            Vector3 mouseScreenPos = Input.mousePosition;
            if (Shoot(mouseScreenPos, out RaycastHit hit)) // Shoot 호출
            {
                //Debug.Log($"Hit at {hit.point}");
            }
        }
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

    public override bool Shoot(Vector3 mouseScreenPos, out RaycastHit hit)
    {
        if (!base.Shoot(mouseScreenPos, out hit)) // WeaponBase의 Shoot 호출
        {
            return false;
        }

        // 적중 처리
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

        float damage = weaponSetting.rapidDamage; // 기본 데미지

        // 태그와 컴포넌트 일치 여부 확인 및 데미지 처리
        if (hit.collider.CompareTag("EnemySmall") && enemy != null)
        {
            damage += 5f; // Rapid -> Small 타입 추가 데미지
            enemy.criticalHit++; // 추가 코인을 위한 criticalHit 증가
            enemy.TakeDamage(damage);
            //Debug.Log("rapidRifle로 EnemySmall에게 추가 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("EnemyMedium") && enemy != null)
        {
            enemy.TakeDamage(damage);
            //Debug.Log("rapidRifle로 EnemyMedium에게 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("EnemyBig") && enemy != null)
        {
            enemy.TakeDamage(damage);
            //Debug.Log("rapidRifle로 EnemyBig에게 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("EnemyMissile") && enemyMissile != null)
        {
            enemyMissile.TakeDamage(damage);
            //Debug.Log("rapidRifle로 EnemyMissile에게 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("BossMissile") && bossMissile != null)
        {
            bossMissile.TakeDamage(damage);
            //Debug.Log("rapidRifle로 BossMissile에게 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("Boss") && bossStatus != null)
        {
            bossStatus.TakeDamage(damage);
            //Debug.Log("rapidRifle로 Boss에게 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("BossDrone") && bossDroneManager != null)
        {
            bossDroneManager.TakeDamage(damage);
        }
        else
        {
            //Debug.Log("태그와 컴포넌트가 일치하지 않거나 알 수 없는 적");
            return false;
        }

        return true;
    }

    /// 아래는 버스트 기능 함수로, 무기별로 다르게 구현합니다.
    /// Rapid 버스트는 burstTime동안 탄약을 무제한으로 설정합니다.
    /// 무제한 탄약을 표현하기 위해 Inspector에서 burst Ammo 변수를 999로 설정합니다.
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
        int tempCurrentAmmo = weaponSetting.currentAmmo; // 현재 탄약 수를 임시로 저장
        int tempMaxAmmo = weaponSetting.maxAmmo; // 최대 탄약 수를 임시로 저장
        float tempDamage = weaponSetting.rapidDamage; // 현재 데미지를 임시로 저장

        weaponSetting.currentAmmo = weaponSetting.burstCurrentAmmo; // 현재 탄약 수를 버스트 상태의 현재 탄약 수로 설정
        weaponSetting.maxAmmo = weaponSetting.burstMaxAmmo; // 최대 탄약 수를 버스트 상태의 최대 탄약 수로 설정
        weaponSetting.rapidDamage += weaponSetting.rapidBurstDamage; // 현재 데미지를 버스트 상태의 데미지로 설정

        // 버스트 지속 시간: 1초마다 1씩 감소하며 isBurstActive가 해제되면 즉시 종료
        float t = weaponSetting.rapidBurstTime;
        float tick = 0f;
        while (PlayerStatusManager.Instance.isBurstActive)
        {
            tick += Time.deltaTime;
            if (tick >= 1f)
            {
                t -= 1f;
                tick -= 1f;
                if (t <= 0f) break; // 0이 되자마자 종료
            }
            yield return null; // 프레임 단위 대기
        }

        weaponSetting.currentAmmo = tempCurrentAmmo; // 원래의 현재 탄약 수로 복원
        weaponSetting.maxAmmo = tempMaxAmmo; // 원래의 최대 탄약 수로 복원
        weaponSetting.rapidDamage = tempDamage; // 원래의 현재 데미지로 복원

        weaponSetting.isBurst = false; // 버스트 기능 비활성화
        BurstCutScene.Instance.ShowBurstActiveImage(false); // 버스트 활성화 이미지 비활성화
        PlayerStatusManager.Instance.isBurstActive = false; // 플레이어의 버스트 상태 비활성화
    }
}
