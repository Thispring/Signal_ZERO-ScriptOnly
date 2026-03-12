using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 인트로 컷씬을 관리하는 스크립트 입니다.
/// </summary>
public class IntroCutScene : MonoBehaviour
{
    [Header("Video Player")]
    [SerializeField]
    private VideoPlayer introVideoPlayer;

    void Awake()
    {
        introVideoPlayer.gameObject.SetActive(false);
    }

    void Start()
    {
        introVideoPlayer.gameObject.SetActive(true);

        if (introVideoPlayer != null)
        {
            introVideoPlayer.Play();
        }

        introVideoPlayer.loopPointReached += OnVideoEnd; // 비디오 종료 이벤트 등록
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        // 비디오가 끝나면 GameScene으로 전환
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}
