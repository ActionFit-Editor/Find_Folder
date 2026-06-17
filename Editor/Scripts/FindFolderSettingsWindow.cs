#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FindFolderSettingsWindow : EditorWindow
{
    #region Types

    private enum DragItemType { None, Entry, Group }

    #endregion

    #region Fields

    private FindFolderSO _settingSO;
    private Vector2 _scrollPosition;

    // 드래그 소스
    private DragItemType _dragType;
    private List<FindFolderSO.FolderGroup> _dragSourceParentList; // 그룹 드래그: 소속 부모 리스트
    private FindFolderSO.FolderGroup _dragSourceGroup; // 엔트리 드래그: 소속 그룹
    private int _dragFromIndex;
    private bool _isDragging;

    // 드롭 타겟 (렌더링 중 감지)
    private FindFolderSO.FolderGroup _hoverMoveToGroup; // 그룹 헤더 위 드롭 (이동)
    private FindFolderSO.FolderGroup _hoverReorderGroup; // 엔트리 재정렬 대상 그룹
    private int _hoverReorderIndex = -1; // 재정렬 삽입 인덱스
    private List<FindFolderSO.FolderGroup> _hoverReorderParentList; // 그룹 재정렬 대상 부모 리스트
    private int _hoverReorderGroupIndex = -1; // 그룹 재정렬 삽입 인덱스

    #endregion

    #region Window

    /// <summary>
    /// 설정 윈도우를 열고 SO 참조를 전달합니다.
    /// </summary>
    public static void ShowWindow(FindFolderSO so)
    {
        var window = GetWindow<FindFolderSettingsWindow>(true, "Find Folder Settings", true);
        window._settingSO = so;
        window.minSize = new Vector2(420, 350);
        window.Show();
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        if (_settingSO == null)
        {
            EditorGUILayout.HelpBox("No settings asset assigned.", MessageType.Error);
            return;
        }

        Event evt = Event.current;

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Folder Settings", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Group", GUILayout.Width(70)))
            AddGroup(_settingSO.folderGroups);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 매 프레임 hover 타겟 초기화
        _hoverMoveToGroup = null;
        _hoverReorderGroup = null;
        _hoverReorderIndex = -1;
        _hoverReorderParentList = null;
        _hoverReorderGroupIndex = -1;

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // 렌더링 + 인라인 hover 감지 (ScrollView 내부에서 좌표 일치)
        DrawGroupListRecursive(_settingSO.folderGroups, 0, evt);

        // 드래그 중 마우스 이벤트 처리 (ScrollView 내부)
        if (_isDragging)
        {
            if (evt.type == EventType.MouseDrag)
            {
                Repaint();
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp)
            {
                FinishDrag();
                evt.Use();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region Draw Methods

    // 그룹 목록을 재귀적으로 렌더링
    private void DrawGroupListRecursive(List<FindFolderSO.FolderGroup> groups, int depth, Event evt)
    {
        int groupToRemove = -1;

        for (int g = 0; g < groups.Count; g++)
        {
            var group = groups[g];
            if (group == null) continue;

            // 그룹 재정렬 드롭 인디케이터 (항상 슬롯 예약)
            DrawIndicatorSlot(ShouldShowGroupReorderIndicator(groups, g), depth);

            // 그룹 헤더
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(depth * 16);

            // 드래그 핸들
            Rect handle = GUILayoutUtility.GetRect(18, 20, GUILayout.Width(18));
            EditorGUI.LabelField(handle, "\u2261");
            EditorGUIUtility.AddCursorRect(handle, MouseCursor.Pan);

            if (evt.type == EventType.MouseDown && handle.Contains(evt.mousePosition))
            {
                StartGroupDrag(groups, g);
                evt.Use();
            }

            EditorGUI.BeginChangeCheck();
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, "", true);
            if (EditorGUI.EndChangeCheck()) SaveSO();

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField(group.groupName, EditorStyles.toolbarTextField);
            if (EditorGUI.EndChangeCheck())
            {
                group.groupName = newName;
                SaveSO();
            }

            if (GUILayout.Button("+G", EditorStyles.toolbarButton, GUILayout.Width(28)))
                AddGroup(group.subGroups);

            if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(22)))
                groupToRemove = g;

            EditorGUILayout.EndHorizontal();

            // 그룹 헤더 hover 감지 (인라인)
            Rect headerRect = GUILayoutUtility.GetLastRect();
            DetectGroupHeaderHover(headerRect, group, groups, g, evt);

            if (!group.isExpanded) continue;

            DrawFolderDropArea(group, depth);
            DrawEntryList(group, depth, evt);

            if (group.subGroups != null && group.subGroups.Count > 0)
                DrawGroupListRecursive(group.subGroups, depth + 1, evt);

            EditorGUILayout.Space(3);
        }

        // 리스트 끝 드롭 인디케이터 (항상 슬롯 예약)
        DrawIndicatorSlot(ShouldShowGroupReorderIndicatorEnd(groups), depth);

        if (groupToRemove != -1)
        {
            groups.RemoveAt(groupToRemove);
            SaveSO();
            GUIUtility.ExitGUI();
        }
    }

    // 엔트리 목록
    private void DrawEntryList(FindFolderSO.FolderGroup group, int depth, Event evt)
    {
        int indexToRemove = -1;

        for (int i = 0; i < group.entries.Count; i++)
        {
            var entry = group.entries[i];

            // 엔트리 재정렬 드롭 인디케이터 (항상 슬롯 예약)
            DrawIndicatorSlot(ShouldShowEntryReorderIndicator(group, i), depth + 1);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space((depth + 1) * 16 + 4);

            // 드래그 핸들
            Rect handle = GUILayoutUtility.GetRect(18, 20, GUILayout.Width(18));
            EditorGUI.LabelField(handle, "\u2261");
            EditorGUIUtility.AddCursorRect(handle, MouseCursor.Pan);

            if (evt.type == EventType.MouseDown && handle.Contains(evt.mousePosition))
            {
                StartEntryDrag(group, i);
                evt.Use();
            }

            EditorGUI.BeginChangeCheck();
            string newLabel = EditorGUILayout.TextField(entry.label, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                entry.label = newLabel;
                SaveSO();
            }

            var currentAsset = AssetDatabase.LoadAssetAtPath<Object>(entry.path);
            EditorGUI.BeginChangeCheck();
            var newAsset = EditorGUILayout.ObjectField(currentAsset, typeof(Object), false);
            if (EditorGUI.EndChangeCheck() && newAsset != null)
            {
                string newPath = AssetDatabase.GetAssetPath(newAsset);
                if (!string.IsNullOrEmpty(newPath))
                {
                    entry.path = newPath;
                    SaveSO();
                }
            }

            if (GUILayout.Button("-", GUILayout.Width(22)))
                indexToRemove = i;

            EditorGUILayout.EndHorizontal();

            // 엔트리 행 hover 감지 (인라인)
            Rect rowRect = GUILayoutUtility.GetLastRect();
            DetectEntryRowHover(rowRect, group, i, evt);
        }

        // 리스트 끝 드롭 인디케이터 (항상 슬롯 예약)
        DrawIndicatorSlot(ShouldShowEntryReorderIndicatorEnd(group), depth + 1);

        if (indexToRemove != -1)
        {
            group.entries.RemoveAt(indexToRemove);
            SaveSO();
            GUIUtility.ExitGUI();
        }
    }

    // 에셋 드래그 앤 드롭 영역
    private void DrawFolderDropArea(FindFolderSO.FolderGroup group, int depth)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space((depth + 1) * 16);

        Rect dropArea = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        var style = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10
        };
        GUI.Box(dropArea, "Drag & Drop Here (Folder / Any File)", style);

        EditorGUILayout.EndHorizontal();

        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated)
        {
            if (DragAndDrop.objectReferences.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                    AddFolderEntry(group, path);
            }
            evt.Use();
        }
    }

    // 드롭 인디케이터 슬롯 (항상 공간 예약, 조건에 따라 색상 표시)
    private void DrawIndicatorSlot(bool visible, int depth)
    {
        if (!_isDragging) return;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(depth * 16);
        Rect lineRect = GUILayoutUtility.GetRect(0, 2, GUILayout.ExpandWidth(true));
        if (visible)
            EditorGUI.DrawRect(lineRect, new Color(0.2f, 0.6f, 1f, 0.8f));
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Drag — Inline Hover Detection

    // 그룹 헤더 위 마우스 감지 (3구역: 상단=위에 삽입, 중앙=안에 넣기, 하단=아래에 삽입)
    private void DetectGroupHeaderHover(Rect headerRect, FindFolderSO.FolderGroup group,
        List<FindFolderSO.FolderGroup> parentList, int index, Event evt)
    {
        if (!_isDragging || evt.type == EventType.Layout) return;
        if (!headerRect.Contains(evt.mousePosition)) return;

        // 엔트리 드래그 → 그룹 헤더 중앙 = 해당 그룹으로 이동
        if (_dragType == DragItemType.Entry)
        {
            if (group != _dragSourceGroup)
            {
                _hoverMoveToGroup = group;
                EditorGUI.DrawRect(headerRect, new Color(0.2f, 0.6f, 1f, 0.15f));
            }
            return;
        }

        // 그룹 드래그
        if (_dragType != DragItemType.Group) return;

        var draggedGroup = _dragSourceParentList[_dragFromIndex];
        if (group == draggedGroup || IsDescendant(group, draggedGroup)) return;

        // 3구역 판정
        float relativeY = (evt.mousePosition.y - headerRect.yMin) / headerRect.height;

        if (relativeY < 0.25f)
        {
            // 상단 25%: 이 그룹 위에 삽입 (같은 레벨)
            _hoverReorderParentList = parentList;
            _hoverReorderGroupIndex = index;
        }
        else if (relativeY > 0.75f)
        {
            // 하단 25%: 이 그룹 아래에 삽입 (같은 레벨)
            _hoverReorderParentList = parentList;
            _hoverReorderGroupIndex = index + 1;
        }
        else
        {
            // 중앙 50%: 이 그룹 안에 넣기 (서브그룹)
            _hoverMoveToGroup = group;
            EditorGUI.DrawRect(headerRect, new Color(0.2f, 0.6f, 1f, 0.15f));
        }
    }

    // 엔트리 행 위 마우스 감지: 같은 그룹 내 재정렬 위치
    private void DetectEntryRowHover(Rect rowRect, FindFolderSO.FolderGroup group, int index, Event evt)
    {
        if (!_isDragging || _dragType != DragItemType.Entry) return;
        if (evt.type == EventType.Layout) return;
        if (_dragSourceGroup != group) return;
        if (!rowRect.Contains(evt.mousePosition)) return;

        float midY = rowRect.center.y;
        _hoverReorderGroup = group;
        _hoverReorderIndex = evt.mousePosition.y < midY ? index : index + 1;
    }

    #endregion

    #region Drag — Indicator Conditions

    private bool ShouldShowEntryReorderIndicator(FindFolderSO.FolderGroup group, int index)
    {
        return _isDragging && _dragType == DragItemType.Entry
            && _hoverMoveToGroup == null
            && _hoverReorderGroup == group && _hoverReorderIndex == index
            && !(_dragSourceGroup == group && _dragFromIndex == index);
    }

    private bool ShouldShowEntryReorderIndicatorEnd(FindFolderSO.FolderGroup group)
    {
        return _isDragging && _dragType == DragItemType.Entry
            && _hoverMoveToGroup == null
            && _hoverReorderGroup == group && _hoverReorderIndex >= group.entries.Count
            && !(_dragSourceGroup == group && _dragFromIndex >= group.entries.Count);
    }

    private bool ShouldShowGroupReorderIndicator(List<FindFolderSO.FolderGroup> parentList, int index)
    {
        return _isDragging && _dragType == DragItemType.Group
            && _hoverMoveToGroup == null
            && _hoverReorderParentList == parentList && _hoverReorderGroupIndex == index
            && !(_dragSourceParentList == parentList && _dragFromIndex == index);
    }

    private bool ShouldShowGroupReorderIndicatorEnd(List<FindFolderSO.FolderGroup> parentList)
    {
        return _isDragging && _dragType == DragItemType.Group
            && _hoverMoveToGroup == null
            && _hoverReorderParentList == parentList && _hoverReorderGroupIndex >= parentList.Count;
    }

    #endregion

    #region Drag — Start & Finish

    private void StartEntryDrag(FindFolderSO.FolderGroup group, int index)
    {
        _dragType = DragItemType.Entry;
        _dragSourceParentList = null;
        _dragSourceGroup = group;
        _dragFromIndex = index;
        _isDragging = true;
    }

    private void StartGroupDrag(List<FindFolderSO.FolderGroup> parentList, int index)
    {
        _dragType = DragItemType.Group;
        _dragSourceParentList = parentList;
        _dragSourceGroup = null;
        _dragFromIndex = index;
        _isDragging = true;
    }

    private void FinishDrag()
    {
        if (_dragType == DragItemType.Entry)
            FinishEntryDrag();
        else if (_dragType == DragItemType.Group)
            FinishGroupDrag();

        _dragType = DragItemType.None;
        _dragSourceParentList = null;
        _dragSourceGroup = null;
        _dragFromIndex = -1;
        _isDragging = false;
        _hoverMoveToGroup = null;
    }

    private void FinishEntryDrag()
    {
        if (_dragSourceGroup == null || _dragFromIndex < 0
            || _dragFromIndex >= _dragSourceGroup.entries.Count) return;

        var entry = _dragSourceGroup.entries[_dragFromIndex];

        // 다른 그룹으로 이동
        if (_hoverMoveToGroup != null && _hoverMoveToGroup != _dragSourceGroup)
        {
            _dragSourceGroup.entries.RemoveAt(_dragFromIndex);
            _hoverMoveToGroup.entries.Add(entry);
            SaveSO();
            return;
        }

        // 같은 그룹 내 재정렬
        if (_hoverReorderGroup == _dragSourceGroup && _hoverReorderIndex >= 0
            && _dragFromIndex != _hoverReorderIndex)
        {
            var entries = _dragSourceGroup.entries;
            entries.RemoveAt(_dragFromIndex);
            int insert = _hoverReorderIndex > _dragFromIndex ? _hoverReorderIndex - 1 : _hoverReorderIndex;
            insert = Mathf.Clamp(insert, 0, entries.Count);
            entries.Insert(insert, entry);
            SaveSO();
        }
    }

    private void FinishGroupDrag()
    {
        if (_dragSourceParentList == null || _dragFromIndex < 0
            || _dragFromIndex >= _dragSourceParentList.Count) return;

        var group = _dragSourceParentList[_dragFromIndex];

        // 다른 그룹의 서브그룹으로 이동
        if (_hoverMoveToGroup != null && _hoverMoveToGroup != group
            && !IsDescendant(_hoverMoveToGroup, group))
        {
            _dragSourceParentList.RemoveAt(_dragFromIndex);
            _hoverMoveToGroup.subGroups.Add(group);
            SaveSO();
            return;
        }

        // 레벨 간 이동 또는 같은 레벨 재정렬
        if (_hoverReorderParentList != null && _hoverReorderGroupIndex >= 0)
        {
            bool sameList = _hoverReorderParentList == _dragSourceParentList;
            if (sameList && _dragFromIndex == _hoverReorderGroupIndex) return;

            _dragSourceParentList.RemoveAt(_dragFromIndex);

            int insert = _hoverReorderGroupIndex;
            // 같은 리스트 내 이동 시 인덱스 보정
            if (sameList && insert > _dragFromIndex) insert--;
            insert = Mathf.Clamp(insert, 0, _hoverReorderParentList.Count);

            _hoverReorderParentList.Insert(insert, group);
            SaveSO();
        }
    }

    private bool IsDescendant(FindFolderSO.FolderGroup target, FindFolderSO.FolderGroup source)
    {
        if (source.subGroups == null) return false;
        foreach (var sub in source.subGroups)
        {
            if (sub == target) return true;
            if (IsDescendant(target, sub)) return true;
        }
        return false;
    }

    #endregion

    #region Private Methods

    private void AddGroup(List<FindFolderSO.FolderGroup> targetList)
    {
        targetList.Add(new FindFolderSO.FolderGroup
        {
            groupName = "New Group",
            isExpanded = true
        });
        SaveSO();
    }

    private void AddFolderEntry(FindFolderSO.FolderGroup group, string path)
    {
        if (group.entries.Exists(e => e.path == path)) return;

        group.entries.Add(new FindFolderSO.FolderEntry
        {
            label = Path.GetFileName(path),
            path = path
        });

        SaveSO();
        Debug.Log($"[FindFolder] Entry added: {path}");
    }

    private void SaveSO()
    {
        EditorUtility.SetDirty(_settingSO);
        AssetDatabase.SaveAssets();
    }

    #endregion
}

#endif
