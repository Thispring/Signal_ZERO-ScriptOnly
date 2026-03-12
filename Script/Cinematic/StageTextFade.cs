using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 스테이지 시작 시 스테이지 정보를 출력하는 텍스트의 페이드 효과를 관리하는 스크립트 입니다.
/// </summary>
public class StageTextFade : MonoBehaviour
{
    // 게임 시작 전 EnemyMemoryPool에서 호출, 게임 진행 중에는 StageManager에서 호출합니다.
    public static StageTextFade Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField]
    private TextMeshProUGUI stageText;
    private Color startTextColor;

    void Awake()
    {
        if (stageText != null)
        {
            startTextColor = new Color(stageText.color.r, stageText.color.g, stageText.color.b, 255f);
            stageText.text = "";
        }

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // string 매개변수를 받아 스테이지 정보를 duration 시간 동안 출력
    public void StartTextFade(string info, float duration)
    {
        stageText.text = info + " STAGE";
        StartCoroutine(TextFadeCoroutine(duration));
    }

    private IEnumerator TextFadeCoroutine(float duration)
    {
        float elapsed = 0f;
        // 알파를 1.0f(불투명)에서 0.0f(투명)으로
        Color startColor = stageText.color;
        startColor.a = 1.0f;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        stageText.color = startColor;

        while (elapsed < duration)
        {
            stageText.color = Color.Lerp(startColor, endColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        stageText.color = endColor;
        stageText.text = "";
        stageText.color = startTextColor;
    }
}
