using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy의 모든 시각 이펙트 관리에 대한 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
public class EnemyEffectController : MonoBehaviour
{
    [Header("References")]
    private EnemyStatusManager status;  // 상태이상을 체크하기 위해 참조, 상태이상이 달라질 경우 이펙트 정지

    [Header("Attack Effect")]
    [SerializeField]
    private GameObject attackEffectPrefab; // 공격 이펙트 프리팹
    [SerializeField]
    private GameObject attackEffectPrefab2; // 공격 이펙트 프리팹2 (모델링의 총구 개수에 따라 추가 확장)

    [Header("Status Effects")]
    [SerializeField]
    private GameObject destoryEffect; // 파괴 이펙트
    [SerializeField]
    private GameObject bonusCoinTextEffect; // 추가 코인 텍스트 이펙트
    [SerializeField]
    private GameObject stunEffect;  // EMP 발동 시 이펙트
    [SerializeField]
    private GameObject stunTextEffect;  // EMP 발동 텍스트 이펙트

    [Header("Next Attack Wait Effect")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private GameObject nextAttackWaitEffectPrefab; // 공격 대기 이펙트 프리팹
    private bool isNextAttackWaitActive = false; // 공격 대기 이펙트 활성화 여부
    public bool isNextAttackWaitStop = false; // Stun 상태로 인해 정지 했는지 여부
    private Vector3 nextAttackWaitCurrentScale; // 이펙트의 처음 크기 저장

    [Header("Teleport Effect")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private GameObject[] teleportEffectPrefab; // 순간이동 이펙트 프리팹
    // 순간이동 이펙트 원복을 위한 메타 데이터 (pool 미사용, 재사용)
    private Transform[] teleportEffectOriginalParents;
    private Vector3[] teleportEffectDefaultLocalPositions;
    private Quaternion[] teleportEffectDefaultLocalRotations;
    private Vector3[] teleportEffectDefaultLocalScales;
    private Coroutine[] teleportEffectDeactivateCoroutines;
    // 순간이동 시, 모델링도 보여주지 않아야 하므로 게임 오브젝트로 활성화 관리
    [SerializeField]
    private GameObject[] modelObjects;

    [Header("Hit Render")]
    [SerializeField]
    public Material enemyMaterial; // 자기 자신 모델링 머티리얼
    private Color originalColor; // 원본 색상

    void Awake()
    {
        status = GetComponent<EnemyStatusManager>();

        if (enemyMaterial != null)
        {
            enemyMaterial = Instantiate(enemyMaterial);
            // Renderer에 복제한 머티리얼을 할당
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = enemyMaterial;
            }
        }
        enemyMaterial.color = Color.white; // 초기 색상 설정
        originalColor = enemyMaterial.color;

        destoryEffect.SetActive(false);
        bonusCoinTextEffect.SetActive(false);
        stunEffect.SetActive(false);
        stunTextEffect.SetActive(false);

        // Enemy 타입별 attackEffectPrefab 개수가 달라 null이 아닐때만 비활성화
        if (attackEffectPrefab != null)
            attackEffectPrefab.SetActive(false);
        if (attackEffectPrefab2 != null)
            attackEffectPrefab2.SetActive(false);

        if (nextAttackWaitEffectPrefab != null)
        {
            nextAttackWaitEffectPrefab.SetActive(false); // 공격 대기 이펙트 비활성화
            nextAttackWaitCurrentScale = nextAttackWaitEffectPrefab.transform.localScale; // 현재 스케일 저장
        }

        if (teleportEffectPrefab != null)
        {
            int len = teleportEffectPrefab.Length;
            teleportEffectOriginalParents = new Transform[len];
            teleportEffectDefaultLocalPositions = new Vector3[len];
            teleportEffectDefaultLocalRotations = new Quaternion[len];
            teleportEffectDefaultLocalScales = new Vector3[len];
            teleportEffectDeactivateCoroutines = new Coroutine[len];

            for (int i = 0; i < len; i++)
            {
                var fx = teleportEffectPrefab[i];
                if (fx == null) continue;
                var t = fx.transform;
                teleportEffectOriginalParents[i] = t.parent;
                teleportEffectDefaultLocalPositions[i] = t.localPosition;
                teleportEffectDefaultLocalRotations[i] = t.localRotation;
                teleportEffectDefaultLocalScales[i] = t.localScale;
                fx.SetActive(false);
            }
        }
    }

    void Update()
    {
        // 성능 최적화: 필요할 때만 함수 호출
        if (nextAttackWaitEffectPrefab != null && nextAttackWaitEffectPrefab.activeInHierarchy)
        {
            if (!isNextAttackWaitStop) // Stun 상태가 아니고 텔레포트 중이 아닐 때만 크기 감소
                DecreaseNextAttackWaitEffect();
            else
                OffNextAttackWaitEffect();

            if (status.isTeleportProtected)
                OffNextAttackWaitEffect();
        }
    }

    // 공격 시 이펙트를 관리하는 함수
    public void ActiveAttackEffect(bool isActive)
    {
        switch (status.setting.enemySize)
        {
            case EnemySize.Small:
                attackEffectPrefab.SetActive(isActive);
                break;
            case EnemySize.Medium:
                attackEffectPrefab.SetActive(isActive);
                break;
            case EnemySize.Big:
                attackEffectPrefab.SetActive(isActive);
                attackEffectPrefab2.SetActive(isActive);
                break;
            default:
                break;
        }
    }

    // 다음 공격까지 대기 이펙트 활성화
    // EnemySetting의 enemyAttackRate 값을 사용하여 대기 이펙트의 지속 시간을 설정
    public void NextAttackWaitEffect(float waitTime)
    {
        if (nextAttackWaitEffectPrefab == null)
        {
            return;
        }

        nextAttackWaitEffectPrefab.SetActive(true);
        isNextAttackWaitActive = true; // 공격 대기 이펙트 활성화 상태로 설정
        // 대기 이펙트의 지속 시간 설정
        Invoke(nameof(OffNextAttackWaitEffect), waitTime);
    }

    private void OffNextAttackWaitEffect()
    {
        if (nextAttackWaitEffectPrefab != null)
        {
            nextAttackWaitEffectPrefab.SetActive(false);
        }

        isNextAttackWaitActive = false; // 공격 대기 이펙트 비활성화 상태로 설정
        nextAttackWaitEffectPrefab.transform.localScale = nextAttackWaitCurrentScale; // 스케일 초기화
    }

    // 공격 대기 이펙트 크기 감소 함수
    private void DecreaseNextAttackWaitEffect()
    {
        if (isNextAttackWaitActive)
        {
            Vector3 currentScale = nextAttackWaitEffectPrefab.transform.localScale;
            Vector3 targetScale = Vector3.one * 0.1f;

            // attackRate 시간 동안 정확히 targetScale에 도달하도록 계산
            // 초기 스케일에서 타겟 스케일까지의 거리를 attackRate로 나눈 속도
            Vector3 scaleDistance = nextAttackWaitCurrentScale - targetScale;
            Vector3 shrinkAmount = scaleDistance * (Time.deltaTime / status.setting.attackRate);

            // 현재 스케일에서 shrinkAmount만큼 감소
            Vector3 newScale = currentScale - shrinkAmount;

            // targetScale보다 작아지지 않도록 제한
            newScale = Vector3.Max(newScale, targetScale);

            nextAttackWaitEffectPrefab.transform.localScale = newScale;
        }
    }

    // 순간이동 이펙트 활성화 함수 
    public void ActivateTeleportEffect(Vector3 position, int index, float duration = 1.0f)
    {
        // 유효성 검사
        if (teleportEffectPrefab == null || index < 0 || index >= teleportEffectPrefab.Length)
            return;

        var fx = teleportEffectPrefab[index];
        if (fx == null)
            return;

        // 이전 비활성화 코루틴이 살아있다면 중단
        if (teleportEffectDeactivateCoroutines != null && teleportEffectDeactivateCoroutines[index] != null)
        {
            StopCoroutine(teleportEffectDeactivateCoroutines[index]);
            teleportEffectDeactivateCoroutines[index] = null;
        }

        // 부모에서 분리 후, 지정 위치에서 활성화
        var t = fx.transform;
        t.SetParent(null, true);
        t.position = position;

        fx.SetActive(true);

        // 간단한 지속시간 기반 비활성화 스케줄
        teleportEffectDeactivateCoroutines[index] = StartCoroutine(SimpleDeactivateAfter(index, duration));
    }

    // 순간이동 후 이펙트 비활성화 함수
    private IEnumerator SimpleDeactivateAfter(int index, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (teleportEffectPrefab == null || index < 0 || index >= teleportEffectPrefab.Length)
            yield break;

        var fx = teleportEffectPrefab[index];
        if (fx == null) yield break;

        fx.SetActive(false);

        // 부모 복귀 및 로컬 트랜스폼 원복
        var t = fx.transform;
        if (teleportEffectOriginalParents != null && index < teleportEffectOriginalParents.Length)
        {
            t.SetParent(teleportEffectOriginalParents[index], true);
            t.localPosition = teleportEffectDefaultLocalPositions[index];
            t.localRotation = teleportEffectDefaultLocalRotations[index];
            t.localScale = teleportEffectDefaultLocalScales[index];
        }

        // 코루틴 핸들 정리
        if (teleportEffectDeactivateCoroutines != null)
            teleportEffectDeactivateCoroutines[index] = null;
    }

    public void SetModelActive(bool enabled)
    {
        if (modelObjects == null || modelObjects.Length == 0) return;

        for (int i = 0; i < modelObjects.Length; i++)
        {
            if (modelObjects[i] != null)
                modelObjects[i].SetActive(enabled);
        }
    }

    // 피격 이펙트는 본인 Material 컬러를 빨간색으로 변경하여 연출
    public void OnHitEffect()
    {
        StartCoroutine(HitEffect());
    }
    private IEnumerator HitEffect()
    {
        enemyMaterial.color = Color.red; // 빨간색으로 변경
        yield return new WaitForSeconds(0.1f); // 0.1초
        enemyMaterial.color = originalColor; // 원래 색상으로 복원
    }

    // 기타 이펙트 활성화 함수
    public void DeathEffect(bool active)
    {
        destoryEffect.SetActive(active);
    }
    public void BonusCoinTextEffect(bool active)
    {
        bonusCoinTextEffect.SetActive(active);
    }
    public void StunEffect(bool active)
    {
        stunEffect.SetActive(active);
        stunTextEffect.SetActive(active);
    }
}
