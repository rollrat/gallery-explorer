// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace gall_archive_zip
{
    [MessagePackObject]
    public class DCInsideArticle
    {
        [Key(0)]
        public string Id { get; set; }
        [Key(1)]
        public string GalleryName { get; set; }
        [Key(2)]
        public string OriginalGalleryName { get; set; }
        [Key(3)]
        public string Thumbnail { get; set; }
        [Key(4)]
        public string Class { get; set; }
        [Key(5)]
        public string Title { get; set; }
        [Key(6)]
        public string Contents { get; set; }
        [Key(7)]
        public List<string> ImagesLink { get; set; }
        [Key(8)]
        public List<string> FilesName { get; set; }
        [Key(9)]
        public string Archive { get; set; }
        [Key(10)]
        public string ESNO { get; set; }
        [Key(11)]
        public string Views { get; set; }
        [Key(12)]
        public string ReplyCount { get; set; }
        [Key(13)]
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

    [MessagePackObject]
    public class DCInsideCommentElement
    {
        [Key(0)]
        public string no;
        [Key(1)]
        public string parent;
        [Key(2)]
        public string user_id;
        [Key(3)]
        public string name;
        [Key(4)]
        public string ip;
        [Key(5)]
        public string reg_date;
        [Key(6)]
        public string nicktype;
        [Key(7)]
        public string t_ch1;
        [Key(8)]
        public string t_ch2;
        [Key(9)]
        public string vr_type;
        [Key(10)]
        public string voice;
        [Key(11)]
        public string rcnt;
        [Key(12)]
        public string c_no;
        [Key(13)]
        public int depth;
        [Key(14)]
        public string del_yn;
        [Key(15)]
        public string is_delete;
        [Key(16)]
        public string memo;
        [Key(17)]
        public string my_cmt;
        [Key(18)]
        public string del_btn;
        [Key(19)]
        public string mod_btn;
        [Key(20)]
        public string a_my_cmt;
        [Key(21)]
        public string reply_w;
        [Key(22)]
        public string gallog_icon;
        [Key(23)]
        public string vr_player;
        [Key(24)]
        public string vr_player_tag;
        [Key(25)]
        public int next_type;
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

    public class DCGalleryAnalyzer
    {
        public static DCGalleryAnalyzer Instance = new DCGalleryAnalyzer();
        string filename = "list.txt";

        public void Open(string filename = "list.txt")
        {
            this.filename = filename;
            Model = MessagePackSerializer.Deserialize<DCInsideGalleryModel>(File.ReadAllBytes(filename));

            for (int i = 0; i < Model.articles.Count; i++)
            {
                Index.Add(Convert.ToInt32(Model.articles[i].no), i);
            }
        }

        public void Save()
        {
            var bbb = MessagePackSerializer.Serialize(Model);
            using (FileStream fsStream = new FileStream(filename, FileMode.Create))
            using (BinaryWriter sw = new BinaryWriter(fsStream))
            {
                sw.Write(bbb);
            }
        }

        public DCInsideGalleryModel Model { get; private set; }
        public List<DCInsidePageArticle> Articles => Model.articles;
        public Dictionary<int, int> Index = new Dictionary<int, int>();
    }

    [MessagePackObject]
    public class NewDCArticle
    {
        [Key(0)]
        public DCInsidePageArticle info;
        [Key(1)]
        public string raw;
        [Key(2)]
        public List<DCInsideCommentElement> comments;
        [Key(3)]
        public List<string> datalinks;
        [Key(4)]
        public List<string> filenames;
    }

    public class XX
    {
    }

    class Program
    {
        static void Main(string[] args)
        {
            DCGalleryAnalyzer.Instance.Open("툴리우스갤 데이터.txt");
            //DCGalleryAnalyzer.Instance.Open(@"F:\GalleryExplorer2\GalleryExplorer\bin\Debug\툴리우스갤 데이터.txt");

            //var dir = @"C:\Users\rollrat\source\repos\tulius-archive\b\Archive\툴리우스 마이너 갤러리 (tullius)"; 
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Archive", "툴리우스 마이너 갤러리 (tullius)");
            var files = Directory.GetFiles(dir);

            //var ll = new List<DCInsideArticle>();
            //var cc = new List<DCInsideCommentElement>();

            var x = new Dictionary<string, NewDCArticle>();

            int i = 0;
            foreach (var file in files)
            {
                var no = file.Split("[")[1].Split("]")[0];
                if (!x.ContainsKey(no))
                    x.Add(no, new NewDCArticle { info = DCGalleryAnalyzer.Instance.Model.articles[DCGalleryAnalyzer.Instance.Index[Convert.ToInt32(no)]] });
                if (file.Contains("]-body-"))
                {
                    if (file.Contains("]-comments-"))
                        throw new Exception("E1");

                    var y = JsonConvert.DeserializeObject<DCInsideArticle>(File.ReadAllText(file));
                    x[no].raw = y.Contents;
                    x[no].datalinks = y.ImagesLink;
                    x[no].filenames = y.FilesName;
                }
                else if (file.Contains("]-comments-"))
                {
                    var y = JsonConvert.DeserializeObject<List<DCInsideCommentElement>>(File.ReadAllText(file));
                    x[no].comments = y;
                }
                else
                {
                    throw new Exception("E2");
                }

                Console.WriteLine($"{++i}/{files.Length}");
            }

            var ll = x.Select(x => x.Value).ToList();

            //using (StreamWriter file = File.CreateText("툴갤 아카이브.json"))
            //{
            //    JsonSerializer serializer = new JsonSerializer();
            //    serializer.Serialize(file, ll);
            //}
            var bbb = MessagePackSerializer.Serialize(ll);
            using (FileStream fsStream = new FileStream("툴갤 아카이브-index.json", FileMode.Create))
            using (BinaryWriter sw = new BinaryWriter(fsStream))
            {
                sw.Write(bbb);
            }
        }
    }
}
