using System;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 비디오 보스 컷씬을 관리하는 스크립트 입니다.
/// </summary>
public class BossCutScene : MonoBehaviour
{
    // 컷신 종료 이벤트
    public event Action OnMiddleBossCutsceneEnd;
    public event Action OnFinalBossCutsceneEnd;
    public event Action OnEndCutsceneFinished; 

    [Header("Middle Boss Cut Scene")]
    [SerializeField]
    private Canvas middleBossCanvas;
    public VideoPlayer middleBossVideoPlayer;

    [Header("Final Boss Cut Scene")]
    [SerializeField]
    private Canvas finalBossCanvas;
    public VideoPlayer finalBossVideoPlayer;

    [Header("EndCutScene")]
    [SerializeField]
    private Canvas endCutSceneCanvas;
    public VideoPlayer endCutSceneVideoPlayer;

    [Header("Flag")]
    private bool isMiddleBossCutScenePlaying = false;
    private bool isFinalBossCutScenePlaying = false;
    // 외부에서 컷씬 재생 시, 필요한 액션을 위한 public 변수
    // 현재 컷씬 재생 시 설정창을 못 열게 하기 위해 사용
    public bool isCutScenePlay = false;
    private bool isCutSceneSkip = false;

    void Awake()
    {
        if (middleBossCanvas != null) middleBossCanvas.gameObject.SetActive(false);
        if (finalBossCanvas != null) finalBossCanvas.gameObject.SetActive(false);
        if (endCutSceneCanvas != null) endCutSceneCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        // 스킵 로직은 컷씬이 재생 중일 때만 작동하도록 변경
        if (Input.GetKeyDown(KeyCode.Space) && !isCutSceneSkip)
        {
            if (isMiddleBossCutScenePlaying)
            {
                isCutSceneSkip = true;
                OnMiddleBossCutSceneComplete();
            }
            else if (isFinalBossCutScenePlaying)
            {
                isCutSceneSkip = true;
                OnFinalBossCutSceneComplete();
            }
        }
    }

    // 중간 보스 컷씬 시작
    public void StartMiddleBossCutsceneVideo()
    {
        isMiddleBossCutScenePlaying = true;

        if (middleBossVideoPlayer != null)
        {
            // video 재생의 끝을 확인하기 위해 loopPointReached 이벤트에 구독
            middleBossVideoPlayer.loopPointReached -= OnMiddleBossVideoEnd;
            middleBossVideoPlayer.loopPointReached += OnMiddleBossVideoEnd;

            middleBossVideoPlayer.Play();

            if (middleBossCanvas != null) middleBossCanvas.gameObject.SetActive(true);
            PauseManager.AddPause();
        }
    }

    // 중간 보스 컷씬 종료 처리, videoPlayer 이벤트에서 호출
    private void OnMiddleBossVideoEnd(VideoPlayer vp)
    {
        OnMiddleBossCutSceneComplete();
    }

    // 중간 보스 컷씬 완료 처리
    private void OnMiddleBossCutSceneComplete()
    {
        if (!isMiddleBossCutScenePlaying) return; // 중복 호출 방지

        middleBossVideoPlayer.Stop();
        isCutScenePlay = false;
        isMiddleBossCutScenePlaying = false;
        PauseManager.RemovePause();
        if (middleBossCanvas != null) middleBossCanvas.gameObject.SetActive(false);

        // 중간 보스 컷씬 종료 이벤트
        OnMiddleBossCutsceneEnd?.Invoke();

        isCutSceneSkip = false;
    }

    // 최종 보스 컷씬 시작
    public void StartFinalBossCutsceneVideo()
    {
        isFinalBossCutScenePlaying = true;

        if (finalBossVideoPlayer != null)
        {
            // video 재생의 끝을 확인하기 위해 loopPointReached 이벤트에 구독
            finalBossVideoPlayer.loopPointReached -= OnFinalBossVideoEnd;
            finalBossVideoPlayer.loopPointReached += OnFinalBossVideoEnd;

            finalBossVideoPlayer.Play();

            if (finalBossCanvas != null) finalBossCanvas.gameObject.SetActive(true);
            PauseManager.AddPause();
        }
    }

    // 최종 보스 컷씬 종료 처리, videoPlayer 이벤트에서 호출
    private void OnFinalBossVideoEnd(VideoPlayer vp)
    {
        OnFinalBossCutSceneComplete();
    }

    // 최종 보스 컷씬 완료 처리
    private void OnFinalBossCutSceneComplete()
    {
        if (!isFinalBossCutScenePlaying) return; // 중복 호출 방지

        finalBossVideoPlayer.Stop();
        isCutScenePlay = false;
        isFinalBossCutScenePlaying = false;
        PauseManager.RemovePause();
        if (finalBossCanvas != null) finalBossCanvas.gameObject.SetActive(false);

        // 최종 보스 컷씬 종료 이벤트
        OnFinalBossCutsceneEnd?.Invoke();

        isCutSceneSkip = false;
    }

    // 엔딩 컷씬 재생
    public void PlayEndCutScene()
    {
        PauseManager.AddPause();
        if (endCutSceneCanvas != null) endCutSceneCanvas.gameObject.SetActive(true);

        if (endCutSceneVideoPlayer != null)
        {
            // video 재생의 끝을 확인하기 위해 loopPointReached 이벤트에 구독
            endCutSceneVideoPlayer.loopPointReached -= OnEndCutSceneVideoEnd;
            endCutSceneVideoPlayer.loopPointReached += OnEndCutSceneVideoEnd;
            endCutSceneVideoPlayer.Play();
        }
    }

    // 엔딩 컷씬 종료 처리, videoPlayer 이벤트에서 호출
    private void OnEndCutSceneVideoEnd(VideoPlayer vp)
    {
        OnEndCutsceneFinished?.Invoke();
    }
}
