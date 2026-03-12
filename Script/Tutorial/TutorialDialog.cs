using UnityEngine;
using System.Collections;

/// <summary>
/// 튜토리얼 단계에서 대사 시퀀스를 관리하는 스크립트 입니다.
/// </summary>
[RequireComponent(typeof(DialogSystem))]
public class TutorialDialog : TutorialStep
{
    // 캐릭터들의 대사를 진행하는 DialogSystem
    private DialogSystem dialogSystem;

    [Header("Flags")]
    private bool isWaiting = false;
    
    private bool isFinished = false;
    public override bool IsFinished => isFinished;

    private Coroutine waitCoroutine; // 코루틴 참조
	
    public override void Enter()
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            isFinished = true;
            return;
        }

        PauseManager.AddPause();
        dialogSystem = GetComponent<DialogSystem>(); 
        dialogSystem.Setup();
        isWaiting = false;
    }

    public override void Execute(TutorialController controller)
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            controller.SetNextTutorial();
            return;
        }

        // 현재 분기에 진행되는 대사 진행
        bool isCompleted = dialogSystem.UpdateDialog();

        if (isCompleted && !isWaiting)
        {
            isWaiting = true;
            waitCoroutine = dialogSystem.StartCoroutine(WaitAndNext(controller));
        }
    }

    // 대사 완료 후 일정 시간 대기 후 다음 튜토리얼 단계로 이동
    private IEnumerator WaitAndNext(TutorialController controller)
    {
        float elapsed = 0f;
        while (ShouldContinue() && elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!ShouldContinue()) yield break;

        controller.SetNextTutorial();
    }

    public override void Skip()
    {
        base.Skip();
        // 코루틴 중단
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
        // 대화 시스템 정리
        if (dialogSystem != null)
        {
            dialogSystem.OffDialog(0);
        }
        // 일시정지 해제
        PauseManager.RemovePause();
    }

    public override void Exit() 
    {
        if (dialogSystem != null)
        {
            dialogSystem.OffDialog(0);
        }
        PauseManager.RemovePause();
    }
}
