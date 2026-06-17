#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class FindFolderWindow : EditorWindow
{
    #region Fields

    private const string EditorPrefsKey = "FindFolder_SettingsPath";
    private const string DefaultSOPath = "Assets/Editor/FindFolder/FindFolderSettings.asset";

    private FindFolderSO _settingSO;
    private Vector2 _scrollPosition;

    #endregion

    #region Window

    [MenuItem("Tools/Find Folder")]
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
                EditorUtility.SetDirty(_settingSO);
            }

            EditorGUILayout.EndHorizontal();

            if (!group.isExpanded) continue;

            // 엔트리 버튼
            if (group.entries != null)
            {
                foreach (var entry in group.entries)
                {
                    if (string.IsNullOrEmpty(entry.path)) continue;

                    string buttonLabel = string.IsNullOrEmpty(entry.label) ? entry.path : entry.label;

                    Rect buttonRect = EditorGUILayout.GetControlRect(false, 26);
                    float indent = (depth + 1) * 12 + 12;
                    buttonRect.x += indent;
                    buttonRect.width -= indent;

                    if (GUI.Button(buttonRect, buttonLabel))
                    {
                        NavigateToFolder(entry.path);
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

    // Project 탭에서 해당 폴더로 이동
    private void NavigateToFolder(string path)
    {
        var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (obj == null)
        {
            Debug.LogWarning($"[FindFolder] Folder not found: {path}");
            return;
        }

        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);
        EditorUtility.FocusProjectWindow();
    }

    // SO 로드 또는 자동 생성
    private void LoadOrCreateSO()
    {
        string savedPath = EditorPrefs.GetString(EditorPrefsKey, "");

        if (!string.IsNullOrEmpty(savedPath))
        {
            _settingSO = AssetDatabase.LoadAssetAtPath<FindFolderSO>(savedPath);
            if (_settingSO != null) return;
        }

        // 기본 경로에서 로드 시도
        _settingSO = AssetDatabase.LoadAssetAtPath<FindFolderSO>(DefaultSOPath);
        if (_settingSO != null)
        {
            EditorPrefs.SetString(EditorPrefsKey, DefaultSOPath);
            return;
        }

        // 타입으로 프로젝트 전체 탐색 (에셋 위치 무관)
        var guids = AssetDatabase.FindAssets($"t:{nameof(FindFolderSO)}");
        if (guids.Length > 0)
        {
            string foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            _settingSO = AssetDatabase.LoadAssetAtPath<FindFolderSO>(foundPath);
            if (_settingSO != null)
            {
                EditorPrefs.SetString(EditorPrefsKey, foundPath);
                return;
            }
        }

        // SO 자동 생성
        _settingSO = CreateInstance<FindFolderSO>();

        string directory = System.IO.Path.GetDirectoryName(DefaultSOPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(_settingSO, DefaultSOPath);
        AssetDatabase.SaveAssets();
        EditorPrefs.SetString(EditorPrefsKey, DefaultSOPath);
        Debug.Log($"[FindFolder] Settings asset created: {DefaultSOPath}");
    }

    #endregion
}

#endif
