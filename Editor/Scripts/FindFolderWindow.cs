#if UNITY_EDITOR

using System;
using System.Reflection;
using ActionFit.SOSingleton.Editor;
using UnityEditor;
using UnityEngine;

public class FindFolderWindow : EditorWindow
{
    #region Fields

    private const string EditorPrefsKey = "FindFolder_SettingsPath";

    private FindFolderSO _settingSO;
    private Vector2 _scrollPosition;

    #endregion

    #region Window

    [MenuItem("Tools/Package/Find Folder/Open Window", false, 20)]
    public static void ShowWindow()
    {
        var window = GetWindow<FindFolderWindow>("Find Folder");
        window.minSize = new Vector2(200, 150);
        window.Show();
    }

    private void OnEnable()
    {
        LoadOrCreateSO();
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        if (_settingSO == null)
        {
            LoadOrCreateSO();
            if (_settingSO == null)
            {
                EditorGUILayout.HelpBox("Settings asset could not be loaded.", MessageType.Error);
                return;
            }
        }

        DrawToolbar();
        EditorGUILayout.Space(5);
        DrawFolderGroups();
    }

    #endregion

    #region Draw Methods

    // 상단 툴바 (Edit 버튼)
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            FindFolderSettingsWindow.ShowWindow(_settingSO);
        }

        EditorGUILayout.EndHorizontal();
    }

    // 그룹별 폴더 바로가기 버튼 목록
    private void DrawFolderGroups()
    {
        if (_settingSO.folderGroups == null || _settingSO.folderGroups.Count == 0)
        {
            EditorGUILayout.HelpBox("No folders registered. Click 'Edit' to add folders.", MessageType.Info);
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawGroupsRecursive(_settingSO.folderGroups, 0);
        EditorGUILayout.EndScrollView();
    }

    // 그룹을 재귀적으로 렌더링 (중첩 그룹 지원)
    private void DrawGroupsRecursive(System.Collections.Generic.List<FindFolderSO.FolderGroup> groups, int depth)
    {
        foreach (var group in groups)
        {
            if (group == null) continue;

            bool hasContent = (group.entries != null && group.entries.Count > 0)
                           || (group.subGroups != null && group.subGroups.Count > 0);
            if (!hasContent) continue;

            string displayName = string.IsNullOrEmpty(group.groupName) ? "Unnamed Group" : group.groupName;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 12);

            EditorGUI.BeginChangeCheck();
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, displayName, true, EditorStyles.foldoutHeader);
            if (EditorGUI.EndChangeCheck())
            {
                FindFolderJsonStore.Save(_settingSO);
            }

            EditorGUILayout.EndHorizontal();

            if (!group.isExpanded) continue;

            // 엔트리 버튼
            if (group.entries != null)
            {
                foreach (var entry in group.entries)
                {
                    string path = ResolveEntryPath(entry);
                    if (string.IsNullOrEmpty(path)) continue;

                    string buttonLabel = string.IsNullOrEmpty(entry.label) ? path : entry.label;

                    Rect buttonRect = EditorGUILayout.GetControlRect(false, 26);
                    float indent = (depth + 1) * 12 + 12;
                    buttonRect.x += indent;
                    buttonRect.width -= indent;

                    if (GUI.Button(buttonRect, buttonLabel))
                    {
                        NavigateToEntry(entry);
                    }
                }
            }

            // 서브그룹 재귀 렌더링
            if (group.subGroups != null && group.subGroups.Count > 0)
            {
                DrawGroupsRecursive(group.subGroups, depth + 1);
            }

            EditorGUILayout.Space(3);
        }
    }

    #endregion

    #region Private Methods

    // Project 창에서 해당 엔트리로 이동하고, 폴더는 내부 콘텐츠를 엽니다.
    private void NavigateToEntry(FindFolderSO.FolderEntry entry)
    {
        string path = ResolveEntryPath(entry, true);
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (obj == null)
        {
            UnityEngine.Debug.LogWarning($"[FindFolder] Asset not found: {path}");
            return;
        }

        Selection.activeObject = obj;
        EditorUtility.FocusProjectWindow();

        if (AssetDatabase.IsValidFolder(path) && TryShowFolderContents(obj))
            return;

        EditorGUIUtility.PingObject(obj);
    }

    // Unity Project Browser에서 폴더 내부 콘텐츠를 표시합니다.
    private static bool TryShowFolderContents(UnityEngine.Object folderObject)
    {
        Type projectBrowserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
        if (projectBrowserType == null) return false;

        EditorWindow projectBrowser = EditorWindow.GetWindow(projectBrowserType);
        if (projectBrowser == null) return false;

        MethodInfo showFolderContents = projectBrowserType.GetMethod(
            "ShowFolderContents",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(int), typeof(bool) },
            null);

        if (showFolderContents == null) return false;

        try
        {
            showFolderContents.Invoke(projectBrowser, new object[] { folderObject.GetInstanceID(), true });
            projectBrowser.Repaint();
            return true;
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[FindFolder] Failed to open folder contents: {exception.Message}");
            return false;
        }
    }

    private string ResolveEntryPath(FindFolderSO.FolderEntry entry, bool saveIfChanged = false)
    {
        if (entry == null) return "";

        string path = entry.path;
        if (!string.IsNullOrWhiteSpace(entry.guid))
        {
            string currentPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                path = currentPath;
                if (!string.Equals(entry.path, currentPath, StringComparison.Ordinal))
                {
                    entry.path = currentPath;
                    if (saveIfChanged)
                        FindFolderJsonStore.Save(_settingSO);
                }
            }
        }

        return path;
    }

    // SO 로드 후 JSON 설정 적용
    private void LoadOrCreateSO()
    {
        _settingSO = ActionFitSettingsAssetProvider.GetOrCreate<FindFolderSO>();
        if (_settingSO != null)
        {
            EditorPrefs.SetString(EditorPrefsKey, AssetDatabase.GetAssetPath(_settingSO));
            FindFolderJsonStore.LoadInto(_settingSO);
        }
    }

    #endregion
}

#endif
