// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GalleryExplorer.Core
{
    public class ImageConsoleOption : IConsoleOption
    {
        [CommandLine("--help", CommandType.OPTION, Default = true)]
        public bool Help;

        [CommandLine("--equal-clustering", CommandType.ARGUMENTS, ArgumentsCount = 1, Help = "use --equal-clustering <Directory>",
            Info = "폴더 내에 위한 모든 이미지들에 대해 완전히 동일한 이미지들을 클러스터링합니다.")]
        public string[] EqualClustering;
        [CommandLine("--soft-clustering", CommandType.ARGUMENTS, ArgumentsCount = 1, Help = "use --soft-clustering <Directory>",
            Info = "폴더 내에 위한 모든 이미지들에 대해 매우 유사한 이미지들을 클러스터링합니다.")]
        public string[] SoftClustering;

        [CommandLine("--test", CommandType.ARGUMENTS, Help = "use --test <what>",
            Info = "테스트 명령을 실행합니다.")]
        public string[] Test;
    }

    class ImageConsole : IConsole
    {
        static bool Redirect(string[] arguments, string contents)
        {
            ImageConsoleOption option = CommandLineParser.Parse<ImageConsoleOption>(arguments, contents != "", contents);

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
            else if (option.EqualClustering != null)
            {
                ProcessEqualClustering(option.EqualClustering);
            }
            else if (option.SoftClustering != null)
            {
                ProcessSoftClustering(option.SoftClustering);
            }
            else if (option.Test != null)
            {
                ProcessTest(option.Test);
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
                "이미지 명령콘솔\r\n"
                );

            var builder = new StringBuilder();
            CommandLineParser.GetFields(typeof(ImageConsoleOption)).ToList().ForEach(
                x =>
                {
                    if (!string.IsNullOrEmpty(x.Value.Item2.Help))
                        builder.Append($" {x.Key} ({x.Value.Item2.Help}) : {x.Value.Item2.Info} [{x.Value.Item1}]\r\n");
                    else
                        builder.Append($" {x.Key} : {x.Value.Item2.Info} [{x.Value.Item1}]\r\n");
                });
            Console.Instance.WriteLine(builder.ToString());
        }

        static void ProcessEqualClustering(string[] args)
        {
            var iss = new ImageEqualSimilarity();
            Console.Instance.WriteLine("이미지들을 해싱하는 중...");
            using (var progressBar = new Console.ConsoleProgressBar())
            {
                var files = Directory.GetFiles(args[0]).Where(x => x.EndsWith(".gif") || x.EndsWith(".png") || x.EndsWith(".jpg") || x.EndsWith(".jpeg") || x.EndsWith(".webp") || x.EndsWith(".bmp"));
                int counts = files.Count();
                int complete = 0;
                Parallel.ForEach(files,
                x =>
                {
                    iss.AppendImage(x);
                    progressBar.SetProgress(Interlocked.Increment(ref complete) / (float)counts * 100);
                });
            }
            var clustered = iss.Clustering();
            clustered.RemoveAll(x => x.Count == 1);
            clustered.Sort((x, y) => y.Count.CompareTo(x.Count));
            Console.Instance.WriteLine(clustered);
        }

        static void ProcessSoftClustering(string[] args)
        {
            var iss = new ImageSoftSimilarity();
            Console.Instance.WriteLine("이미지들을 해싱하는 중...");
            using (var progressBar = new Console.ConsoleProgressBar())
            {
                var files = Directory.GetFiles(args[0]).Where(x => x.EndsWith(".png") || x.EndsWith(".jpg") || x.EndsWith(".jpeg") || x.EndsWith(".webp") || x.EndsWith(".bmp"));
                int counts = files.Count();
                int complete = 0;
                Parallel.ForEach(files,
                x =>
                {
                    iss.AppendImage(x);
                    progressBar.SetProgress(Interlocked.Increment(ref complete) / (float)counts * 100);
                });
            }
            List<List<string>> clustered;
            using (var progressBar = new Console.ConsoleProgressBar())
            {
                Console.Instance.WriteLine("클러스터링 중...");
                clustered = iss.Clustering(x =>
                {
                    progressBar.SetProgress(x.Item1 / (float)x.Item2 * 100);
                });
            }
            clustered.RemoveAll(x => x.Count == 1);
            Console.Instance.WriteLine(clustered);
        }

        static void ProcessTest(string[] args)
        {
            switch (args[0])
            {
                case "1":
                    {
                        var iss = new ImageSoftSimilarity();
                        //foreach (var file in Directory.GetFiles(@"C:\Users\rollrat\Desktop\새 폴더"))
                        //    if (file.EndsWith(".png"))
                        //        iss.AppendImage(file);

                        Parallel.ForEach(Directory.GetFiles(@"C:\Users\rollrat\Desktop\새 폴더").Where(x => x.EndsWith(".png")),
                            //new ParallelOptions { MaxDegreeOfParallelism = 2 },
                            (x) =>
                        {
                            iss.AppendImage(x);
                        });

                        iss.FindForSoft(ImageSoftSimilarity.MakeSoftHash(@"C:\Users\rollrat\Desktop\새 폴더\1577170523.png"), 20);
                    }
                    break;

                case "2":
                    {
                        var iss = new ImageSoftSimilarity();
                        Parallel.ForEach(Directory.GetFiles(@"C:\Users\rollrat\Desktop\새 폴더").Where(x => x.EndsWith(".png") || x.EndsWith(".jpg")),
                            //new ParallelOptions { MaxDegreeOfParallelism = 4 },
                            x => iss.AppendImage(x));

                        var clustered = iss.Clustering();
                    }
                    break;
            }
        }
    }
}
