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

    public void Setup(Buff data, int id, BuffSelectionUI parent)
    {
        _buffId = id;
        _parentUI = parent;

        nameText.text = data.buffName;
        // descText�� ������ ���� ǥ��, ������ �̸���
        if (descText != null) descText.text = data.discription;
        // if (data.icon != null) iconImage.sprite = data.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        _parentUI.OnCardSelected(_buffId);
    }
}