using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy의 구체적인 행동 로직을 관리하는 스크립트입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
public class EnemyBehaviorSystem : MonoBehaviour
{
    [Header("References")]
    private EnemyFSM enemyFSM;  // EnemyFSM 참조
    private EnemyStatusManager status; // Enemy 상태 관리
    private EnemyAudioController audioController; // Enemy 오디오 컨트롤러
    private EnemyAnimatorController animatorController; // Enemy 애니메이터 컨트롤러
    private EnemyEffectController effectController; // Enemy 이펙트 컨트롤러

    [Header("Enemy Weapon Pools")]
    // RapidFire, Missile 타입용 projectile pool
    private MemoryPool projectileMemoryPool;
    private MemoryPool missileMemoryPool; // Missile 전용 pool
    // RapidFire 전용 projectile 배열
    private GameObject[] rapidFireProjectiles = new GameObject[4];

    void Awake()
    {
        effectController = GetComponent<EnemyEffectController>();
        animatorController = GetComponent<EnemyAnimatorController>();
        enemyFSM = GetComponent<EnemyFSM>();
        status = GetComponent<EnemyStatusManager>();
        audioController = GetComponent<EnemyAudioController>();

        // RapidFire Pool 초기화
        if (status.setting.enemyType == EnemyType.RapidFire)
        {
            if (enemyFSM.projectile == null)
            {
                Debug.LogError("EnemyBehaviorSystem: RapidFire용 projectile(prefab)이 설정되지 않았습니다.");
                return;
            }

            projectileMemoryPool = new MemoryPool(enemyFSM.projectile);
            projectileMemoryPool.SetAllPoolObjectsParent(this.transform);

            // RapidFire 타입은 발사체를 미리 4개 생성하여 배열에 저장
            for (int i = 0; i < rapidFireProjectiles.Length; i++)
            {
                GameObject projectile = projectileMemoryPool.ActivatePoolItem();
                if (projectile == null) continue;

                var projComponent = projectile.GetComponent<EnemyProjectile>();
                if (projComponent != null)
                {
                    projComponent.SetOwner(enemyFSM);
                    projComponent.SetPool(projectileMemoryPool); // 풀 참조 설정
                }

                projectile.SetActive(false); // 비활성화 상태로 보관
                rapidFireProjectiles[i] = projectile;
            }
        }
        // Missile Pool 초기화
        else if (status.setting.enemyType == EnemyType.Missile)
        {
            if (enemyFSM.enemyMissilePrefab == null)
            {
                Debug.LogError("EnemyBehaviorSystem: Missile용 enemyMissilePrefab이 설정되지 않았습니다.");
                return;
            }
            missileMemoryPool = new MemoryPool(enemyFSM.enemyMissilePrefab);
            missileMemoryPool.SetAllPoolObjectsParent(this.transform);
        }
    }

    #region Attack methods
    public void StartAttack()
    {
        StopMove(); // Move 코루틴 중지
        if (enemyFSM.attackCoroutine != null)
        {
            StopCoroutine(enemyFSM.attackCoroutine); // 기존 Attack 코루틴 중지
        }
        enemyFSM.attackCoroutine = StartCoroutine(Attack()); // 새로운 Attack 코루틴 시작
    }

    public void StopAttack()
    {
        if (enemyFSM.attackCoroutine != null)
        {
            StopCoroutine(enemyFSM.attackCoroutine); // Attack 코루틴 중지
            enemyFSM.attackCoroutine = null; // 참조 초기화
        }

        effectController.ActiveAttackEffect(false);
        enemyFSM.isAttackSign = false; // 공격 신호 초기화
        animatorController.SetBool("isShot", false);    // 공격 애니메이션 종료
    }

    private IEnumerator Attack()
    {
        yield return Reload();

        while (status.isDead == false) // 적이 죽지 않은 동안만 실행
        {
            if (!enemyFSM.isAttackSign)
            {
                animatorController.SetBool("isShot", false);
                effectController.ActiveAttackEffect(true);
                if (enemyFSM.isMoving)
                    enemyFSM.isMoving = false; // 이동 중지

                enemyFSM.isAttackSign = true;
                LookRotationToTarget(); // 타겟을 바라보도록 회전

                // EnemyType에 따라 공격 패턴을 switch-case로 처리
                switch (status.setting.enemyType)
                {
                    case EnemyType.Missile:
                        if (missileMemoryPool != null)
                        {
                            audioController.PlayAttackSound();
                            GameObject missileObj = missileMemoryPool.ActivatePoolItem();
                            EnemyMissile enemyMissile = missileObj.GetComponent<EnemyMissile>();
                            if (enemyMissile != null)
                            {
                                enemyMissile.SetPool(missileMemoryPool);
                                enemyMissile.Setup(enemyFSM.target.position, status.setting.missileDamage, status.setting.missileHP, enemyFSM);
                            }
                        }
                        break;
                    case EnemyType.Normal:
                    case EnemyType.Shield:
                        if (enemyFSM.projectile != null)
                        {
                            audioController.PlayAttackSound();
                            enemyFSM.projectile.transform.position = enemyFSM.projectileSpawnPoint[0].position;
                            enemyFSM.projectile.transform.rotation = enemyFSM.projectileSpawnPoint[0].rotation;
                            EnemyProjectile projectileComponent = enemyFSM.projectile.GetComponent<EnemyProjectile>();
                            if (projectileComponent != null)
                            {
                                projectileComponent.Setup(enemyFSM.target.position, status.setting.damage);
                            }
                            else
                            {
                                Debug.LogError("EnemyProjectile 컴포넌트를 찾을 수 없습니다.");
                            }
                            enemyFSM.projectile.SetActive(true);
                        }
                        else
                        {
                            Debug.LogError("Projectile이 설정되지 않았습니다.");
                        }
                        break;
                    case EnemyType.RapidFire:
                        // 미리 배열에 저장된 4개만 사용
                        for (int i = 0; i < rapidFireProjectiles.Length; i++)
                        {
                            GameObject pooledProjectile = rapidFireProjectiles[i];
                            if (pooledProjectile != null && !pooledProjectile.activeInHierarchy)
                            {
                                int j = i % enemyFSM.projectileSpawnPoint.Length;
                                audioController.PlayAttackSound();
                                // NOTE: RapidFire 타입은 발사체 위치를 두개 이상 사용하므로, 배열을 이용
                                pooledProjectile.transform.position = enemyFSM.projectileSpawnPoint[j].position;
                                pooledProjectile.transform.rotation = enemyFSM.projectileSpawnPoint[j].rotation;

                                EnemyProjectile projectileComponent = pooledProjectile.GetComponent<EnemyProjectile>();
                                if (projectileComponent != null)
                                {
                                    projectileComponent.Setup(enemyFSM.target.position, status.setting.damage);
                                    pooledProjectile.SetActive(true);
                                }
                                else
                                {
                                    Debug.LogError("EnemyProjectile 컴포넌트를 찾을 수 없습니다.");
                                }
                            }
                            yield return new WaitForSeconds(0.5f); // 0.5초 간격으로 연속 발사
                        }
                        break;
                    default:
                        Debug.LogError("알 수 없는 EnemyType입니다.");
                        break;
                }
            }

            yield return new WaitForSeconds(0.5f);
            enemyFSM.attackCount++;
            effectController.ActiveAttackEffect(false);
            animatorController.SetBool("isShot", false);

            // 공격 횟수 초기화 및 재장전 대기
            if (enemyFSM.attackCount >= 2)
            {
                enemyFSM.attackCount = 0; // 공격 횟수 초기화
                enemyFSM.isAttackSign = true; // 공격 신호 초기화
                yield return new WaitForSeconds(status.setting.attackRate); // EnemySetting의 공격 속도 사용
                StartMove();
            }
            // 재장전 대기
            yield return Reload();

            yield return null; // 다음 프레임까지 대기
        }
    }

    private IEnumerator Reload()
    {
        effectController.NextAttackWaitEffect(status.setting.attackRate);
        yield return new WaitForSeconds(status.setting.attackRate); // EnemySetting의 공격 속도 사용
    }

    // 시각적으로 Enemy가 공격대상을 바라보게 하는 함수    
    private void LookRotationToTarget()
    {
        // 목표 위치
        Vector3 to = new Vector3(enemyFSM.target.position.x, transform.position.y, enemyFSM.target.position.z);
        // 내 위치
        Vector3 from = transform.position;

        // 목표를 바라보도록 회전 (Z 축 기준)
        Vector3 direction = to - from;
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // 로컬 축 보정 (예: 180도 회전)
        transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);
    }
    #endregion

    #region Move methods
    public void StartMove()
    {
        StopAttack(); // Attack 코루틴 중지
        if (enemyFSM.moveCoroutine != null)
        {
            StopCoroutine(enemyFSM.moveCoroutine); // 기존 Move 코루틴 중지
        }
        enemyFSM.isMoving = true;

        // 랜덤 방향 설정 (-1: 왼쪽, 1: 오른쪽)
        int moveDirection = Random.Range(0, 2) == 0 ? -1 : 1;

        animatorController.SetMoveDirection(moveDirection);
        enemyFSM.moveCoroutine = StartCoroutine(Move(moveDirection)); // 새로운 Move 코루틴 시작
    }

    public void StopMove()
    {
        if (enemyFSM.moveCoroutine != null)
        {
            StopCoroutine(enemyFSM.moveCoroutine); // Move 코루틴 중지
            enemyFSM.moveCoroutine = null; // 참조 초기화
        }
        enemyFSM.isMoving = false; // 이동 상태 초기화
        animatorController.SetMoveDirection(0); // 이동 애니메이션 종료
        enemyFSM.isAttackSign = false;
    }

    private IEnumerator Move(int moveDirection)
    {
        float moveTime = 0f; // 이동 누적 시간
        audioController.PlayMoveSound();

        // 스폰 영역 정보 가져오기
        Bounds spawnBounds = enemyFSM.enemySpawnBounds;

        while (enemyFSM.isMoving)
        {
            if (!enemyFSM.isAttackSign)
                enemyFSM.isAttackSign = true;

            // 일반 이동
            Vector3 newPosition = transform.position + Vector3.right * moveDirection * status.setting.moveSpeed * Time.deltaTime;

            // 스폰 영역을 벗어나지 않도록 제한 (선택적)
            newPosition.x = Mathf.Clamp(newPosition.x, spawnBounds.min.x, spawnBounds.max.x);
            newPosition.z = Mathf.Clamp(newPosition.z, spawnBounds.min.z, spawnBounds.max.z);

            transform.position = newPosition;
            // 이동 시간 누적
            moveTime += Time.deltaTime;

            // 2초가 지나면 이동 멈춤
            if (moveTime >= 2f)
            {
                animatorController.SetMoveDirection(0);
                StartAttack();
                yield break;
            }

            // EndPoint와의 충돌(콜라이더 접촉) 체크, 충돌 시 이동 멈춤
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.1f);
            foreach (var hit in hitColliders)
            {
                if (hit.CompareTag("EndPoint"))
                {
                    // EndPoint에 닿았을 때
                    animatorController.SetMoveDirection(0);
                    StartAttack();
                    yield break;
                }
            }

            yield return null;
        }
        yield return null;
    }
    #endregion

    #region Teleport methods
    // 스폰 영역 정보를 가져오는 헬퍼 메서드
    private Bounds GetSpawnBounds()
    {
        return enemyFSM.enemySpawnBounds;
    }

    // 텔레포트 기반 이동 패턴
    public void StartTeleportMove()
    {
        StopAttack();
        if (enemyFSM.moveCoroutine != null)
        {
            StopCoroutine(enemyFSM.moveCoroutine);
        }
        enemyFSM.isMoving = true;
        enemyFSM.isTeleporting = true; // 텔레포트 중 상태 설정
        StartCoroutine(TeleportMove());
    }

    private IEnumerator TeleportMove()
    {
        audioController.PlayTeleportSound();
        Vector3 currentPosition = transform.position;
        status.isTeleportProtected = true; // 텔레포트 보호 상태 설정
        effectController.SetModelActive(false); // 모델링을 GameObject 변수로 선언하고 비활성화
        effectController.ActivateTeleportEffect(currentPosition, 0); // 현재위치를 파라미터로 전달 후, 이펙트 활성화

        if (!enemyFSM.isAttackSign)
            enemyFSM.isAttackSign = true;

        // 1번만 랜덤 위치로 텔레포트
        TeleportRandomly(GetSpawnBounds());
        // 텔레포트 후 잠깐 대기 (이동 완료 효과)
        yield return new WaitForSeconds(1f);

        audioController.PlayTeleportSound();
        Vector3 afterTeleportPosition = transform.position;
        effectController.ActivateTeleportEffect(afterTeleportPosition, 1);
        status.isTeleportProtected = false; // 텔레포트 보호 상태 해제
        effectController.SetModelActive(true); // 모델링 다시 활성화

        // EndPoint 체크는 텔레포트 후에도 수행
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.1f);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("EndPoint"))
            {
                animatorController.SetMoveDirection(0);
                StartAttack();
                yield break;
            }
        }

        // 텔레포트 이동 완료 후 공격 시작
        animatorController.SetMoveDirection(0);
        StartAttack();
    }

    // Enemy타입에 따라 전달받은 Spawn Area Bounds 내부에서 랜덤 순간이동
    private void TeleportRandomly(Bounds spawnBounds)
    {
        // status -> setting에서 enemySize을 가져와, size별로 순간이동 위치 조정
        // C#의 Switch Expression 기능을 활용
        float yPosOffset = status.setting.enemySize switch
        {
            // Small 타입은 스폰 영역의 y 범위 내에서 랜덤하게 위치 설정
            EnemySize.Small => Random.Range(spawnBounds.min.y, spawnBounds.max.y),
            // Medium, Big 타입은 지상형 Enemy이므로 y축 위치 고정
            EnemySize.Medium => spawnBounds.center.y,// 실제 보다 +0.1f로 조정
            EnemySize.Big => spawnBounds.center.y,
            _ => 1.0f
        };

        // Bounds 내에서 랜덤한 위치 생성
        Vector3 randomPosition = new Vector3(
            Random.Range(spawnBounds.min.x, spawnBounds.max.x),
            yPosOffset,
            Random.Range(spawnBounds.min.z, spawnBounds.max.z)
        );

        // 생성된 랜덤 위치로 이동
        transform.position = randomPosition;
    }
    #endregion
}
