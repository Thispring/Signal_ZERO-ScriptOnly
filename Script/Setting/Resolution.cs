using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임의 해상도를 관리하는 스크립트입니다.
/// </summary>
///
/// <remarks>
/// 게임 사격 범위가 1920x1080으로 고정되어 있어, 그 외에 해상도의 기기에서 원활하게 실행하기 위헤
/// 강제로 1920x1080 창모드로 실행시킵니다. 필요하다면 PlayerShotController.cs의 고정 해상도를 수정하고
/// 해당 스크립트의 해상도 관련 설정을 변경하세요.
/// </remarks>
public class Resolution : MonoBehaviour
{
    // 모든 UI 요소는 Inspector에서 끌어서 사용
    [Header("UI")]
    [SerializeField]
    private TextMeshProUGUI resolutionText;
    [SerializeField]
    private Button fullScreenButton;
    [SerializeField]
    private Button windowedButton;

    // 전체화면, 창모드 버튼 이미지 변경용 변수
    // 모든 Sprite는 Inspector에서 끌어서 사용
    [Header("Sprite")]
    [SerializeField]
    private Sprite enabledSprite;
    [SerializeField]
    private Sprite disabledSprite;

    [Header("Values")]
    public int setWidth = 1920; // 사용자 설정 너비
    public int setHeight = 1080; // 사용자 설정 높이

    [Header("Flags")]
    [SerializeField]
    private bool isAllowFullScreen = true;

    void Awake()
    {
        // 해상도 텍스트 설정
        if (resolutionText != null)
        {
            resolutionText.text = "";
        }
    }

    void Start()
    {
        int deviceWidth = Display.main.systemWidth;
        int deviceHeight = Display.main.systemHeight;

        // 이전 모드 불러오기
        int savedMode = PlayerPrefs.GetInt("FullScreenMode", 1); // 기본값: 전체화면

        // 16:9 비율 체크 (오차 허용 0.01)
        float aspectRatio = (float)deviceWidth / deviceHeight;
        bool is16by9 = Mathf.Abs(aspectRatio - (16f / 9f)) < 0.01f;

#if UNITY_STANDALONE_OSX
        // macOS에서 16:9가 아닌 경우 강제 창모드
        if (!is16by9)
        {
            Screen.SetResolution(setWidth, setHeight, false);
            Screen.fullScreenMode = FullScreenMode.Windowed;
            isAllowFullScreen = false;
            PlayerPrefs.SetInt("FullScreenMode", 0);
            PlayerPrefs.Save();
            SetFullScreenMode(false);
            Debug.Log($"macOS: 16:9가 아닌 화면 비율 감지 ({deviceWidth}x{deviceHeight}). 창모드로 전환.");
            return;
        }
#endif

        if (deviceWidth > setWidth || deviceHeight > setHeight)
        {
            Screen.SetResolution(setWidth, setHeight, false);
            Screen.fullScreenMode = FullScreenMode.Windowed;
            isAllowFullScreen = false;
            PlayerPrefs.SetInt("FullScreenMode", 0); // 강제로 창모드 저장
            PlayerPrefs.Save();
            SetFullScreenMode(false);
        }
        else
        {
#if UNITY_STANDALONE_OSX
            Screen.SetResolution(deviceWidth, deviceHeight, savedMode == 1);
#elif UNITY_STANDALONE_WIN
            Screen.SetResolution(deviceWidth, deviceHeight, savedMode == 1);
#else
            Screen.SetResolution(setWidth, setHeight, savedMode == 1);
#endif
            isAllowFullScreen = true;
            // 모드 적용
            if (savedMode == 1)
            {
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                Screen.fullScreen = true;
                SetFullScreenMode(true);
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.fullScreen = false;
                SetFullScreenMode(false);
            }
        }
    }

    public void SetFullScreen()
    {
        int deviceWidth = Display.main.systemWidth;
        int deviceHeight = Display.main.systemHeight;

#if UNITY_STANDALONE_OSX
        // macOS에서 16:9 비율 체크
        float aspectRatio = (float)deviceWidth / deviceHeight;
        bool is16by9 = Mathf.Abs(aspectRatio - (16f / 9f)) < 0.01f;
        
        if (!is16by9)
        {
            // 16:9가 아니면 전체화면 전환 불가
            StartCoroutine(FullScreenTextCoroutine());
            return;
        }
#endif

        // 1920x1080을 넘어서는 기기에서만 전체화면 전환 허용
        if (deviceWidth > setWidth || deviceHeight > setHeight)
        {
            Screen.SetResolution(deviceWidth, deviceHeight, true);
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            Screen.fullScreen = true;
            PlayerPrefs.SetInt("FullScreenMode", 1);
            PlayerPrefs.Save();
        }
        else
        {
            // 그 외에는 전체화면 전환 불가 메시지
            StartCoroutine(FullScreenTextCoroutine());
        }
    }

    // 창모드, 전체화면 설정 함수 (버튼에 할당)
    public void SetFullScreenMode(bool isFullScreen)
    {
        if (!isAllowFullScreen && isFullScreen)
        {
            StartCoroutine(FullScreenTextCoroutine());
            return;
        }

#if UNITY_STANDALONE_OSX
        // macOS에서 전체화면 시도 시 16:9 비율 체크
        if (isFullScreen)
        {
            int deviceWidth = Display.main.systemWidth;
            int deviceHeight = Display.main.systemHeight;
            float aspectRatio = (float)deviceWidth / deviceHeight;
            bool is16by9 = Mathf.Abs(aspectRatio - (16f / 9f)) < 0.01f;
            
            if (!is16by9)
            {
                StartCoroutine(FullScreenTextCoroutine());
                return;
            }
        }
#endif

        if (isFullScreen)
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            Screen.fullScreen = true;
            if (fullScreenButton != null && enabledSprite != null)
                fullScreenButton.image.sprite = enabledSprite;
            if (windowedButton != null && disabledSprite != null)
                windowedButton.image.sprite = disabledSprite;
            PlayerPrefs.SetInt("FullScreenMode", 1); // 전체화면 저장
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
            if (windowedButton != null && enabledSprite != null)
                windowedButton.image.sprite = enabledSprite;
            if (fullScreenButton != null && disabledSprite != null)
                fullScreenButton.image.sprite = disabledSprite;
            PlayerPrefs.SetInt("FullScreenMode", 0); // 창모드 저장
        }
        PlayerPrefs.Save();
    }

    private IEnumerator FullScreenTextCoroutine()
    {
        if (resolutionText != null)
        {
            resolutionText.text = "";
            yield return new WaitForSeconds(2f);
            resolutionText.text = "";
        }
    }
}
