// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using XamlAnimatedGif;

namespace GalleryExplorer
{
    /// <summary>
    /// ImageElements.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImageElements : UserControl
    {
        string file_name;
        public ImageElements(string path, double inv_score)
        {
            InitializeComponent();

            file_name = path;
            Title.Text = System.IO.Path.GetFileName(path);
            Artist.Text = inv_score.ToString();
            Loaded += ImageElements_Loaded;
        }

        public void AdjustWidth(int width)
        {
            MaxWidth = width;
            MinWidth = width;
        }

        private void ImageElements_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Extends.Post(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.DecodePixelWidth = 600;
                    bitmap.CacheOption = BitmapCacheOption.None;
                    bitmap.UriSource = new Uri(file_name);
                    bitmap.EndInit();

                    Image.Source = bitmap;
                    //Image.Source = new BitmapImage(new Uri(file_name));
                });
            });
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
