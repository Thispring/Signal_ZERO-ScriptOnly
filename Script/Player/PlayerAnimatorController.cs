using UnityEngine;

/// <summary>
/// Player의 애니메이션에 관련된 스크립트 입니다.
/// 모든 Player에 관련한 스크립트는 Player을 앞에 붙여줍니다.
/// </summary>
public class PlayerAnimatorController : MonoBehaviour
{
    [SerializeField]    // Inspector 할당
    private Animator animator;

    public void OnReload() 
    {
        animator.SetTrigger("onReload");
    }

    public void OffReload()
    {
        animator.ResetTrigger("onReload");
    }

    // NOTE: 추후 애니메이션 동작 수정 시 사용
    public bool CurrentAnimationIs(string name) // name 애니메이션을 받고
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(name);    // 해당 애니메이션이 재생 중인지 확인 후 반환
    }

    // 애니메이션 상태를 bool로 제어
    public void SetAnimationBool(string parameterName, bool value)
    {
        animator.SetBool(parameterName, value); // Animator의 Bool 파라미터 설정
    }
}
