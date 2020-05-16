// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GalleryExplorer.Domain
{
    public class ImageSimilarityDataGridItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<(string, double)> results;

        private string _num;
        public string 개수
        {
            get { return _num; }
            set
            {
                if (_num == value) return;
                _num = value;
                OnPropertyChanged();
            }
        }

        private string _class;
        public string 평균_정확도
        {
            get { return _class; }
            set
            {
                if (_class == value) return;
                _class = value;
                OnPropertyChanged();
            }
        }
    }

    public class ImageSimilarityDataGridViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<ImageSimilarityDataGridItemViewModel> _items;
        public ObservableCollection<ImageSimilarityDataGridItemViewModel> Items => _items;

        public ImageSimilarityDataGridViewModel(IEnumerable<ImageSimilarityDataGridItemViewModel> collection = null)
        {
            if (collection == null)
                _items = new ObservableCollection<ImageSimilarityDataGridItemViewModel>();
            else
                _items = new ObservableCollection<ImageSimilarityDataGridItemViewModel>(collection);
        }
    }
}
