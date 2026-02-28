using UnityEngine;
using UnityEngine.UI;

public class BuffCardUI : MonoBehaviour
{
    public Image iconImage;
    public Text nameText;
    public Text descText;
    public Button selectButton;

    private int _buffId;
    private BuffSelectionUI _parentUI;

    public void Setup(BuffData data, int id, BuffSelectionUI parent)
    {
        _buffId = id;
        _parentUI = parent;

        nameText.text = data.buffName;
        // descText가 있으면 설명 표시, 없으면 이름만
        if (descText != null) descText.text = data.description;
        if (data.icon != null) iconImage.sprite = data.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        _parentUI.OnCardSelected(_buffId);
    }
}