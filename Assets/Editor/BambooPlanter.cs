using UnityEngine;
using UnityEditor;

/// <summary>
/// Map004 바닥 콜라이더 바운즈 위에 일정 간격 그리드로 레이캐스트를 쏴서,
/// 바닥에 맞은 지점마다 대나무(원본 메시)를 복제해 심는다.
/// 맵 크기를 바꾼 뒤 메뉴만 다시 실행하면 새 바닥 크기에 맞춰 재배치된다.
/// 심어진 대나무는 "BambooField" 오브젝트 아래에 모인다(재실행 시 기존 것 제거 후 재생성).
/// </summary>
public static class BambooPlanter
{
    const string MapPath = "Assets/Prefabs/Maps/Prefabs/Map004.prefab";
    const string BambooName = "Meshy_AI_A_single_stylized_lo_0308105543_texture";

    // ===== 조정 파라미터 =====
    const float Spacing = 3f;         // 대나무 간격(월드 유닛)
    const float RayUp = 50f;          // 바닥 바운즈 위 이만큼에서 레이 시작
    const float RayDist = 500f;       // 레이 최대 거리
    const float GroundOffsetY = 0f;   // 바닥에서 띄우거나 묻을 Y 오프셋
    const bool RandomYaw = true;      // Y축 랜덤 회전
    const int MaxCount = 4000;        // 안전 상한(폭주 방지)
    // ========================

    [MenuItem("Tools/ProjectT/Plant Bamboo (Map004)")]
    public static void Plant()
    {
        var root = PrefabUtility.LoadPrefabContents(MapPath);

        Transform src = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == BambooName) src = t;
        if (src == null) { Debug.LogError("[BambooPlanter] 대나무 원본을 못 찾음: " + BambooName); PrefabUtility.UnloadPrefabContents(root); return; }

        // 바닥 = 대나무를 제외한 콜라이더들의 합 바운즈
        Bounds gb = new Bounds();
        bool gbSet = false;
        foreach (var col in root.GetComponentsInChildren<Collider>(true))
        {
            if (col.transform == src || col.transform.IsChildOf(src)) continue;
            if (!gbSet) { gb = col.bounds; gbSet = true; } else gb.Encapsulate(col.bounds);
        }
        if (!gbSet) { Debug.LogError("[BambooPlanter] 바닥 콜라이더를 못 찾음"); PrefabUtility.UnloadPrefabContents(root); return; }

        var old = root.transform.Find("BambooField");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var field = new GameObject("BambooField");
        field.transform.SetParent(root.transform, false);

        Physics.SyncTransforms();
        var phys = root.scene.GetPhysicsScene();

        float y0 = gb.max.y + RayUp;
        int gx = Mathf.FloorToInt(gb.size.x / Spacing);
        int gz = Mathf.FloorToInt(gb.size.z / Spacing);
        int count = 0;
        bool capped = false;

        for (int ix = 0; ix <= gx && !capped; ix++)
        {
            for (int iz = 0; iz <= gz; iz++)
            {
                float wx = gb.min.x + ix * Spacing;
                float wz = gb.min.z + iz * Spacing;
                var origin = new Vector3(wx, y0, wz);
                RaycastHit hit;
                if (phys.Raycast(origin, Vector3.down, out hit, RayDist, ~0, QueryTriggerInteraction.Ignore))
                {
                    var clone = (GameObject)Object.Instantiate(src.gameObject, field.transform);
                    clone.transform.position = hit.point + Vector3.up * GroundOffsetY;
                    clone.transform.rotation = src.rotation;
                    if (RandomYaw) clone.transform.Rotate(0f, Random.Range(0f, 360f), 0f, Space.World);
                    clone.name = "Bamboo_" + ix + "_" + iz;
                    count++;
                    if (count >= MaxCount) { capped = true; break; }
                }
            }
        }

        // 원본 템플릿은 숨김(중복 방지). 필요하면 다시 켜면 됨.
        src.gameObject.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(root, MapPath);
        Debug.Log("[BambooPlanter] 심음: " + count + (capped ? " (상한 도달)" : "") +
                  " | 바닥 bounds size=" + gb.size + " min=" + gb.min);
        PrefabUtility.UnloadPrefabContents(root);
    }

    [MenuItem("Tools/ProjectT/Clear Bamboo (Map004)")]
    public static void Clear()
    {
        var root = PrefabUtility.LoadPrefabContents(MapPath);
        var field = root.transform.Find("BambooField");
        if (field != null) Object.DestroyImmediate(field.gameObject);
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == BambooName) t.gameObject.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(root, MapPath);
        Debug.Log("[BambooPlanter] BambooField 제거, 원본 대나무 복원");
        PrefabUtility.UnloadPrefabContents(root);
    }
}
