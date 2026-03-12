using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 버튼을 클릭했을 때 효과음을 재생하는 스크립트입니다.
/// </summary>
/// 
/// <remarks>
/// 사운드 재생은 SoundManager 클래스에서 동작하며, 싱글톤 인스턴스를 사용해 호출합니다.
/// 필요한 Button 오브젝트에 이 스크립트를 추가하여 사용합니다.
/// </remarks>
public class UIButtonClickSound : MonoBehaviour, IPointerClickHandler
/// IPointerClickHandler는 UnityEngine.EventSystems 네임스페이스에 정의되어 있습니다.
/// 현재 OnPointerClick 메서드 매개변수로 활용
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // SoundManager의 인스턴스가 이미 있으므로, 인스턴스로 접근하여 메서드 호출
        SoundManager.Instance.PlayButtonClick();
    }
}
