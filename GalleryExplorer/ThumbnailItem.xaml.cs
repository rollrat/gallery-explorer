// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using GalleryExplorer.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using XamlAnimatedGif;

namespace GalleryExplorer
{
    /// <summary>
    /// ThumbnailItem.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ThumbnailItem : UserControl
    {
        DCInsidePageArticle article;
        string URL;

        public ThumbnailItem(DCInsidePageArticle article)
        {
            InitializeComponent();

            this.article = article;
            Title.Text = article.title;
            if (article.uid != "")
                Author.Text = $"{article.nick} ({article.uid})";
            else
                Author.Text = $"{article.nick} ({article.ip})";
            ViewCount.Text = article.count + " Views";
            if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery)
                URL = $"https://gall.dcinside.com/mgallery/board/view/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&no={article.no}";
            else
                URL = $"https://gall.dcinside.com/mgallery/board/view/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&no={article.no}";
            //DateTime.Text = article.date.ToString();
            var upvote = article.recommend.ToInt();
            if (upvote > 0)
            {
                Rating.Text = article.recommend;
                UpVotePanel.Visibility = Visibility.Visible;

                if (upvote < 5)
                {
                    RatingShadow.Opacity = 0;
                    Rating.Opacity = 0.56;
                    Rating.Foreground = Brushes.White;
                }
                else if (upvote < 10)
                {
                    RatingShadow.Opacity = 1;
                    RatingShadow.Color = Colors.Yellow;
                    Rating.Opacity = 1;
                    Rating.Foreground = Brushes.Yellow;
                }
                else if (upvote < 15)
                {
                    RatingShadow.Opacity = 1;
                    RatingShadow.Color = Colors.Orange;
                    Rating.Opacity = 1;
                    Rating.Foreground = Brushes.Orange;
                }
                else
                {
                    RatingShadow.Opacity = 1;
                    RatingShadow.Color = Colors.HotPink;
                    Rating.Opacity = 1;
                    Rating.Foreground = Brushes.HotPink;
                }
            }
            Loaded += ThumbnailItem_Loaded;
        }

        bool loaded = false;
        string temp_file;
        private void ThumbnailItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;
            loaded = true;

            switch (article.type)
            {
                case "icon_recomimg":
                case "icon_pic":
                    Icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Image;
                    break;

                default:
                    Icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pen;
                    break;
            }

            if (article.type == "icon_pic" || article.type == "icon_recomimg")
            {
                Task.Run(() =>
                {
                    var html = NetTools.DownloadString(URL);
                    if (html == null)
                    {
                        Extends.Post(() =>
                        {
                            Icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.TrashCanOutline;
                        });
                    }
                    var page = DCInsideUtils.ParseBoardView(html);
                    if (page.ImagesLink.Count == 0) return;
                    temp_file = TemporaryFiles.UseNew();
                    NetTools.DownloadFile(page.ImagesLink[0], temp_file);

                    Extends.Post(() => AnimationBehavior.SetSourceUri(Image, new Uri(temp_file)));
                });
            }
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(URL);
        }

    }
}
