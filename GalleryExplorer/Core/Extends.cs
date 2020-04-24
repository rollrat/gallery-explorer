// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GalleryExplorer.Core
{
    public static class Extends
    {
        public static int ToInt(this string str) => Convert.ToInt32(str);

        public static string MyText(this HtmlNode node) =>
            string.Join("", node.ChildNodes.Where(x => x.Name == "#text").Select(x => x.InnerText.Trim()));

        public static void Post(Action acc) =>
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
               new Action(() => acc()));

        public static HtmlNode ToHtmlNode(this string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);
            return document.DocumentNode;
        }
    }
}
