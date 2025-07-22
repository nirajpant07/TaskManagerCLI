using System;
using System.Collections.Generic;

namespace TaskManager.CLI.Utilities
{
    public static class TaskAliasManager
    {
        private static Dictionary<int, Guid> _aliasToGuid = new();

        public static void SetAliases(List<Guid> guids)
        {
            _aliasToGuid.Clear();
            for (int i = 0; i < guids.Count; i++)
            {
                _aliasToGuid[i + 1] = guids[i];
            }
        }

        public static Guid? GetGuidByAlias(int alias)
        {
            if (_aliasToGuid.TryGetValue(alias, out var guid))
                return guid;
            return null;
        }

        public static void Clear()
        {
            _aliasToGuid.Clear();
        }
    }
} 