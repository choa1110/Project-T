using System.Collections.Generic;
using UnityEngine;

public class BuffDatabase : MonoBehaviour
{
    public static BuffDatabase Instance;

    [Header("모든 버프 리스트 (인스펙터에서 드래그로 등록)")]
    public List<BuffData> allBuffs;

    void Awake()
    {
        // 싱글톤 패턴: 어디서든 BuffDatabase.Instance로 접근 가능하게 함
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }

    // BuffData -> ID (int) 변환
    public int GetBuffID(BuffData data)
    {
        if (data == null) return -1;
        int index = allBuffs.IndexOf(data);

        if (index == -1)
            Debug.LogError($"[BuffDatabase] 등록되지 않은 버프입니다: {data.name}. 인스펙터 리스트에 추가해주세요.");

        return index;
    }

    // ID (int) -> BuffData 변환
    public BuffData GetBuffByID(int id)
    {
        if (id < 0 || id >= allBuffs.Count)
        {
            Debug.LogError($"[BuffDatabase] 잘못된 버프 ID입니다: {id}");
            return null;
        }
        return allBuffs[id];
    }
}