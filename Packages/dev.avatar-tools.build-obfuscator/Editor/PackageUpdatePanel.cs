// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    internal sealed class PackageUpdatePanel
    {
        private bool expanded;

        internal void Draw(InspectorLanguage language)
        {
            string channel = PackageUpdates.Channel;
            bool vpm = PackageUpdates.Vpm;
            string title = InspectorLocalization.Text("update.title", language) + "  " + PackageUpdates.CurrentVersion +
                (vpm ? " · VPM" : channel == null ? "" : " · " + channel + (PackageUpdates.Legacy ? " · .unitypackage" : " · Git")) +
                (PackageUpdates.Available ? " → " + PackageUpdates.Settings.latestVersion : "");
            expanded = EditorGUILayout.Foldout(expanded, title, true);
            if (!expanded) return;

            string key = vpm ? "update.vpm" : channel == null ? "update.unmanaged" :
                PackageUpdates.EditorBusy && !PackageUpdates.Working ? "update.busy" :
                PackageUpdates.Available && !PackageUpdates.Working && PackageUpdates.Status != "update.failed" ?
                "update.available" : PackageUpdates.Status;
            string status = InspectorLocalization.Text(key, language);
            if (!string.IsNullOrEmpty(PackageUpdates.Error))
                status += "\n" + PackageUpdates.Error;
            EditorGUILayout.HelpBox(status, MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope((!vpm && channel == null) || PackageUpdates.Working || PackageUpdates.EditorBusy))
                    if (GUILayout.Button(InspectorLocalization.Text(vpm ? "update.vpmGuide" : "update.check", language)))
                    {
                        if (vpm) Application.OpenURL(VpmInstallPolicy.GuideUrl);
                        else PackageUpdates.Check(true);
                    }
                using (new EditorGUI.DisabledScope(!PackageUpdates.Available || PackageUpdates.Working || PackageUpdates.EditorBusy))
                    if (GUILayout.Button(InspectorLocalization.Text("update.install", language))) PackageUpdates.Install(false);
                if (GUILayout.Button(InspectorLocalization.Text("update.notes", language)))
                    Application.OpenURL("https://github.com/" + UpdatePolicy.Repository + "/blob/" +
                        (UpdatePolicy.IsCommit(PackageUpdates.Settings.latestCommit) ? PackageUpdates.Settings.latestCommit : "main") + "/CHANGELOG.md");
            }

            using (new EditorGUI.DisabledScope(channel == null || PackageUpdates.Working))
            {
                bool checks = EditorGUILayout.Toggle(InspectorLocalization.Text("update.autoCheck", language), PackageUpdates.Settings.automaticChecks);
                if (checks != PackageUpdates.Settings.automaticChecks)
                { PackageUpdates.Settings.automaticChecks = checks; PackageUpdates.Settings.Persist(); }
                using (new EditorGUI.DisabledScope(!checks))
                {
                    bool install = EditorGUILayout.Toggle(new GUIContent(InspectorLocalization.Text("update.autoInstall", language),
                        InspectorLocalization.Text(vpm ? "update.vpm" : "update.autoInstall.tip", language)), PackageUpdates.Settings.automaticInstall);
                    if (install != PackageUpdates.Settings.automaticInstall)
                    { PackageUpdates.Settings.automaticInstall = install; PackageUpdates.Settings.Persist(); }
                }
            }
        }
    }
}
