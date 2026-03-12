/// <summary>
/// Boss 정보를 담은 구조체 스크립트 입니다.
/// 모든 Boss에 관련한 스크립트는 Boss를 앞에 붙여줍니다.
/// </summary>
/// 
/// <remarks>
/// 현재 middle/final Boss의 로직은 통합된 스크립트에서 BossType을 조건식을 사용하는 방법으로 구현되어 있습니다.
/// 추후 필요에 따라 middle/final Boss 스크립트를 분리하여 다시 작성합니다.
/// </remarks>
public enum BossPhase
{
    // Boss의 Phase단계를 체력에 기반하여 구분
    fullHP = 0,
    halfHP = 1,
}

public enum BossType
{
    middle = 0, 
    final = 1   
}

[System.Serializable]
public struct BossSetting
{
    public BossPhase bossPhase; // Boss의 현재 단계
    public BossType bossType; // Boss의 타입 (중간, 최종)
    public float HP;   // Boss의 체력
    public int damage;   // Boss의 공격력
    public float missileHP; // Boss의 미사일 체력
    public int missileDamage;   // Boss의 미사일 공격력
    public float attackRate;   // Boss의 공격속도, projectile 발사 간격에 사용 중
    public float patternDelay; // Boss의 패턴 사이 대기 시간
    public int fireCount; // Boss의 한 번 공격에서 발사 횟수
}
