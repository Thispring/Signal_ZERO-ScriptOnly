using UnityEngine;

/// <summary>
/// Small 타입 Enemy의 소환 애니메이션을 위한 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
/// 
/// <remarks>
/// Small은 드론 타입을 가지고 있으므로 날라오는 애니메이션을 구현합니다.
/// </remarks>
public class EnemySmallSpawn : MonoBehaviour
{
    // 다른 스크립트나 객체를 참조
    [Header("References")]
    [SerializeField]
    private EnemyAnimatorController animatorController;

    [Header("Values")]
    private float moveSpeed = 20f; // 이동 속도
    public bool isMoving = false;   // 이동 중 상태 확인
    private Vector3 targetPosition; // 이동 할 위치

    void Awake()
    {
        animatorController = GetComponent<EnemyAnimatorController>();
    }

    void Update()
    {
        if (isMoving)
        {
            // 적 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // 목표 위치에 도달하면 이동 중지
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isMoving = false;
                animatorController.SetMoveDirection(0);
            }
        }
    }

    public void StartMovement(Vector3 spawnPosition, int randomSide, Bounds spawnBounds)
    {
        Vector3 startPosition;

        // 랜덤 방향에 따라 시작 위치 설정
        if (randomSide == 1)
        {
            // rightSpawnBounds의 랜덤 위치 중 하나를 선정
            startPosition = new Vector3(
            Random.Range(spawnBounds.min.x, spawnBounds.max.x),
            Random.Range(spawnBounds.min.y, spawnBounds.max.y),
            spawnBounds.center.z
            );
        }
        else if (randomSide == -1)
        {
            // leftSpawnBounds의 랜덤 위치 중 하나를 선정
            startPosition = new Vector3(
            Random.Range(spawnBounds.min.x, spawnBounds.max.x),
            Random.Range(spawnBounds.min.y, spawnBounds.max.y),
            spawnBounds.center.z
            );
        }
        else
        {
            Debug.LogWarning("randomSide 값이 잘못되었습니다. 기본 위치로 설정합니다.");
            startPosition = spawnPosition; // 기본 위치로 설정
        }

        // 시작 위치 설정
        transform.position = startPosition;

        // 목표 위치 설정
        targetPosition = new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z);

        // 이동 시작
        isMoving = true;
        animatorController.SetMoveDirection(randomSide);
    }
}
