using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 로비 맵 선택 UI - 맵 1개 항목 컴포넌트.
/// </summary>
public class MapEntryUI : MonoBehaviour
{
    [SerializeField] Image           thumbnailImage;
    [SerializeField] TextMeshProUGUI mapNameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] Button          toggleButton;
    [SerializeField] TextMeshProUGUI toggleButtonText;
    [SerializeField] Image           toggleButtonBackground;
    [SerializeField] Color inPoolColor    = new Color(0.25f, 0.75f, 0.35f);
    [SerializeField] Color notInPoolColor = new Color(0.45f, 0.45f, 0.45f);
    [SerializeField] Color disabledColor  = new Color(0.30f, 0.30f, 0.30f);

    MapData _data;
    MapDB   _db;
    bool    _canEdit;
    Action  _onChange;

    public void Initialize(MapData mapData, MapDB mapDB, bool canEdit, Action onPoolChanged)
    {
        _data = mapData; _db = mapDB; _canEdit = canEdit; _onChange = onPoolChanged;
        if (mapNameText != null)     mapNameText.text     = mapData.mapName;
        if (descriptionText != null) descriptionText.text = mapData.description;
        if (thumbnailImage  != null) { thumbnailImage.enabled = mapData.thumbnail != null; if (mapData.thumbnail != null) thumbnailImage.sprite = mapData.thumbnail; }
        if (toggleButton    != null) { toggleButton.interactable = canEdit; toggleButton.onClick.AddListener(() => OnToggleClicked(this)); }
        OnToggleClicked(null);
    }

    void OnToggleClicked(MapEntryUI sender)
    {
        if (sender != null)
        {
            if (!_canEdit || _db == null) return;
            if (_db.IsInPool(_data)) _db.RemoveFromPool(_data);
            else _db.AddToPool(_data);
            _onChange?.Invoke();
        }
        bool p = _db != null && _db.IsInPool(_data);
        if (toggleButtonText       != null) toggleButtonText.text        = p ? "풀 제거" : "풀 추가";
        if (toggleButtonBackground != null) toggleButtonBackground.color = !_canEdit ? disabledColor : p ? inPoolColor : notInPoolColor;
    }
}
