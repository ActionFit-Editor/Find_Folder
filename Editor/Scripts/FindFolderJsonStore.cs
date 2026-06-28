#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FindFolderJsonStore
{
    #region Types

    [Serializable]
    private class RootGroupFile
    {
        public int version = 2;
        public string id;
        public string groupName;
        public bool isExpanded = true;
        public List<GroupData> groups = new();
        public List<EntryData> entries = new();
    }

    [Serializable]
    private class GroupData
    {
        public string id;
        public string parentId;
        public string groupName;
        public bool isExpanded = true;
    }

    [Serializable]
    private class EntryData
    {
        public string id;
        public string parentId;
        public string label;
        public string guid;
        public string path;
    }

    #endregion

    #region Fields

    private const string SharedPath = "Assets/_Data/FindFolder/Shared";
    private const string LocalPath = "UserSettings/FindFolder/Local";

    private static readonly HashSet<string> LoadedSharedRootIds = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> LoadedLocalRootIds = new(StringComparer.OrdinalIgnoreCase);

    #endregion

    #region Public Methods

    public static void LoadInto(FindFolderSO settings)
    {
        if (settings == null) return;

        LoadedSharedRootIds.Clear();
        LoadedLocalRootIds.Clear();

        var rootGroups = new Dictionary<string, FindFolderSO.FolderGroup>(StringComparer.OrdinalIgnoreCase);
        LoadRootDirectory(SharedPath, FindFolderSO.StorageScope.Shared, rootGroups, LoadedSharedRootIds);
        LoadRootDirectory(LocalPath, FindFolderSO.StorageScope.Local, rootGroups, LoadedLocalRootIds);

        settings.folderGroups = rootGroups.Values
            .OrderBy(g => g.groupName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (SyncEntryAssetReferences(settings))
            Save(settings);
    }

    public static void Save(FindFolderSO settings)
    {
        if (settings == null) return;

        EnsureIds(settings);
        SyncEntryAssetReferences(settings);

        string sharedDirectory = GetAbsolutePath(SharedPath);
        string localDirectory = GetAbsolutePath(LocalPath);
        Directory.CreateDirectory(sharedDirectory);
        Directory.CreateDirectory(localDirectory);

        var activeSharedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activeLocalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootGroup in settings.folderGroups.Where(g => g != null))
        {
            var file = CreateRootFile(rootGroup);
            if (rootGroup.storageScope == FindFolderSO.StorageScope.Local)
            {
                activeLocalIds.Add(rootGroup.id);
                WriteJson(GetRootFilePath(LocalPath, rootGroup.id), file);
            }
            else
            {
                activeSharedIds.Add(rootGroup.id);
                WriteJson(GetRootFilePath(SharedPath, rootGroup.id), file);

                string localOverridePath = GetRootFilePath(LocalPath, rootGroup.id);
                if (File.Exists(localOverridePath))
                    File.Delete(localOverridePath);
            }
        }

        DeleteInactiveLocalFiles(activeLocalIds);
        DeleteInactiveSharedFiles(activeSharedIds, activeLocalIds);
        AssetDatabase.Refresh();
    }

    #endregion

    #region Private Methods

    private static void LoadRootDirectory(string projectRelativeDirectory, FindFolderSO.StorageScope scope,
        Dictionary<string, FindFolderSO.FolderGroup> rootGroups, HashSet<string> loadedIds)
    {
        string directory = GetAbsolutePath(projectRelativeDirectory);
        if (!Directory.Exists(directory)) return;

        foreach (string path in Directory.GetFiles(directory, "*.json"))
        {
            var file = ReadJson<RootGroupFile>(path);
            if (file == null || string.IsNullOrEmpty(file.id)) continue;

            loadedIds.Add(file.id);
            rootGroups[file.id] = CreateRootGroup(file, scope);
        }
    }

    private static FindFolderSO.FolderGroup CreateRootGroup(RootGroupFile file, FindFolderSO.StorageScope scope)
    {
        var root = new FindFolderSO.FolderGroup
        {
            id = file.id,
            storageScope = scope,
            groupName = file.groupName,
            isExpanded = file.isExpanded,
            entries = new List<FindFolderSO.FolderEntry>(),
            subGroups = new List<FindFolderSO.FolderGroup>()
        };

        var groupById = new Dictionary<string, FindFolderSO.FolderGroup>(StringComparer.OrdinalIgnoreCase)
        {
            [root.id] = root
        };

        if (file.groups == null) file.groups = new List<GroupData>();
        if (file.entries == null) file.entries = new List<EntryData>();

        foreach (var groupData in file.groups)
        {
            if (string.IsNullOrEmpty(groupData.id)) continue;
            groupById[groupData.id] = new FindFolderSO.FolderGroup
            {
                id = groupData.id,
                storageScope = scope,
                groupName = groupData.groupName,
                isExpanded = groupData.isExpanded,
                entries = new List<FindFolderSO.FolderEntry>(),
                subGroups = new List<FindFolderSO.FolderGroup>()
            };
        }

        foreach (var groupData in file.groups)
        {
            if (!groupById.TryGetValue(groupData.id, out var group)) continue;
            if (string.IsNullOrEmpty(groupData.parentId) || !groupById.TryGetValue(groupData.parentId, out var parent))
                parent = root;

            parent.subGroups.Add(group);
        }

        foreach (var entryData in file.entries)
        {
            if (string.IsNullOrEmpty(entryData.id)) continue;
            if (string.IsNullOrEmpty(entryData.parentId) || !groupById.TryGetValue(entryData.parentId, out var parent))
                parent = root;

            parent.entries.Add(new FindFolderSO.FolderEntry
            {
                id = entryData.id,
                label = entryData.label,
                guid = entryData.guid,
                path = entryData.path
            });
        }

        return root;
    }

    private static RootGroupFile CreateRootFile(FindFolderSO.FolderGroup rootGroup)
    {
        var file = new RootGroupFile
        {
            id = rootGroup.id,
            groupName = rootGroup.groupName,
            isExpanded = rootGroup.isExpanded,
            groups = new List<GroupData>(),
            entries = new List<EntryData>()
        };

        AddEntries(file.entries, rootGroup.entries, rootGroup.id);
        FlattenChildGroups(rootGroup.subGroups, rootGroup.id, file.groups, file.entries);
        return file;
    }

    private static void FlattenChildGroups(List<FindFolderSO.FolderGroup> source, string parentId,
        List<GroupData> groups, List<EntryData> entries)
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
                groupName = group.groupName,
                isExpanded = group.isExpanded
            });

            AddEntries(entries, group.entries, group.id);
            FlattenChildGroups(group.subGroups, group.id, groups, entries);
        }
    }

    private static void AddEntries(List<EntryData> target, List<FindFolderSO.FolderEntry> source, string parentId)
    {
        if (source == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            var entry = source[i];
            if (entry == null) continue;

            target.Add(new EntryData
            {
                id = entry.id,
                parentId = parentId,
                label = entry.label,
                guid = entry.guid,
                path = entry.path
            });
        }
    }

    public static bool SyncEntryAssetReferences(FindFolderSO settings)
    {
        if (settings == null) return false;
        return SyncEntryAssetReferences(settings.folderGroups);
    }

    private static bool SyncEntryAssetReferences(List<FindFolderSO.FolderGroup> groups)
    {
        if (groups == null) return false;

        bool changed = false;
        foreach (var group in groups)
        {
            if (group == null) continue;

            if (group.entries != null)
            {
                foreach (var entry in group.entries)
                {
                    if (entry == null) continue;
                    changed |= SyncEntryAssetReference(entry);
                }
            }

            changed |= SyncEntryAssetReferences(group.subGroups);
        }

        return changed;
    }

    private static bool SyncEntryAssetReference(FindFolderSO.FolderEntry entry)
    {
        bool changed = false;

        if (string.IsNullOrWhiteSpace(entry.guid) && !string.IsNullOrWhiteSpace(entry.path))
        {
            string guid = AssetDatabase.AssetPathToGUID(entry.path);
            if (!string.IsNullOrWhiteSpace(guid))
            {
                entry.guid = guid;
                changed = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.guid))
        {
            string currentPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            if (!string.IsNullOrWhiteSpace(currentPath) &&
                !string.Equals(entry.path, currentPath, StringComparison.Ordinal))
            {
                entry.path = currentPath;
                changed = true;
            }
        }

        return changed;
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

    private static void DeleteInactiveLocalFiles(HashSet<string> activeLocalIds)
    {
        foreach (string id in LoadedLocalRootIds)
        {
            if (activeLocalIds.Contains(id)) continue;

            string path = GetRootFilePath(LocalPath, id);
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void DeleteInactiveSharedFiles(HashSet<string> activeSharedIds, HashSet<string> activeLocalIds)
    {
        foreach (string id in LoadedSharedRootIds)
        {
            if (activeSharedIds.Contains(id)) continue;
            if (activeLocalIds.Contains(id)) continue;
            if (LoadedLocalRootIds.Contains(id)) continue;

            string path = GetRootFilePath(SharedPath, id);
            if (File.Exists(path))
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

    private static string GetRootFilePath(string projectRelativeDirectory, string id)
    {
        return Path.Combine(GetAbsolutePath(projectRelativeDirectory), $"{id}.json");
    }

    private static string GetAbsolutePath(string projectRelativePath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
    }

    #endregion
}

#endif
