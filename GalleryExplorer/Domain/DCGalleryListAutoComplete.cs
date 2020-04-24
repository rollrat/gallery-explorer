// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryExplorer.Domain
{
    public class DCGalleryList : ILazy<DCGalleryList>
    {
        public SortedDictionary<string, string> GalleryList;
        public SortedDictionary<string, string> MinorGalleryList;
        public HashSet<string> GalleryIds;
        public HashSet<string> MinorGalleryIds;
        public List<string> Lists;

        public DCGalleryList()
        {
            if (!File.Exists("ng_list.json"))
            {
                GalleryList = DCInsideUtils.GetGalleryList();
                File.WriteAllText("ng_list.json", JsonConvert.SerializeObject(GalleryList));
            }
            else
            {
                GalleryList = JsonConvert.DeserializeObject<SortedDictionary<string, string>>(File.ReadAllText("ng_list.json"));
            }

            if (!File.Exists("mg_list.json"))
            {
                MinorGalleryList = DCInsideUtils.GetMinorGalleryList();
                File.WriteAllText("mg_list.json", JsonConvert.SerializeObject(MinorGalleryList));
            }
            else
            {
                MinorGalleryList = JsonConvert.DeserializeObject<SortedDictionary<string, string>>(File.ReadAllText("mg_list.json"));
            }

            GalleryIds = new HashSet<string>();
            MinorGalleryIds = new HashSet<string>();
            GalleryList.ToList().ForEach(x => GalleryIds.Add(x.Value));
            MinorGalleryList.ToList().ForEach(x => MinorGalleryIds.Add(x.Value));

            Lists = new List<string>();
            GalleryList.ToList().ForEach(x => Lists.Add($"{x.Key} 갤러리 ({x.Value})"));
            MinorGalleryList.ToList().ForEach(x => Lists.Add($"{x.Key} 마이너 갤러리 ({x.Value})"));
            Lists.Sort();
        }
    }

    public class DCGalleryListAutoComplete : IAutoCompleteAlgorithm
    {
        public List<AutoCompleteTagData> GetResults(ref string word, ref int position, bool using_fuzzy)
        {
            var match = new List<AutoCompleteTagData>();

            foreach (var gall in DCGalleryList.Instance.Lists)
                if (gall.StartsWith(word))
                    match.Add(new AutoCompleteTagData { Tag = gall });

            foreach (var gall in DCGalleryList.Instance.Lists)
                if (gall.Contains(word) && !gall.StartsWith(word))
                    match.Add(new AutoCompleteTagData { Tag = gall });

            return match;
        }
    }

}
