using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 생성되는 오브젝트를 Destroy하지 않고, 비활성화해서 관리하는 스크립트 입니다.
/// </summary>
public class MemoryPool
{
    // MemoryPool로 관리되는 오브젝트 정보
    private class PoolItem
    {
        public bool isActive;   // 'gameObject'의 활성화/비활성화 정보
        public GameObject gameObject;   // 화면에 보이는 실제 게임오브젝트
    }

    private int increaseCount = 5;  // 오브젝트가 부족할 때 Instantiate()로 추가 생성되는 오브젝트 개수
    private int maxCount;   // 현재 리스트에 등록되어 있는 오브젝트 개수(단일 모드)
    private int activeCount;    // 현재 게임에 사용되고 있는(활성화) 오브젝트 개수(단일 모드)

    private GameObject poolObject;  // 오브젝트 풀링에서 관리하는 게임 오브젝트 Prefab(단일 모드)
    private List<PoolItem> poolItemList;    // 관리되는 모든 오브젝트를 저장하는 리스트(단일 모드)

    // ----- 멀티 프리팹 모드 ----- //
    // 여러 프리팹을 인덱스별로 독립 관리
    private bool multiMode = false;
    private GameObject[] poolObjects;              // 멀티: 각 인덱스별 프리팹
    private List<PoolItem>[] poolItemLists;        // 멀티: 각 인덱스별 풀 리스트
    private int[] maxCountsPerPool;                // 멀티: 인덱스별 총 개수
    private int[] activeCountsPerPool;             // 멀티: 인덱스별 활성 개수

    public int MaxCount
    {
        get
        {
            if (!multiMode) return maxCount;
            if (maxCountsPerPool == null) return 0;
            int sum = 0; for (int i = 0; i < maxCountsPerPool.Length; i++) sum += maxCountsPerPool[i];
            return sum;
        }
    }

    public int ActiveCount
    {
        get
        {
            if (!multiMode) return activeCount;
            if (activeCountsPerPool == null) return 0;
            int sum = 0; for (int i = 0; i < activeCountsPerPool.Length; i++) sum += activeCountsPerPool[i];
            return sum;
        }
    }

    // 기존: 단일 프리팹 풀
    public MemoryPool(GameObject poolObject)    // 변수 초기화 후, InstantiateObjects를 통해 최소 5개의 오브젝트 생성
    {
        maxCount = 0;
        activeCount = 0;
        this.poolObject = poolObject;

        poolItemList = new List<PoolItem>();

        InstantiateObjects();
    }

    // 신규: 멀티 프리팹 풀 (중간보스/최종보스 등 그룹 분리 저장)
    public MemoryPool(GameObject[] poolObjects)
    {
        multiMode = true;
        this.poolObjects = poolObjects ?? new GameObject[0];
        int n = this.poolObjects.Length;
        poolItemLists = new List<PoolItem>[n];
        maxCountsPerPool = new int[n];
        activeCountsPerPool = new int[n];
        for (int i = 0; i < n; i++)
        {
            poolItemLists[i] = new List<PoolItem>();
            maxCountsPerPool[i] = 0;
            activeCountsPerPool[i] = 0;
            InstantiateObjects(i); // 각 풀에 기본 increaseCount만큼 생성
        }
    }

    // increaseCount 단위로 오브젝트를 생성
    public void InstantiateObjects()
    {
        // 단일 모드 전용
        if (multiMode)
        {
            Debug.LogWarning("MemoryPool: 단일 InstantiateObjects()는 단일 모드에서만 사용됩니다. 멀티 모드에서는 InstantiateObjects(int index)를 사용하세요.");
            return;
        }

        maxCount += increaseCount;

        for (int i = 0; i < increaseCount; ++i)
        {
            PoolItem poolItem = new PoolItem();

            poolItem.isActive = false;  // 바로 사용하지 않을 수 있기에 active를 false로 설정
            poolItem.gameObject = GameObject.Instantiate(poolObject);
            poolItem.gameObject.SetActive(false);

            poolItemList.Add(poolItem);
        }
    }

    // 멀티 모드: 특정 인덱스 풀에 increaseCount 만큼 생성
    public void InstantiateObjects(int index)
    {
        if (!multiMode)
        {
            Debug.LogWarning("MemoryPool: InstantiateObjects(int)는 멀티 모드에서만 사용됩니다.");
            return;
        }
        if (poolObjects == null || index < 0 || index >= poolObjects.Length) return;

        maxCountsPerPool[index] += increaseCount;
        var list = poolItemLists[index];
        var prefab = poolObjects[index];
        for (int i = 0; i < increaseCount; ++i)
        {
            PoolItem poolItem = new PoolItem();
            poolItem.isActive = false;
            poolItem.gameObject = GameObject.Instantiate(prefab);
            poolItem.gameObject.SetActive(false);
            list.Add(poolItem);
        }
    }

    // 현재 관리중인(활성/비활성) 모든 오브젝트를 삭제
    public void DestroyObject()
    {
        if (!multiMode)
        {
            if (poolItemList == null) return;

            int count = poolItemList.Count;
            for (int i = 0; i < count; i++)
            {
                GameObject.Destroy(poolItemList[i].gameObject); // 씬이 바뀌거나 게임이 종료될 때 한 번만 수행
            }

            poolItemList.Clear();   // 리스트 초기화
            return;
        }

        // 멀티 모드
        if (poolItemLists == null) return;
        for (int p = 0; p < poolItemLists.Length; p++)
        {
            var list = poolItemLists[p];
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++)
            {
                GameObject.Destroy(list[i].gameObject);
            }
            list.Clear();
            maxCountsPerPool[p] = 0;
            activeCountsPerPool[p] = 0;
        }
    }

    // poolItemList에 저장되어 있는 오브젝트를 활성화해서 사용
    // 현재 모든 오브젝트가 사용중이면 InstantiateObjects()로 추가 생성
    public GameObject ActivatePoolItem()
    {
        if (multiMode)
        {
            Debug.LogWarning("MemoryPool: ActivatePoolItem()는 단일 모드에서만 사용됩니다. 멀티 모드에서는 ActivatePoolItem(int index)를 사용하세요.");
            return null;
        }
        if (poolItemList == null) return null; // List가 null이라면(관리 중인 오브젝트가 없는 상태) null 반환

        // 현재 생성해서 관리하는 모든 오브젝트 개수와 현재 활성화 상태인 오브젝트 개수 비교
        // 모든 오브젝트가 활성화 상태이면 새로운 오브젝트 필요
        if (maxCount == activeCount)  // 모든 오브젝트가 활성화 된 상태
        {
            InstantiateObjects();   // 추가 오브젝트 생성
        }

        int count = poolItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            PoolItem poolItem = poolItemList[i];

            if (poolItem.isActive == false)
            {
                activeCount++;

                poolItem.isActive = true;
                poolItem.gameObject.SetActive(true);

                return poolItem.gameObject;
            }
        }

        return null;
    }

    // 멀티 모드: 지정한 인덱스 풀에서 활성화 요청
    public GameObject ActivatePoolItem(int index)
    {
        if (!multiMode)
        {
            Debug.LogWarning("MemoryPool: ActivatePoolItem(int)는 멀티 모드에서만 사용됩니다.");
            return null;
        }
        if (poolItemLists == null || index < 0 || index >= poolItemLists.Length) return null;
        var list = poolItemLists[index];
        if (list == null) return null;

        if (maxCountsPerPool[index] == activeCountsPerPool[index])
        {
            InstantiateObjects(index);
        }

        int count = list.Count;
        for (int i = 0; i < count; ++i)
        {
            var poolItem = list[i];
            if (!poolItem.isActive)
            {
                activeCountsPerPool[index]++;
                poolItem.isActive = true;
                poolItem.gameObject.SetActive(true);
                return poolItem.gameObject;
            }
        }
        return null;
    }

    // 현재 사용이 완료된 오브젝트를 비활성화 상태로 설정
    public void DeactivatePoolItem(GameObject removeObject)
    {
        if (!multiMode)
        {
            if (poolItemList == null || removeObject == null) return;

            int count = poolItemList.Count;
            for (int i = 0; i < count; i++)
            {
                PoolItem poolItem = poolItemList[i];

                if (poolItem.gameObject == removeObject)
                {
                    activeCount--;

                    poolItem.isActive = false;
                    poolItem.gameObject.SetActive(false);

                    return;
                }
            }
            return;
        }

        // 멀티 모드: 모든 리스트에서 탐색
        if (poolItemLists == null || removeObject == null) return;
        for (int p = 0; p < poolItemLists.Length; p++)
        {
            var list = poolItemLists[p];
            if (list == null) continue;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var poolItem = list[i];
                if (poolItem.gameObject == removeObject)
                {
                    activeCountsPerPool[p]--;
                    poolItem.isActive = false;
                    poolItem.gameObject.SetActive(false);
                    return;
                }
            }
        }
    }

    // 게임에 사용중인 모든 오브젝트를 비활성화 상태로 설정
    // 반복문을 통해 List를 탐색 후 null이 아닌 오브젝트를 비활성화
    public void DeactivateAllPoolItems()
    {
        if (!multiMode)
        {
            if (poolItemList == null) return;

            int count = poolItemList.Count;
            for (int i = 0; i < count; ++i)
            {
                PoolItem poolItem = poolItemList[i];

                if (poolItem.gameObject != null && poolItem.isActive == true)
                {
                    poolItem.isActive = false;
                    poolItem.gameObject.SetActive(false);
                }
            }

            activeCount = 0;
            return;
        }

        // 멀티 모드
        if (poolItemLists == null) return;
        for (int p = 0; p < poolItemLists.Length; p++)
        {
            var list = poolItemLists[p];
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++)
            {
                var poolItem = list[i];
                if (poolItem.gameObject != null && poolItem.isActive)
                {
                    poolItem.isActive = false;
                    poolItem.gameObject.SetActive(false);
                }
            }
            activeCountsPerPool[p] = 0;
        }
    }

    // 풀에 있는 모든 오브젝트들의 부모 트랜스폼을 변경
    public void SetAllPoolObjectsParent(Transform parent)
    {
        if (!multiMode)
        {
            if (poolItemList == null) return;
            int count = poolItemList.Count;
            for (int i = 0; i < count; ++i)
            {
                PoolItem poolItem = poolItemList[i];
                if (poolItem.gameObject != null)
                {
                    poolItem.gameObject.transform.SetParent(parent);
                }
            }
            return;
        }

        if (poolItemLists == null) return;
        for (int p = 0; p < poolItemLists.Length; p++)
        {
            var list = poolItemLists[p];
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++)
            {
                var poolItem = list[i];
                if (poolItem.gameObject != null)
                {
                    poolItem.gameObject.transform.SetParent(parent);
                }
            }
        }
    }
}
