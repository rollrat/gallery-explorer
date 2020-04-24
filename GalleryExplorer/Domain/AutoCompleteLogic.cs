// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GalleryExplorer.Domain
{
    public class AutoCompleteTagData
    {
        public string Tag { get; set; }
        public int Count { get; set; }
    }

    public interface IAutoCompleteAlgorithm
    {
        List<AutoCompleteTagData> GetResults(ref string word, ref int position, bool using_fuzzy);
    }

    public class AutoCompleteLogic
    {
        private TextBox SearchText;
        private Popup AutoComplete;
        private ListBox AutoCompleteList;
        private IAutoCompleteAlgorithm Algorithm;

        private int global_position = 0;
        private string global_text = "";
        private bool selected_part = false;
        public bool skip_enter = false;

        public bool IsOpen => AutoComplete.IsOpen;

        public bool DoNotHighlight;
        public bool UsingFuzzySearch;
        public int MaxCount { get; set; } = 100;

        public bool PickOne = false;
        public bool IgnoreCount = true;

        public Brush Foreground { get; set; } = Brushes.White;
        public Brush DetectColor { get; set; } = Brushes.LightPink;

        public AutoCompleteLogic(IAutoCompleteAlgorithm Algorithm, TextBox SearchText, Popup AutoComplete, ListBox AutoCompleteList)
        {
            this.SearchText = SearchText;
            this.AutoComplete = AutoComplete;
            this.AutoCompleteList = AutoCompleteList;
            this.Algorithm = Algorithm;

            SearchText.PreviewKeyDown += SearchText_PreviewKeyDown;
            SearchText.KeyUp += SearchText_KeyUp;
            AutoCompleteList.PreviewKeyUp += AutoCompleteList_KeyUp;
            AutoCompleteList.KeyUp += AutoCompleteList_KeyUp;
            AutoCompleteList.MouseDoubleClick += AutoCompleteList_MouseDoubleClick;
        }

        public void ClosePopup()
        {
            AutoComplete.IsOpen = false;
        }

        public void SearchText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            AutoCompleteList.Width = SearchText.RenderSize.Width;

            if (e.Key == Key.Escape)
            {
                AutoComplete.IsOpen = false;
                SearchText.Focus();
            }
            else
            {
                if (AutoComplete.IsOpen)
                {
                    if (e.Key == Key.Down)
                    {
                        AutoCompleteList.SelectedIndex = 0;
                        AutoCompleteList.Focus();
                    }
                    else if (e.Key == Key.Up)
                    {
                        AutoCompleteList.SelectedIndex = AutoCompleteList.Items.Count - 1;
                        AutoCompleteList.Focus();
                    }
                }

                if (selected_part)
                {
                    selected_part = false;
                    if (e.Key != Key.Back)
                    {
                        SearchText.SelectionStart = global_position;
                        SearchText.SelectionLength = 0;
                    }
                }
            }
        }

        public Size MeasureString(string candidate)
        {
            var  formattedText = new FormattedText(
                candidate, 
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(SearchText.FontFamily, SearchText.FontStyle, SearchText.FontWeight, SearchText.FontStretch),
                SearchText.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(SearchText).PixelsPerDip);

            return new Size(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
        }

        public void SearchText_KeyUp(object sender, KeyEventArgs e)
        {
            AutoCompleteList.Width = SearchText.RenderSize.Width;

            if (e.Key == Key.Enter) return;
            int position = SearchText.SelectionStart;

            /////////////////////////////////////////////////
            string word = "";
            if (!PickOne)
            {
                while (position > 0 && !" ()-+&|~".Contains(SearchText.Text[position - 1]))
                    position -= 1;

                for (int i = position; i < SearchText.Text.Length; i++)
                {
                    if (" ()-+&|~".Contains(SearchText.Text[i])) break;
                    word += SearchText.Text[i];
                }
                if (word == "") { AutoComplete.IsOpen = false; return; }
            }
            else
            {
                word = SearchText.Text;
                position = 0;
                if (word == "") { AutoComplete.IsOpen = false; return; }
            }
            var match = Algorithm.GetResults(ref word, ref position, UsingFuzzySearch);
            /////////////////////////////////////////////////

            if (match.Count > 0)
            {
                AutoComplete.IsOpen = true;
                AutoCompleteList.Items.Clear();
                List<string> listing = new List<string>();
                for (int i = 0; i < MaxCount && i < match.Count; i++)
                {
                    if (match[i].Count != 0)
                        listing.Add(match[i].Tag + $" ({match[i].Count})");
                    else
                        listing.Add(match[i].Tag);
                }
                var MaxColoredTextLength = word.Length;
                var ColoredTargetText = word;
                listing.ForEach(x => {
                    if (DoNotHighlight)
                    {
                        AutoCompleteList.Items.Add(x);
                    }
                    else if (!UsingFuzzySearch)
                    {
                        var Result = new TextBlock();
                        Result.Foreground = Foreground;
                        int StartColoredTextPosition = x.ToLower().IndexOf(ColoredTargetText.ToLower());
                        string firstdraw = x.Substring(0, StartColoredTextPosition);
                        Result.Text = firstdraw;

                        var Detected = new Run();
                        Detected.Foreground = DetectColor;
                        string seconddraw = x.Substring(StartColoredTextPosition, MaxColoredTextLength);
                        Detected.Text = seconddraw;

                        var Postfix = new Run();
                        Postfix.Foreground = Foreground;
                        Postfix.Text = x.Substring(StartColoredTextPosition + MaxColoredTextLength);

                        Result.Inlines.Add(Detected);
                        Result.Inlines.Add(Postfix);
                        AutoCompleteList.Items.Add(Result);
                    }
                    else
                    {
                        var Result = new TextBlock();
                        Result.Foreground = Brushes.Black;
                        string prefix = "";
                        if (x.Contains(":") && !ColoredTargetText.Contains(":") && x.Split(':')[1] != "")
                        {
                            prefix = x.Split(':')[0] + ":";
                            x = x.Split(':')[1];
                        }
                        string postfix = x.Split(' ').Length > 1 ? x.Split(' ')[1] : "";
                        x = x.Split(' ')[0];

                        if (prefix != "")
                        {
                            Result.Text = prefix;
                        }
                        int[] diff = Strings.GetLevenshteinDistance(x.ToLower(), ColoredTargetText.ToLower());
                        for (int i = 0; i < x.Length; i++)
                        {
                            var Temp = new Run();
                            Temp.Text = x[i].ToString();
                            if (diff[i + 1] == 1)
                                Temp.Foreground = Brushes.HotPink;
                            else
                                Temp.Foreground = Brushes.Black;
                            Result.Inlines.Add(Temp);
                        }
                        var Postfix = new Run();
                        Postfix.Text = postfix;
                        Postfix.Foreground = Brushes.Black;
                        Result.Inlines.Add(Postfix);
                        AutoCompleteList.Items.Add(Result);
                    }
                });
                AutoComplete.HorizontalOffset = MeasureString(SearchText.Text.Substring(0, position)).Width;
            }
            else { AutoComplete.IsOpen = false; return; }

            global_position = position;
            global_text = word;

            if (e.Key == Key.Down)
            {
                AutoCompleteList.SelectedIndex = 0;
                AutoCompleteList.Focus();
            }
            else if (e.Key == Key.Up)
            {
                AutoCompleteList.SelectedIndex = AutoCompleteList.Items.Count - 1;
                AutoCompleteList.Focus();
            }
            else if (e.Key == Key.Enter || (!PickOne && e.Key == Key.Space))
            {
                var inline = (AutoCompleteList.Items[0] as TextBlock).Inlines;
                PutStringIntoTextBox(string.Join("", inline.Select(x => new TextRange(x.ContentStart, x.ContentEnd).Text)));
            }
        }

        public void PutStringIntoTextBox(string text)
        {
            if (IgnoreCount)
                text = text.Split('(')[0].Trim();
            SearchText.Text = SearchText.Text.Substring(0, global_position) +
                text +
                SearchText.Text.Substring(global_position + global_text.Length);
            AutoComplete.IsOpen = false;

            SearchText.SelectionStart = global_position;
            SearchText.SelectionLength = text.Length;
            skip_enter = true;
            SearchText.Focus();

            global_position = global_position + SearchText.SelectionLength;
            selected_part = true;
        }

        public void AutoCompleteList_KeyUp(object sender, KeyEventArgs e)
        {
            AutoCompleteList.Width = SearchText.RenderSize.Width;

            if (e.Key == Key.Enter || (!PickOne && e.Key == Key.Space))
            {
                if (AutoCompleteList.SelectedItems.Count > 0)
                {
                    var inline = (AutoCompleteList.SelectedItem as TextBlock).Inlines;
                    PutStringIntoTextBox(string.Join("", inline.Select(x => new TextRange(x.ContentStart, x.ContentEnd).Text)));
                }
            }
            else if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Escape)
            {
                AutoComplete.IsOpen = false;
                SearchText.Focus();
            }
        }

        public void AutoCompleteList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AutoCompleteList.Width = SearchText.RenderSize.Width;

            var inline = (AutoCompleteList.SelectedItem as TextBlock).Inlines;
            PutStringIntoTextBox(string.Join("", inline.Select(x => new TextRange(x.ContentStart, x.ContentEnd).Text)));
        }
    }
}
