using UnityEngine;

/// <summary>
/// Boss의 애니메이션에 관한 스크립트입니다.
/// 모든 Boss에 관련한 스크립트는 Boss를 앞에 붙여줍니다.
/// </summary>
public class BossAnimatorController : MonoBehaviour
{
    [Header("Animator")]
    private Animator animator;

    // 테스트를 위한 애니메이션 비활성화 bool 변수
    [Header("Animation Toggle")]
    [SerializeField]
    private bool animationsEnabled = true; // 모델만 있을 때 임시 비활성화 토글
    // true면 애니메이션 활성화, false면 비활성화
    private bool CanAnimate => animationsEnabled && animator != null && animator.isActiveAndEnabled;

    void Awake()
    {
        // 애니메이터는 자식 Prefab에 등록
        animator = GetComponentInChildren<Animator>();
    }

    // 외부에서 호출 할 수 있도록 메소드를 정의
    public void Play(string stateName, int layer, float normalizedTime)
    {
        //if (!CanAnimate) return;
        animator.Play(stateName, layer, normalizedTime);
    }

    // 사망 애니메이션
    public void isDeath()
    {
        //if (!CanAnimate) return;
        animator.SetTrigger("isDeath");
    }

    public bool CurrentAnimationIs(string name) // name 애니메이션을 받고
    {
        //if (!CanAnimate) return false;
        return animator.GetCurrentAnimatorStateInfo(0).IsName(name);    // 해당 애니메이션이 재생 중인지 확인 후 반환
    }

    // animation의 bool 파라미터를 제어하는 메서드
    public void SetBool(string parameterName, bool value)
    {
        //if (!CanAnimate) return;
        animator.SetBool(parameterName, value);
    }

    // 외부에서 임시 비활성화 토글 제어
    public void SetAnimationsEnabled(bool enabled)
    {
        animationsEnabled = enabled && animator != null;
    }

    // Animator 컴포넌트 자체를 켜고/끄는 헬퍼(옵션)
    public void SetAnimatorComponentEnabled(bool enabled)
    {
        if (animator != null) animator.enabled = enabled;
    }
}
