using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 로켓 폭발 이펙트의 범위 내 적에게 일정 시간 동안 데미지를 1회만 입히는 스크립트 입니다.
/// 기존 Rocket 데미지 함수가 여러번 호출되는 것을 막고, 광역 데미지 기능을 유지하기 위해 작성되었습니다.
/// </summary>
public class RocketExplosionArea : MonoBehaviour
{
    [Header("Values")]
    public float damage = 50f;
    public float duration = 1f; // 폭발 이펙트가 유지되는 시간
    private float timer = 0f;
    private bool isActive = false;
    // 이미 데미지를 입은 적을 추적하기 위한 HashSet
    private HashSet<GameObject> damagedEnemies = new HashSet<GameObject>();

    public void Activate(float damage, float duration)
    {
        this.damage = damage;
        this.duration = duration;
        damagedEnemies.Clear();
        timer = 0f;
        isActive = true;
        gameObject.SetActive(true);
    }

    void OnEnable()
    {
        timer = 0f;
        isActive = true;
        damagedEnemies.Clear();
    }

    void Update()
    {
        if (!isActive) return;
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }

    // HashSet을 사용하여, 이미 데미지를 입은 적을 등록해 중복 데미지를 방지합니다.
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (other.CompareTag("EnemySmall") || other.CompareTag("EnemyMedium") || other.CompareTag("EnemyBig") 
        || other.CompareTag("Boss") || other.CompareTag("BossDrone"))
        {
            if (!damagedEnemies.Contains(other.gameObject))
            {
                var enemy = other.GetComponent<EnemyStatusManager>();
                var boss = other.GetComponent<BossStatusManager>();
                var bossDrone = other.GetComponent<BossDroneManager>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    damagedEnemies.Add(other.gameObject);
                }
                if (boss != null)
                {
                    boss.TakeDamage(damage);
                    damagedEnemies.Add(other.gameObject);
                }
                if (bossDrone != null)
                {
                    bossDrone.TakeDamage(damage);
                    damagedEnemies.Add(other.gameObject);
                }
            }
        }
    }
}
