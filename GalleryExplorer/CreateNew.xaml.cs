// This source code is a part of DCInside Gallery Explorer Project.
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
    /// CreateNew.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateNew : UserControl
    {
        public CreateNew()
        {
            InitializeComponent();

            logic = new AutoCompleteLogic(algorithm, GallerySelectText, AutoComplete, AutoCompleteList) 
            {
                PickOne = true,
                IgnoreCount = false,
                Foreground = Brushes.Black,
                DetectColor = Brushes.HotPink
            };
        }

        DCGalleryListAutoComplete algorithm = new DCGalleryListAutoComplete();
        AutoCompleteLogic logic;

        public string SelectedGallery;
        public string SelectedGalleryName;
        private void GallerySelectText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var id = GallerySelectText.Text.Split('(').Last().Split(')').First().Trim();
            if (DCGalleryList.Instance.GalleryIds.Contains(id) || DCGalleryList.Instance.MinorGalleryIds.Contains(id))
            {
                string url;
                if (DCGalleryList.Instance.MinorGalleryIds.Contains(id))
                    url = $"https://gall.dcinside.com/mgallery/board/lists?id={id}";
                else
                    url = $"https://gall.dcinside.com/board/lists?id={id}";

                var node = NetTools.DownloadString(url).ToHtmlNode().SelectSingleNode("//a[@class='page_end']");
                var page_end = 10;

                if (node != null)
                    page_end = node.GetAttributeValue("href", "").Split('=').Last().ToInt() + 10;

                PageEnds.Text = page_end.ToString();
                Status.Text = $"예상 수행시간 {page_end * 0.7}초";
                Status.Visibility = Visibility.Visible;
                SelectedGallery = id;
                SelectedGalleryName = GallerySelectText.Text.Split('(')[0].Trim();
            }
            else
            {
                Status.Visibility = Visibility.Collapsed;
                SelectedGallery = "";
            }
        }

        public int PageStartsNum;
        private void PageStarts_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(PageStarts.Text, out PageStartsNum))
            {
                MessageBox.Show("숫자만 입력해 주세요!", "Gallery Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
                PageStarts.Text = "1";
            }
        }

        public int PageEndsNum;
        private void PageEnds_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(PageEnds.Text, out PageEndsNum))
            {
                MessageBox.Show("숫자만 입력해 주세요!", "Gallery Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
                PageEnds.Text = "1";
            }
        }
    }
}
