using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 버스트 컷씬을 관리하는 스크립트 입니다.
/// </summary>
public class BurstCutScene : MonoBehaviour
{
    // 각 무기 클래스에 인스턴스를 할당하여, 컷씬 재생 함수를 실행합니다.
    public static BurstCutScene Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private SoundManager soundManager;

    // UI 요소 Inspector에서 할당
    [Header("UI")]
    [SerializeField]
    private Canvas burstCutSceneCanvas;
    [SerializeField]
    private Image[] burstCutSceneImages; // 3컷으로 되어 있어, 배열 사용
    [SerializeField]
    private Image burstActiveImage;

    [Header("Values")]
    private float waitTime;

    [Header("Flags")]
    // 첫 재생 여부 이후, 컷씬 재생 시 대기 시간 단축
    private bool isFirstPlay = false;

    // Audio 요소 Inspector에서 할당
    [Header("Audio")]
    [SerializeField]
    private AudioSource burstAudioSource;
    [SerializeField]
    private AudioSource burstVoiceAudioSource;
    [SerializeField]
    private AudioClip burstSoundClip;
    [SerializeField]
    private AudioClip burstVoiceClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        waitTime = 0.5f;
        isFirstPlay = false;

        if (burstActiveImage != null)
            burstActiveImage.gameObject.SetActive(false);

        if (burstCutSceneCanvas != null)
            burstCutSceneCanvas.gameObject.SetActive(false);

        if (burstCutSceneImages != null)
        {
            for (int i = 0; i < burstCutSceneImages.Length; i++)
            {
                if (burstCutSceneImages[i] != null)
                    burstCutSceneImages[i].gameObject.SetActive(false);
            }
        }

        if (soundManager != null)
        {
            soundManager.RegisterSoundEffect(burstAudioSource, 0);
            soundManager.RegisterSoundEffect(burstVoiceAudioSource, 1);
        }
    }

    // 외부에서 호출하여 버스트 컷씬 시작
    public void PlayBurstCutScene()
    {
        // 첫 재생 이후 시간 단축
        if (isFirstPlay)
            waitTime = 0.5f;

        burstAudioSource.PlayOneShot(burstSoundClip);
        burstVoiceAudioSource.PlayOneShot(burstVoiceClip);
        StartCoroutine(StartBurstCutScene());
    }

    private IEnumerator StartBurstCutScene()
    {
        float time = 0.3f;

        PauseManager.AddPause();

        if (burstCutSceneCanvas != null)
            burstCutSceneCanvas.gameObject.SetActive(true);

        if (burstCutSceneImages != null && !isFirstPlay)
        {
            // 이미지 순차적 활성화
            // 등장 시간을 조정하려면 time 변수 조정
            for (int i = 0; i < burstCutSceneImages.Length; i++)
            {
                if (burstCutSceneImages[i] != null)
                    burstCutSceneImages[i].gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(time);
                time -= 0.1f;
                if (i <= 1)
                    time = 0.5f;
            }
        }
        else
        {
            for (int i = 0; i < burstCutSceneImages.Length; i++)
            {
                if (burstCutSceneImages[i] != null)
                    burstCutSceneImages[i].gameObject.SetActive(true);
            }
        }

        yield return new WaitForSecondsRealtime(waitTime);
        burstCutSceneCanvas.gameObject.SetActive(false);

        if (!isFirstPlay)
            isFirstPlay = true;

        PauseManager.RemovePause();

        yield return null;
    }

    // 버스트 상태임을 알리는 UI 이미지 표시
    public void ShowBurstActiveImage(bool isActive)
    {
        if (burstActiveImage != null)
        {
            burstActiveImage.gameObject.SetActive(true);
            StartCoroutine(HideBurstActiveImageAfterDelay(isActive));
        }
    }

    private IEnumerator HideBurstActiveImageAfterDelay(bool isActive)
    {
        if (!isActive)
        {
            // isActive가 false면 바로 비활성화
            if (burstActiveImage != null)
                burstActiveImage.gameObject.SetActive(false);
            yield break;
        }
    }
}
