// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using GalleryExplorer.Domain;
using MessagePack;
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
    /// CreateNewProgress.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateNewProgress : UserControl
    {
        public CreateNewProgress(string id, string name, int starts, int ends)
        {
            InitializeComponent();

            Message.Text = $"{name}의 목록을 가져오는 중 입니다...";

            Task.Run(() =>
            {
                var page_end = ends;

                var is_minor = DCGalleryList.Instance.MinorGalleryIds.Contains(id);
                var articles = new List<DCInsidePageArticle>();

                for (int i = starts; i <= ends; i++)
                {
                    string url;
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

                    Extends.Post(() => Message2.Text = $"작업 중...[{i}/{page_end - starts + 1} | {(100.0 * i / (page_end - starts + 1)).ToString("#0.00")}%]" );
                }

                var overlap = new HashSet<string>();
                var articles_trim = new List<DCInsidePageArticle>();
                foreach (var article in articles)
                    if (!overlap.Contains(article.no))
                    {
                        articles_trim.Add(article);
                        overlap.Add(article.no);
                    }

                articles_trim.Sort((x, y) => y.no.ToInt().CompareTo(x.no.ToInt()));

                var result = new DCInsideGalleryModel();

                result.is_minor_gallery = is_minor;
                result.gallery_id = id;
                result.gallery_name = name;
                result.articles = articles_trim;

                var bbb = MessagePackSerializer.Serialize(result);
                using (FileStream fsStream = new FileStream($"list-{id}-{DateTime.Now.Ticks}.txt", FileMode.Create))
                using (BinaryWriter sw = new BinaryWriter(fsStream))
                {
                    sw.Write(bbb);
                }
                Extends.Post(() => MainWindow.Instance.CloseDialog());
            });
        }
    }
}
