// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Linq;
using UnityEditor;
namespace AvatarTools.Obfuscation.Validation
{
    public sealed class BatchImportObserver : AssetPostprocessor
    {
        public static bool Enabled;
        public static string Folder;
        public static int Calls, Files;
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!Enabled || string.IsNullOrEmpty(Folder)) return;
            int count=imported.Count(p=>p.StartsWith(Folder+"/",StringComparison.Ordinal));
            if(count>0){Calls++;Files+=count;}
        }
    }
}
