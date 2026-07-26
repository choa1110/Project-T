using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class MapSelectDesign
{
    const string EntryPrefab = "Assets/Prefabs/UI/MapEntryUI.prefab";

    [MenuItem("Tools/ProjectT/Design MapSelect (Chat)")]
    public static void Design()
    {
        // ===== 1) MapEntryUI 프리팹: 항목이 리스트에서 크기 잡히게 + 썸네일 비율 =====
        var root = PrefabUtility.LoadPrefabContents(EntryPrefab);

        var vlg = root.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 6f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var csf = root.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = root.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var thumbT = root.transform.Find("Thumbnail");
        if (thumbT != null)
        {
            var le = thumbT.GetComponent<LayoutElement>();
            if (le == null) le = thumbT.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 150f; le.flexibleWidth = 1f;
            var img = thumbT.GetComponent<Image>();
            if (img != null) img.preserveAspect = true;
        }
        var nameT = root.transform.Find("MapNameText");
        if (nameT != null) { var t = nameT.GetComponent<TextMeshProUGUI>(); if (t) { t.fontSize = 30; t.alignment = TextAlignmentOptions.Center; } }
        var descT = root.transform.Find("DescriptionText");
        if (descT != null) { var t = descT.GetComponent<TextMeshProUGUI>(); if (t) { t.fontSize = 22; t.alignment = TextAlignmentOptions.Center; t.color = new Color(0.8f, 0.8f, 0.85f); } }
        var togT = root.transform.Find("ToggleButton");
        if (togT != null) { var le = togT.GetComponent<LayoutElement>(); if (le == null) le = togT.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 52f; }

        PrefabUtility.SaveAsPrefabAsset(root, EntryPrefab);
        PrefabUtility.UnloadPrefabContents(root);

        // ===== 2) ChatScene MapSelectPanel 레이아웃 =====
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) { Debug.LogError("[Design] Canvas 없음"); return; }
        var panel = canvasGo.transform.Find("MapSelectPanel");
        if (panel == null) { Debug.LogError("[Design] MapSelectPanel 없음"); return; }

        // 패널
        var pRt = (RectTransform)panel;
        pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.5f); pRt.pivot = new Vector2(0.5f, 0.5f);
        pRt.anchoredPosition = Vector2.zero; pRt.sizeDelta = new Vector2(900f, 780f);
        var pImg = panel.GetComponent<Image>(); if (pImg != null) pImg.color = new Color(0.12f, 0.13f, 0.18f, 0.97f);

        // 타이틀
        var title = panel.Find("TitleText");
        if (title != null)
        {
            var rt = (RectTransform)title; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -26f); rt.sizeDelta = new Vector2(780f, 74f);
            var t = title.GetComponent<TextMeshProUGUI>(); if (t) { t.text = "맵 선택"; t.fontSize = 46; t.color = Color.white; t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold; t.enableWordWrapping = false; }
        }
        // 풀 카운트
        var pool = panel.Find("PoolCountText");
        if (pool != null)
        {
            var rt = (RectTransform)pool; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -108f); rt.sizeDelta = new Vector2(780f, 36f);
            var t = pool.GetComponent<TextMeshProUGUI>(); if (t) { t.fontSize = 26; t.color = new Color(0.8f, 0.82f, 0.9f); t.alignment = TextAlignmentOptions.Center; }
        }
        // 호스트 안내
        var hint = panel.Find("HostOnlyHint");
        if (hint != null)
        {
            var rt = (RectTransform)hint; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 130f); rt.sizeDelta = new Vector2(780f, 34f);
            var t = hint.GetComponent<TextMeshProUGUI>(); if (t) { t.fontSize = 24; t.color = new Color(1f, 0.85f, 0.45f); t.alignment = TextAlignmentOptions.Center; }
        }
        // 스크롤뷰 (중앙 채움)
        var sv = panel.Find("MapListScrollView");
        if (sv != null)
        {
            var rt = (RectTransform)sv; rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(40f, 120f); rt.offsetMax = new Vector2(-40f, -150f);
            var svImg = sv.GetComponent<Image>(); if (svImg != null) svImg.color = new Color(0f, 0f, 0f, 0.22f);
            var scroll = sv.GetComponent<ScrollRect>(); if (scroll != null) { scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 25f; }

            var viewport = sv.Find("Viewport");
            if (viewport != null)
            {
                var vrt = (RectTransform)viewport; vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f);
                vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero; vrt.pivot = new Vector2(0f, 1f);
                var container = viewport.Find("MapListContainer");
                if (container != null)
                {
                    var crt = (RectTransform)container; crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
                    crt.pivot = new Vector2(0.5f, 1f); crt.anchoredPosition = Vector2.zero; crt.sizeDelta = new Vector2(0f, 0f);
                    var cvlg = container.GetComponent<VerticalLayoutGroup>();
                    if (cvlg == null) cvlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
                    cvlg.padding = new RectOffset(12, 12, 12, 12); cvlg.spacing = 12f;
                    cvlg.childControlWidth = true; cvlg.childControlHeight = true;
                    cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
                    cvlg.childAlignment = TextAnchor.UpperCenter;
                    var ccsf = container.GetComponent<ContentSizeFitter>();
                    if (ccsf == null) ccsf = container.gameObject.AddComponent<ContentSizeFitter>();
                    ccsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    ccsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
            }
        }
        // 버튼들
        var startB = panel.Find("StartButton");
        if (startB != null) { var rt = (RectTransform)startB; rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(1f, 0f); rt.anchoredPosition = new Vector2(-40f, 36f); rt.sizeDelta = new Vector2(250f, 72f); }
        var backB = panel.Find("BackButton");
        if (backB != null) { var rt = (RectTransform)backB; rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); rt.pivot = new Vector2(0f, 0f); rt.anchoredPosition = new Vector2(40f, 36f); rt.sizeDelta = new Vector2(210f, 72f); }

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        EditorSceneManager.SaveScene(canvasGo.scene);
        Debug.Log("[Design] MapSelect 패널 레이아웃 + MapEntry 프리팹 디자인 완료");
    }
}
