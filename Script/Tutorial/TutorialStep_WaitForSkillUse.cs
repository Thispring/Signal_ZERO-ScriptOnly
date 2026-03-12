using UnityEngine;
using System.Collections;

/// <summary>
/// 보조 로봇의 특정 스킬 사용을 감지할 때까지 대기하는 튜토리얼 단계입니다.
/// 실제 스킬 사용 감지를 위한 참조가 필요합니다.
/// </summary>
[RequireComponent(typeof(DialogSystem))]
public class TutorialStep_WaitForSkillUse : TutorialStep
{
    // 캐릭터들의 대사를 진행하는 DialogSystem
    private DialogSystem dialogSystem;

    [Header("Flags")]
    private bool isDialog = false;
    private bool isDialogFinished = false;

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
        isFinished = false;
        isDialog = false;
        isDialogFinished = false;
        dialogSystem = GetComponent<DialogSystem>();
    }

    public override void Execute(TutorialController controller)
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            controller.SetNextTutorial();
            return;
        }

        // 재소환된 적의 FSM을 활성화합니다.
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

        // 대기 후, 대사 출력하는 코루틴 실행
        if (!isDialog)
        {
            isDialog = true;
            StartCoroutine(DialogSkillSystem());
        }

        // 이벤트 기반으로 처리하는 것이 더 효율적이므로,
        // Execute는 비워두거나 폴백(Fallback) 로직을 넣을 수 있습니다.
        // 임시로 키 입력을 통해 스킬 사용을 시뮬레이션합니다.
        if (isDialog && isDialogFinished && Input.GetKeyDown(KeyCode.D)) // D키를 스킬 사용 키로 설정
        {
            PauseManager.RemovePause();
            StartCoroutine(WaitForSkillUseCoroutine());
        }

        if (isFinished)
        {
            controller.SetNextTutorial();
        }
    }

    public override void Skip()
    {
        base.Skip();
        // 코루틴 중단
        StopAllCoroutines();
        // 대화 시스템 정리
        if (dialogSystem != null)
        {
            dialogSystem.OffDialog(0);
        }
    }

    public override void Exit()
    {
        if (dialogSystem != null)
        {
            dialogSystem.OffDialog(0);
        }
    }

    private IEnumerator WaitForSkillUseCoroutine()
    {
        float elapsed = 0f;
        while (ShouldContinue() && elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!ShouldContinue()) yield break;

        isFinished = true;
    }

    private IEnumerator DialogSkillSystem()
    {
        // 2초 대기 후 대사 재생
        float elapsed = 0f;
        while (ShouldContinue() && elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!ShouldContinue()) yield break;

        PauseManager.AddPause();
        dialogSystem.PlayDialogRange(0, 1, () =>
        {
            if (ShouldContinue())
            {
                isDialogFinished = true;
            }
        });
    }
}
