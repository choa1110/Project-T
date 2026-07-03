using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class GameOverBuilder
{
    [MenuItem("Tools/ProjectT/Build GameOver UI")]
    public static void Build()
    {
        var existing = GameObject.Find("GameOverCanvas");
        if (existing) Object.DestroyImmediate(existing);

        // 자체 오버레이 캔버스 (HUD 위)
        var canvasGo = new GameObject("GameOverCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        int uiLayer = LayerMask.NameToLayer("UI");
        canvasGo.layer = uiLayer;

        // 딤 패널 (전체화면, 시작 시 비활성)
        var panelGo = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        panelGo.layer = uiLayer; panelGo.transform.SetParent(canvasGo.transform, false);
        var pRt = (RectTransform)panelGo.transform;
        pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one; pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
        panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // 창
        var winGo = new GameObject("Window", typeof(RectTransform), typeof(Image));
        winGo.layer = uiLayer; winGo.transform.SetParent(panelGo.transform, false);
        var wRt = (RectTransform)winGo.transform;
        wRt.anchorMin = wRt.anchorMax = new Vector2(0.5f, 0.5f); wRt.pivot = new Vector2(0.5f, 0.5f);
        wRt.anchoredPosition = Vector2.zero; wRt.sizeDelta = new Vector2(720, 460);
        winGo.GetComponent<Image>().color = new Color(0.13f, 0.14f, 0.2f, 0.98f);

        // "게임 오버" 타이틀
        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.layer = uiLayer; titleGo.transform.SetParent(winGo.transform, false);
        var tRt = (RectTransform)titleGo.transform;
        tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0.85f); tRt.anchoredPosition = Vector2.zero; tRt.sizeDelta = new Vector2(600, 100);
        var tt = titleGo.GetComponent<TextMeshProUGUI>();
        tt.text = "게임 오버"; tt.fontSize = 64; tt.color = Color.white; tt.alignment = TextAlignmentOptions.Center; tt.enableWordWrapping = false;

        // 승자 텍스트 (런타임에 채워짐)
        var winnerGo = new GameObject("WinnerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        winnerGo.layer = uiLayer; winnerGo.transform.SetParent(winGo.transform, false);
        var wtRt = (RectTransform)winnerGo.transform;
        wtRt.anchorMin = wtRt.anchorMax = new Vector2(0.5f, 0.5f); wtRt.anchoredPosition = new Vector2(0, 20); wtRt.sizeDelta = new Vector2(640, 160);
        var wt = winnerGo.GetComponent<TextMeshProUGUI>();
        wt.text = "승자: -"; wt.fontSize = 44; wt.color = new Color(1f, 0.92f, 0.5f); wt.alignment = TextAlignmentOptions.Center;

        // "로비로" 버튼
        var btnGo = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.layer = uiLayer; btnGo.transform.SetParent(winGo.transform, false);
        var bRt = (RectTransform)btnGo.transform;
        bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 0.16f); bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = Vector2.zero; bRt.sizeDelta = new Vector2(300, 80);
        var bImg = btnGo.GetComponent<Image>(); bImg.color = new Color(1f, 1f, 1f, 0.92f);
        var btn = btnGo.GetComponent<Button>(); btn.targetGraphic = bImg;
        var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.layer = uiLayer; lblGo.transform.SetParent(btnGo.transform, false);
        var lRt = (RectTransform)lblGo.transform;
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one; lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
        var lt = lblGo.GetComponent<TextMeshProUGUI>();
        lt.text = "로비로"; lt.fontSize = 32; lt.color = new Color(0.15f, 0.15f, 0.2f); lt.alignment = TextAlignmentOptions.Center;

        // GameOverUI 컴포넌트 + 참조 연결 (캔버스는 항상 활성)
        var ui = canvasGo.AddComponent<GameOverUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue = panelGo;
        so.FindProperty("winnerText").objectReferenceValue = wt;
        so.ApplyModifiedProperties();

        UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(ui.OnBackToLobby));

        panelGo.SetActive(false);

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("[GameOverBuilder] GameOver UI built and wired.");
    }
}
