#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FindFolderSettings", menuName = "CustomEditor/FindFolder")]
public class FindFolderSO : ScriptableObject
{
    [Serializable]
    public class FolderEntry
    {
        [HideInInspector] public string id;
        public string label; // 버튼에 표시할 이름
        public string path;  // 폴더 에셋 경로
    }

    [Serializable]
    public class FolderGroup
    {
        [HideInInspector] public string id;
        public string groupName;            // 그룹 이름
        public bool isExpanded = true;      // 접기/펼치기 상태
        public List<FolderEntry> entries = new(); // 그룹 내 폴더 목록
        public List<FolderGroup> subGroups = new(); // 중첩 그룹
    }

    public List<FolderGroup> folderGroups = new(); // 등록된 그룹 목록
}

#endif
