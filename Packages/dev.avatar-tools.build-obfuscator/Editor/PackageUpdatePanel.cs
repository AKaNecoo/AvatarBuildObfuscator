// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarTools.Obfuscation.Editor
{
    internal sealed class PackageUpdatePanel : VisualElement
    {
        private readonly Foldout details = new Foldout { value = false, name = "avatar-obfuscator-updates" };
        private readonly Label status = new Label();
        private readonly Toggle checks = new Toggle(), install = new Toggle();
        private readonly Button check, update, notes;
        private InspectorLanguage language;

        internal PackageUpdatePanel(InspectorLanguage selected)
        {
            language = selected;
            Add(details); details.Add(status);
            status.style.whiteSpace = WhiteSpace.Normal;
            var buttons = new VisualElement(); buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexWrap = Wrap.Wrap;
            check = new Button(() => { if (PackageUpdates.Vpm) Application.OpenURL(VpmInstallPolicy.GuideUrl); else PackageUpdates.Check(true); });
            update = new Button(() => PackageUpdates.Install(false));
            notes = new Button(() => Application.OpenURL("https://github.com/" + UpdatePolicy.Repository + "/blob/" +
                (UpdatePolicy.IsCommit(PackageUpdates.Settings.latestCommit) ? PackageUpdates.Settings.latestCommit : "main") + "/CHANGELOG.md"));
            buttons.Add(check); buttons.Add(update); buttons.Add(notes); details.Add(buttons);
            details.Add(checks); details.Add(install);
            checks.RegisterValueChangedCallback(evt =>
            { PackageUpdates.Settings.automaticChecks = evt.newValue; PackageUpdates.Settings.Persist(); Refresh(); });
            install.RegisterValueChangedCallback(evt =>
            { PackageUpdates.Settings.automaticInstall = evt.newValue; PackageUpdates.Settings.Persist(); Refresh(); });
            RegisterCallback<AttachToPanelEvent>(evt => { PackageUpdates.Changed += Refresh; Refresh(); });
            RegisterCallback<DetachFromPanelEvent>(evt => PackageUpdates.Changed -= Refresh);
            schedule.Execute(Refresh).Every(1000);
            Refresh();
        }

        internal void SetLanguage(InspectorLanguage selected) { language = selected; Refresh(); }
        private string Text(string key) { return InspectorLocalization.Text(key, language); }
        private static void Caption(Label label, string text)
        { ((INotifyValueChanged<string>)label).SetValueWithoutNotify(text); }

        private void Refresh()
        {
            var channel = PackageUpdates.Channel;
            bool vpm = PackageUpdates.Vpm;
            details.text = Text("update.title") + "  " + PackageUpdates.CurrentVersion +
                (vpm ? " · VPM" : channel == null ? "" : " · " + channel + (PackageUpdates.Legacy ? " · .unitypackage" : " · Git")) + (PackageUpdates.Available ? " → " + PackageUpdates.Settings.latestVersion : "");
            Caption(status, Text(vpm ? "update.vpm" : channel == null ? "update.unmanaged" : PackageUpdates.EditorBusy && !PackageUpdates.Working ?
                "update.busy" : PackageUpdates.Available && !PackageUpdates.Working && PackageUpdates.Status != "update.failed" ? "update.available" : PackageUpdates.Status));
            status.tooltip = PackageUpdates.Error;
            check.text = Text(vpm ? "update.vpmGuide" : "update.check"); update.text = Text("update.install"); notes.text = Text("update.notes");
            checks.label = Text("update.autoCheck"); install.label = Text("update.autoInstall");
            install.tooltip = Text(vpm ? "update.vpm" : "update.autoInstall.tip");
            checks.SetValueWithoutNotify(PackageUpdates.Settings.automaticChecks);
            install.SetValueWithoutNotify(PackageUpdates.Settings.automaticInstall);
            check.SetEnabled((vpm || channel != null) && !PackageUpdates.Working && !PackageUpdates.EditorBusy);
            update.SetEnabled(PackageUpdates.Available && !PackageUpdates.Working && !PackageUpdates.EditorBusy);
            checks.SetEnabled(channel != null && !PackageUpdates.Working);
            install.SetEnabled(channel != null && checks.value && !PackageUpdates.Working);
        }
    }
}
