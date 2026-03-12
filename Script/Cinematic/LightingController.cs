using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 게임에 존재하는 조명 효과를 관리하는 스크립트 입니다.
/// </summary>
/// 
/// <remarks>
/// 현재 스테이지 1~4에 존재하는 신호등에 적용 중
/// 신호등을 위아래로 떠다니게 하는 효과를 parentObjects에 적용
/// </remarks>
public class LightingController : MonoBehaviour
{
    // Object 요소 Inspector에서 할당
    [Header("Objects")]
    [SerializeField]
    private GameObject[] lightingPrefabs;
    [SerializeField]
    private GameObject[] parentObjects;

    [Header("LightingObject Values")]
    private Coroutine lightingCoroutine;
    private float lightStartDelay = 0.25f;
    private bool isLightingRunning = false;

    [Header("ParentObject Values")]
    private Tween[] parentObjectTweens;
    private float parentObjectAmplitude = 0.05f;
    private float parentObjectDuration = 1f;
    private float[] parentObjectBaseYs;
    private bool parentObjectUnscaledTime = false;

    void Awake()
    {
        for (int i = 0; i < lightingPrefabs.Length; i++)
        {
            if (lightingPrefabs[i] != null)
                lightingPrefabs[i].SetActive(false);
        }

        // Tween 설정
        if (parentObjects != null)
        {
            parentObjectTweens = new Tween[parentObjects.Length];
            parentObjectBaseYs = new float[parentObjects.Length];
            for (int i = 0; i < parentObjects.Length; i++)
            {
                if (parentObjects[i] != null)
                    parentObjectBaseYs[i] = parentObjects[i].transform.localPosition.y;
            }
        }
    }

    void Update()
    {
        /// 조명 오브젝트는 스테이지 1~4만 적용
        /// 스테이지 5 이상이면 코루틴 정리 및 컴포넌트 비활성화
        /// 스테이지 체크는 parentObjects의 활성화로 판단
        if (parentObjects[0].activeInHierarchy && parentObjects[1].activeInHierarchy)
        {
            if (!isLightingRunning && lightingPrefabs != null && lightingPrefabs.Length > 0)
            {
                lightingCoroutine = StartCoroutine(LightingSequenceCoroutine());
            }

            // parentObjects에 대해 떠다니는 효과 적용
            if (parentObjects != null)
            {
                for (int i = 0; i < Mathf.Min(2, parentObjects.Length); i++)
                {
                    PlayTweenParentObject(i);
                }
            }
        }
        else
        {
            // 스테이지가 5 이상이고, 실행 중인 코루틴이 있으면 정리
            if (isLightingRunning)
            {
                if (lightingCoroutine != null)
                {
                    StopCoroutine(lightingCoroutine);
                    lightingCoroutine = null;
                }
                isLightingRunning = false;

                if (lightingPrefabs != null)
                {
                    for (int i = 0; i < lightingPrefabs.Length; i++)
                    {
                        if (lightingPrefabs[i] != null)
                            lightingPrefabs[i].SetActive(false);
                    }
                }

                if (parentObjectTweens != null)
                {
                    for (int i = 0; i < parentObjectTweens.Length; i++)
                    {
                        if (parentObjectTweens[i] != null)
                        {
                            parentObjectTweens[i].Kill();
                            parentObjectTweens[i] = null;
                        }
                    }
                }
            }
            // 스테이지 1~4에서만 사용되므로 컴포넌트 비활성화
            this.enabled = false;
        }
    }

    // 조명 오브젝트를 순차적으로 활성화시켜 깜빡거리는 효과를 주는 코루틴
    private IEnumerator LightingSequenceCoroutine()
    {
        if (lightingPrefabs == null || lightingPrefabs.Length == 0)
            yield break;

        isLightingRunning = true;
        int index = 0;

        while (parentObjects[0].activeInHierarchy && parentObjects[1].activeInHierarchy)
        {
            for (int i = 0; i < lightingPrefabs.Length; i++)
            {
                if (lightingPrefabs[i] != null)
                    lightingPrefabs[i].SetActive(false);
            }

            // 현재 인덱스만 활성화
            if (lightingPrefabs[index] != null)
            {
                lightingPrefabs[index].SetActive(true);
                // 3초 대기
                float elapsed = 0f;
                while (elapsed < 3f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                lightingPrefabs[index].SetActive(false);
            }

            // 다음 인덱스로 이동
            index = (index + 1) % lightingPrefabs.Length;

            // 다음 루프는 바로 진행(원하면 짧은 프레임 대기로 변경)
            yield return null;
        }
    }

    // parentObjects에 대해 떠다니는 효과 적용
    private void PlayTweenParentObject(int idx)
    {
        if (parentObjects == null || idx < 0 || idx >= parentObjects.Length)
            return;

        var go = parentObjects[idx];

        if (go != null && go.activeInHierarchy)
        {
            if (parentObjectBaseYs == null || parentObjectBaseYs.Length <= idx)
            {
                if (parentObjects != null)
                {
                    parentObjectTweens = new Tween[parentObjects.Length];
                    parentObjectBaseYs = new float[parentObjects.Length];
                }
            }

            if (parentObjectBaseYs != null && parentObjectBaseYs.Length > idx)
                parentObjectBaseYs[idx] = go.transform.localPosition.y;

            if (parentObjectTweens == null)
                parentObjectTweens = new Tween[parentObjects.Length];

            if (parentObjectTweens[idx] == null)
            {
                parentObjectTweens[idx] = go.transform
                    .DOLocalMoveY(go.transform.localPosition.y + parentObjectAmplitude, parentObjectDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(parentObjectUnscaledTime)
                    .SetDelay(lightStartDelay * idx)
                    .SetAutoKill(false);

                if (!parentObjectTweens[idx].IsPlaying())
                    parentObjectTweens[idx].Play();
            }
            else
            {
                if (!parentObjectTweens[idx].IsPlaying())
                    parentObjectTweens[idx].Play();
            }
        }
        else
        {
            if (parentObjectTweens != null && idx < parentObjectTweens.Length && parentObjectTweens[idx] != null)
            {
                parentObjectTweens[idx].Kill();
                parentObjectTweens[idx] = null;
            }
        }
    }

    void OnDisable()
    {
        // Tween 안전하게 정리
        if (parentObjectTweens != null)
        {
            for (int i = 0; i < parentObjectTweens.Length; i++)
            {
                if (parentObjectTweens[i] != null)
                {
                    parentObjectTweens[i].Kill();
                    parentObjectTweens[i] = null;
                }
            }
        }
    }
}
