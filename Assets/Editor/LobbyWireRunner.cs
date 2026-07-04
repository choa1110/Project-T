using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LobbyWireRunner
{
    [MenuItem("Tools/Wire Lobby Buttons")]
    public static void Wire()
    {
        var lobbyMgr = GameObject.Find("LobbyManager");
        var ctrl = lobbyMgr != null ? lobbyMgr.GetComponent<LobbyUIController>() : null;
        if (ctrl == null) { Debug.LogError("[Wire] LobbyUIController 없음"); return; }

        string[] paths = {
            "Canvas/MainPanel/Button",
            "Canvas/MainPanel/Button (1)",
            "Canvas/CreateRoomPanel/Button_Confirm",
            "Canvas/CreateRoomPanel/Button_Back",
            "Canvas/JoinRoomPanel/Button_back",
            "Canvas/MapSelectPanel/StartButton",
            "Canvas/MapSelectPanel/BackButton"
        };
        string[] methods = {
            "OnOpenCreatePanel",
            "OnOpenJoinPanel",
            "OnCreateRoomConfirm",
            "OnBackToMain",
            "OnBackToMain",
            "OnStartGame",
            "OnMapSelectBack"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            var go  = GameObject.Find(paths[i]);
            var btn = go != null ? go.GetComponent<Button>() : null;
            if (btn == null) { Debug.LogWarning($"없음: {paths[i]}"); continue; }
            btn.onClick.RemoveAllListeners();
            var mi  = typeof(LobbyUIController).GetMethod(methods[i]);
            if (mi  == null) { Debug.LogWarning($"메서드 없음: {methods[i]}"); continue; }
            var act = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), ctrl, mi)
                      as UnityEngine.Events.UnityAction;
            UnityEventTools.AddVoidPersistentListener(btn.onClick, act);
            EditorUtility.SetDirty(btn);
            Debug.Log($"연결: {paths[i]} → {methods[i]}");
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Wire] 완료 — Ctrl+S 로 저장하세요");
    }
}
