// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using GalleryExplorer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GalleryExplorer
{
    /// <summary>
    /// SignalWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SignalWindow : Window
    {
        Thread loop_thread;
        public SignalWindow()
        {
            InitializeComponent();

            loop_thread = new Thread(new ParameterizedThreadStart(loop));
            loop_thread.Start();

            Closed += SignalWindow_Closed;
        }

        private void SignalWindow_Closed(object sender, EventArgs e)
        {
            loop_thread.Abort();
        }

        
        private void loop(object param)
        {
            int latest_index = DCGalleryAnalyzer.Instance.Articles[0].no.ToInt();
            var rand = new Random();
            var dict = new Dictionary<int, ThumbnailItem>();
            int min_sleep = 2000;
            int rand_sleep;
            while (true)
            {
                var url = "";
                if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery)
                    url = $"https://gall.dcinside.com/mgallery/board/lists/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&page={1}";
                else
                    url = $"https://gall.dcinside.com/board/lists/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&page={1}";

                var html = NetTools.DownloadString(url);
                DCInsideGallery gall = null;

                if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery)
                    gall = DCInsideUtils.ParseMinorGallery(html);
                else
                    gall = DCInsideUtils.ParseGallery(html);

                if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery 
                    && (gall.articles == null || gall.articles.Length == 0))
                    gall = DCInsideUtils.ParseGallery(html);

                var articles = gall.articles.ToList();
                articles.Sort((x, y) => y.no.ToInt().CompareTo(x.no.ToInt()));

                // Update
                foreach (var article in articles)
                {
                    if (dict.ContainsKey(article.no.ToInt()))
                    {
                        Extends.Post(() => dict[article.no.ToInt()].Update(article));
                    }
                }

                if (latest_index < articles[0].no.ToInt())
                {
                    int i = 0;
                    while (latest_index < articles[i].no.ToInt())
                        i++;

                    // Create
                    while (--i >= 0)
                    {
                        Extends.Post(() =>
                        {
                            var ti = new ThumbnailItem(articles[i], true);
                            dict.Add(articles[i].no.ToInt(), ti);
                            SignalPannel.Children.Insert(0, ti);
                        });
                        Thread.Sleep(300);
                    }

                    latest_index = articles[0].no.ToInt();
                }

                Extends.Post(() => 
                {
                    while (SignalPannel.Children.Count > 10)
                    {
                        dict.Remove((SignalPannel.Children[SignalPannel.Children.Count - 1] as ThumbnailItem).Article.no.ToInt());
                        SignalPannel.Children.RemoveAt(SignalPannel.Children.Count - 1);
                    }
                });
                rand_sleep = rand.Next(500, 3000);
                Thread.Sleep(min_sleep + rand_sleep);
            }
        }

        // https://stackoverflow.com/questions/339620/how-do-i-remove-minimize-and-maximize-from-a-resizable-window-in-wpf
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16,
                          WS_MAXIMIZEBOX = 0x10000,
                          WS_MINIMIZEBOX = 0x20000;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper((Window)sender).Handle;
            var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
        }
    }
}
