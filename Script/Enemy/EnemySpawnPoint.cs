using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy의 스폰포인트에 관련된 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
public class EnemySpawnPoint : MonoBehaviour
{
    private float fadeSpeed = 4;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void OnEnable()
    {
        StartCoroutine(ShowSpawnEffect());
    }

    void OnDisable()
    {
        StopCoroutine(ShowSpawnEffect());
    }

    // 오브젝트 meshRenderer의 컬러 fade효과를 줍니다.
    private IEnumerator ShowSpawnEffect()
    {
        while (true)
        {
            Color color = meshRenderer.material.color;
            color.a = Mathf.Lerp(1, 0, Mathf.PingPong(Time.time * fadeSpeed, 1));
            meshRenderer.material.color = color;

            yield return null;
        }
    }
}
