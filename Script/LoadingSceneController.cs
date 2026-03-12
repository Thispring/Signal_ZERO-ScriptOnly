using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 로딩 씬을 관리하는 스크립트입니다.
/// </summary>
/// 
/// <remarks>
/// Slider를 이용해 로딩 진행률을 표시합니다.
/// LoadingScene에 사용 중인 Video 재생은 CutSceneManager에서 관리합니다.
/// </remarks>
public class LoadingSceneController : MonoBehaviour
{
    [Header("String")]
    private static string nextScene;    // Scene 이름을 저장하는 변수

    /// 현재 Video 재생으로 화면을 구성하고 있어, 실질적으로 Slider가 필요없지만
    /// 추후 로딩 진행도를 시각적으로 표시할 때 사용할 수 있도록 남겨둠
    [Header("UI")]
    [SerializeField]
    private Slider progressBar;

    [Header("Flags")]
    private bool loadingComplete = false;
    private bool canTransition = false; // 씬 전환 가능 플래그

    // string 타입의 nextScene 변수를 설정하여, 로드 할 씬을 설정합니다.
    public static void LoadScene(string sceneName)
    {
        nextScene = sceneName;
        SceneManager.LoadScene("GameScene");
    }

    void Start()
    {
        nextScene = nextScene == null ? "GameScene" : nextScene;
        // 코루틴을 통해 비동기 씬 로드 시작
        StartCoroutine(LoadSceneProcess());
    }

    void Update()
    {
        // 로딩이 완료되고 스페이스 키를 누르면 씬 전환
        if (loadingComplete && Input.GetKeyDown(KeyCode.Space))
        {
            canTransition = true;
        }
    }

    private IEnumerator LoadSceneProcess()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false; // 자동 씬 전환 방지

        float timer = 0f;
        while (!op.isDone)
        {
            yield return null;

            if (op.progress < 0.9f)
            {
                progressBar.value = op.progress; // 로딩 진행률 업데이트
            }
            else
            {
                // 게임이 일시정지되거나 타임스케일이 변경되더라도 로딩을 진행하기위해 unscaledDeltaTime 사용
                timer += Time.unscaledDeltaTime;
                progressBar.value = Mathf.Lerp(0.9f, 1f, timer); // 마지막 1초 동안 부드럽게 채움
                if (progressBar.value >= 1f)
                {
                    loadingComplete = true;

                    // 스페이스 키 입력 또는 영상 완료를 기다림
                    yield return new WaitUntil(() => canTransition);

                    // 씬 전환 허용
                    op.allowSceneActivation = true;
                }
            }
        }
    }
}
