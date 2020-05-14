// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using HtmlAgilityPack;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace GalleryExplorer.Core
{
    public class DCInsideArticle
    {
        public string Id { get; set; }
        public string GalleryName { get; set; }
        public string OriginalGalleryName { get; set; }
        public string Thumbnail { get; set; }
        public string Class { get; set; }
        public string Title { get; set; }
        public string Contents { get; set; }
        public List<string> ImagesLink { get; set; }
        public List<string> FilesName { get; set; }
        public string Archive { get; set; }
        public string ESNO { get; set; }
        public string Views { get; set; }
        public string ReplyCount { get; set; }
        public string CommentCount { get; set; }
    }

    [MessagePackObject]
    public class DCInsidePageArticle
    {
        [Key(0)]
        public string no;
        [Key(1)]
        public string classify;
        [Key(2)]
        public string type;
        [Key(3)]
        public string title;
        [Key(4)]
        public string replay_num;
        [Key(5)]
        public string nick;
        [Key(6)]
        public string uid;
        [Key(7)]
        public string ip;
        [Key(8)]
        public bool islogined;
        [Key(9)]
        public bool isfixed;
        [Key(10)]
        public DateTime date;
        [Key(11)]
        public string count;
        [Key(12)]
        public string recommend;
    }

    public class DCInsideGallery
    {
        public string id;
        public string name;
        public string esno;
        public string cur_page;
        public string max_page;
        public DCInsidePageArticle[] articles;
    }

    public class DCInsideCommentElement
    {
        public string no;
        public string parent;
        public string user_id;
        public string name;
        public string ip;
        public string reg_date;
        public string nicktype;
        public string t_ch1;
        public string t_ch2;
        public string vr_type;
        public string voice;
        public string rcnt;
        public string c_no;
        public int depth;
        public string del_yn;
        public string is_delete;
        public string memo;
        public string my_cmt;
        public string del_btn;
        public string mod_btn;
        public string a_my_cmt;
        public string reply_w;
        public string gallog_icon;
        public string vr_player;
        public string vr_player_tag;
        public int next_type;
    }

    public class DCInsideComment
    {
        public int total_cnt;
        public int comment_cnt;
        public List<DCInsideCommentElement> comments;
    }

    [MessagePackObject]
    public class DCInsideGalleryModel
    {
        [Key(0)]
        public bool is_minor_gallery;
        [Key(1)]
        public string gallery_id;
        [Key(2)]
        public string gallery_name;
        [Key(3)]
        public List<DCInsidePageArticle> articles;
    }

    public class DCInsideManager : ILazy<DCInsideManager>
    {
        public string ESNO { get; set; }

        public DCInsideManager()
        {
            ESNO = DCInsideUtils.ParseGallery(NetTools.DownloadString("https://gall.dcinside.com/board/lists?id=hit")).esno;
        }
    }

    public class DCInsideUtils
    {
        public static async Task<DCInsideComment> GetComments(DCInsideArticle article, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", article.OriginalGalleryName },
                { "no", article.Id },
                { "cmt_id", article.OriginalGalleryName },
                { "cmt_no", article.Id },
                { "e_s_n_o", article.ESNO },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<DCInsideComment> GetComments(DCInsideGallery g, DCInsidePageArticle article, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", g.id },
                { "no", article.no },
                { "cmt_id", g.id },
                { "cmt_no", article.no },
                { "e_s_n_o", g.esno },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<DCInsideComment> GetComments(string gall_id, string article_id, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", gall_id },
                { "no", article_id },
                { "cmt_id", gall_id },
                { "cmt_no", article_id },
                { "e_s_n_o", DCInsideManager.Instance.ESNO },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<List<DCInsideCommentElement>> GetAllComments(string gall_id, string article_id)
        {
            var first = await GetComments(gall_id, article_id, "1");
            TidyComments(ref first);
            if (first.comments.Count == first.total_cnt)
                return first.comments.ToList();
            var cur = first.comments.Count;
            int iter = 2;
            while (cur < first.total_cnt)
            {
                var ll = await GetComments(gall_id, article_id, iter++.ToString());
                TidyComments(ref ll);
                cur += ll.comments.Count;
                first.comments.AddRange(ll.comments);
            }
            return first.comments;
        }

        public static void TidyComments(ref DCInsideComment comment)
        {
            for (int i = 0; i < comment.comments.Count; i++)
                if (comment.comments[i].nicktype == "COMMENT_BOY")
                    comment.comments.RemoveAt(i--);
        }

        /// <summary>
        /// 일반 갤러리의 리스트를 가져옵니다.
        /// </summary>
        /// <returns></returns>
        public static SortedDictionary<string, string> GetGalleryList()
        {
            var dic = new SortedDictionary<string, string>();
            var src = NetTools.DownloadString("http://wstatic.dcinside.com/gallery/gallindex_iframe_new_gallery.html");

            var parse = new List<Match>();
            parse.AddRange(Regex.Matches(src, @"onmouseover=""gallery_view\('(\w+)'\);""\>[\s\S]*?\<.*?\>([\w\s]+)\<").Cast<Match>().ToList());
            parse.AddRange(Regex.Matches(src, @"onmouseover\=""gallery_view\('(\w+)'\);""\>\s*([\w\s]+)\<").Cast<Match>().ToList());

            foreach (var match in parse)
            {
                var identification = match.Groups[1].Value;
                var name = match.Groups[2].Value.Trim();

                if (!string.IsNullOrEmpty(name))
                {
                    if (name[0] == '-')
                        name = name.Remove(0, 1).Trim();
                    if (!dic.ContainsKey(name))
                        dic.Add(name, identification);
                }
            }

            return dic;
        }

        public static SortedDictionary<string, string> GetMinorGalleryList()
        {
            var dic = new SortedDictionary<string, string>();
            var html = NetTools.DownloadString("https://gall.dcinside.com/m");

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            foreach (var a in document.DocumentNode.SelectNodes("//a[@onmouseout='thumb_hide();']"))
                dic.Add(a.InnerText.Trim(), a.GetAttributeValue("href", "").Split('=').Last());

            var under_name = new List<string>();
            foreach (var b in document.DocumentNode.SelectNodes("//button[@class='btn_cate_more']"))
                under_name.Add(b.GetAttributeValue("data-lyr", ""));

            int count = 1;
            foreach (var un in under_name)
            {
            RETRY:
                //var wc = NetCommon.GetDefaultClient();
                //wc.Headers.Add("X-Requested-With", "XMLHttpRequest");
                //wc.QueryString.Add("under_name", un);
                //var subhtml = Encoding.UTF8.GetString(wc.UploadValues("https://gall.dcinside.com/ajax/minor_ajax/get_under_gall", "POST", wc.QueryString));
                var subhtml = NetTools.DownloadString($"https://wstatic.dcinside.com/gallery/mgallindex_underground/{un}.html");
                if (subhtml.Trim() == "")
                {
                    Console.Instance.WriteLine($"[{count}/{under_name.Count}] Retry {un}...");
                    goto RETRY;
                }

                HtmlDocument document2 = new HtmlDocument();
                document2.LoadHtml(subhtml);
                foreach (var c in document2.DocumentNode.SelectNodes("//a[@class='list_title']"))
                    if (!dic.ContainsKey(c.InnerText.Trim()))
                        dic.Add(c.InnerText.Trim(), c.GetAttributeValue("href", "").Split('=').Last());
                Console.Instance.WriteLine($"[{count++}/{under_name.Count}] Complete {un}");
            }

            return dic;
        }

        public static DCInsideArticle ParseBoardView(string html, bool is_minor = false)
        {
            DCInsideArticle article = new DCInsideArticle();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//div[@class='view_content_wrap']")[0];

            article.Id = Regex.Match(html, @"name=""gallery_no"" value=""(\d+)""").Groups[1].Value;
            article.GalleryName = Regex.Match(html, @"<h4 class=""block_gallname"">\[(.*?) ").Groups[1].Value;
            article.OriginalGalleryName = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            if (is_minor)
                article.Class = node.SelectSingleNode("//span[@class='title_headtext']").InnerText;
            article.Contents = node.SelectSingleNode("//div[@class='writing_view_box']").InnerHtml;
            article.Title = HttpUtility.HtmlDecode(node.SelectSingleNode("//span[@class='title_subject']").InnerText);
            article.Views = node.SelectSingleNode("//span[@class='gall_count']").InnerText.Trim().Split(' ').Last().Replace(",", "");
            article.ReplyCount = node.SelectSingleNode("//span[@class='gall_reply_num']").InnerText.Trim().Split(' ').Last().Replace(",", "");
            article.CommentCount = node.SelectSingleNode("//span[@class='gall_comment']").InnerText.Trim().Split(' ').Last().Replace(",", "");
            try
            {
                article.ImagesLink = node.SelectNodes("//ul[@class='appending_file']/li").Select(x => x.SelectSingleNode("./a").GetAttributeValue("href", "")).ToList();
                article.FilesName = node.SelectNodes("//ul[@class='appending_file']/li").Select(x => x.SelectSingleNode("./a").InnerText).ToList();
            }
            catch { }
            article.ESNO = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");

            return article;
        }

        public static DCInsideGallery ParseGallery(string html)
        {
            var gall = new DCInsideGallery();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//tbody")[0];

            gall.id = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            gall.name = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", "");
            gall.esno = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");
            gall.cur_page = document.DocumentNode.SelectSingleNode("//div[@class='bottom_paging_box']/em").InnerText;
            try { gall.max_page = document.DocumentNode.SelectSingleNode("//a[@class='page_end']").GetAttributeValue("href", "").Split('=').Last(); } catch { }

            var pas = new List<DCInsidePageArticle>();

            foreach (var tr in node.SelectNodes("./tr"))
            {
                var gall_num = tr.SelectSingleNode("./td[1]").InnerText;
                int v;
                if (!int.TryParse(gall_num, out v)) continue;

                var pa = new DCInsidePageArticle();
                pa.no = gall_num;
                pa.type = tr.SelectSingleNode("./td[2]/a/em").GetAttributeValue("class", "").Split(' ')[1];
                pa.title = HttpUtility.HtmlDecode(tr.SelectSingleNode("./td[2]/a").InnerText);
                try { pa.replay_num = tr.SelectSingleNode(".//span[@class='reply_num']").InnerText; } catch { }
                pa.nick = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-nick", "");
                pa.uid = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-uid", "");
                pa.ip = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-ip", "");
                if (pa.ip == "")
                {
                    pa.islogined = true;
                    if (tr.SelectSingleNode("./td[3]/a/img") != null && tr.SelectSingleNode("./td[3]/a/img").GetAttributeValue("src", "").Contains("fix_nik.gif"))
                        pa.isfixed = true;
                }
                pa.date = DateTime.Parse(tr.SelectSingleNode("./td[4]").GetAttributeValue("title", ""));
                pa.count = tr.SelectSingleNode("./td[5]").InnerText;
                pa.recommend = tr.SelectSingleNode("./td[6]").InnerText;

                pas.Add(pa);
            }

            gall.articles = pas.ToArray();

            return gall;
        }

        public static DCInsideGallery ParseMinorGallery(string html)
        {
            var gall = new DCInsideGallery();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//tbody")[0];

            gall.id = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            gall.name = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", "");
            gall.esno = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");
            gall.cur_page = document.DocumentNode.SelectSingleNode("//div[@class='bottom_paging_box']/em").InnerText;
            try { gall.max_page = document.DocumentNode.SelectSingleNode("//a[@class='page_end']").GetAttributeValue("href", "").Split('=').Last(); } catch { }

            List<DCInsidePageArticle> pas = new List<DCInsidePageArticle>();

            foreach (var tr in node.SelectNodes("./tr"))
            {
                try
                {
                    var gall_num = tr.SelectSingleNode("./td[1]").InnerText;
                    int v;
                    if (!int.TryParse(gall_num, out v)) continue;

                    var pa = new DCInsidePageArticle();
                    pa.no = gall_num;
                    pa.classify = tr.SelectSingleNode("./td[2]").InnerText;
                    pa.type = tr.SelectSingleNode("./td[3]/a/em").GetAttributeValue("class", "").Split(' ')[1];
                    pa.title = HttpUtility.HtmlDecode(tr.SelectSingleNode("./td[3]/a").InnerText);
                    try { pa.replay_num = tr.SelectSingleNode(".//span[@class='reply_num']").InnerText; } catch { }
                    pa.nick = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-nick", "");
                    pa.uid = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-uid", "");
                    pa.ip = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-ip", "");
                    if (pa.ip == "")
                    {
                        pa.islogined = true;
                        if (tr.SelectSingleNode("./td[4]/a/img") != null && tr.SelectSingleNode("./td[4]/a/img").GetAttributeValue("src", "").Contains("fix_nik.gif"))
                            pa.isfixed = true;
                    }
                    pa.date = DateTime.Parse(tr.SelectSingleNode("./td[5]").GetAttributeValue("title", ""));
                    pa.count = tr.SelectSingleNode("./td[6]").InnerText;
                    pa.recommend = tr.SelectSingleNode("./td[7]").InnerText;

                    pas.Add(pa);
                }
                catch { }
            }

            gall.articles = pas.ToArray();

            return gall;
        }
    }
}
