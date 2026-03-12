using UnityEngine;

/// <summary>
/// 모든 튜토리얼 단계의 기반이 되는 추상 클래스입니다.
/// 각 튜토리얼 단계는 이 클래스를 상속받아 고유한 로직을 구현합니다.
/// </summary>
public abstract class TutorialStep : MonoBehaviour
{
    [Tooltip("이 단계에 대한 설명 (인스펙터에서 확인용)")]
    [SerializeField, TextArea] private string description;

    [Header("Skip Control")]
    public bool isSkipped = false; // 스킵 플래그

    /// <summary>
    /// 이 단계의 목표가 완료되었는지 여부를 반환합니다.
    /// TutorialController는 이 값이 true가 되면 다음 단계로 진행합니다.
    /// </summary>
    public abstract bool IsFinished { get; }

    /// <summary>
    /// 이 단계가 시작될 때 1회 호출됩니다.
    /// 상태 초기화, UI 표시 등의 로직을 여기에 구현합니다.
    /// </summary>
    public abstract void Enter();

    /// <summary>
    /// 이 단계가 활성화된 동안 매 프레임 호출됩니다.
    /// 플레이어의 입력이나 상태 변화를 감지하는 로직을 여기에 구현합니다.
    /// </summary>
    public virtual void Execute(TutorialController controller) { }

    /// <summary>
    /// 이 단계가 종료되고 다음 단계로 넘어갈 때 1회 호출됩니다.
    /// 사용했던 리소스를 정리하는 로직을 여기에 구현합니다.
    /// </summary>
    public virtual void Exit() { }

    /// <summary>
    /// 튜토리얼 스킵 시 호출되는 메서드입니다.
    /// 진행 중인 모든 작업을 중단하고 정리합니다.
    /// </summary>
    public virtual void Skip()
    {
        isSkipped = true;
        Exit(); // 기본적으로 Exit 호출
    }

    /// <summary>
    /// 스킵 상태를 확인하는 헬퍼 메서드입니다.
    /// 코루틴이나 반복 작업에서 계속 진행할지 확인할 때 사용합니다.
    /// </summary>
    protected bool ShouldContinue()
    {
        return !isSkipped;
    }
}
