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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using XamlAnimatedGif;

namespace GalleryExplorer
{
    /// <summary>
    /// ThumbnailItem.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ThumbnailItem : UserControl
    {
        public DCInsidePageArticle Article { get; set; }
        public DCInsideArticle ArticleBody { get; set; }
        string URL;

        public ThumbnailItem(DCInsidePageArticle article, bool r2l = false)
        {
            InitializeComponent();

            if (!r2l)
                LoadedAnimation.Actions.Clear();
            if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery)
                URL = $"https://gall.dcinside.com/mgallery/board/view/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&no={article.no}";
            else
                URL = $"https://gall.dcinside.com/board/view/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&no={article.no}";
            Update(article);
            Loaded += ThumbnailItem_Loaded;
        }

        public void Update(DCInsidePageArticle article)
        {
            Article = article;
            Title.Text = article.title;
            if (article.uid != "")
                Author.Text = $"{article.nick} ({article.uid})";
            else
                Author.Text = $"{article.nick} ({article.ip})";
            ViewCount.Text = article.count + " Views";
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
        }

        bool loaded = false;
        string temp_file;
        private void ThumbnailItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;
            loaded = true;

            switch (Article.type)
            {
                case "icon_recomimg":
                case "icon_pic":
                    Icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Image;
                    break;

                default:
                    Icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pen;
                    break;
            }

            if (Article.type == "icon_pic" || Article.type == "icon_recomimg")
            {
                Task.Run(() =>
                {
                    var html = NetTools.DownloadString(URL);
                    if (html == null || html == "")
                    {
                        Extends.Post(() =>
                        {
                            Icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.TrashCanOutline;
                        });
                        return;
                    }
                    ArticleBody = DCInsideUtils.ParseBoardView(html);
                    if (ArticleBody.ImagesLink.Count == 0) return;
                    temp_file = TemporaryFiles.UseNew();
                    NetTools.DownloadFile(ArticleBody.ImagesLink[0], temp_file);

                    Extends.Post(() => AnimationBehavior.SetSourceUri(Image, new Uri(temp_file)));
                });
            }
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(URL);
        }

        //DispatcherTimer timer;
        //private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    InfoPopup.Width = ActualWidth;
        //    InfoPopup.Height = ActualHeight;

        //    // Move Title to Top
        //    var pos = TranslatePoint(new Point(0, 0), Title);
        //    PopupTitle.Text = Title.Text;
        //    {
        //        var T = new TranslateTransform(-pos.X, -pos.Y);
        //        DoubleAnimation anim1 = new DoubleAnimation(8, TimeSpan.FromSeconds(0.6));
        //        DoubleAnimation anim2 = new DoubleAnimation(4, TimeSpan.FromSeconds(0.3));
        //        anim1.DecelerationRatio = 1;
        //        anim2.DecelerationRatio = 1;
        //        PopupTitle.RenderTransform = T;
        //        T.BeginAnimation(TranslateTransform.XProperty, anim1);
        //        T.BeginAnimation(TranslateTransform.YProperty, anim2);
        //    }

        //    // Expand
        //    {
        //        DoubleAnimation d1 = new DoubleAnimation(ActualWidth, 300, TimeSpan.FromSeconds(0.1));
        //        InfoPopup.BeginAnimation(FrameworkElement.WidthProperty, d1);
        //        DoubleAnimation d2 = new DoubleAnimation(ActualHeight, 600, TimeSpan.FromSeconds(0.1));
        //        InfoPopup.BeginAnimation(FrameworkElement.HeightProperty, d2);
        //    }

        //    timer = new DispatcherTimer();
        //    timer.Interval = TimeSpan.FromSeconds(0.05);
        //    timer.Tick += Timer_Tick;
        //    timer.Start();

        //    InfoPopup.IsOpen = true;
        //}

        //private void Timer_Tick(object sender, EventArgs e)
        //{
        //    var pos = Mouse.GetPosition(this);
        //    if (pos.X < 0 || pos.Y < 0 || pos.X > ActualWidth || pos.Y > ActualHeight)
        //    {
        //        InfoPopup.IsOpen = false;
        //        timer.Stop();
        //    }
        //}

        //private void Card_MouseMove(object sender, MouseEventArgs e)
        //{
        //    var pos = e.GetPosition(this);
        //    if (pos.X < 0 || pos.Y < 0 || pos.X > ActualWidth || pos.Y > ActualHeight)
        //        InfoPopup.IsOpen = false;
        //}

        //private void Card_MouseLeave(object sender, MouseEventArgs e)
        //{
        //    InfoPopup.IsOpen = false;
        //}
    }
}
