using System.Collections.Generic;
using UnityEngine;

public class BuffDatabase : MonoBehaviour
{
    public static BuffDatabase Instance;

    [Header("��� ���� ����Ʈ (�ν����Ϳ��� �巡�׷� ���)")]
    public List<Buff> allBuffs;

    void Awake()
    {
        // �̱��� ����: ��𼭵� BuffDatabase.Instance�� ���� �����ϰ� ��
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� �ٲ� �ı����� ����
        }
        else
        {
            Destroy(gameObject); // �ߺ� ���� ����
        }
    }

    // BuffData -> ID (int) ��ȯ
    public int GetBuffID(Buff data)
    {
        if (data == null) return -1;
        int index = allBuffs.IndexOf(data);

        if (index == -1)
            Debug.LogError($"[BuffDatabase] ��ϵ��� ���� �����Դϴ�: {data.name}. �ν����� ����Ʈ�� �߰����ּ���.");

        return index;
    }

    // ID (int) -> BuffData ��ȯ
    public Buff GetBuffByID(int id)
    {
        if (id < 0 || id >= allBuffs.Count)
        {
            Debug.LogError($"[BuffDatabase] �߸��� ���� ID�Դϴ�: {id}");
            return null;
        }
        return allBuffs[id];
    }
}