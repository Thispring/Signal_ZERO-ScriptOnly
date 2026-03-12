using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 인게임 스테이지를 관리하는 스크립트입니다.
/// </summary>
public class StageManager : MonoBehaviour
{
    // Enemy 등 직접 참조가 어려운 경우 Instance를 통해 접근합니다.
    public static StageManager Instance { get; private set; }

    /// 스테이지가 끝난 후의 행동을 이벤트로 정의하여 사용합니다.
    /// onPauseGameEvent -> 각 스테이지가 끝나고, 상점 단계로 이동
    /// BossStageStart -> 컷씬이 끝나고, 보스의 소환 담당
    [Header("Events")]
    public UnityEvent onPauseGameEvent;
    public static event System.Action OnMiddleBossStageStart;
    public static event System.Action OnFinalBossStageStart;

    [Header("References")]
    public SceneController sceneController;
    [SerializeField]
    private GameSetting gameSetting;        // 인게임에서 커서 활성화를 관리하기 위해 참조 
    [SerializeField]
    private BossCutScene bossCutScene;

    [Header("Values")]
    public int stageNumber;

    [Header("Flags")]
    public bool isStart = false; // 스테이지 시작 여부, false는 스테이지 시작 전, true는 스테이지 시작 후 입니다.
    public bool isFirstStage = false; // 1스테이지에서 설정 창 활성 용
    public bool isMiddleBoss = false; // 중간 보스 스테이지 시작 여부
    public bool isFinalBoss = false; // 최종 보스 스테이지 시작 여부

    [Header("Player Info")]
    [SerializeField]    // Inspector에서 끌어서 사용
    private PlayerStatusManager playerStatusManager;    // Player의 버스트와 코인 관리를 위해 참조
    [SerializeField]    // Inspector에서 끌어서 사용
    private ShopManager shopManager;
    [SerializeField]    // Inspector에서 끌어서 사용
    private Canvas shopCanvas;

    [Header("Audio")]
    public AudioSource stageAudio;
    [SerializeField]
    private AudioClip[] audioClips;
    private int lastStageAudioIndex = -1; // 마지막으로 재생한 오디오 클립 인덱스
    private int audioIndex = -1;

    [Header("World Map")]
    [SerializeField]
    private GameObject[] worldMaps; // 스테이지에 따른 월드맵 오브젝트
    // [0]: 스테이지 0~4, [1]: 스테이지 5~9, [2]: 스테이지 10

    void Awake()
    {
        // BossCutScene의 이벤트 구독
        if (bossCutScene != null)
        {
            bossCutScene.OnMiddleBossCutsceneEnd += HandleMiddleBossCutsceneEnd;
            bossCutScene.OnFinalBossCutsceneEnd += HandleFinalBossCutsceneEnd;
            bossCutScene.OnEndCutsceneFinished += HandleEndCutsceneFinished;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        // 스테이지 정보 초기화
        // 튜토리얼을 위해 0으로 초기화 후, 튜토리얼이 끝나면 1로 변경
        stageNumber = 0;
        isStart = true;
        isFirstStage = false;
        isMiddleBoss = false;
        isFinalBoss = false;
        shopCanvas.gameObject.SetActive(false);
        // UnityEvent에 PauseGame 함수 등록 
        onPauseGameEvent.AddListener(PauseGame);

        if (worldMaps != null)
        {
            for (int i = 0; i < worldMaps.Length; i++)
            {
                worldMaps[i].SetActive(false);
            }
        }
    }

    // 이벤트 구독 해제
    void OnDestroy()
    {
        if (bossCutScene != null)
        {
            bossCutScene.OnMiddleBossCutsceneEnd -= HandleMiddleBossCutsceneEnd;
            bossCutScene.OnFinalBossCutsceneEnd -= HandleFinalBossCutsceneEnd;
            bossCutScene.OnEndCutsceneFinished -= HandleEndCutsceneFinished;
        }
    }

    void Start()
    {
        CheckAudioAndWorldMap();
    }

    //private bool isTest = false; // 중간 보스 테스트 용 변수
    //private bool isFinalTest = false; // 최종 보스 테스트 용 변수
    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.M) && !isTest)
        {
            // 테스트: 중간 보스 강제 플래그 세팅 후 이벤트 호출
            stageNumber = 5;
            isMiddleBoss = true;
            isFinalBoss = false;

            // BossStatusManager의 중간 보스 활성화 메서드 호출
            bossSpawnManager.HandleMiddleBossStageStart_FromStageManager();
            isTest = true;
            Debug.Log("Boss Stage Start Triggered");
        }

        if (Input.GetKeyDown(KeyCode.K) && !isFinalTest)
        {
            // 테스트: 최종 보스 강제 플래그 세팅 후 이벤트 호출
            stageNumber = 10;
            isFinalBoss = true;
            isMiddleBoss = false;

            // BossStatusManager의 최종 보스 활성화 메서드 호출
            bossSpawnManager.HandleFinalBossStageStart_FromStageManager();
            isFinalTest = true;
            Debug.Log("Final Boss Stage Start Triggered");
        }
        */

        // 오디오 클립이 바뀌어야 할 때만 교체 및 재생
        if (audioIndex != -1 && audioClips.Length > audioIndex)
        {
            if (lastStageAudioIndex != audioIndex)
            {
                stageAudio.Stop();
                stageAudio.clip = audioClips[audioIndex];
                stageAudio.Play();
                lastStageAudioIndex = audioIndex;
            }
        }

        // 게임이 시작되면 커서 비활성화
        if (!isStart || gameSetting.isSetOn)
        {
            Cursor.visible = true;  // 커서 보임
        }
        else
        {
            Cursor.visible = false; // 커서 숨김
        }
    }

    public void PauseGame() // 스테이지 정지 함수
    {
        if (stageNumber == 4 && !isMiddleBoss)
        {
            isMiddleBoss = true;
        }
        else if (stageNumber == 5)
        {
            isMiddleBoss = false;
        }

        if (stageNumber == 9 && !isFinalBoss)
        {
            isFinalBoss = true;
        }
        else if (stageNumber == 10)
        {
            isFinalBoss = false;
        }

        // 스테이지 10 클리어 시 사운드 정지 및 컷신 재생
        if (stageNumber == 10)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopAllSoundEffects();
            }
            stageAudio.Stop();
            bossCutScene.PlayEndCutScene();
        }

        // isStart가 true이면 게임이 진행되었다는 뜻
        // isStart를 false로 변경하고, 상점 단계 진입
        if (isStart == true)
        {
            isStart = false;
            PauseManager.AddPause();
            playerStatusManager.isBurstActive = false; // 버스트 비활성화
            shopCanvas.gameObject.SetActive(true);
            shopManager.saveCoin(playerStatusManager.coin);
            shopManager.openShop();
            onPauseGameEvent.Invoke();
        }
    }

    public void ResumeGame()    // 스테이지 재개 함수
    {
        // 튜토리얼 스테이지
        if (stageNumber == 0)
        {
            isStart = true;
            PauseManager.RemovePause();
            shopCanvas.gameObject.SetActive(false);
        }

        if (isStart == false)    // 스테이지가 정지되었을 때만 발동
        {
            // 스테이지 다시 시작 시 보조로봇 수리
            if (SupportBotStatusManager.Instance != null)
            {
                SupportBotStatusManager.Instance.Repair();
            }

            // 스테이지가 5인 경우 Boss 스테이지 시작 이벤트 호출
            if (stageNumber == 5 && isMiddleBoss)
            {
                // 상점 캔버스만 닫고, 컷신을 시작합니다.
                shopCanvas.gameObject.SetActive(false);
                stageAudio.Pause();
                if (bossCutScene != null)
                {
                    bossCutScene.StartMiddleBossCutsceneVideo();
                }
                StageTextFade.Instance.StartTextFade("MIDDLE BOSS", 2f);
            }
            else if (stageNumber == 10 && isFinalBoss)
            {
                // 상점 캔버스만 닫고, 컷신을 시작합니다.
                shopCanvas.gameObject.SetActive(false);
                stageAudio.Pause();
                if (bossCutScene != null)
                {
                    bossCutScene.StartFinalBossCutsceneVideo();
                }
                StageTextFade.Instance.StartTextFade("FINAL BOSS", 2f);
            }
            else
            {
                isStart = true;
                PauseManager.RemovePause();
                shopCanvas.gameObject.SetActive(false);
                EnemyMemoryPool.onSpawnTileAction?.Invoke();    //  EnemyMemoryPool의 스폰타일 액션 호출
                // 스테이지 번호를 문자열로 변환하여 전달
                string info = stageNumber.ToString();
                StageTextFade.Instance.StartTextFade(info, 2f);
            }
        }
    }

    // 스테이지 번호에 따른 오디오 및 월드맵 활성화 체크
    private void CheckAudioAndWorldMap()
    {
        switch (stageNumber)
        {
            case >= 0 and <= 4:
                audioIndex = 0;
                worldMaps[0].SetActive(true);

                worldMaps[1].SetActive(false);
                worldMaps[2].SetActive(false);
                break;
            case >= 5 and <= 9:
                audioIndex = 1;
                worldMaps[1].SetActive(true);

                worldMaps[0].SetActive(false);
                worldMaps[2].SetActive(false);
                break;
            case 10:
                audioIndex = 2;
                worldMaps[2].SetActive(true);

                worldMaps[1].SetActive(false);
                worldMaps[0].SetActive(false);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 컷씬 관련 이벤트 핸들러
    /// </summary>
    // 중간 보스 컷씬이 종료되었을 때 호출될 메서드
    private void HandleMiddleBossCutsceneEnd()
    {
        CheckAudioAndWorldMap();

        // 컷신 종료 시 BGM 재생
        if (stageAudio != null)
            stageAudio.UnPause();

        // 보스 스테이지 시작 이벤트 발행
        OnMiddleBossStageStart?.Invoke();

        // 적 생성 시작
        EnemyMemoryPool.onSpawnTileAction?.Invoke();
        isStart = true;
    }

    // 최종 보스 컷씬이 종료되었을 때 호출될 메서드
    private void HandleFinalBossCutsceneEnd()
    {
        CheckAudioAndWorldMap();

        // 컷신 종료 시 BGM 재생
        if (stageAudio != null)
            stageAudio.UnPause();

        // 보스 스테이지 시작 이벤트 발행
        OnFinalBossStageStart?.Invoke();

        // 적 생성 시작
        EnemyMemoryPool.onSpawnTileAction?.Invoke();
        isStart = true;
    }

    // 엔딩 컷씬이 종료되었을 때 호출될 메서드
    private void HandleEndCutsceneFinished()
    {
        // 씬 컨트롤러를 통해 WinScene을 로드합니다.
        sceneController.WinScene();
    }
}
