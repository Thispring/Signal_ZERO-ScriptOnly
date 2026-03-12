using UnityEngine;
using System.Collections;

/// <summary>
/// 로켓 발사체 스크립트입니다.
/// </summary>
/// 
/// <remarks>
/// Player로 부터 Rocket 공격 함수가 발동되면, 로켓프리팹을 생성하고 이동과 폭발을 담당합니다.
/// 프리팹은 pool을 통해 재활용합니다.
/// </remarks>
public class Rocket : MonoBehaviour
{
    [Header("Vector3")]
    public Vector3 originPos = new Vector3(0, 0, 0);
    private Vector3 targetDirection;
    private Vector3 startPos;
    private Vector3 endPos;

    [Header("Values")]
    private float rocketTravelTime = 2f; // 로켓이 날아가는 시간
    public float explosionDamage;
    public float explosionRadius = 5f;  // 폭발 범위
    private bool isExploded = false;
    private float travelElapsed = 0f; // 비행 경과 시간

    [Header("Flags")]
    private bool isRocketMove = false;

    // Effects 요소 Inspector 할당
    [Header("Effects")]
    [SerializeField]
    private GameObject explosionEffect;
    [SerializeField]
    private GameObject explosionTextEffect;
    [SerializeField]
    private GameObject childRocketPrefab;   // 로켓 모델링 

    [Header("Audio")]
    [SerializeField]
    private AudioClip audioExplosion;
    private AudioSource audioSource;

    [Header("Pool")]
    private MemoryPool ownerPool;

    void Awake()
    {
        // 이펙트 비활성화
        explosionEffect.SetActive(false);
        explosionTextEffect.SetActive(false);
    }

    void Start()
    {
        // SoundEffect 조절을 위한 audioSource 등록
        audioSource = GetComponent<AudioSource>(); // AudioSource 가져오기
        SoundManager soundManager = SoundManager.Instance;
        if (soundManager != null && audioSource != null)
            soundManager.RegisterEnemyAudio(audioSource);
    }

    void Update()
    {
        if (!isRocketMove) return;

        // 비행 진행도 계산 (0 -> 1)
        travelElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.0001f, rocketTravelTime);
        float t = Mathf.Clamp01(travelElapsed / duration);

        // 직선 보간으로 위치 계산
        Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);

        // 다음 프레임 예상 위치로 바라볼 방향 계산하여 자연스러운 회전
        float nextT = Mathf.Clamp01(t + (Time.deltaTime / duration));
        Vector3 nextPos = Vector3.Lerp(startPos, endPos, nextT);
        Vector3 velocity = nextPos - currentPos;

        transform.position = currentPos;
        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }

        // 비행 종료 시 폭발 처리 (진행도 1 도달 시)
        if (t >= 1f)
        {
            Explosion();
            isRocketMove = false;
            return;
        }
    }

    // 지정된 월드 목표 지점으로 비행 설정
    public void SetupToTarget(Vector3 worldTargetPosition, float damage)
    {
        explosionDamage = damage;
        // 비행 파라미터 초기화
        travelElapsed = 0f;
        isRocketMove = true;
        isExploded = false;
        startPos = transform.position;
        endPos = worldTargetPosition;
        targetDirection = (endPos - startPos).normalized;

        // Rigidbody 비활성화
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (targetDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(targetDirection);
        }
        if (childRocketPrefab != null) childRocketPrefab.SetActive(true);
    }

    // 풀 소유권 설정
    public void SetOwnerPool(MemoryPool pool)
    {
        ownerPool = pool;
    }

    private void Explosion()
    {
        // NOTE: bool 변수 return을 통해 중복 폭발 방지
        if (isExploded) return;
        isExploded = true;

        // 폭발 범위 내의 모든 Collider 탐지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hitCollider in hitColliders)
        {
            // 탐지된 Collider가 어떤 Enemy인지 확인
            // 충돌한 스크립트에서 TakeDamage 메서드 호출
            EnemyStatusManager enemy = hitCollider.GetComponent<EnemyStatusManager>();
            BossStatusManager bossStatus = hitCollider.GetComponent<BossStatusManager>();
            EnemyShield enemyShield = hitCollider.GetComponent<EnemyShield>();
            BossDroneManager bossDroneManager = hitCollider.GetComponent<BossDroneManager>();

            // 게임에 Shield 타입 Enemy가 있을 때
            if (enemyShield != null)
            {
                if (enemyShield.shieldActiveCount <= 0)
                {
                    if (enemy != null) enemy.TakeDamage(explosionDamage);
                    if (bossStatus != null) bossStatus.TakeDamage(explosionDamage);
                    if (bossDroneManager != null) bossDroneManager.TakeDamage(explosionDamage);
                }
                // shieldActiveCount가 0보다 크면 Shield 타입 Enemy가 있는 상태이므로 데미지 무시하기
                else
                {

                }
            }
            // Shield 타입 Enemy가 없을 때
            else
            {
                if (enemy != null) enemy.TakeDamage(explosionDamage);
                if (bossStatus != null) bossStatus.TakeDamage(explosionDamage);
                if (bossDroneManager != null) bossDroneManager.TakeDamage(explosionDamage);
                // RocketExplosionArea 스크립트를 통한 추가 폭발 데미지 처리
                explosionEffect.GetComponent<RocketExplosionArea>().Activate(explosionDamage, 1f);
                explosionEffect.SetActive(true);
            }
        }

        StartCoroutine(WaitEffect()); // 폭발 효과 대기
    }

    // Collider 충돌을 통해 폭발 공격 처리
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemySmall") || other.CompareTag("EnemyMedium") || other.CompareTag("EnemyBig")
        || other.CompareTag("Boss") || other.CompareTag("BossDrone"))
        {
            Explosion();
        }
    }

    // Gizmo로 폭발 범위 표시
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // Gizmo 색상 설정
        float explosionRadius = 5f; // 폭발 범위 반경
        Gizmos.DrawWireSphere(transform.position, explosionRadius); // 폭발 범위 그리기
    }

    // 폭발 효과를 위한 대기시간 코루틴
    private IEnumerator WaitEffect()
    {
        explosionEffect.SetActive(true); // 폭발 효과 활성화
        explosionTextEffect.SetActive(true); // 폭발 텍스트 활성화
        audioSource.PlayOneShot(audioExplosion); // 폭발 사운드 재생
        yield return new WaitForSeconds(1f);

        // 풀 반환 또는 일반 비활성화
        if (ownerPool != null)
        {
            ownerPool.DeactivatePoolItem(gameObject);
        }
        else
        {
            gameObject.SetActive(false); // 로켓 비활성화
        }

        childRocketPrefab.SetActive(false); // 자식 로켓 비활성화
        explosionEffect.SetActive(false); // 폭발 효과 비활성화
        explosionTextEffect.SetActive(false); // 폭발 텍스트 비활성화
        isRocketMove = false; // 로켓 이동 상태 비활성화
        transform.position = originPos; // 원래 위치로 초기화
    }
}
