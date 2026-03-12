using UnityEngine;
using System.Collections;

/// <summary>
/// 게임에 존재하는 특수 돌격 소총의 '단발'(Single)사격에 대한 스크립트 입니다.
/// 모든 무기에 관련한 스크립트는 Weapon을 앞에 붙여줍니다.
/// WeaponBase를 상속받아 기본적인 무기 기능을 구현합니다.
/// </summary>
public class WeaponSingleRifle : WeaponBase
{
    [Header("Effects")]
    [SerializeField]
    private GameObject flashEffect;  // 총구 이펙트

    [Header("Audio")]
    [SerializeField]
    private AudioClip audioRapid;   // 단발 공격 사운드
    [SerializeField]
    private AudioClip audioClipReload;  // 재장전 사운드

    private WeaponSwitchSystem switchSystem;

    void Awake()
    {
        base.Setup();   // WeaponBase의 Setup() 메소드 호출

        mainCamera = Camera.main; // 메인 카메라 캐싱
        audioSource = GetComponent<AudioSource>();  // AudioSource 가져오기

        weaponSetting.currentAmmo = weaponSetting.maxAmmo;  // 처음 탄 수는 최대로 설정
        SetCurrentDamage(15);  // WeaponBase의 SetCurrentDamage 함수를 통해 Single 타입 초기 데미지 설정, 괄호 안에 숫자가 초기 값

        // Burst 공격을 위한 Rocket 풀 초기화 (초기 10개 생성)
        if (rocketPrefab != null)
        {
            rocketPool = new MemoryPool(rocketPrefab);
            // 기본 생성 5개 + 추가 5개 = 10개
            rocketPool.InstantiateObjects();
        }
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
        if (switchSystem.weaponSwitchNumber != 0) return;

        // 버스트 상태일 때는 다른 공격으로 전환
        // 마우스 왼쪽 클릭 (공격 시작)
        if (type == 0 && !IsBurst)
        {
            // 연속 공격이 설정되어 있으면, OnAttackLoop 코루틴을 시작
            if (weaponSetting.isAutomaticAttack == true)
            {
                StartCoroutine("OnAttackLoop");
            }
        }
        else if (type == 0 && IsBurst)
        {
            // 버스트 상태에서 공격을 시작하면, 버스트 공격을 실행
            SingleBurstRocket(Input.mousePosition);
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
                if (Input.GetMouseButton(0) && weaponSetting.weaponName == WeaponName.Single)
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
        // 탄 수가 0이 되면 자동재장전
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
            // 현재 탄수 정보를 갱신
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

        float damage = weaponSetting.singleDamage; // 기본 데미지

        // 태그와 컴포넌트 일치 여부 확인 및 데미지 처리
        if (hit.collider.CompareTag("EnemySmall") && enemy != null)
        {
            enemy.TakeDamage(damage);
            //Debug.Log("singleRifle로 EnemySmall에게 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("EnemyMedium") && enemy != null)
        {
            damage += 15; // Single -> Medium 타입 추가 데미지
            enemy.criticalHit++; // 추가 코인을 위한 criticalHit 증가
            enemy.TakeDamage(damage);
            //Debug.Log("singleRifle로 EnemyMedium에게 추가 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("EnemyBig") && enemy != null)
        {
            enemy.TakeDamage(damage);
            //Debug.Log("singleRifle로 EnemyBig에게 적 명중! 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("EnemyMissile") && enemyMissile != null)
        {
            enemyMissile.TakeDamage(damage);
            //Debug.Log("singleRifle로 EnemyMissile에게 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("BossMissile") && bossMissile != null)
        {
            bossMissile.TakeDamage(damage);
            //Debug.Log("singleRifle로 BossMissile에게 데미지 적용 완료");
        }
        else if (hit.collider.CompareTag("Boss") && bossStatus != null)
        {
            bossStatus.TakeDamage(damage);
            //Debug.Log("singleRifle로 Boss에게 데미지 적용 완료");
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
    /// Single Weapon의 Burst는 로켓 발사 기능입니다.
    [Header("Burst Variables")]
    [SerializeField]
    private GameObject rocketPrefab; // single 버스트 발사체 프리팹
    [SerializeField]
    private Transform rocketSpawnPoint; // 로켓 발사 위치
    private float rocketFireInterval = 1f; // 로켓 발사 간격(초)
    private float rocketFireCooldown = 0f;   // 남은 쿨다운 시간
    private Coroutine rocketCooldownRoutine; // 쿨다운 코루틴 핸들
    private MemoryPool rocketPool; // 로켓 풀

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
        int tempCurrentAmmo = weaponSetting.currentAmmo;
        int tempMaxAmmo = weaponSetting.maxAmmo;
        float tempDamage = weaponSetting.singleDamage;

        weaponSetting.currentAmmo = weaponSetting.burstCurrentAmmo;
        weaponSetting.maxAmmo = weaponSetting.burstMaxAmmo;
        weaponSetting.singleDamage += weaponSetting.singleBurstDamage;

        float t = weaponSetting.singleBurstTime;
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
            yield return null; // 프레임 단위 대기(중간에 플래그 변경을 즉시 반영)
        }
        weaponSetting.currentAmmo = tempCurrentAmmo;
        weaponSetting.maxAmmo = tempMaxAmmo;
        weaponSetting.singleDamage = tempDamage;

        weaponSetting.isBurst = false;
        BurstCutScene.Instance.ShowBurstActiveImage(false); // 버스트 활성화 이미지 비활성화
        PlayerStatusManager.Instance.isBurstActive = false;
    }

    private void SingleBurstRocket(Vector3 mouseScreenPos)
    {
        // 쿨다운 중이면 발사 불가
        if (rocketFireCooldown > 0f)
        {
            return;
        }

        // 마우스 위치를 월드 좌표로 변환
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
        Vector3 targetPosition;

        targetPosition = ray.GetPoint(100f); // Ray의 100 유닛 앞 지점을 목표로 설정

        // 로켓의 방향(및 목표 지점) 설정
        Vector3 direction = (targetPosition - rocketSpawnPoint.position).normalized;

        if (rocketPrefab != null && rocketPool != null)
        {
            // 풀에서 로켓 꺼내기
            GameObject rocketObj = rocketPool.ActivatePoolItem();
            if (rocketObj == null)
            {
                // 안전장치: 풀에서 못 꺼내면 즉시 생성
                rocketObj = Instantiate(rocketPrefab);
            }
            rocketObj.transform.SetPositionAndRotation(rocketSpawnPoint.position, Quaternion.LookRotation(direction));
            Rocket rocket = rocketObj.GetComponent<Rocket>();
            if (rocket != null)
            {
                // 풀 참조 전달
                rocket.SetOwnerPool(rocketPool);
                // targetPosition을 직접 전달하여 해당 지점을 향하도록 설정
                rocket.SetupToTarget(targetPosition, weaponSetting.singleDamage);
            }
            else
            {
                //Debug.LogError("Rocket 컴포넌트를 찾을 수 없습니다. rocketPrefab에 Rocket 스크립트를 추가하세요.");
            }
            animator.SetAnimationBool("isShot", true);
            // 첫 발사 후 쿨다운 시작
            StartRocketCooldown();
        }
        else
        {
            //Debug.LogError("rocketPrefab이 설정되지 않았습니다. Inspector에서 rocketPrefab을 확인하세요.");
        }
    }

    // 로켓 발사 쿨다운 시작(0 이하가 될 때까지 반복 감소)
    private void StartRocketCooldown()
    {
        if (rocketCooldownRoutine != null)
            StopCoroutine(rocketCooldownRoutine);
        rocketCooldownRoutine = StartCoroutine(RocketCooldownRoutine());
        animator.SetAnimationBool("isShot", false);
    }

    private IEnumerator RocketCooldownRoutine()
    {
        rocketFireCooldown = rocketFireInterval;
        while (rocketFireCooldown > 0f)
        {
            rocketFireCooldown -= Time.deltaTime;
            yield return null;
        }
        rocketFireCooldown = 0f;
        rocketCooldownRoutine = null;
    }
}
