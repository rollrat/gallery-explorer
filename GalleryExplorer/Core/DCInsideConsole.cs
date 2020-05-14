// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Domain;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GalleryExplorer.Core
{
    public class DCInsideConsoleOption : IConsoleOption
    {
        [CommandLine("--help", CommandType.OPTION, Default = true)]
        public bool Help;

        [CommandLine("--collect-articles", CommandType.ARGUMENTS, ArgumentsCount = 3, Help = "use --collect-articles <Gallery Id> <Start Page> <End Page>",
            Info = "Parse gallery pages.")]
        public string[] CollectArticles;
        [CommandLine("--json-to-msgpack", CommandType.ARGUMENTS, ArgumentsCount = 1, Help = "use --json-to-msgpack <Source>",
            Info = "디시인사이드 갤러리 데이터 정보인 Json 형식을 MessagePack 형식으로 바꿉니다.")]
        public string[] JsonToMessagePack;
        [CommandLine("--archive", CommandType.ARGUMENTS, ArgumentsCount = 1, Help = "use --json-to-msgpack <Count>",
            Info = "현재 로딩된 갤러리의 게시물들을 아카이브합니다. <Count>는 게시물의 no를 내림차순으로 정렬했을 때 첫 번째 요소부터 아카이브할 게시물들의 개수입니다.")]
        public string[] Archive;

        [CommandLine("--test", CommandType.ARGUMENTS, Help = "use --test <what>",
            Info = "테스트 명령을 실행합니다.")]
        public string[] Test;
    }

    class DCInsideConsole : IConsole
    {
        static bool Redirect(string[] arguments, string contents)
        {
            DCInsideConsoleOption option = CommandLineParser.Parse<DCInsideConsoleOption>(arguments, contents != "", contents);

            if (option.Error)
            {
                Console.Instance.WriteLine(option.ErrorMessage);
                if (option.HelpMessage != null)
                    Console.Instance.WriteLine(option.HelpMessage);
                return false;
            }
            else if (option.Help)
            {
                PrintHelp();
            }
            else if (option.CollectArticles != null)
            {
#if DEBUG
                ProcessCollectArticles(option.CollectArticles);
#endif
            }
            else if (option.JsonToMessagePack != null)
            {
                ProcessJsonToMessagePack(option.JsonToMessagePack);
            }
            else if (option.Archive != null)
            {
                ProcessArchive(option.Archive);
            }

            return true;
        }

        bool IConsole.Redirect(string[] arguments, string contents)
        {
            return Redirect(arguments, contents);
        }

        static void PrintHelp()
        {
            Console.Instance.WriteLine(
                "디시인사이드 명령콘솔\r\n"
                );

            var builder = new StringBuilder();
            CommandLineParser.GetFields(typeof(DCInsideConsoleOption)).ToList().ForEach(
                x =>
                {
                    if (!string.IsNullOrEmpty(x.Value.Item2.Help))
                        builder.Append($" {x.Key} ({x.Value.Item2.Help}) : {x.Value.Item2.Info} [{x.Value.Item1}]\r\n");
                    else
                        builder.Append($" {x.Key} : {x.Value.Item2.Info} [{x.Value.Item1}]\r\n");
                });
            Console.Instance.WriteLine(builder.ToString());
        }

        static void ProcessCollectArticles(string[] args)
        {
            var rstarts = Convert.ToInt32(args[1]);
            var starts = Convert.ToInt32(args[1]);
            var ends = Convert.ToInt32(args[2]);

            bool is_minorg = !DCGalleryList.Instance.GalleryIds.Contains(args[0]);

            var result = new DCInsideGalleryModel();
            var articles = new List<DCInsidePageArticle>();

            using (var progressBar = new Console.ConsoleProgressBar())
            {
                for (; starts <= ends; starts++)
                {
                    var url = "";
                    if (is_minorg)
                        url = $"https://gall.dcinside.com/mgallery/board/lists/?id={args[0]}&page={starts}";
                    else
                        url = $"https://gall.dcinside.com/board/lists/?id={args[0]}&page={starts}";

                    Console.Instance.WriteLine($"Download URL: {url}");

                    var html = NetTools.DownloadString(url);
                    DCInsideGallery gall = null;

                    if (is_minorg)
                        gall = DCInsideUtils.ParseMinorGallery(html);
                    else
                        gall = DCInsideUtils.ParseGallery(html);

                    if (is_minorg && (gall.articles == null || gall.articles.Length == 0))
                        gall = DCInsideUtils.ParseGallery(html);

                    articles.AddRange(gall.articles);

                    progressBar.SetProgress((((ends - rstarts + 1) - (ends - starts)) / (float)(ends - rstarts + 1)) * 100);
                }

                var overlap = new HashSet<string>();
                var articles_trim = new List<DCInsidePageArticle>();
                foreach (var article in articles)
                    if (!overlap.Contains(article.no))
                    {
                        articles_trim.Add(article);
                        overlap.Add(article.no);
                    }

                articles_trim.Sort((x, y) => y.no.ToInt().CompareTo(x.no.ToInt()));

                result.is_minor_gallery = is_minorg;
                result.gallery_id = args[0];
                result.articles = articles_trim;

                File.WriteAllText($"list-{args[0]}-{DateTime.Now.Ticks}.txt", JsonConvert.SerializeObject(result));

                var bbb = MessagePackSerializer.Serialize(result);
                using (FileStream fsStream = new FileStream($"list-{args[0]}-{DateTime.Now.Ticks}-index.txt", FileMode.Create))
                using (BinaryWriter sw = new BinaryWriter(fsStream))
                {
                    sw.Write(bbb);
                }
            }
        }

        static void ProcessJsonToMessagePack(string[] args)
        {
            var src = JsonConvert.DeserializeObject<DCInsideGalleryModel>(File.ReadAllText(args[0]));
            var x = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + "-index.txt");

            foreach (var s in src.articles)
            {
                s.title = HttpUtility.HtmlDecode(s.title);
            }

            var bbb = MessagePackSerializer.Serialize(src);
            Logger.Instance.Push("Write file: " + x);
            using (FileStream fsStream = new FileStream(x, FileMode.Create))
            using (BinaryWriter sw = new BinaryWriter(fsStream))
            {
                sw.Write(bbb);
            }
        }

        static void ProcessArchive(string[] args)
        {
            var counts = Convert.ToInt32(args[0]);

            var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var sp = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Archive",
                $"{DCGalleryAnalyzer.Instance.Model.gallery_name} ({DCGalleryAnalyzer.Instance.Model.gallery_id})");

            Directory.CreateDirectory(sp);

            for (int i = 0; i < counts; i++)
            {
                var article = DCGalleryAnalyzer.Instance.Articles[i];
                var ttitle = $"{article.title}";
                foreach (char c in invalid)
                    ttitle = ttitle.Replace(c.ToString(), "");

                string url;

                if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery)
                    url = $"https://gall.dcinside.com/mgallery/board/view/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&no={article.no}";
                else
                    url = $"https://gall.dcinside.com/board/view/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&no={article.no}";

                var html = NetTools.DownloadString(url);
                var info = DCInsideUtils.ParseBoardView(html, DCGalleryAnalyzer.Instance.Model.is_minor_gallery);

                File.WriteAllText(Path.Combine(sp, $"[{article.no}]-body-{ttitle}.json"), JsonConvert.SerializeObject(info, Formatting.Indented));

                int com;
                if (int.TryParse(info.CommentCount.Replace(",", ""), out com) && com > 0)
                {
                    var comments = DCInsideUtils.GetAllComments(DCGalleryAnalyzer.Instance.Model.gallery_id, article.no).Result;
                    File.WriteAllText(Path.Combine(sp, $"[{article.no}]-comments-{ttitle}.json"), JsonConvert.SerializeObject(comments, Formatting.Indented));
                }

                Console.Instance.WriteLine($"{counts}중 {i}개 완료");
                Thread.Sleep(700);
            }
        }
    }
}
