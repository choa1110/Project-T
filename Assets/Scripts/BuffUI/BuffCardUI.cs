using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffCardUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Button selectButton;

    int _buffId;
    BuffSelectionUI _parentUI;

    public void Setup(Buff data, int id, BuffSelectionUI parent)
    {
        _buffId   = id;
        _parentUI = parent;

        if (nameText != null) nameText.text = data.buffName;
        if (descText != null) descText.text = data.discription;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClicked);
    }

    void OnClicked() { _parentUI?.OnCardSelected(_buffId); }
}
