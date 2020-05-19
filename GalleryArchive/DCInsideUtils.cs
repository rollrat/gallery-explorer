// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

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

    [MessagePackObject]
    public class DCInsideArchiveModel
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
}
