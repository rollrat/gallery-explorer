// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using GalleryExplorer.Domain;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GalleryExplorer
{
    /// <summary>
    /// ArchiveWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ArchiveWindow : Window
    {
        public ArchiveWindow()
        {
            InitializeComponent();

            SearchText.GotFocus += SearchText_GotFocus;
            SearchText.LostFocus += SearchText_LostFocus;

            ResultList.DataContext = new GalleryDataGridViewModel();
            ResultList.Sorting += new DataGridSortingEventHandler(new DataGridSorter<GalleryDataGridItemViewModel>(ResultList).SortHandler);
        }

        SQLiteWrapper<ArticleColumnModel> article;
        SQLiteWrapper<CommentColumnModel> comment;

        private void SearchText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchText.Text))
                SearchText.Text = "검색";
        }

        private void SearchText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchText.Text == "검색")
                SearchText.Text = "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            ofd.Filter = "아카이브 파일 (*.db)|*.db";
            if (ofd.ShowDialog() == false)
                return;

            Task.Run(() =>
            {
                article = new SQLiteWrapper<ArticleColumnModel>(ofd.FileName);
                comment = new SQLiteWrapper<CommentColumnModel>(ofd.FileName);

                var prefix = Path.Combine(Path.GetDirectoryName(ofd.FileName), Path.GetFileNameWithoutExtension(ofd.FileName));
                DCInsideArchiveIndex.Instance.Load(prefix);
                Extends.Post(() => LoadProgress.Visibility = Visibility.Collapsed);
                Extends.Post(() => SearchText.IsEnabled = true);
            });
        }

        private async void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            //{
            //    if (SearchText.Text != "검색")
            //    {
            //        if (e.Key == Key.Return && !logic.skip_enter)
            //        {
            //            ButtonAutomationPeer peer = new ButtonAutomationPeer(SearchButton);
            //            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            //            invokeProv.Invoke();
            //            logic.ClosePopup();
            //        }
            //        logic.skip_enter = false;
            //    }
            //}

            if (SearchText.Text != "검색" && SearchText.Text != "")
            {
                var ss = SearchText.Text.Trim().Split(' ');

                List<ArticleColumnModel> rx = null;

                await Task.Run(() =>
                {
                    var aa = ss.Select(x => DCInsideArchiveIndex.Instance.of_article(x)).ToList();

                    // And
                    var dd = new Dictionary<int, int>();
                    aa.ForEach(x => x.ToList().ForEach(y =>
                    {
                        if (!dd.ContainsKey(y))
                            dd.Add(y, 0);
                        dd[y]++;
                    }));

                    var rr = dd.Where(x => x.Value == aa.Count).Take(20).ToList();
                    rx = article.Query("no IN(" + string.Join(",", rr.Select(x => x.Key)) + ")");
                });

                var tldx = ResultList.DataContext as GalleryDataGridViewModel;
                tldx.Items.Clear();
                foreach (var article in rx)
                {
                    tldx.Items.Add(new GalleryDataGridItemViewModel
                    {
                        번호 = article.no.ToString(),
                        제목 = article.title ?? "",
                        클래스 = article.classify ?? "",
                        날짜 = article.date.ToString(),
                        닉네임 = article.nick ?? "",
                        답글 = article.replay_num ?? "",
                        아이디 = article.uid != "" ? article.uid : $"({article.ip})",
                        조회수 = article.count ?? "",
                        추천수 = article.recommend ?? "",
                    });
                }
            }
        }

        private void PageNumber_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultList.SelectedItems.Count > 0)
            {
                var no = (ResultList.SelectedItems[0] as GalleryDataGridItemViewModel).번호;
                //var id = DCGalleryAnalyzer.Instance.Model.gallery_id;
                //
                //if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery)
                //    Process.Start($"https://gall.dcinside.com/mgallery/board/view/?id={id}&no={no}");
                //else
                //    Process.Start($"https://gall.dcinside.com/board/view/?id={id}&no={no}");
                Process.Start($"https://gall.dcinside.com/mgallery/board/view/?id=tullius&no={no}");
            }
        }

        private void PageFunction_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
