#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FindFolderSettings", menuName = "CustomEditor/FindFolder")]
public class FindFolderSO : ScriptableObject
{
    public enum StorageScope
    {
        Shared,
        Local
    }

    [Serializable]
    public class FolderEntry
    {
        [HideInInspector] public string id;
        public string label; // 버튼에 표시할 이름
        public string guid;  // 이동/이름 변경에도 유지되는 에셋 GUID
        public string path;  // 사람이 읽기 위한 현재 에셋 경로 캐시
    }

    [Serializable]
    public class FolderGroup
    {
        [HideInInspector] public string id;
        [HideInInspector] public StorageScope storageScope = StorageScope.Shared;
        public string groupName;            // 그룹 이름
        public bool isExpanded = true;      // 접기/펼치기 상태
        public List<FolderEntry> entries = new(); // 그룹 내 폴더 목록
        public List<FolderGroup> subGroups = new(); // 중첩 그룹
    }

    public List<FolderGroup> folderGroups = new(); // 등록된 그룹 목록
}

#endif
