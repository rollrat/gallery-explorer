// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using GalleryExplorer.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GalleryExplorer
{
    /// <summary>
    /// SyncProgress.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SyncProgress : UserControl
    {
        public SyncProgress()
        {
            InitializeComponent();

            Message.Text = $"{DCGalleryAnalyzer.Instance.Model.gallery_name}의 목록을 가져오는 중 입니다...";

            var name = DCGalleryAnalyzer.Instance.Model.gallery_name;
            var id = DCGalleryAnalyzer.Instance.Model.gallery_id;
            var is_minor = DCGalleryAnalyzer.Instance.Model.is_minor_gallery;

            Task.Run(() =>
            {
                string url;
                if (is_minor)
                    url = $"https://gall.dcinside.com/mgallery/board/lists?id={id}";
                else
                    url = $"https://gall.dcinside.com/board/lists?id={id}";

                var html_board = NetTools.DownloadString(url);
                DCInsideGallery gall_board;
                if (is_minor)
                    gall_board = DCInsideUtils.ParseMinorGallery(html_board);
                else
                    gall_board = DCInsideUtils.ParseGallery(html_board);

                if (is_minor && (gall_board.articles == null || gall_board.articles.Length == 0))
                    gall_board = DCInsideUtils.ParseGallery(html_board);

                var ll = gall_board.articles.ToList();
                ll.Sort((x, y) => y.no.ToInt().CompareTo(x.no.ToInt()));
                var latest = ll[0].no.ToInt();
                var last = DCGalleryAnalyzer.Instance.Articles[0].no.ToInt();

                var page_end = (latest - last) / 50;

                var articles = new List<DCInsidePageArticle>();

                for (int i = 1; i < page_end + 3; i++)
                {
                    if (is_minor)
                        url = $"https://gall.dcinside.com/mgallery/board/lists/?id={id}&page={i}";
                    else
                        url = $"https://gall.dcinside.com/board/lists/?id={id}&page={i}";

                    var html = NetTools.DownloadString(url);

                    DCInsideGallery gall;

                    if (is_minor)
                        gall = DCInsideUtils.ParseMinorGallery(html);
                    else
                        gall = DCInsideUtils.ParseGallery(html);

                    if (is_minor && (gall.articles == null || gall.articles.Length == 0))
                        gall = DCInsideUtils.ParseGallery(html);

                    if (gall.articles.Length == 0)
                        break;

                    articles.AddRange(gall.articles);

                    Extends.Post(() => Message2.Text = $"동기화 중...[{i}/{page_end + 3} | {(100.0 * i / (page_end + 3)).ToString("#0.00")}%]");
                }

                articles.AddRange(DCGalleryAnalyzer.Instance.Articles);

                var overlap = new HashSet<string>();
                var articles_trim = new List<DCInsidePageArticle>();
                foreach (var article in articles)
                    if (!overlap.Contains(article.no))
                    {
                        articles_trim.Add(article);
                        overlap.Add(article.no);
                    }

                articles_trim.Sort((x, y) => y.no.ToInt().CompareTo(x.no.ToInt()));

                DCGalleryAnalyzer.Instance.Model.articles = articles_trim;
                DCGalleryAnalyzer.Instance.Save();
                Extends.Post(() => MainWindow.Instance.CloseDialog());
            });
        }
    }
}
