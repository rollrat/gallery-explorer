// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryExplorer.Domain
{
    public class DCGalleryAnalyzer : ILazy<DCGalleryAnalyzer>
    {
        string filename = "list.txt";

        public void Open(string filename = "list.txt")
        {
            this.filename = filename;
            Model = MessagePackSerializer.Deserialize<DCInsideGalleryModel>(File.ReadAllBytes(filename));
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
    }

    public class DCGalleryAutoComplete : IAutoCompleteAlgorithm
    {
        public List<AutoCompleteTagData> GetResults(ref string word, ref int position, bool using_fuzzy)
        {
            var match = new List<AutoCompleteTagData>();

            if (DCGalleryAnalyzer.Instance.Model == null)
                return match;

            if (word.Contains(":"))
            {
                var dic = new Dictionary<string, int>();
                if (word.StartsWith("nick:"))
                {
                    word = word.Substring("nick:".Length);
                    position += "nick:".Length;
                    foreach (var article in DCGalleryAnalyzer.Instance.Articles)
                        if (article.nick != null && article.nick.Contains(word))
                        {
                            if (!dic.ContainsKey(article.nick))
                                dic.Add(article.nick, 0);
                            dic[article.nick]++;
                        }
                }
                else if (word.StartsWith("ip:"))
                {
                    word = word.Substring("ip:".Length);
                    position += "ip:".Length;
                    foreach (var article in DCGalleryAnalyzer.Instance.Articles)
                        if (article.ip != null && article.ip.Contains(word))
                        {
                            if (!dic.ContainsKey(article.ip))
                                dic.Add(article.ip, 0);
                            dic[article.ip]++;
                        }
                }
                else if (word.StartsWith("id:"))
                {
                    word = word.Substring("id:".Length);
                    position += "id:".Length;
                    foreach (var article in DCGalleryAnalyzer.Instance.Articles)
                        if (article.uid != null && article.uid.Contains(word))
                        {
                            if (!dic.ContainsKey(article.uid))
                                dic.Add(article.uid, 0);
                            dic[article.uid]++;
                        }
                }
                else if (word.StartsWith("class:"))
                {
                    word = word.Substring("class:".Length);
                    position += "class:".Length;
                    foreach (var article in DCGalleryAnalyzer.Instance.Articles)
                        if (article.classify != null && article.classify.Contains(word))
                        {
                            if (!dic.ContainsKey(article.classify))
                                dic.Add(article.classify, 0);
                            dic[article.classify]++;
                        }
                }
                match = dic.Select(x => new AutoCompleteTagData { Tag = x.Key, Count = x.Value }).ToList();
            }
            match.Sort((x, y) => y.Count.CompareTo(x.Count));

            string[] match_target = {
                    "nick:",
                    "ip:",
                    "id:",
                    "class:",
                };

            string w = word;
            var data_col = (from ix in match_target where ix.StartsWith(w) select new AutoCompleteTagData { Tag = ix }).ToList();
            if (data_col.Count > 0)
                match.AddRange(data_col);

            return match;
        }
    }

    public class DCGalleryDataQuery
    {
        public List<string> Title;
        public List<string> Nickname;
        public List<string> Id;
        public List<string> Ip;
        public List<string> Type;
    }
}
