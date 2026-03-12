using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 이미지에 페이드 효과를 적용하는 스크립트 입니다.    
/// </summary>
/// 
/// <remarks>
/// 현재 게임 시작 시 화면 연출에 사용 중
/// </remarks>
public class FadeImage : MonoBehaviour
{
    [Header("Image")]
    public Image[] fadeImages; // 페이드 효과를 적용할 이미지

    [Header("Values")]
    private float fadeDuration = 3f; // 페이드 지속 시간
    private Color targetColor;

    void Awake()
    {
        targetColor = Color.white; // 기본값 설정

        // 모든 이미지의 초기 색상을 투명으로 설정
        for (int i = 0; i < fadeImages.Length; i++)
        {
            if (fadeImages[i] != null)
            {
                fadeImages[i].color = Color.clear;
            }
        }
    }

    void Start()
    {
        StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine()
    {
        if (fadeImages != null && fadeImages.Length > 0)
        {
            float elapsed = 0f;
            Color[] startColors = new Color[fadeImages.Length];

            // 모든 이미지의 시작 색상 저장 (초기에는 투명)
            for (int i = 0; i < fadeImages.Length; i++)
            {
                if (fadeImages[i] != null)
                {
                    startColors[i] = fadeImages[i].color;
                }
            }

            // 페이드 애니메이션 실행
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                // 모든 이미지에 동시에 페이드 적용
                for (int i = 0; i < fadeImages.Length; i++)
                {
                    if (fadeImages[i] != null)
                    {
                        fadeImages[i].color = Color.Lerp(startColors[i], targetColor, t);
                    }
                }

                yield return null;
            }

            // 코루틴 종료 시 모든 이미지의 색상을 목표 색상으로 설정
            for (int i = 0; i < fadeImages.Length; i++)
            {
                if (fadeImages[i] != null)
                {
                    fadeImages[i].color = targetColor;
                }
            }
        }
    }
}
