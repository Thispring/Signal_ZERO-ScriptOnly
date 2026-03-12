using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼 전체를 관리하는 컨트롤러 스크립트 입니다.
/// </summary>
public class TutorialController : MonoBehaviour
{
    // EnemyFSM의 변수 접근을 위한 싱글톤
    public static TutorialController Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private PlayerStatusManager playerStatusManager;
    [SerializeField]
    private PlayerShotController playerShotController;
    [SerializeField]
    private SupportBotStatusManager supportBotStatusManager;
    [SerializeField]
    private StageManager stageManager;
    [SerializeField]
    private EnemyMemoryPool enemyMemoryPool;

    [Header("Tutorial Settings")]
    [SerializeField]
    private List<TutorialStep> tutorials;
    private TutorialStep currentTutorial = null;
    public List<GameObject> lastSpawnedEnemies = new List<GameObject>();
    private int currentIndex = -1;
    public int CurrentIndex => currentIndex;
    public int lastSpawnedCount = 0;
    public bool isTutorialClear = false;

    [Header("Canvas")]
    [SerializeField]
    private GameObject tutorialCanvas;

    void Awake()
    {
        isTutorialClear = false;
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(true); // 튜토리얼 캔버스 활성화
        }
    }

    void Start()
    {
        SetNextTutorial();
    }

    private bool isTest = false;

    void Update()
    {
        // TEST: P키를 누르면 튜토리얼 스킵
        if (!isTest && Input.GetKeyDown(KeyCode.P))
        {
            isTest = true;
            SkipAllTutorials();
        }

        if (currentTutorial != null && !currentTutorial.isSkipped)
        {
            currentTutorial.Execute(this);
        }
    }

    public void RegisterSpawnedEnemies(List<GameObject> spawned)
    {
        lastSpawnedEnemies = spawned != null ? new List<GameObject>(spawned) : new List<GameObject>();
        lastSpawnedCount = lastSpawnedEnemies.Count;
    }

    public void SetNextTutorial()
    {
        if (currentTutorial != null)
        {
            currentTutorial.Exit();
        }

        if (currentIndex >= tutorials.Count - 1)
        {
            CompletedAllTutorials();
            return;
        }

        currentIndex++;

        currentTutorial = tutorials[currentIndex];

        // 스킵된 상태라면 바로 다음 단계로
        if (currentTutorial.isSkipped)
        {
            SetNextTutorial();
            return;
        }

        currentTutorial.Enter();
    }

    // 모든 튜토리얼을 스킵하는 메서드
    public void SkipAllTutorials()
    {
        tutorialCanvas.SetActive(false); // 튜토리얼 캔버스 비활성화

        // 모든 튜토리얼 단계에 스킵 플래그 설정
        for (int i = 0; i < tutorials.Count; i++)
        {
            if (tutorials[i] != null)
            {
                tutorials[i].isSkipped = true;
            }
        }

        // 현재 실행 중인 튜토리얼 스킵 처리
        if (currentTutorial != null)
        {
            currentTutorial.Skip();
        }

        // 튜토리얼 완료 처리
        CompletedAllTutorials();
    }

    // 모든 튜토리얼 단계를 완료했을 때 호출되는 함수
    public void CompletedAllTutorials()
    {
        // 이미 튜토리얼이 완료된 상태라면 중복 실행 방지
        if (isTutorialClear)
        {
            Debug.Log("튜토리얼이 이미 완료된 상태입니다.");
            return;
        }

        // 현재 진행 중인 튜토리얼 단계가 있다면 안전하게 종료
        if (currentTutorial != null)
        {
            try
            {
                currentTutorial.Exit(); // 현재 튜토리얼 단계 정리
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"튜토리얼 단계 종료 중 오류 발생: {e.Message}");
            }
            currentTutorial = null;
        }

        // 남은 모든 튜토리얼 단계들을 안전하게 정리
        CleanupAllTutorialSteps();

        // 튜토리얼 완료 상태로 설정
        isTutorialClear = true;
        currentIndex = tutorials.Count; // 인덱스를 마지막으로 설정

        // 게임 상태 초기화
        ResetGameStateAfterTutorial();

        Debug.Log("튜토리얼이 완전히 스킵되었습니다.");
    }

    // 모든 튜토리얼 단계를 안전하게 정리하는 헬퍼 메서드
    private void CleanupAllTutorialSteps()
    {
        // 생성된 적들 정리
        if (lastSpawnedEnemies != null && lastSpawnedEnemies.Count > 0)
        {
            foreach (GameObject enemy in lastSpawnedEnemies)
            {
                if (enemy != null)
                {
                    enemy.SetActive(false); // 오브젝트 풀 방식이므로 비활성화
                }
            }
            lastSpawnedEnemies.Clear();
            lastSpawnedCount = 0;
        }

        // 모든 튜토리얼 단계들의 정리 작업 수행
        for (int i = 0; i <= currentIndex && i < tutorials.Count; i++)
        {
            if (tutorials[i] != null)
            {
                try
                {
                    tutorials[i].Exit(); // 각 단계별 정리 작업
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"튜토리얼 단계 {i} 정리 중 오류 발생: {e.Message}");
                }
            }
        }
    }

    // 튜토리얼 완료 후 게임 상태를 초기화하는 헬퍼 메서드
    private void ResetGameStateAfterTutorial()
    {
        // 행동 양식이 여러 종류가 되었을 때 코드 추가 작성
        PauseManager.ResetPause(); // 모든 일시정지 해제

        if (playerStatusManager != null)
        {
            playerStatusManager.isTutorialDamageAction = false;
            playerStatusManager.isTutorialBurstAction = false;
            playerStatusManager.Heal(100); // 체력 완전 회복
        }

        if (playerShotController != null)
        {
            playerShotController.isTutorialAction = true;
            playerShotController.isTutorialBurstAction = true;
        }

        if (supportBotStatusManager != null)
        {
            supportBotStatusManager.Repair(); // 보조 로봇 수리
            supportBotStatusManager.isTutorialActive = true;
        }

        if (stageManager != null)
        {
            stageManager.stageNumber = 1; // 스테이지 번호 초기화
        }

        if (enemyMemoryPool != null)
        {
            enemyMemoryPool.Setup(); // 적 리스폰 시작
        }
    }
}
