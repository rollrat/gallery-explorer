// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using GalleryExplorer.Domain;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GalleryExplorer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;
        DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            TemporaryFiles.DeleteAllPreviouslyUsed();
            foreach (var page_number in PageNumberPanel.Children)
            {
                page_number_buttons.Add(page_number as Button);
            }
            initialize_page();

            SearchText.GotFocus += SearchText_GotFocus;
            SearchText.LostFocus += SearchText_LostFocus;

            ResultList.DataContext = new GalleryDataGridViewModel();
            ResultList.Sorting += new DataGridSortingEventHandler(new DataGridSorter<GalleryDataGridItemViewModel>(ResultList).SortHandler);
            logic = new AutoCompleteLogic(algorithm, SearchText, AutoComplete, AutoCompleteList);

#if DEBUG
            Logger.Instance.Push("Welcome to DCInside Gallery Explorer!");
            Core.Console.Instance.Start();
#endif
            Closed += MainWindow_Closed;

            Instance = this;
            timer.Interval = new TimeSpan(0, 0, 2);

            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logger.Instance.PushError("unhandled: " + (e.ExceptionObject as Exception).ToString());
            };
        }

        #region Search Box Action

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (Logger.IsValueCreated)
            {
                if (Logger.Instance.ControlEnable)
                    Core.Console.Instance.Stop();
            }
            TemporaryFiles.DeleteAllPreviouslyUsed();
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }

        private void SearchText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchText.Text))
                SearchText.Text = "검색";
        }

        private void SearchText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchText.Text == "검색")
                SearchText.Text = "";
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            PathIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A));
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            PathIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x71, 0x71, 0x71));
        }

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PathIcon.Margin = new Thickness(2, 0, 0, 0);
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PathIcon.Margin = new Thickness(0, 0, 0, 0);
        }

        #endregion

        #region Auto Complete

        DCGalleryAutoComplete algorithm = new DCGalleryAutoComplete();
        AutoCompleteLogic logic;

        private void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (SearchText.Text != "검색")
                {
                    if (e.Key == Key.Return && !logic.skip_enter)
                    {
                        ButtonAutomationPeer peer = new ButtonAutomationPeer(SearchButton);
                        IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                        invokeProv.Invoke();
                        logic.ClosePopup();
                    }
                    logic.skip_enter = false;
                }
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            var offset = AutoComplete.HorizontalOffset;
            AutoComplete.HorizontalOffset = offset + 1;
            AutoComplete.HorizontalOffset = offset;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var offset = AutoComplete.HorizontalOffset;
            AutoComplete.HorizontalOffset = offset + 1;
            AutoComplete.HorizontalOffset = offset;
        }

        #endregion

        private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultList.SelectedItems.Count > 0)
            {
                var no = (ResultList.SelectedItems[0] as GalleryDataGridItemViewModel).번호;
                var id = DCGalleryAnalyzer.Instance.Model.gallery_id;

                if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery)
                    Process.Start($"https://gall.dcinside.com/mgallery/board/view/?id={id}&no={no}");
                else
                    Process.Start($"https://gall.dcinside.com/board/view/?id={id}&no={no}");
            }
        }

        #region Search

        static void IntersectCountSplit(string[] target, List<string> source, ref bool[] check)
        {
            if (target != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    if (target.Any(e => e.ToLower().Split(' ').Any(x => x.Contains(source[i].ToLower()))))
                        check[i] = true;
                    else if (target.Any(e => e.ToLower().Replace(' ', '_') == source[i]))
                        check[i] = true;
                }
            }
        }

        private List<DCInsidePageArticle> search_internal(DCGalleryDataQuery query, int starts, int ends)
        {
            var result = new List<DCInsidePageArticle>();
            for (int i = starts; i < ends; i++)
            {
                var article = DCGalleryAnalyzer.Instance.Articles[i];

                if (query.Type != null)
                {
                    if (article.classify == null)
                        continue;
                    else if (article.classify != query.Type[0])
                        continue;
                }

                if (query.Nickname != null)
                {
                    if (article.nick == null)
                        continue;
                    else if (article.nick != query.Nickname[0])
                        continue;
                }

                if (query.Id != null)
                {
                    if (article.uid == null)
                        continue;
                    else if (article.uid != query.Id[0])
                        continue;
                }

                if (query.Ip != null)
                {
                    if (article.ip == null)
                        continue;
                    else if (article.ip != query.Ip[0])
                        continue;
                }

                if (query.Title != null)
                {
                    bool[] check = new bool[query.Title.Count];
                    IntersectCountSplit(article.title.Split(' '), query.Title, ref check);
                    if (!check.All((x => x)))
                        continue;
                }

                result.Add(article);
            }
            return result;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DCGalleryAnalyzer.Instance.Model == null)
                return;

            var search = SearchText.Text;
            if (search == "검색") search = "";

            DCGalleryDataQuery query = new DCGalleryDataQuery();
            List<string> positive_data = new List<string>();

            search.Trim().Split(' ').ToList().ForEach((a) => { if (!a.Contains(":") && !a.StartsWith("/") && !a.StartsWith("?")) positive_data.Add(a.Trim()); });
            query.Title = positive_data;
            foreach (var elem in from elem in search.Trim().Split(' ') where elem.Contains(":") where !elem.StartsWith("/") where !elem.StartsWith("?") select elem)
            {
                if (elem.StartsWith("nick:"))
                    if (query.Nickname == null)
                        query.Nickname = new List<string>() { elem.Substring("nick:".Length) };
                    else
                        query.Nickname.Add(elem.Substring("nick:".Length));
                else if (elem.StartsWith("id:"))
                    if (query.Id == null)
                        query.Id = new List<string>() { elem.Substring("id:".Length) };
                    else
                        query.Id.Add(elem.Substring("id:".Length));
                else if (elem.StartsWith("ip:"))
                    if (query.Ip == null)
                        query.Ip = new List<string>() { elem.Substring("ip:".Length) };
                    else
                        query.Ip.Add(elem.Substring("ip:".Length));
                else if (elem.StartsWith("class:"))
                    if (query.Type == null)
                        query.Type = new List<string>() { elem.Substring("class:".Length) };
                    else
                        query.Type.Add(elem.Substring("class:".Length));
                else
                {
                    Core.Console.Instance.WriteErrorLine($"Unknown rule '{elem}'.");
                }
            }

            int number = Environment.ProcessorCount;
            int term = DCGalleryAnalyzer.Instance.Articles.Count / number;

            List<Task<List<DCInsidePageArticle>>> arr_task = new List<Task<List<DCInsidePageArticle>>>();
            for (int i = 0; i < number; i++)
            {
                int k = i;
                if (k != number - 1)
                    arr_task.Add(new Task<List<DCInsidePageArticle>>(() => search_internal(query, k * term, k * term + term)));
                else
                    arr_task.Add(new Task<List<DCInsidePageArticle>>(() => search_internal(query, k * term, DCGalleryAnalyzer.Instance.Articles.Count)));
            }

            Parallel.ForEach(arr_task, task => task.Start());
            await Task.WhenAll(arr_task);

            List<DCInsidePageArticle> result = new List<DCInsidePageArticle>();
            for (int i = 0; i < number; i++)
            {
                result.AddRange(arr_task[i].Result);
            }

            if (SearchListView.Visibility == Visibility.Visible)
            {
                var tldx = ResultList.DataContext as GalleryDataGridViewModel;
                tldx.Items.Clear();
                foreach (var article in result)
                {
                    tldx.Items.Add(new GalleryDataGridItemViewModel
                    {
                        번호 = article.no,
                        제목 = article.title ?? "",
                        클래스 = article.classify ?? "",
                        날짜 = article.date.ToString(),
                        닉네임 = article.nick ?? "",
                        답글 = article.replay_num ?? "",
                        아이디 = article.uid != "" ? article.uid : $"({article.ip})",
                        조회수 = article.count ?? "",
                        추천수 = article.recommend ?? "",
                    });
                }
            }
            else
            {
                items = result;
                max_page = items.Count / show_elem_per_page;
                initialize_page();
            }
        }

        List<DCInsidePageArticle> items;
        int show_elem_per_page = 20;
        private void show_page_impl(int page)
        {
            SearchMaterialPanel.Children.Clear();
            if (items == null)
                return;

            Task.Run(() =>
            {
                for (int i = page * show_elem_per_page; i < (page + 1) * show_elem_per_page && i < items.Count; i++)
                {
                    Extends.Post(() => SearchMaterialPanel.Children.Add(new ThumbnailItem(items[i])));
                    Thread.Sleep(100);
                }
            });
        }

        #endregion

        #region Buttons

        public void CloseDialog()
        {
            RootDialogHost.IsOpen = false;
        }

        private async void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var tag = (sender as ListBoxItem).Tag as string;

            if (tag == "New")
            {
                var dialog = new CreateNew();
                if ((bool)await DialogHost.Show(dialog, "RootDialog") && !string.IsNullOrEmpty(dialog.SelectedGallery))
                {
                    var prog = new CreateNewProgress(dialog.SelectedGallery, dialog.SelectedGalleryName);
                    await DialogHost.Show(prog, "RootDialog");
                    var cp = new CreateNewComplete();
                    await DialogHost.Show(cp, "RootDialog");
                }
            }
            else if (tag == "Open")
            {
                var ofd = new OpenFileDialog();
                ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                ofd.Filter = "데이터 파일 (*.txt)|*.txt";
                if (ofd.ShowDialog() == false)
                    return;

                DCGalleryAnalyzer.Instance.Open(ofd.FileName);
                SyncButton.IsEnabled = true;
                Button_Click(null, null);
            }
            else if (tag == "Sync")
            {
                var dialog = new SyncProgress();
                await DialogHost.Show(dialog, "RootDialog");
                Button_Click(null, null);
            }
            else if (tag == "Console")
            {
                Logger.Instance.ControlEnable = true;
                Logger.Instance.Push("Welcome to DCInside Gallery Explorer!");
                Logger.Instance.Start();
            }
            else if(tag == "Help")
            {
                var dialog = new InfoMessage();
                await DialogHost.Show(dialog, "RootDialog");
            }
        }

        private void FuzzingButton_Click(object sender, RoutedEventArgs e)
        {
            if (FuzzingIcon.Tag is string && (string)FuzzingIcon.Tag == "checked")
            {
                FuzzingIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.LightningBolt;
                (FuzzingButton.FindResource("GlowOff") as Storyboard).Begin(FuzzingButton);
                FuzzingIcon.Foreground = new SolidColorBrush(Colors.White);
                FuzzingIcon.Tag = "unchecked";
                logic.UsingFuzzySearch = false;
            }
            else
            {
                FuzzingIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.LightningBolt;
                (FuzzingButton.FindResource("GlowOn") as Storyboard).Begin(FuzzingButton);
                FuzzingIcon.Foreground = new SolidColorBrush(Colors.Yellow);
                FuzzingIcon.Tag = "checked";
                logic.UsingFuzzySearch = true;
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ThumbnailPanel.Visibility = Visibility.Collapsed;
            SearchListView.Visibility = Visibility.Visible;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SearchListView.Visibility = Visibility.Collapsed;
            ThumbnailPanel.Visibility = Visibility.Visible;
        }

        #endregion

        #region Pager

        int max_page = 0; // 1 ~ 250
        int current_page_segment = 0;
        int selected_page = 0;

        List<Button> page_number_buttons = new List<Button>();

        /// <summary>
        /// 페이저를 초기화합니다.
        /// </summary>
        /// <param name="show"></param>
        private void initialize_page(bool show = true)
        {
            current_page_segment = 0;
            page_number_buttons.ForEach(x => x.Visibility = Visibility.Visible);
            set_page_segment(0);
            if (show) show_page(0);
        }

        /// <summary>
        /// 특정 페이지로 이동합니다.
        /// </summary>
        /// <param name="i"></param>
        private void show_page(int i)
        {
            page_number_buttons.ForEach(x => {
                x.Background = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30));
                x.Foreground = new SolidColorBrush(Color.FromRgb(0x71, 0x71, 0x71));
            });
            page_number_buttons[i % 10].Background = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
            page_number_buttons[i % 10].Foreground = new SolidColorBrush(Color.FromRgb(0x17, 0x17, 0x17));

            show_page_impl(i);
            selected_page = i;

            ScrollViewer.ScrollToTop();
        }

        /// <summary>
        /// 페이저 세그먼트의 표시여부를 설정합니다.
        /// </summary>
        /// <param name="seg"></param>
        private void set_page_segment(int seg)
        {
            for (int i = 0, j = current_page_segment * 10; i < 10; i++, j++)
            {
                page_number_buttons[i].Content = (j + 1).ToString();

                if (j <= max_page)
                    page_number_buttons[i].Visibility = Visibility.Visible;
                else
                    page_number_buttons[i].Visibility = Visibility.Collapsed;
            }
        }

        private void PageNumber_Click(object sender, RoutedEventArgs e)
        {
            show_page(Convert.ToInt32((string)(sender as Button).Content) - 1);
        }

        private void PageFunction_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag.ToString())
            {
                case "LeftLeft":
                    if (current_page_segment == 0) break;

                    current_page_segment = 0;
                    set_page_segment(0);
                    show_page(0);
                    break;

                case "Left":
                    if (current_page_segment == 0) break;

                    current_page_segment--;
                    set_page_segment(current_page_segment);
                    show_page(current_page_segment * 10);
                    break;

                case "Right":
                    if (max_page < 10) break;
                    if (current_page_segment == max_page / 10) break;

                    current_page_segment++;
                    set_page_segment(current_page_segment);
                    show_page(current_page_segment * 10);
                    break;

                case "RightRight":
                    if (max_page < 10) break;
                    if (current_page_segment == max_page / 10) break;

                    current_page_segment = max_page / 10;
                    set_page_segment(current_page_segment);
                    show_page(max_page);
                    break;
            }
        }

        #endregion

        #region Stack

        public struct StackElements
        {
            public int selected_page;
            public int max_page;
            public int current_page_segment;
            public double scroll_status;

            public string search_text;
            public DateTime? starts;
            public DateTime? ends;
            public bool show_bookmark;
            public int align_row;
            public int align_column;
        }

        List<StackElements> status_stack = new List<StackElements>();
        int stack_pointer = -1;

        private void stack_clear()
        {
            status_stack.Clear();
            stack_pointer = -1;
        }

        private void stack_push()
        {
            if (stack_pointer >= 0 && stack_pointer != status_stack.Count - 1)
            {
                status_stack.RemoveRange(stack_pointer + 1, status_stack.Count - stack_pointer - 1);
                stack_pointer = status_stack.Count - 1;
            }
            status_stack.Add(new StackElements
            {
                selected_page = selected_page,
                max_page = max_page,
                current_page_segment = current_page_segment,
                scroll_status = ScrollViewer.VerticalOffset,

                //align_column = align_column,
                //align_row = align_row,
                //search_text = latest_search_text,
            });
            stack_pointer++;
        }

        private void stack_back()
        {
            if (stack_pointer <= 0) return;
            stack_pointer--;
            stack_regression(stack_pointer);
        }

        private void stack_forward()
        {
            if (stack_pointer >= status_stack.Count - 1) return;
            stack_pointer++;
            stack_regression(stack_pointer);
        }

        private void stack_jump(int ptr)
        {
            stack_pointer = ptr;
            stack_regression(ptr);
        }

        string latest_search = "";
        private void stack_regression(int ptr)
        {
            var elem = status_stack[ptr];
            //elems = raws;
            //starts = elem.starts;
            //ends = elem.ends;
            //show_bookmark = elem.show_bookmark;
            //align_row = elem.align_row;
            //align_column = elem.align_column;
            if (latest_search != elem.search_text)
            {
                //day_before = elems = Search(elem.search_text, raws);
                SearchText.Text = elem.search_text;
                latest_search = elem.search_text;
            }

            max_page = elem.max_page;
            current_page_segment = elem.current_page_segment;

            //sort_data(align_column, align_row);
            //filter_data();
            page_number_buttons.ForEach(x => x.Visibility = Visibility.Visible);
            set_page_segment(current_page_segment);
            show_page(elem.selected_page);
            ScrollViewer.ScrollToVerticalOffset(elem.scroll_status);
        }

        #endregion
    }
}
