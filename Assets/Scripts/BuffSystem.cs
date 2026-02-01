using Fusion; // Fusion 네임스페이스 필수
using System.Collections.Generic;
using UnityEngine;

public class BuffSystem : NetworkBehaviour // MonoBehaviour -> NetworkBehaviour 변경
{
    Player _player;
    PlayerStats _playerStats;

    // 로컬 로직 관리용 리스트 (서버에서만 실질적으로 운용됨)
    List<Buff> _activeBuffs = new List<Buff>();

    // [Fusion] 네트워크 동기화용 배열 (상황판 UI 등에서 조회용)
    // 최대 16개 버프까지 동기화 (개수는 필요에 따라 조절)
    [Networked, Capacity(16)]
    private NetworkArray<int> ActiveBuffIDs { get; }

    [Networked]
    private int ActiveBuffCount { get; set; }

    void Awake()
    {
        _player = GetComponent<Player>();
        _playerStats = _player.stats;
    }

    // Update -> FixedUpdateNetwork로 변경 (Fusion 동기화 주기와 일치)
    public override void FixedUpdateNetwork()
    {
        // 중요: 버프 시간 계산과 로직은 '서버(Host)' 권한이 있는 곳에서만 수행
        if (Object.HasStateAuthority)
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                // Runner.DeltaTime 사용 (네트워크 델타타임)
                _activeBuffs[i].UpdateTick(Runner.DeltaTime);

                if (_activeBuffs[i].expired)
                    RemoveBuff(_activeBuffs[i]);
            }
        }
    }

    public void ApplyBuff(BuffData data)
    {
        // 서버만 버프를 추가할 수 있음 (클라이언트는 무시됨)
        if (!Object.HasStateAuthority) return;

        Buff buff = new Buff(data, _player);
        _activeBuffs.Add(buff);

        // 변경 사항을 네트워크 배열에 반영
        UpdateNetworkBuffs();
    }

    private void RemoveBuff(Buff buff)
    {
        buff.Remove();
        _activeBuffs.Remove(buff);

        // 변경 사항을 네트워크 배열에 반영
        UpdateNetworkBuffs();
    }

    // 내부 리스트(_activeBuffs)를 네트워크 배열(ActiveBuffIDs)로 동기화하는 함수
    private void UpdateNetworkBuffs()
    {
        int count = 0;
        foreach (var buff in _activeBuffs)
        {
            if (count >= 16) break;

            // [주의] Buff 클래스 안에 원본 BuffData를 가리키는 변수가 있다고 가정했습니다 (예: buff.data)
            // 만약 변수명이 다르다면 buff.data 부분을 실제 변수명으로 수정해주세요.
            ActiveBuffIDs.Set(count++, BuffDatabase.Instance.GetBuffID(buff.Data));
        }
        ActiveBuffCount = count;
    }

    // [공개 함수] UI(상황판)에서 이 플레이어의 버프 목록을 가져올 때 사용
    public List<BuffData> GetSyncBuffs()
    {
        List<BuffData> list = new List<BuffData>();

        // 동기화된 ID 배열을 순회하며 실제 BuffData로 복원
        for (int i = 0; i < ActiveBuffCount; i++)
        {
            int id = ActiveBuffIDs[i];
            BuffData data = BuffDatabase.Instance.GetBuffByID(id);
            if (data != null) list.Add(data);
        }

        return list;
    }
}