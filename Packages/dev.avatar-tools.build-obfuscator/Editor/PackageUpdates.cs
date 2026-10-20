using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AvatarTools.Obfuscation.Editor
{
    [FilePath("UserSettings/AvatarBuildObfuscatorUpdates.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class PackageUpdateSettings : ScriptableSingleton<PackageUpdateSettings>
    {
        public bool automaticChecks = true, automaticInstall;
        public bool importingArchive;
        public string managedCommit = "", managedChannel = "", lastAttempt = "";
        public string checkedChannel = "", attemptChannel = "", checkedAt = "", latestVersion = "", latestCommit = "";
        public void Persist() { Save(true); }
    }

    [InitializeOnLoad]
    internal static class PackageUpdates
    {
        [Serializable] private sealed class Runs { public Run[] workflow_runs; }
        [Serializable] private sealed class RepositoryInfo { public string full_name; }
        [Serializable] private sealed class Run
        {
            public string head_branch, head_sha, status, conclusion, @event;
            public RepositoryInfo head_repository;
        }
        [Serializable] private sealed class Manifest { public string name, version, unity; }
        [Serializable] private sealed class Release { public string tag_name; public bool draft; public ReleaseAsset[] assets; }
        [Serializable] private sealed class ReleaseAsset { public string name, browser_download_url, digest; }
        private enum RequestStage { Runs, Manifest, Release, Archive }

        private static UnityWebRequest request;
        private static AddRequest installation;
        private static PackageInfo installed;
        private static string requestChannel, candidateCommit, installingVersion;
        private static string previousManagedCommit, previousManagedChannel;
        private static string archiveDigest;
        private static Manifest legacyManifest;
        private static bool legacyRead, checkedThisSession, manualCheck, automaticOperation;
        private static RequestStage stage;
        private static double startupTime, lastRequestTime = -60, lastTick;
        private static int buildLeases;
        internal static string Status { get; private set; } = "update.ready";
        internal static string Error { get; private set; } = "";
        internal static event Action Changed;
        internal static PackageUpdateSettings Settings => PackageUpdateSettings.instance;
        internal static string CurrentVersion => Installed == null ? LegacyManifest?.version ?? "?" : Installed.version;
        internal static PackageInfo Installed => installed ?? (installed = PackageInfo.FindForAssembly(typeof(AvatarBuildObfuscator).Assembly));
        private static Manifest LegacyManifest
        {
            get
            {
                if (!legacyRead)
                {
                    legacyRead = true;
                    if (AssetDatabase.GUIDToAssetPath("7c5e3c2dfef2cf4469c50c0ba239aac9") == "Assets/AvatarBuildObfuscator/Runtime/AvatarBuildObfuscator.cs")
                    {
                        try
                        {
                            var value = JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/AvatarBuildObfuscator/package.json"));
                            if (value.name == UpdatePolicy.PackageName && UpdatePolicy.IsVersion(value.version)) legacyManifest = value;
                        }
                        catch (Exception) { /* Unrecognized layouts are never overwritten. */ }
                    }
                }
                return legacyManifest;
            }
        }
        internal static bool Legacy => Installed == null && LegacyManifest != null;
        internal static string Channel => Legacy ?
            (Settings.managedChannel == "main" || Settings.managedChannel == "beta" ? Settings.managedChannel :
                CurrentVersion.Contains("-") ? "beta" : "main") :
            Installed == null || Installed.source != PackageSource.Git || Installed.git == null ? null :
                UpdatePolicy.Channel(Installed.packageId, Installed.git.hash, Settings.managedCommit, Settings.managedChannel);
        internal static bool Working => request != null || installation != null || Settings.importingArchive;
        internal static bool Available => Channel != null && Settings.checkedChannel == Channel &&
            UpdatePolicy.CanInstall(CurrentVersion, Settings.latestVersion, Legacy ? "" : Installed.git.hash, Settings.latestCommit, Channel);
        private static string ArchivePath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/AvatarBuildObfuscatorUpdates/pending.unitypackage");
        internal static bool EditorBusy => Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer || buildLeases != 0 ||
            GUIUtility.hotControl != 0 || EditorGUIUtility.editingTextField ||
            Resources.FindObjectsOfTypeAll<EditorWindow>().Any(w => w.GetType().Name == "VRCSdkControlPanel");

        static PackageUpdates()
        {
            startupTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += DisposeRequest;
            EditorApplication.quitting += DisposeRequest;
            AssetDatabase.importPackageCompleted += OnImported;
            AssetDatabase.importPackageCancelled += name => { if (Settings.importingArchive) Fail("update.failed", "Import cancelled"); };
            AssetDatabase.importPackageFailed += (name, error) => { if (Settings.importingArchive) Fail("update.failed", error); };
        }

        private sealed class BuildLease : IDisposable
        { public BuildLease() { buildLeases++; } public void Dispose() { buildLeases = Math.Max(0, buildLeases - 1); } }
        internal static IDisposable DuringBuild() { return new BuildLease(); }
        private static void Notify() { Changed?.Invoke(); }
        private static void Fail(string key, string error)
        { Status = key; Error = error; DisposeRequest(); CleanupArchive(); Notify(); }
        private static void DisposeRequest()
        { if (request != null) { request.Abort(); request.Dispose(); request = null; } }

        internal static void Check(bool manual)
        {
            if (Working || Channel == null || EditorBusy || EditorApplication.timeSinceStartup - lastRequestTime < 30) return;
            checkedThisSession = true; manualCheck = manual; requestChannel = Channel;
            Settings.checkedAt = DateTime.UtcNow.ToString("O"); Settings.checkedChannel = "";
            Settings.attemptChannel = requestChannel; Settings.Persist();
            Status = "update.checking"; Error = ""; stage = RequestStage.Runs;
            Send("https://api.github.com/repos/" + UpdatePolicy.Repository +
                "/actions/workflows/package-ci.yml/runs?branch=" + requestChannel + "&event=push&status=success&per_page=10");
        }

        private static void Send(string url)
        {
            request = UnityWebRequest.Get(url); request.timeout = 12;
            request.SetRequestHeader("User-Agent", "Avatar-Build-Obfuscator");
            request.SetRequestHeader("Accept", "application/json");
            request.SendWebRequest(); lastRequestTime = EditorApplication.timeSinceStartup; Notify();
        }

        private static void Receive()
        {
            if (!request.isDone) return;
            if (stage == RequestStage.Archive && (EditorBusy || automaticOperation && UnityEditorInternal.InternalEditorUtility.isApplicationActive)) return;
            if (request.result != UnityWebRequest.Result.Success)
            { Fail("update.failed", request.error); return; }
            var bytes = request.downloadHandler.data;
            string json = stage == RequestStage.Archive ? null : request.downloadHandler.text;
            request.Dispose(); request = null;
            try
            {
                if (Channel != requestChannel) { Fail("update.failed", "Installation channel changed"); return; }
                if (stage == RequestStage.Archive)
                {
                    if (!Legacy || !Available || !UpdatePolicy.ValidArchive(bytes, archiveDigest)) throw new InvalidOperationException("Package checksum, version or installation type mismatch");
                    Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath));
                    Settings.importingArchive = true; Settings.Persist();
                    File.WriteAllBytes(ArchivePath, bytes);
                    AssetDatabase.ImportPackage(ArchivePath, false);
                    return;
                }
                if (stage == RequestStage.Release)
                {
                    var release = JsonUtility.FromJson<Release>(json);
                    var asset = release?.assets?.FirstOrDefault(a => a.name == "AvatarBuildObfuscator-" + installingVersion + ".unitypackage");
                    if (release == null || release.draft || release.tag_name != "v" + installingVersion || asset == null ||
                        !UpdatePolicy.TrustedArchive(asset.browser_download_url, installingVersion) || string.IsNullOrEmpty(asset.digest))
                        throw new InvalidOperationException("Verified release archive unavailable");
                    archiveDigest = asset.digest; stage = RequestStage.Archive; Send(asset.browser_download_url); return;
                }
                if (stage == RequestStage.Runs)
                {
                    var runs = JsonUtility.FromJson<Runs>(json);
                    var run = runs?.workflow_runs?.FirstOrDefault(r => UpdatePolicy.VerifiedRun(r.head_branch, r.head_sha,
                        r.status, r.conclusion, r.@event, r.head_repository?.full_name, requestChannel));
                    if (run == null) { Fail("update.unverified", "No successful push workflow found"); return; }
                    candidateCommit = run.head_sha; stage = RequestStage.Manifest;
                    Send("https://raw.githubusercontent.com/" + UpdatePolicy.Repository + "/" + candidateCommit +
                        "/Packages/" + UpdatePolicy.PackageName + "/package.json");
                    return;
                }
                var manifest = JsonUtility.FromJson<Manifest>(json);
                if (manifest == null || manifest.name != UpdatePolicy.PackageName || !UpdatePolicy.IsVersion(manifest.version))
                    throw new InvalidOperationException("Invalid package metadata");
                // This release supports Unity 2022.3; do not auto-install a future, higher minimum.
                if (manifest.unity != "2022.3") throw new InvalidOperationException("Package requires a different Unity version");
                Settings.checkedChannel = requestChannel; Settings.latestVersion = manifest.version;
                Settings.latestCommit = candidateCommit; Settings.Persist();
                Status = Available ? "update.available" : "update.current"; Notify();
            }
            catch (Exception exception) { Fail("update.failed", exception.Message); }
        }

        internal static void Install(bool automatic)
        {
            if (Working || EditorBusy || !Available) return;
            if (automatic && !UpdatePolicy.CanAutomaticallyInstall(Settings.automaticInstall, EditorBusy, Available,
                Settings.latestCommit, Settings.lastAttempt)) return;
            string channel = Channel, target = Settings.latestCommit;
            automaticOperation = automatic;
            previousManagedCommit = Settings.managedCommit; previousManagedChannel = Settings.managedChannel;
            Settings.managedCommit = target; Settings.managedChannel = channel; Settings.lastAttempt = target; Settings.Persist();
            Status = "update.installing"; Error = ""; installingVersion = Settings.latestVersion;
            if (Legacy)
            {
                requestChannel = channel; stage = RequestStage.Release;
                Send("https://api.github.com/repos/" + UpdatePolicy.Repository + "/releases/tags/v" + installingVersion);
                return;
            }
            // Persist channel before Add: a successful update can reload this assembly before polling completes.
            try { installation = Client.Add(UpdatePolicy.GitUrl + "#" + target); Notify(); }
            catch (Exception exception) { RestoreFailedInstall(exception.Message); }
        }

        private static void RestoreFailedInstall(string message)
        {
            Settings.managedCommit = previousManagedCommit; Settings.managedChannel = previousManagedChannel; Settings.Persist();
            installation = null; Fail("update.failed", message);
        }

        private static bool CleanupArchive()
        {
            try
            {
                if (File.Exists(ArchivePath)) File.Delete(ArchivePath);
                if (Settings.importingArchive) { Settings.importingArchive = false; Settings.Persist(); }
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private static void OnImported(string name)
        {
            legacyRead = false; legacyManifest = null;
            if (!Settings.importingArchive)
            {
                // Manual imports can change only package.json and never reload the assembly.
                if (Installed == null && !Working) Status = Available ? "update.available" : "update.current";
                Notify(); return;
            }
            if (name != "pending") return;
            if (CleanupArchive()) { Status = "update.current"; Notify(); }
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup - lastTick < .25) return;
            lastTick = EditorApplication.timeSinceStartup;
            if (Settings.importingArchive && !EditorApplication.isCompiling && !EditorApplication.isUpdating &&
                (CurrentVersion == Settings.latestVersion || EditorApplication.timeSinceStartup - startupTime > 30))
            { if (CleanupArchive()) { legacyRead = false; Status = CurrentVersion == Settings.latestVersion ? "update.current" : "update.failed"; Notify(); } }
            if (request != null) { Receive(); return; }
            if (installation != null)
            {
                if (!installation.IsCompleted) return;
                if (installation.Status == StatusCode.Failure) { RestoreFailedInstall(installation.Error.message); return; }
                installation = null; installed = null; Status = "update.current"; Notify();
                Debug.Log("[Avatar Build Obfuscator] Updated package to " + installingVersion);
                return;
            }
            if (EditorBusy || EditorApplication.timeSinceStartup - startupTime < 60) return;
            if (!checkedThisSession && Settings.automaticChecks && Channel != null)
            {
                DateTime checkedAt;
                if (Settings.attemptChannel == Channel && DateTime.TryParse(Settings.checkedAt, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out checkedAt) && DateTime.UtcNow - checkedAt.ToUniversalTime() < TimeSpan.FromHours(6))
                { checkedThisSession = true; Status = Settings.checkedChannel == Channel ? (Available ? "update.available" : "update.current") : "update.failed"; Notify(); }
                else { Check(false); return; }
            }
            else if (checkedThisSession && Settings.automaticChecks && Channel != null)
            {
                DateTime checkedAt;
                if (DateTime.TryParse(Settings.checkedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out checkedAt) &&
                    DateTime.UtcNow - checkedAt.ToUniversalTime() >= TimeSpan.FromHours(6)) { Check(false); return; }
            }
            // Failed checks invalidate the cached channel. Automatic installs require a verified candidate.
            if (checkedThisSession && !manualCheck && Status == "update.available" && Settings.automaticChecks &&
                !UnityEditorInternal.InternalEditorUtility.isApplicationActive) Install(true);
        }
    }
}
