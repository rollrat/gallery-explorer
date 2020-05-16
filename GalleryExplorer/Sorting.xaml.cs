// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

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

namespace GalleryExplorer
{
    /// <summary>
    /// Sorting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Sorting : UserControl
    {
        public Sorting(int c, int r)
        {
            InitializeComponent();

            AlignColumn.SelectedIndex = c;
            AlignRow.SelectedIndex = r;
        }

        public int AlignColumnIndex = 0;
        private void AlignColumn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AlignColumnIndex = AlignColumn.SelectedIndex;
        }

        public int AlignRowIndex = 0;
        private void AlignRow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AlignRowIndex = AlignRow.SelectedIndex;
        }
    }
}
