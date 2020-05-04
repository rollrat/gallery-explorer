// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using GalleryExplorer.Domain;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using OpenCvSharp;
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
using System.Windows.Shapes;

namespace GalleryExplorer
{
    /// <summary>
    /// ImageSimilarityWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImageSimilarityWindow : System.Windows.Window
    {
        ImageSoftSimilarity iss;

        public ImageSimilarityWindow()
        {
            InitializeComponent();

            ResultList.DataContext = new ImageSimilarityDataGridViewModel();
            ResultList.Sorting += new DataGridSortingEventHandler(new DataGridSorter<ImageSimilarityDataGridItemViewModel>(ResultList).SortHandler);
            iss = new ImageSoftSimilarity();
        }

        private void ProjectOpen_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            ofd.Filter = "이미지 유사도 분석 정보 파일 (*.iss)|*.iss";
            if (ofd.ShowDialog() == false)
                return;

            var bb = JsonConvert.DeserializeObject<List<(string, ulong[])>>(File.ReadAllText(ofd.FileName));
            bb.ForEach(x => 
            { 
                if (!iss.Hashs.ContainsKey(x.Item1)) 
                    iss.Hashs.Add(x.Item1, x.Item2);
            });
            Extends.Post(() => StatusText.Text = $"'{System.IO.Path.GetFileName(ofd.FileName)}' 파일을 불러왔습니다.");
        }

        private async void FolderOpen_Click(object sender, RoutedEventArgs e)
        {
            var cofd = new CommonOpenFileDialog();
            cofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            cofd.IsFolderPicker = true;
            if (cofd.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                StatusProgress.Visibility = Visibility.Visible;
                await Task.Run(() => process_folder(cofd.FileName));
                StatusProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void process_folder(string path)
        {
            var files = Directory.GetFiles(path).Where(x => x.EndsWith(".png") || x.EndsWith(".jpg") || x.EndsWith(".jpeg") || x.EndsWith(".bmp") || x.EndsWith(".webp"));
            var total = files.Count();
            var count = 0;
            Parallel.ForEach(files,
                x => 
                { 
                    iss.AppendImage(x);
                    Extends.Post(() =>
                    {
                        var cc = Interlocked.Increment(ref count);
                        StatusText.Text = $"이미지 파일 분석 중 ... [{cc.ToString("#,#")}/{total.ToString("#,#")}] ({(cc / (double)total * 100.0).ToString("#0.00")} %) {x}";
                    });
                });
            var result = new List<(string, ulong[])>();
            foreach (var kv in iss.Hashs)
            {
                result.Add((kv.Key, kv.Value));
            }
            var fn = $"iss-{System.IO.Path.GetFileName(path)}-{DateTime.Now.Ticks}.iss";
            File.WriteAllText(fn, JsonConvert.SerializeObject(result));
            Extends.Post(() => StatusText.Text = $"'{fn}' 파일로 분석 결과가 자동저장 되었습니다.");
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            ofd.Multiselect = true;
            ofd.Filter = "이미지 파일 (*.png, *.jpg, *.jpeg, *.bmp, *.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp";
            if (ofd.ShowDialog() == false)
                return;

            foreach (var file in ofd.FileNames)
                if (!iss.Hashs.ContainsKey(file))
                    iss.AppendImage(file);
            Extends.Post(() => StatusText.Text = $"{ofd.FileNames.Length}개 이미지 파일(들)을 불러왔습니다.");
        }

        private void FindPhoto_Click(object sender, RoutedEventArgs e)
        {
            double rate;
            if (!double.TryParse(MaxRate.Text, out rate))
            {
                MessageBox.Show("클러스터링 최고 역치는 실수여야합니다!", "Gallery Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            ofd.Filter = "이미지 파일 (*.png, *.jpg, *.jpeg, *.bmp, *.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp";
            if (ofd.ShowDialog() == false)
                return;

            var hash = ImageSoftSimilarity.MakeSoftHash(ofd.FileName);
            if (hash == null)
            {
                Extends.Post(() => StatusText.Text = $"'{ofd.FileName}'는 지원하는 이미지 파일이 아닌 것 같아요.");
                return;
            }
            var result = iss.FindForSoft(hash, 100).Where(x => x.Item2 <= rate).ToList();
            if (result.Count == 0)
            {
                Extends.Post(() => StatusText.Text = $"검색 결과가 없습니다 :(");
                return;
            }
            else
                Extends.Post(() => StatusText.Text = $"{result.Count}개 항목이 검색되었습니다.");

            ImagePanel.Children.Clear();
            result.ForEach(x => ImagePanel.Children.Add(new ImageElements(x.Item1, x.Item2)));
            current = result.Select(x => x.Item1).ToList();
            ImagePanel.Children.OfType<ImageElements>().ToList().ForEach(x => x.AdjustWidth((int)WidthSlider.Value));
        } 

        private async void Clustering_Click(object sender, RoutedEventArgs e)
        {
            StatusProgress.Visibility = Visibility.Visible;
            Extends.Post(() => StatusText.Text = $"VP-Tree를 생성하는 중...");
            double rate;
            if (!double.TryParse(MaxRate.Text, out rate))
            {
                MessageBox.Show("클러스터링 최고 역치는 실수여야합니다!", "Gallery Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await Task.Run(() => {
                var clustered = iss.Clustering(x =>
                {
                    Extends.Post(() => StatusText.Text = $"클러스터링 중 ... [{x.Item1.ToString("#,#")}/{x.Item2.ToString("#,#")}] ({(x.Item1 / (double)x.Item2 * 100.0).ToString("#0.00")} %)");
                }, 50, rate);

                Extends.Post(() =>
                {
                    var vm = ResultList.DataContext as ImageSimilarityDataGridViewModel;
                    vm.Items.Clear();
                    clustered.Where(x => x.Count >= 2).ToList().ForEach(x => vm.Items.Add(new ImageSimilarityDataGridItemViewModel 
                    {
                        개수 = x.Count.ToString(),
                        평균_정확도 = x.Max(y => y.Item2).ToString(),
                        results = x
                    }));
                });
            });
            StatusProgress.Visibility = Visibility.Collapsed;
            Extends.Post(() => StatusText.Text = $"클러스터링이 완료되었습니다.");
        }

        List<string> current;
        private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultList.SelectedItems.Count > 0)
            {
                var no = (ResultList.SelectedItems[0] as ImageSimilarityDataGridItemViewModel).results;
                ImagePanel.Children.Clear();
                no.ForEach(x => ImagePanel.Children.Add(new ImageElements(x.Item1, x.Item2)));
                current = no.Select(x => x.Item1).ToList();
                ImagePanel.Children.OfType<ImageElements>().ToList().ForEach(x => x.AdjustWidth((int)WidthSlider.Value));
            }
        }

        private void Detail_Click(object sender, RoutedEventArgs e)
        {
            if (current == null) return;
            var hard = new ImageHardSimilarity();
            var hash = current.Select(x => hard.MakeHash(x));
            var similarity = hash.Skip(1).Select(x => ImageHardSimilarity.GetCosineSimilarity(hash.First(), x)).ToList();

            ImagePanel.Children.Clear();
            ImagePanel.Children.Add(new ImageElements(current[0], 1));

            for (int i = 0; i < similarity.Count; i++)
            {
                ImagePanel.Children.Add(new ImageElements(current[i + 1], similarity[i]));
            }
            ImagePanel.Children.OfType<ImageElements>().ToList().ForEach(x => x.AdjustWidth((int)WidthSlider.Value));
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ImagePanel != null && ImagePanel.Children != null)
                ImagePanel.Children.OfType<ImageElements>().ToList().ForEach(x => x.AdjustWidth((int)WidthSlider.Value));
        }
    }
}
