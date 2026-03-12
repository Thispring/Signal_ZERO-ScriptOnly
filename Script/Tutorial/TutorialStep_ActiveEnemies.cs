using UnityEngine;

/// <summary>
/// 소환된 적들을 활성화하는 튜토리얼 단계입니다.
/// </summary>
public class TutorialStep_ActiveEnemies : TutorialStep
{
    [Header("References")]
    [SerializeField]    // Inspector에서 직접 할당
    private PlayerShotController playerShotController;
    [SerializeField]    // Inspector에서 직접 할당
    private EnemyMemoryPool enemyMemoryPool;

    [Header("Flags")]
    private bool isFinished = false;
    public override bool IsFinished => isFinished;

    public override void Enter()
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            isFinished = true;
            return;
        }

        PauseManager.RemovePause();
        playerShotController.isTutorialAction = true; // 튜토리얼 모드 활성화
        isFinished = false;
    }

    public override void Execute(TutorialController controller)
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            controller.SetNextTutorial();
            return;
        }

        // 현재 활성화된 적을 확인하고, 모든 적이 처치되었는지 검사합니다.
        if (controller.lastSpawnedEnemies.Count == 0)
        {
            isFinished = true;
        }

        if (enemyMemoryPool.killScore >= 3)
        {
            enemyMemoryPool.killScore = 0; // 점수 초기화
            // 리스트 초기화
            controller.lastSpawnedEnemies.Clear();
            controller.SetNextTutorial();
        }

        var list = controller.lastSpawnedEnemies;
        // 각 적에 대해 EnemyFSM을 찾아 동작/변수 변경
        for (int i = 0; i < list.Count; i++)
        {
            GameObject enemyGO = list[i];
            if (enemyGO == null) continue;

            EnemyFSM fsm = enemyGO.GetComponent<EnemyFSM>();
            if (fsm == null)
            {
                Debug.LogWarning($"{enemyGO.name}에 EnemyFSM이 없습니다.");
            }
            fsm.isTutorialSpawn = true; // 튜토리얼용 변수 활성화
        }
    }

    public override void Exit()
    {
        // 단계 종료 시 필요한 정리 작업을 수행합니다.
    }
}
