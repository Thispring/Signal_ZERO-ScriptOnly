/// <summary>
/// Enemy 정보를 담은 구조체 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
/// 
/// <remarks>
/// Enemy 유형 별 명칭
/// 크기 분류: 소형 -> Small, 중형 -> Medium, 대형 -> Big
/// 공격 방식 분류: 일반 -> Normal, 연사 -> RapidFire, 미사일 -> Missile
/// 기타: 쉴드(다른 타입 Enemy에게 무적 부여) -> Shield
/// </remarks>
public enum EnemySize { Small = 0, Medium = 1, Big = 2 }
public enum EnemyType { Normal = 0, RapidFire = 1, Missile = 2, Shield = 3 }
[System.Serializable]
public struct EnemySetting
{
    public EnemySize enemySize; // enemy 크기
    public EnemyType enemyType; // enemy 타입
    public float HP;   // enemy의 체력
    public int damage;   // enemy의 공격력
    public float attackRate;   // enemy의 공격속도
    public float missileHP;  // enemy 미사일의 체력
    public int missileDamage;  // enemy 미사일의 공격력  
    public float moveSpeed;   // enemy의 이동속도
    public int dropCoin;   // enemy의 코인 드랍량
    public int bonusCoin;   // enemy의 추가 코인 드랍량

    public float gaugeReward; // 플레이어가 적을 공격할 때 얻는 버스트 게이지 양
}
