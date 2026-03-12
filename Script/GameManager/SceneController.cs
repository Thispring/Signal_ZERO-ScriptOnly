using UnityEngine;

/// <summary>
/// 전체 게임의 Scene을 관리하는 스크립트입니다.
/// </summary>
/// 
/// <remarks>
/// string으로 씬 이름을 받아 해당 씬으로 전환합니다.
/// </remarks>
public class SceneController : MonoBehaviour
{
    void Awake()
    {
        // timeScale을 1로 설정하여, 게임 재개
        Time.timeScale = 1;
    }

    void OnEnable()
    {
        // 플레이어 사망 이벤트 구독
        PlayerStatusManager.OnPlayerDied += LoseScene;
    }

    void OnDisable()
    {
        // 플레이어 사망 이벤트 구독 해제
        PlayerStatusManager.OnPlayerDied -= LoseScene;
    }

    public void MainMenu()  // Button OnClick 이벤트에 할당하여 사용
    {
        PauseManager.ResetPause();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void GameScene() // Button OnClick 이벤트에 할당하여 사용
    {
        LoadingSceneController.LoadScene("GameScene"); // 로딩 씬을 통해 게임 씬으로 전환
    }

    public void LoadingScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoadingScene"); 
    }

    public void WinScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Win"); 
    }

    public void LoseScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lose"); 
    }
}
