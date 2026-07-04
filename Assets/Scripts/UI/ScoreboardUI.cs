using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardUI : MonoBehaviour
{
    public static ScoreboardUI Instance { get; private set; }

    public Button tabButton;
    public GameObject scorePanel;
    public Transform rowContainer;
    public ScoreboardRow rowPrefab;
    public TMPro.TMP_Text headerText;

    bool _open;
    readonly List<ScoreboardRow> _rows = new List<ScoreboardRow>();

    void Awake()
    {
        Instance = this;
        if (scorePanel != null) scorePanel.SetActive(false);
    }

    void Start()
    {
        if (tabButton != null) tabButton.onClick.AddListener(() => SetOpen(!_open));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) SetOpen(!_open);
    }

    public void SetOpen(bool open)
    {
        _open = open;
        if (scorePanel != null) scorePanel.SetActive(_open);
        if (_open) BuildRows(0);
    }

    void BuildRows(int dummy)
    {
        DestroyRows(0);

        if (GameManager.Instance == null || GameManager.Instance.playerList == null) return;

        var list = GameManager.Instance.playerList;
        if (headerText != null) headerText.text = string.Format("PLAYERS  {0}", list.Count);

        Player mine = null;
        foreach (var p in list)
            if (p.Object != null && p.Object.HasInputAuthority) { mine = p; break; }

        var sorted = new List<Player>(list);
        sorted.Sort((a, b) => a.team.CompareTo(b.team));

        foreach (var player in sorted)
        {
            if (rowPrefab == null || rowContainer == null) break;
            var row = Instantiate(rowPrefab, rowContainer);
            row.Setup(player, player == mine);
            _rows.Add(row);
        }
    }

    void DestroyRows(int dummy)
    {
        foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
        _rows.Clear();
    }
}
