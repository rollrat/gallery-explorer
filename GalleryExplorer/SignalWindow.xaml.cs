// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Domain;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GalleryExplorer
{
    /// <summary>
    /// SignalWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SignalWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        public SignalWindow()
        {
            InitializeComponent();

            timer.Interval = new TimeSpan(0, 0, 2);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            var rand = new Random();
            var gg = DCGalleryAnalyzer.Instance.Articles[rand.Next(DCGalleryAnalyzer.Instance.Articles.Count)];
            SignalPannel.Children.Insert(0, new ThumbnailItem(gg, true));
        }
    }
}
