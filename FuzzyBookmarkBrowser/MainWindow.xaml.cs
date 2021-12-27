using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Environment;

namespace FuzzyBookmarkBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var bookmark = ((TreeViewItem)sender).DataContext as Child;
            if (bookmark != null && bookmark.Type != "folder")
            {
                LaunchURL(bookmark.Url);
            }
        }

        internal string GetSystemDefaultBrowser()
        {
            const string userChoice = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
            string progId;
            using (RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey(userChoice))
            {
                object progIdValue = userChoiceKey?.GetValue("Progid");
                if (progIdValue == null)
                {
                    throw new InvalidOperationException();
                }
                progId = progIdValue.ToString();

                const string exeSuffix = ".exe";
                string path = progId + @"\shell\open\command";
                //FileInfo browserPath;
                using (RegistryKey pathKey = Registry.ClassesRoot.OpenSubKey(path))
                {
                    if (pathKey == null)
                    {
                        throw new InvalidOperationException();
                    }

                    // Trim parameters.
                    try
                    {
                        path = pathKey.GetValue(null).ToString().ToLower().Replace("\"", "");
                        if (!path.EndsWith(exeSuffix))
                        {
                            path = path.Substring(0, path.LastIndexOf(exeSuffix, StringComparison.Ordinal) + exeSuffix.Length);
                            //browserPath = new FileInfo(path);
                        }
                    }
                    catch
                    {
                        // Assume the registry value is set incorrectly, or some funky browser is used which currently is unknown.
                        throw new InvalidOperationException();
                    }
                }
                return path;
            }


        }

        private void LaunchURL(string url)
        {
            string privateModeParam = string.Empty;
            string browserName = GetSystemDefaultBrowser();
            if (string.IsNullOrEmpty(browserName))
            {
                MessageBox.Show("No default browser found!");
            }
            else

            {
                if (cbx_openInIncognito.IsChecked.HasValue && cbx_openInIncognito.IsChecked.Value)
                {
                    if (browserName.Contains("firefox"))
                    {
                        privateModeParam = " -private-window";
                    }
                    else if ((browserName.Contains("iexplore")) || (browserName.Contains("Opera")))
                    {
                        privateModeParam = " -private";
                    }
                    else if (browserName.Contains("chrome"))
                    {
                        privateModeParam = " -incognito";
                    }
                }
                Process.Start(browserName, $"{privateModeParam} {url}");
            }
        }

        List<Child> bookmarks;

        public MainWindow()
        {
            InitializeComponent();

            Task.Run(() =>
            {
                UpdateBookmarkList();
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    tv_bookmarks.ItemsSource = bookmarks;
                }));
            });
        }

        private List<Child> SortBookmarks(List<Child> unorderedBookmarks)
        {
            List<Child> sort = new List<Child>();
            Func<List<Child>, List<Child>> selector = null;
            selector = (inputList) => {
                var topLayer = inputList.OrderBy(x => x.Type).ThenByDescending(x => x.Likeness).ThenBy(x => x.Name).ToList();
                foreach (var bm in topLayer)
                {
                    if (bm.Children != null)
                        bm.Children = selector.Invoke(bm.Children);
                }
                return topLayer;
            };

            sort = selector.Invoke(unorderedBookmarks);

            return sort;
        }

        //private List<Child> SortBookmarks(List<Child> unorderedBookmarks)
        //{
        //    List<Child> sort = new List<Child>();
        //    Action<List<Child>, int> selector = null;
        //    var lvl = 0;
        //    selector = (inputList, curLvl) => {
        //        var topLayer = inputList.OrderByDescending(x => x.Likeness).ThenBy(x => x.Name);
        //        foreach (var bm in topLayer)
        //        {
        //            bm.Lvl = curLvl;
        //            sort.Add(bm);
        //            if (bm.Children != null)
        //                selector.Invoke(bm.Children, curLvl + 1);
        //        }
        //    };

        //    selector.Invoke(unorderedBookmarks, lvl);

        //    return sort;
        //}

        private void UpdateBookmarkList()
        {
            var bookmarksJson = File.ReadAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default\bookmarks")));
            if (!string.IsNullOrWhiteSpace(bookmarksJson))
            {
                Root bookmarksRoot = JsonConvert.DeserializeObject<Root>(bookmarksJson);
                if (bookmarksJson != null)
                {
                    string srchTxt = "";
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        srchTxt = tb_search.Text;
                    }));
                    bookmarks = new List<Child>();
                    bookmarks.AddRange(bookmarksRoot.Roots.BookmarkBar.Children);
                    bookmarks.AddRange(bookmarksRoot.Roots.Other.Children);
                    bookmarks.AddRange(bookmarksRoot.Roots.Synced.Children);
                    bookmarks = RankBookmarks(srchTxt, bookmarks);
                    bookmarks = SortBookmarks(bookmarks);
                }

                //lv_bookmarks.ItemsSource = bookmarks;
                //callback();
            }
        }

        DateTime _nextUpdateAllowed;
        private Task _refreshQueueTask;
        int _cntMatches = 0;

        private List<Child> RankBookmarks(string srchTxt, List<Child> unorderedBookmarks)
        {
            bool onlyShowMatches = false;
            Application.Current.Dispatcher.Invoke(new Action(() => {
                onlyShowMatches = cbx_onlyShowMatches.IsChecked.HasValue && cbx_onlyShowMatches.IsChecked.Value;
            }));

            _cntMatches = 0;
            char[] splitChars = new[] { ' ', '-', '/' };

            string[] filters = srchTxt.Split(splitChars).Where(n => !String.IsNullOrWhiteSpace(n)).ToArray();
            List<Child> sort = new List<Child>();
            Func<List<Child>, List<Child>> selector = null;
            selector = (inputList) => {

                inputList.ForEach(b =>
                {
                    var nameWords = b.Name.Split(splitChars).Where(n => !String.IsNullOrWhiteSpace(n));
                    b.Likeness = (filters.Contains(b.Id) ? 1 : 0)
                        + filters.Count(f => f.Contains(b.Name))
                        + filters.Count(f => nameWords.Any(nw => nw.Contains(f)))
                        + filters.Count(f => b.Name.Contains(f))
                        + filters.Count(f => f.Contains(b.Url ?? ""))
                        + filters.Count(f => b.Url != null && b.Url.Contains(f));
                    if (b.Likeness > 0)
                    {
                        _cntMatches++;
                    }
                    b.Hidden = onlyShowMatches && b.Likeness <= 0 && b.Type != "folder";
                });
                foreach (var bm in inputList)
                {
                    if (bm.Children != null)
                        bm.Children = selector.Invoke(bm.Children);
                }
                return inputList;
            };

            sort = selector.Invoke(unorderedBookmarks);

            return sort;
        }

        private void tb_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DateTime.Now < _nextUpdateAllowed)
            {
                if (_refreshQueueTask == null)
                    _refreshQueueTask = Task.Delay(_nextUpdateAllowed - DateTime.Now).ContinueWith((t) => tb_search_TextChanged(null, null));
                return;
            }

            Task.Run(() =>
            {
                string srchTxt = "";
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    srchTxt = tb_search.Text;
                }));
                bookmarks = RankBookmarks(srchTxt, bookmarks);
                bookmarks = SortBookmarks(bookmarks);
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    tv_bookmarks.ItemsSource = bookmarks;
                    tb_numMatches.Text = _cntMatches.ToString();
                }));

            });

            //Func<List<Child>, List<Child>> setLikeness = null;
            //setLikeness = (bs) =>
            //{
            //    foreach (var b in bs)
            //    {
            //        b.Likeness = filters.Select(f => FuzzySharp.Fuzz.Ratio(f, b.Name) + FuzzySharp.Fuzz.Ratio(f, b.Url)).Sum();
            //        if (b.Children != null)
            //        {
            //            b.Children = setLikeness.Invoke(b.Children);
            //        }
            //    }
            //    return bs;
            //};

            //bookmarks = setLikeness.Invoke(bookmarks);

            _refreshQueueTask = null;
            _nextUpdateAllowed = DateTime.Now.AddSeconds(1).AddMilliseconds(500);
        }

        private async void btnRefresh_ClickAsync(object sender, RoutedEventArgs e)
        {
            await Task.Run(UpdateBookmarkList);
            tv_bookmarks.ItemsSource = bookmarks;
            tb_numMatches.Text = _cntMatches.ToString();
        }

        private void cbx_onlyShowMatches_Click(object sender, RoutedEventArgs e)
        {
            tb_search_TextChanged(null, null);
        }
    }
}
