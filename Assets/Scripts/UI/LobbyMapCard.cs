using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyMapCard : MonoBehaviour
{
    public Image thumbnail;
    public TMP_Text mapNameText;
    public TMP_Text descriptionText;
    public Image selectedOverlay;
    public GameObject hostOnlyGroup;

    MapData _mapData;
    bool _selected;
    bool _isHost;
    Action<MapData, bool> _onToggle;

    public void Setup(MapData mapData, bool initialSelected, bool isHost, Action<MapData, bool> onToggle)
    {
        _mapData = mapData;
        _selected = initialSelected;
        _isHost = isHost;
        _onToggle = onToggle;

        if (mapNameText != null) mapNameText.text = mapData.mapName;
        if (descriptionText != null) descriptionText.text = mapData.description;
        if (thumbnail != null) { thumbnail.sprite = mapData.thumbnail; thumbnail.enabled = mapData.thumbnail != null; }
        if (hostOnlyGroup != null) hostOnlyGroup.SetActive(isHost);
        if (selectedOverlay != null) selectedOverlay.gameObject.SetActive(_selected);
    }

    public void OnCardClicked()
    {
        if (!_isHost) return;
        _selected = !_selected;
        if (selectedOverlay != null) selectedOverlay.gameObject.SetActive(_selected);
        _onToggle?.Invoke(_mapData, _selected);
    }
}
