using UnityEngine;
using System.Collections;

/// <summary>
/// Boss 미사일 발사체에 대한 스크립트 입니다.
/// 모든 Boss에 관련한 스크립트는 Boss를 앞에 붙여줍니다.
/// </summary>
/// 
/// <remarks>
/// 플레이어 방향으로 곡선 비행하는 미사일을 구현합니다.
/// </remarks>
public class BossMissile : MonoBehaviour
{
    [Header("Values")]
    private float missileHP; // 미사일의 체력
    private float maxmissileHP; // 미사일의 최대 체력
    private int missileDamage; // 미사일의 공격력
    [SerializeField]
    private float hpIncreasePerSequence = 5f; // 시퀀스당 증가하는 체력
    private float travelTime = 2f; // 타겟까지 도달하는 데 걸리는 시간
    private float arcHeight = 5f; // 포물선의 최대 높이
    private float elapsedTime; // 발사 후 경과 시간

    [Header("Vectors")]
    private Vector3 targetPos; // 발사체가 날아갈 목표 위치
    private Vector3 firePos; // 미사일의 발사 위치

    [Header("Flags")]
    private bool isMoving = false; // 미사일 이동 상태 플래그
    private bool isDead = false; // 미사일이 파괴되었는지 여부

    [Header("Rendering")]
    [SerializeField]
    private Renderer missileRenderer; // 미사일의 렌더러 컴포넌트
    private Material originalMaterial; // 원본 머테리얼
    private Color originalColor; // 원본 색상
    [SerializeField]
    private GameObject childMissilePrefab;   // 자식 미사일 오브젝트

    void Awake()
    {
        missileRenderer = childMissilePrefab.GetComponentInChildren<Renderer>();
        // 렌더러 컴포넌트 가져오기
        if (missileRenderer != null)
        {
            originalMaterial = missileRenderer.material;
            originalColor = originalMaterial.color;
        }

        // 초기 상태 설정
        isDead = false;
        isMoving = false;
        elapsedTime = 0f;

        gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        // 미사일이 움직이는 상태일 때만 이동 로직 실행
        if (isMoving && !isDead)
        {
            MovementLogic();
        }
    }

    public void Setup(Vector3 targetPosition, int missileDamage, float baseMissileHP, int sequenceCount)
    {
        // Rigidbody 추가 (물리 기반 이동을 위해)
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true; // 물리 계산 비활성화하여 스크립트로 제어

        if (isDead) return; // 이미 파괴된 상태라면 무시

        // 타겟 위치와 발사 위치 설정
        targetPos = targetPosition;
        firePos = transform.position;

        this.missileDamage = missileDamage; // 미사일의 공격력 설정
        
        // 시퀀스에 따른 체력 증가 (예: 기본 체력 + 시퀀스당 5)
        this.maxmissileHP = baseMissileHP + (hpIncreasePerSequence * sequenceCount);
        this.missileHP = this.maxmissileHP; // 미사일의 체력 설정

        // 초기 상태 설정
        elapsedTime = 0f;
        isMoving = true;

        // 미사일 활성화 (메모리풀에서 재사용)
        gameObject.SetActive(true);
        childMissilePrefab.SetActive(true); // 자식 미사일 프리팹 활성화
    }

    // 포물선 궤도를 그리며 플레이어 방향으로 떨어지는 로직
    private void MovementLogic()
    {
        // 경과 시간 증가
        elapsedTime += Time.fixedDeltaTime;

        // 이동 시간이 완료되었는지 확인
        if (elapsedTime >= travelTime)
        {
            // 목표 도달 시 폭발 및 비활성화
            ExplodeAndDeactivate();
            return;
        }

        // 포물선 위치 계산
        Vector3 currentPosition = CalculateParabolicPosition(elapsedTime);

        // 이전 위치와 현재 위치를 비교하여 이동 방향 계산
        Vector3 direction = (currentPosition - transform.position).normalized;

        // 미사일 위치 업데이트
        transform.position = currentPosition;

        // 미사일이 이동 방향을 바라보도록 회전
        if (direction != Vector3.zero)
        {
            transform.LookAt(transform.position + direction);
        }
    }

    // 포물선 궤도 위의 현재 위치를 계산하는 함수
    private Vector3 CalculateParabolicPosition(float time)
    {
        // 시간 비율 (0 ~ 1)
        float t = time / travelTime;

        // 수평 이동: 발사점에서 목표점으로 선형 보간
        Vector3 horizontalPosition = Vector3.Lerp(firePos, targetPos, t);

        // 수직 이동: 포물선 계산 (최고점을 거쳐 떨어지는 궤도)
        // y = -4h*t*(t-1) : 포물선 공식 (h는 최대 높이)
        float verticalOffset = -4f * arcHeight * t * (t - 1f);

        // 최종 위치 = 수평 위치 + 수직 오프셋
        Vector3 finalPosition = horizontalPosition;
        finalPosition.y = Mathf.Lerp(firePos.y, targetPos.y, t) + verticalOffset;

        return finalPosition;
    }

    // 데미지를 받는 함수
    public void TakeDamage(float damage)
    {
        if (isDead) return; // 이미 파괴된 상태라면 무시

        missileHP -= damage;
        missileHP = Mathf.Clamp(missileHP, 0, maxmissileHP);

        // 데미지를 받을 때 빨간색 깜빡임 효과
        StartCoroutine(FlashRed());

        // 체력이 0이 되면 파괴
        if (missileHP <= 0)
        {
            DestroyMissile();
        }
    }

    // 빨간색으로 깜빡이는 효과 코루틴
    private IEnumerator FlashRed()
    {
        if (missileRenderer == null) yield break;

        // 빨간색으로 변경
        missileRenderer.material.color = Color.red;

        yield return new WaitForSeconds(0.1f); // 0.1초 동안 빨간색 유지

        // 원본 색상으로 복원
        missileRenderer.material.color = originalColor;

        yield return new WaitForSeconds(0.1f); // 0.1초 동안 원본 색상 유지

        // 한 번 더 깜빡임
        missileRenderer.material.color = Color.red;

        yield return new WaitForSeconds(0.1f); // 0.1초 동안 빨간색 유지

        // 최종적으로 원본 색상으로 복원
        missileRenderer.material.color = originalColor;
    }

    // 미사일 파괴 처리
    private void DestroyMissile()
    {
        isDead = true;
        isMoving = false;

        // 메모리풀 방식으로 비활성화
        StartCoroutine(DeactivateMissile());
    }

    // 플레이어와 충돌 시 폭발
    private void ExplodeAndDeactivate()
    {
        isMoving = false;
        // 메모리풀 방식으로 비활성화
        StartCoroutine(DeactivateMissile());
    }

    // 메모리풀 방식으로 미사일 비활성화
    private IEnumerator DeactivateMissile()
    {
        // 약간의 지연 후 비활성화 (폭발 이펙트 등을 위해)
        yield return new WaitForSeconds(0.1f);

        // 상태 초기화
        missileHP = maxmissileHP;
        isDead = false;
        isMoving = false;
        elapsedTime = 0f;
        transform.position = firePos; // 발사 위치로 되돌리기

        // 오브젝트 비활성화 (메모리풀에서 재사용 가능)
        gameObject.SetActive(false);
        childMissilePrefab.SetActive(false);
    }

    // 충돌 처리
    private void OnTriggerEnter(Collider other)
    {
        if (isDead || !isMoving) return; // 파괴되었거나 움직이지 않는 상태라면 무시

        if (other.CompareTag("SupportBot"))
        {
            // 보조 로봇에게 데미지 입히기
            SupportBotStatusManager supportBot = other.GetComponent<SupportBotStatusManager>();
            if (supportBot != null)
            {
                supportBot.TakeDamage(missileDamage);
            }
            else
            {
                Debug.LogWarning("SupportBotStatusManager 컴포넌트가 없습니다.");
            }

            // 미사일 폭발 및 비활성화
            ExplodeAndDeactivate();
        }
        else if (other.CompareTag("Player"))
        {
            // 플레이어에게 데미지 입히기
            PlayerStatusManager player = other.GetComponent<PlayerStatusManager>();
            if (player != null)
            {
                player.TakeDamage(missileDamage);
            }

            // 미사일 폭발 및 비활성화
            ExplodeAndDeactivate();
        }
    }
}
