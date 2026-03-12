using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 관리 정적 클래스
/// </summary>
public static class PauseManager
{
    public static int pauseCount = 0;

    public static void AddPause()
    {
        pauseCount++;
        Time.timeScale = 0;
    }

    public static void RemovePause()
    {
        pauseCount = Mathf.Max(0, pauseCount - 1);
        if (pauseCount == 0)
            Time.timeScale = 1;
    }

    public static void ResetPause()
    {
        pauseCount = 0;
        Time.timeScale = 1;
    }
}

/// <summary>
/// 게임 설정에 관련된 스크립트 입니다.
/// </summary>
public class GameSetting : MonoBehaviour
{
    [Header("References")]
    [SerializeField]    // Inspector 할당
    private BossCutScene bossCutScene;  // 컷신 재생 확인을 위한 참조

    [Header("Flags")]
    public bool isSetOn = false;    // settingsCanvas가 열려있는지 여부, 열려있으면 커서 보이게 설정

    // 모든 요소 Inspector 할당
    [Header("Canvas")]
    [SerializeField]
    private Canvas settingsCanvas; // 설정 UI 캔버스
    [SerializeField]
    private Canvas creditCanvas; // 크레딧 UI 
    [SerializeField]
    private Canvas startButtonCanvas; // 시작 버튼 캔버스

    // 모든 요소 Inspector 할당    
    [Header("Credit Image")]
    [SerializeField]
    private Sprite[] creditSprites; // 스프라이트 배열로 변경
    [SerializeField]
    private Image creditImage;      // 실제 이미지를 출력할 Image 컴포넌트
    private int currentCreditIndex = 0;   // 이미지 인덱스 변수

    void Awake()
    {
        // 초기 상태에서 모든 캔버스 비활성화
        settingsCanvas.gameObject.SetActive(false);
        if (creditCanvas != null) creditCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        // ESC 키로 모든 Canvas 관리
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (startButtonCanvas != null && startButtonCanvas.gameObject.activeSelf)
            {
                startButtonCanvas.gameObject.SetActive(false);
                return;
            }
            if (creditCanvas != null && creditCanvas.gameObject.activeSelf)
            {
                CloseCredit();
                return;
            }
            if (settingsCanvas != null && settingsCanvas.gameObject.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }
    }

    #region UI Button Methods
    // 용도에 맞는 Button UI를 찾아 OnClick 이벤트에 할당
    public void OpenSettings()
    {
        if (bossCutScene != null && bossCutScene.isCutScenePlay)
            return; // 컷신 재생 중에는 설정창 열지 않음

        isSetOn = true; // 커서 보이게 설정
        // 다른 캔버스 모두 닫기
        if (startButtonCanvas != null) startButtonCanvas.gameObject.SetActive(false);
        if (creditCanvas != null) creditCanvas.gameObject.SetActive(false);
        settingsCanvas.gameObject.SetActive(true); // 설정 UI 활성화
        PauseManager.AddPause();
    }

    public void CloseSettings()
    {
        if (bossCutScene != null && bossCutScene.isCutScenePlay)
            return; // 컷신 재생 중에는 설정창 닫지 않음

        isSetOn = false; // 커서 숨기기 설정
        settingsCanvas.gameObject.SetActive(false); // 설정 UI 비활성화
        PauseManager.RemovePause();
    }

    public void OpenCredit()
    {
        // 다른 캔버스 모두 닫기
        if (startButtonCanvas != null) startButtonCanvas.gameObject.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.gameObject.SetActive(false);
        creditCanvas.gameObject.SetActive(true); // 크레딧 UI 활성화
        PauseManager.AddPause();
    }

    public void CloseCredit()
    {
        creditCanvas.gameObject.SetActive(false); // 크레딧 UI 비활성화
        PauseManager.RemovePause();
    }

    // 현재 인덱스의 스프라이트를 이미지에 출력
    private void ShowCreditImage(int index)
    {
        if (creditSprites == null || creditSprites.Length == 0 || creditImage == null)
            return;

        creditImage.sprite = creditSprites[index];
        creditImage.gameObject.SetActive(true);
    }

    // 다음 이미지 출력
    public void ShowNextCreditImage()
    {
        if (creditSprites == null || creditSprites.Length == 0)
            return;

        currentCreditIndex = (currentCreditIndex + 1) % creditSprites.Length;
        ShowCreditImage(currentCreditIndex);
    }

    // 이전 이미지 출력
    public void ShowPreviousCreditImage()
    {
        if (creditSprites == null || creditSprites.Length == 0)
            return;

        currentCreditIndex = (currentCreditIndex - 1 + creditSprites.Length) % creditSprites.Length;
        ShowCreditImage(currentCreditIndex);
    }

    public void Exit()
    {
        Application.Quit();
    }

    #endregion
}
