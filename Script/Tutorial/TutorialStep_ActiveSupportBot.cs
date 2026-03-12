using UnityEngine;

/// <summary>
/// 보조 로봇을 활성화하는 튜토리얼 단계입니다.
/// </summary>
public class TutorialStep_ActiveSupportBot : TutorialStep
{
    [SerializeField]    // Inspector에서 직접 할당
    private SupportBotStatusManager supportBotStatusManager;
    
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

        isFinished = false;

        // 보조 로봇 상태 매니저를 찾아서 튜토리얼 활성화 설정
        if (supportBotStatusManager != null)
        {
            supportBotStatusManager.isTutorialActive = true;
        }
        else
        {
            Debug.LogError("SupportBotStatusManager를 찾을 수 없습니다.");
        }
    }

    public override void Execute(TutorialController controller)
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            controller.SetNextTutorial();
            return;
        }

        controller.SetNextTutorial();
        // 이 단계는 Enter에서 즉시 완료되므로 Execute에서는 별도의 처리가 필요 없습니다.
    }

    public override void Exit()
    {

    }
}
