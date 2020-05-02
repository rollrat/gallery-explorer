// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
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
            var files = Directory.GetFiles(path);
            var total = files.Length;
            var count = 0;
            Parallel.ForEach(Directory.GetFiles(path).Where(x => x.EndsWith(".png") || x.EndsWith(".jpg") || x.EndsWith(".jpeg") || x.EndsWith(".bmp") || x.EndsWith(".webp")),
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

        }

        private void FindPhoto_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Clustering_Click(object sender, RoutedEventArgs e)
        {
            StatusProgress.Visibility = Visibility.Visible;
            Extends.Post(() => StatusText.Text = $"VP-Tree를 생성하는 중...");
            await Task.Run(() => {
                var clustered = iss.Clustering(x =>
                {
                    Extends.Post(() => StatusText.Text = $"클러스터링 중 ... [{x.Item1.ToString("#,#")}/{x.Item2.ToString("#,#")}] ({(x.Item1 / (double)x.Item2 * 100.0).ToString("#0.00")} %)");
                });
            });
            StatusProgress.Visibility = Visibility.Collapsed;
            Extends.Post(() => StatusText.Text = $"클러스터링이 완료되었습니다.");
        }
    }
}
