/// <summary>
/// 보조 로봇의 정보를 담은 구조체 입니다.
/// 모든 보조 로봇에 관련한 스크립트는 SupportBot을 앞에 붙여줍니다.
/// </summary>
public enum SupportBotType { Guard = 0, Shield = 1, Heal = 2 }
[System.Serializable]
public class SupportBotSetting
{
    public SupportBotType botType; // 보조 로봇의 타입
    public float HP;   // 보조 로봇의 체력
    public int healAmount;   // 보조 로봇의 회복량
    public float coolTime;  // 보조 로봇의 쿨타임
    public float healTime;   // 보조 로봇의 회복 지속시간
    public float shieldTime;   // 보조 로봇의 방어막 지속시간
    public float attackTime;   // 보조 로봇의 공격 지속시간
    public float guardTime;   // 보조 로봇의 가드 지속시간
    public int damage;   // 보조 로봇의 공격력
    public float attackRate;   // 보조 로봇의 공격속도
    
    // 타입별 업그레이드 레벨을 개별로 두어, 고레벨 시 능력 추가 고려
    public int guardLevel;
    public int shieldLevel;
    public int healLevel;

}
