using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace FuzzyBookmarkBrowser
{
    internal class GithubUpdateChecker
    {
        private Octokit.ReleaseAsset _downloadedUpdateAsset;
        private const string UPDATE_BACKUP_PATH = "update_backup";
        private const string UPDATE_EXTRACT_PATH = "update";

        public bool IsUpdateReady { get; internal set; } = false;

        internal async Task CheckForUpdatesAsync()
        {
            if (Directory.Exists(UPDATE_BACKUP_PATH))
                Directory.Delete(UPDATE_BACKUP_PATH, true); // Clean up any previous version

            Version localVersion = Assembly.GetExecutingAssembly().GetName().Version; // Local version.
            Octokit.GitHubClient ghc = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("fuzzy-bookmark-browser", localVersion.ToString(2)));
            var releases = await ghc.Repository.Release.GetAll("MaverickMartyn", "FuzzyBookmarkBrowser");

            //Setup the versions
            Version latestGitHubVersion = new Version(releases[0].TagName.Replace("Release-v", String.Empty));

            //Compare the Versions
            //Source: https://stackoverflow.com/questions/7568147/compare-version-numbers-without-using-split-function
            if (localVersion.CompareTo(latestGitHubVersion) < 0)
            {
                // A new version is available
                var asset = releases[0].Assets.FirstOrDefault(a => a.Name == releases[0].TagName + ".zip");
                if (asset == null)
                    MessageBox.Show("A newer version is available, but no downloadable assets could be found.\n" +
                        "Try manually updating from here: " + releases[0].Url, "Error getting update", MessageBoxButton.OK, MessageBoxImage.Error);
                WebClient wc = new WebClient();
                
                wc.DownloadFile(new Uri(asset.BrowserDownloadUrl), asset.Name);
                _downloadedUpdateAsset = asset;

                if (MessageBox.Show("An update is ready to install.\n" +
                    "Would you like to close now?", "Update ready", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    InstallDownloadedUpdate();
                    System.Diagnostics.Process.Start(Application.ResourceAssembly.Location, String.Join(" ", Environment.GetCommandLineArgs()));
                    Application.Current.Shutdown();
                }
                else
                    IsUpdateReady = true;
            }
        }

        internal void InstallDownloadedUpdate()
        {
            Directory.CreateDirectory(UPDATE_BACKUP_PATH);
            Directory.CreateDirectory(UPDATE_EXTRACT_PATH);
            // 1. unzip to temp "update" folder
            ZipFile.ExtractToDirectory(_downloadedUpdateAsset.Name, UPDATE_EXTRACT_PATH);

            // 2. iterate through and copy files and folders
            var allDirs = Directory.EnumerateDirectories(UPDATE_EXTRACT_PATH, "*", SearchOption.AllDirectories);
            foreach (var dir in allDirs)
                Directory.CreateDirectory(dir.Replace(UPDATE_EXTRACT_PATH + "\\", String.Empty));

            var allFiles = Directory.EnumerateFiles(UPDATE_EXTRACT_PATH, "*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                var new_path = file.Replace(UPDATE_EXTRACT_PATH + "\\", String.Empty);
                // If exists, move existing file to backup folder, then continue
                if (File.Exists(new_path))
                {
                    var backup_path = file.Replace(UPDATE_EXTRACT_PATH, UPDATE_BACKUP_PATH);
                    File.Move(new_path, backup_path);
                }
                File.Move(file, new_path);
            }

            Directory.Delete(UPDATE_EXTRACT_PATH, true); // Clean up
        }
    }
}
