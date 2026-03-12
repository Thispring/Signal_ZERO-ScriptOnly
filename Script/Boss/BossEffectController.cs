using UnityEngine;
using System.Collections;

/// <summary>
/// Boss의 시각 이펙트를 관리하는 스크립트입니다.
/// 모든 Boss에 관련한 스크립트는 Boss를 앞에 붙여줍니다.
/// </summary>
public class BossEffectController : MonoBehaviour
{
    [Header("Material")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private Material bossMaterial; // 타격 시 붉은 색으로 변하게 하기 위한 보스 머테리얼
    private Color originalColor; // 원본 색상

    // Teleport Effect 모든 요소 Inspector에서 끌어서 사용
    [Header("TeleportEffect")]
    [SerializeField]
    private GameObject[] teleportEffectPrefab; // 텔레포트 이펙트 프리팹 배열
    [SerializeField]
    private GameObject bossModel; // 보스 모델링 GameObject

    [Header("DestroyEffect")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private GameObject destroyEffectPrefab; // 파괴 이펙트 프리팹

    // 텔레포트 이펙트 재사용을 위한 메타 데이터
    [Header("Teleport Effect Meta")]
    private Transform[] teleportEffectOriginalParents;
    private Vector3[] teleportEffectDefaultLocalPositions;
    private Quaternion[] teleportEffectDefaultLocalRotations;
    private Vector3[] teleportEffectDefaultLocalScales;
    private Coroutine[] teleportEffectDeactivateCoroutines;
    private const float DefaultTeleportFxLifetime = 1.2f;

    void Awake()
    {
        // 초기 색상 설정
        bossMaterial.color = Color.white; 
        originalColor = bossMaterial.color;

        // 텔레포트 이펙트 초기화
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

                // 이펙트의 부모를 bossModel의 transform으로 설정합니다.
                // 텔레포트 시 이펙트가 보스 모델과 함께 움직이도록 하기 위함입니다.
                if (bossModel != null)
                {
                    fx.transform.SetParent(bossModel.transform);
                }

                var t = fx.transform;
                teleportEffectOriginalParents[i] = t.parent;
                teleportEffectDefaultLocalPositions[i] = t.localPosition;
                teleportEffectDefaultLocalRotations[i] = t.localRotation;
                teleportEffectDefaultLocalScales[i] = t.localScale;
                fx.SetActive(false);
            }
        }

        if (destroyEffectPrefab != null)
            destroyEffectPrefab.SetActive(false);
    }

    // 보스 모델 활성화/비활성화 메서드
    public void SetModelActive(bool isActive)
    {
        if (bossModel != null)
        {
            bossModel.SetActive(isActive);
        }
    }

    // 텔레포트 이펙트 활성화 메서드
    public void ActivateTeleportEffect(Vector3 position, int index)
    {
        if (teleportEffectPrefab == null || index < 0 || index >= teleportEffectPrefab.Length || teleportEffectPrefab[index] == null)
        {
            Debug.LogWarning("BossEffectController: Teleport effect prefab is not assigned or index is out of range.");
            return;
        }

        var fx = teleportEffectPrefab[index];

        if (teleportEffectDeactivateCoroutines != null && teleportEffectDeactivateCoroutines[index] != null)
        {
            StopCoroutine(teleportEffectDeactivateCoroutines[index]);
            teleportEffectDeactivateCoroutines[index] = null;
        }

        var t = fx.transform;
        t.SetParent(null, true);
        // 이펙트 위치를 고정된 좌표로 설정합니다. (필요시 조정 가능)
        t.position = new Vector3(position.x, 0.5f, 60f);

        var particleSystems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.Clear(true);
        }

        fx.SetActive(true);

        foreach (var ps in particleSystems)
        {
            ps.Play(true);
        }

        float life = ComputeEffectLifetime(particleSystems);
        teleportEffectDeactivateCoroutines[index] = StartCoroutine(DeactivateAndReattachAfter(index, life));
    }

    // 이펙트의 예상 수명 계산
    private float ComputeEffectLifetime(ParticleSystem[] psList)
    {
        if (psList == null || psList.Length == 0) return DefaultTeleportFxLifetime;
        float max = 0f;
        foreach (var ps in psList)
        {
            var main = ps.main;
            if (main.loop)
            {
                max = Mathf.Max(max, DefaultTeleportFxLifetime);
                continue;
            }
            float startLifetime = 0f;
            var curve = main.startLifetime;
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    startLifetime = curve.constant;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    startLifetime = curve.constantMax;
                    break;
                default:
                    startLifetime = DefaultTeleportFxLifetime * 0.5f;
                    break;
            }
            max = Mathf.Max(max, main.duration + startLifetime);
        }
        if (max <= 0f) max = DefaultTeleportFxLifetime;
        return max;
    }

    // 이펙트 비활성화 및 원래 부모에 재부착 코루틴
    private IEnumerator DeactivateAndReattachAfter(int index, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (teleportEffectPrefab == null || index < 0 || index >= teleportEffectPrefab.Length || teleportEffectPrefab[index] == null)
            yield break;

        var fx = teleportEffectPrefab[index];
        var particleSystems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        fx.SetActive(false);

        var t = fx.transform;
        if (teleportEffectOriginalParents != null && index < teleportEffectOriginalParents.Length)
        {
            t.SetParent(teleportEffectOriginalParents[index], true);
            t.localPosition = teleportEffectDefaultLocalPositions[index];
            t.localRotation = teleportEffectDefaultLocalRotations[index];
            t.localScale = teleportEffectDefaultLocalScales[index];
        }

        if (teleportEffectDeactivateCoroutines != null)
            teleportEffectDeactivateCoroutines[index] = null;
    }

    // 피격 시 이펙트
    public void OnHitEffect()
    {
        StartCoroutine(HitEffect());
    }
    
    private IEnumerator HitEffect()
    {
        bossMaterial.color = Color.red; // 빨간색으로 변경
        yield return new WaitForSeconds(0.1f); // 0.1초
        bossMaterial.color = originalColor; // 원래 색상으로 복원
    }

    // 파괴 이펙트 활성화/비활성화 메서드
    public void SetDestoryEffectActive(bool isActive)
    {
        if (destroyEffectPrefab != null)
            destroyEffectPrefab.SetActive(isActive);
    }
}
