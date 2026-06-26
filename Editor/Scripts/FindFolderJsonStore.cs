#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class FindFolderJsonStore
{
    #region Types

    [Serializable]
    private class IndexData
    {
        public int version = 1;
        public List<string> groupIds = new();
        public List<string> entryIds = new();
    }

    [Serializable]
    private class GroupData
    {
        public string id;
        public string parentId;
        public int order;
        public string groupName;
        public bool isExpanded;
    }

    [Serializable]
    private class EntryData
    {
        public string id;
        public string groupId;
        public int order;
        public string label;
        public string path;
    }

    #endregion

    #region Fields

    private const string RootPath = "ProjectSettings/FindFolder";
    private const string GroupDirectoryName = "groups";
    private const string EntryDirectoryName = "entries";
    private const string IndexFileName = "index.json";

    #endregion

    #region Public Methods

    public static void LoadInto(FindFolderSO settings)
    {
        if (settings == null) return;

        string root = GetAbsolutePath(RootPath);
        string indexPath = Path.Combine(root, IndexFileName);
        if (!File.Exists(indexPath))
        {
            settings.folderGroups = new List<FindFolderSO.FolderGroup>();
            return;
        }

        var index = ReadJson<IndexData>(indexPath) ?? new IndexData();
        var groups = LoadGroupData(root, index);
        var entries = LoadEntryData(root, index);

        var groupById = groups.ToDictionary(g => g.id, g => new FindFolderSO.FolderGroup
        {
            id = g.id,
            groupName = g.groupName,
            isExpanded = g.isExpanded,
            entries = new List<FindFolderSO.FolderEntry>(),
            subGroups = new List<FindFolderSO.FolderGroup>()
        });

        foreach (var entryData in entries.OrderBy(e => e.order))
        {
            if (!groupById.TryGetValue(entryData.groupId, out var group)) continue;
            group.entries.Add(new FindFolderSO.FolderEntry
            {
                id = entryData.id,
                label = entryData.label,
                path = entryData.path
            });
        }

        var rootGroups = new List<FindFolderSO.FolderGroup>();
        foreach (var groupData in groups.OrderBy(g => g.order))
        {
            if (!groupById.TryGetValue(groupData.id, out var group)) continue;
            if (!string.IsNullOrEmpty(groupData.parentId) && groupById.TryGetValue(groupData.parentId, out var parent))
                parent.subGroups.Add(group);
            else
                rootGroups.Add(group);
        }

        settings.folderGroups = rootGroups;
    }

    public static void Save(FindFolderSO settings)
    {
        if (settings == null) return;

        EnsureIds(settings);

        var index = new IndexData();
        var groups = new List<GroupData>();
        var entries = new List<EntryData>();

        FlattenGroups(settings.folderGroups, "", groups, entries, index);

        string root = GetAbsolutePath(RootPath);
        string groupsRoot = Path.Combine(root, GroupDirectoryName);
        string entriesRoot = Path.Combine(root, EntryDirectoryName);
        Directory.CreateDirectory(groupsRoot);
        Directory.CreateDirectory(entriesRoot);

        WriteJson(Path.Combine(root, IndexFileName), index);
        WriteSplitFiles(groupsRoot, groups, g => g.id);
        WriteSplitFiles(entriesRoot, entries, e => e.id);
    }

    #endregion

    #region Private Methods

    private static List<GroupData> LoadGroupData(string root, IndexData index)
    {
        string groupsRoot = Path.Combine(root, GroupDirectoryName);
        return index.groupIds
            .Select(id => ReadJson<GroupData>(Path.Combine(groupsRoot, $"{id}.json")))
            .Where(g => g != null && !string.IsNullOrEmpty(g.id))
            .ToList();
    }

    private static List<EntryData> LoadEntryData(string root, IndexData index)
    {
        string entriesRoot = Path.Combine(root, EntryDirectoryName);
        return index.entryIds
            .Select(id => ReadJson<EntryData>(Path.Combine(entriesRoot, $"{id}.json")))
            .Where(e => e != null && !string.IsNullOrEmpty(e.id))
            .ToList();
    }

    private static void FlattenGroups(List<FindFolderSO.FolderGroup> source, string parentId,
        List<GroupData> groups, List<EntryData> entries, IndexData index)
    {
        if (source == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            var group = source[i];
            if (group == null) continue;

            groups.Add(new GroupData
            {
                id = group.id,
                parentId = parentId,
                order = i,
                groupName = group.groupName,
                isExpanded = group.isExpanded
            });
            index.groupIds.Add(group.id);

            if (group.entries != null)
            {
                for (int j = 0; j < group.entries.Count; j++)
                {
                    var entry = group.entries[j];
                    if (entry == null) continue;

                    entries.Add(new EntryData
                    {
                        id = entry.id,
                        groupId = group.id,
                        order = j,
                        label = entry.label,
                        path = entry.path
                    });
                    index.entryIds.Add(entry.id);
                }
            }

            FlattenGroups(group.subGroups, group.id, groups, entries, index);
        }
    }

    private static void EnsureIds(FindFolderSO settings)
    {
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        EnsureGroupIds(settings.folderGroups, usedIds);
    }

    private static void EnsureGroupIds(List<FindFolderSO.FolderGroup> groups, HashSet<string> usedIds)
    {
        if (groups == null) return;

        foreach (var group in groups)
        {
            if (group == null) continue;

            group.id = NormalizeId(group.id, usedIds);
            if (group.entries == null) group.entries = new List<FindFolderSO.FolderEntry>();
            if (group.subGroups == null) group.subGroups = new List<FindFolderSO.FolderGroup>();

            foreach (var entry in group.entries)
            {
                if (entry == null) continue;
                entry.id = NormalizeId(entry.id, usedIds);
            }

            EnsureGroupIds(group.subGroups, usedIds);
        }
    }

    private static string NormalizeId(string id, HashSet<string> usedIds)
    {
        if (string.IsNullOrWhiteSpace(id) || usedIds.Contains(id))
            id = Guid.NewGuid().ToString("N");

        usedIds.Add(id);
        return id;
    }

    private static void WriteSplitFiles<T>(string directory, List<T> values, Func<T, string> idGetter)
    {
        var activeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            string path = Path.Combine(directory, $"{idGetter(value)}.json");
            activeFiles.Add(path);
            WriteJson(path, value);
        }

        foreach (string path in Directory.GetFiles(directory, "*.json"))
        {
            if (!activeFiles.Contains(path))
                File.Delete(path);
        }
    }

    private static T ReadJson<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;

        try
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[FindFolder] Failed to read JSON: {path}\n{exception.Message}");
            return null;
        }
    }

    private static void WriteJson<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(value, true));
    }

    private static string GetAbsolutePath(string projectRelativePath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
    }

    #endregion
}

#endif
