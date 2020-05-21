// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using MessagePack;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GalleryExplorer.Core
{
    public class DCInsideArchive : ILazy<DCInsideArchive>
    {
        public List<DCInsideArchiveModel> Model { get; private set; }
        public DCInsideArchiveQuery Query { get; private set; }

        public void Load(string path)
        {
            Model = MessagePackSerializer.Deserialize<List<DCInsideArchiveModel>>(File.ReadAllBytes(path));
            Query = new DCInsideArchiveQuery(Model);
        }
    }

    public class DCInsideArchiveQuery
    {
        public List<DCInsideArchiveModel> Results { get; private set; }

        public DCInsideArchiveQuery(List<DCInsideArchiveModel> model)
        {
            Results = model;
        }

        public void Save()
        {
            File.WriteAllText($"archive-query-{DateTime.Now.Ticks}.txt", Logger.SerializeObject(Results));
        }

        public DCInsideArchiveQuery Query(DCInsideArchiveQueryNode[] query_info)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                if (DCInsideArchiveQueryHelper.match_article(query_info, x))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryComment(DCInsideArchiveQueryNode[] query_info)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments == null) return;
                foreach (var comm in x.comments)
                    if (DCInsideArchiveQueryHelper.match_comment(query_info, comm))
                        result.Add(comm);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        #region Query Contains

        public DCInsideArchiveQuery QueryContentSimpleContains(string text)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                if (x.raw.Contains(text) || (x.comments != null && x.comments.Any(y => y.memo.Contains(text))))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveQuery QueryContentSimpleContainsBody(string text)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                if (x.raw.Contains(text))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveQuery QueryContentSimpleContainsComment(string text)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments != null && x.comments.Any(y => y.memo.Contains(text)))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveQuery QueryContentHardContains(string text)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                var cc = x.raw.ToHtmlNode().InnerText;
                if (cc.Contains(text) || (x.comments != null && x.comments.Any(y => y.memo.ToHtmlNode().InnerText.Contains(text))))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveQuery QueryContentHardContainsBody(string text)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                var cc = x.raw.ToHtmlNode().InnerText;
                if (cc.Contains(text))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveQuery QueryContentHardContainsComment(string text)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                var cc = x.raw.ToHtmlNode().InnerText;
                if (x.comments != null && x.comments.Any(y => y.memo.ToHtmlNode().InnerText.Contains(text)))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveQuery QueryTitleContains(string text)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                if (x.info.title.Contains(text))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        #endregion

        #region Comment Author

        public DCInsideArchiveQuery QueryByCommentsNickName(string nick)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments != null && x.comments.Any(y => y.name == nick))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveQuery QueryByCommentsIp(string ip)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments != null && x.comments.Any(y => y.ip == ip))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        public DCInsideArchiveQuery QueryByCommentsUserId(string user_id)
        {
            var result = new List<DCInsideArchiveModel>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments != null && x.comments.Any(y => y.user_id == user_id))
                    result.Add(x);
            });
            return new DCInsideArchiveQuery(result);
        }

        #endregion

        #region Query Comments

        public DCInsideArchiveCommentQuery QueryCommentBySimpleContentsContains(string text)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments == null) return;
                foreach (var com in x.comments)
                    if (com.memo.Contains(text))
                        result.Add(com);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByHardContentsContains(string text)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments == null) return;
                foreach (var com in x.comments)
                    if (com.memo.ToHtmlNode().InnerText.Contains(text))
                        result.Add(com);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByNickName(string nick)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments == null) return;
                foreach (var com in x.comments)
                    if (com.name == nick)
                        result.Add(com);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByNickNameContains(string nick)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments == null) return;
                foreach (var com in x.comments)
                    if (com.name.Contains(nick))
                        result.Add(com);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByIp(string ip)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments == null) return;
                foreach (var com in x.comments)
                    if (com.ip == ip)
                        result.Add(com);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByUserId(string user_id)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.comments == null) return;
                foreach (var com in x.comments)
                    if (com.user_id == user_id)
                        result.Add(com);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        #endregion
    }

    public class DCInsideArchiveCommentQuery
    {
        public List<DCInsideCommentElement> Results { get; private set; }

        public DCInsideArchiveCommentQuery(List<DCInsideCommentElement> model)
        {
            Results = model;
        }

        public void Save()
        {
            File.WriteAllText($"archive-comment-query-{DateTime.Now.Ticks}.txt", Logger.SerializeObject(Results));
        }

        public DCInsideArchiveCommentQuery Query(DCInsideArchiveQueryNode[] query_info)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (DCInsideArchiveQueryHelper.match_comment(query_info, x))
                    result.Add(x);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        #region Query Comments

        public DCInsideArchiveCommentQuery QueryCommentBySimpleContentsContains(string text)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.memo.Contains(text))
                    result.Add(x);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByHardContentsContains(string text)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.memo.ToHtmlNode().InnerText.Contains(text))
                    result.Add(x);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByNickName(string nick)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.name == nick)
                    result.Add(x);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByNickNameContains(string nick)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.name.Contains(nick))
                    result.Add(x);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByIp(string ip)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.ip == ip)
                    result.Add(x);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        public DCInsideArchiveCommentQuery QueryCommentByUserId(string user_id)
        {
            var result = new List<DCInsideCommentElement>();
            Parallel.ForEach(Results, x =>
            {
                if (x.user_id == user_id)
                    result.Add(x);
            });
            return new DCInsideArchiveCommentQuery(result);
        }

        #endregion
    }

    /// <summary>
    /// DCInside Archive Query System
    /// 
    /// Article: Parsing -> Input All of Article -> Searching -> Sorting
    /// Comment: Parsing -> Input Demand Article -> Filtering -> Sorting
    /// </summary>

    public enum DCInsideArchiveQueryCombinationOption
    {
        /// <summary>
        /// And 연산으로 하위 옵션을 결합합니다.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Or 연산으로 하위 옵션을 결합합니다.
        /// </summary>
        Combination = 1,

        /// <summary>
        /// 차집합 연산입니다.
        /// Right operand에 포함된 모든 정보는 결합에 제외됩니다.
        /// </summary>
        Difference = 2,
    }

    /// <summary>
    /// 쿼리의 각 토큰에 부여되는 옵션목록입니다.
    /// </summary>
    public enum DCInsideArchiveQueryTokenOption
    {
        /// <summary>
        /// 기본 설정입니다. 
        /// 해당 토큰은 하위 역할 중 어떤 역할도 갖지 않습니다.
        /// </summary>
        Default = 0,

        /// <summary>
        /// 여집합 설정입니다.
        /// </summary>
        Complement = 1,
    }

    public enum DCInsideArchiveQueryTokenType
    {
        None,
        Common, // 어떤 
        ContentContainsSimple, // 단순 검색
        ContentContainsHard, // 내부 텍스트를 HTML로 변환 후 텍스트만 추출하여 검색
        ContentAuthorNick,
        ContentAuthorIp,
        ContentAuthorId,
        ContentAuthorType, // 0: 유동, 1: 반고닉, 2: 고닉
        ContentHasComment,
        ContentHasImage,
        ContentViews,
        ContentUpVote,
        ContentClass, // 말머리
        BodyContainsSimple,
        BodyContainsHard,
        TitleContains,
        CommentContainsSimple,
        CommentContainsHard,
        CommentAuthorNick,
        CommentAuthorIp,
        CommentAuthorId,
        CommentAuthorType, // 0: 유동, 1: 반고닉, 2: 고닉
        CommentIsDCCon,
        CommentIsVoice,
    }

    /// <summary>
    /// 쿼리노드입니다.
    /// </summary>
    public class DCInsideArchiveQueryNode
    {
        public DCInsideArchiveQueryCombinationOption combination;
        public DCInsideArchiveQueryTokenOption option;

        public DCInsideArchiveQueryNode left_query;
        public DCInsideArchiveQueryNode right_query;

        public bool is_operator;

        public DCInsideArchiveQueryTokenType token_type;
        public string token;
    }

    /// <summary>
    /// 글 또는 댓글을 검색하기 위한 쿼리를 만듭니다.
    /// </summary>
    public class DCInsideArchiveQueryHelper
    {
        static Dictionary<char, int> priority_dic = new Dictionary<char, int>
        {
            {'(', -1},
            {'-',  0},
            {'+',  0},
            {'&',  0},
            {'|',  1},
            {'~',  2},
        };

        static Dictionary<string, DCInsideArchiveQueryTokenType> token_dic = new Dictionary<string, DCInsideArchiveQueryTokenType>()
        {
            {"contentcontainssimple",     DCInsideArchiveQueryTokenType.ContentContainsSimple},
            {"contentcontainshard",       DCInsideArchiveQueryTokenType.ContentContainsHard},
#if DEBUG
            {"contentauthornick",         DCInsideArchiveQueryTokenType.ContentAuthorNick},
            {"contentauthorip",           DCInsideArchiveQueryTokenType.ContentAuthorIp},
            {"contentauthorid",           DCInsideArchiveQueryTokenType.ContentAuthorId},
            {"contentauthortype",         DCInsideArchiveQueryTokenType.ContentAuthorType},
#endif
            {"contenthascomment",         DCInsideArchiveQueryTokenType.ContentHasComment},
            {"contenthasimage",           DCInsideArchiveQueryTokenType.ContentHasImage},
            {"contentviews",              DCInsideArchiveQueryTokenType.ContentViews},
            {"contentupvote",             DCInsideArchiveQueryTokenType.ContentUpVote},
            {"contentclass",              DCInsideArchiveQueryTokenType.ContentClass},
            {"bodycontainssimple",        DCInsideArchiveQueryTokenType.BodyContainsSimple},
            {"bodycontainshard",          DCInsideArchiveQueryTokenType.BodyContainsHard},
            {"titlecontains",             DCInsideArchiveQueryTokenType.TitleContains},
            {"commentcontainssimple",     DCInsideArchiveQueryTokenType.CommentContainsSimple},
            {"commentcontainshard",       DCInsideArchiveQueryTokenType.CommentContainsHard},
#if DEBUG
            {"commentauthornick",         DCInsideArchiveQueryTokenType.CommentAuthorNick},
            {"commentauthorip",           DCInsideArchiveQueryTokenType.CommentAuthorIp},
            {"commentauthorid",           DCInsideArchiveQueryTokenType.CommentAuthorId},
            {"commentauthortype",         DCInsideArchiveQueryTokenType.CommentAuthorType},
#endif
            {"commentisdccon",            DCInsideArchiveQueryTokenType.CommentIsDCCon},
            {"commentisvoice",            DCInsideArchiveQueryTokenType.CommentIsVoice},
        };

        public static int get_priority(char op)
        {
            return priority_dic[op];
        }

        public static Stack<string> to_postfix(string query_string)
        {
            var stack = new Stack<char>();
            var result_stack = new Stack<string>();
            bool latest = false;
            bool complement = false;
            for (int i = 0; i < query_string.Length; i++)
            {
                var builder = new StringBuilder();
                while (i < query_string.Length && char.IsWhiteSpace(query_string[i])) i++;

                if ("()-+&|~".Contains(query_string[i]))
                    builder.Append(query_string[i]);
                else
                {
                    var scope = false;
                    for (; i < query_string.Length && (scope || !char.IsWhiteSpace(query_string[i])); i++)
                    {
                        if (query_string[i] == '[')
                            scope = true;
                        else if (query_string[i] == ']')
                            scope = false;
                        else if ("()-+&|~".Contains(query_string[i]))
                            break;
                        builder.Append(query_string[i]);
                    }
                }
                var token = builder.ToString().ToLower();

                if (token == "and") token = "+";
                else if (token == "or") token = "|";

                switch (token[0])
                {
                    case '(':
                        if (latest)
                        {
                            stack.Push('+');
                            latest = false;
                        }
                        if (complement)
                        {
                            stack.Push('~');
                            complement = false;
                        }
                        stack.Push('(');
                        break;

                    case ')':
                        while (stack.Peek() != '(' && stack.Count > 0) result_stack.Push(stack.Pop().ToString());
                        if (stack.Count == 0)
                        {
                            Logger.Instance.PushError($"[Advanced Search] Missmatch ')' token on '{query_string}'.");
                            throw new Exception("Missmatch closer!");
                        }
                        stack.Pop();
                        if (stack.Count > 0 && stack.Peek() == '~')
                            result_stack.Push(stack.Pop().ToString());
                        break;

                    case '-':
                    case '+':
                    case '&':
                    case '|':
                        var p = get_priority(token[0]);
                        while (stack.Count > 0)
                        {
                            if (get_priority(stack.Peek()) >= p)
                                result_stack.Push(stack.Pop().ToString());
                            else break;
                        }
                        stack.Push(token[0]);
                        latest = false;
                        break;

                    case '~':
                        complement = true;
                        break;

                    default:
                        if (latest == true)
                        {
                            var p1 = get_priority('+');
                            while (stack.Count > 0)
                            {
                                if (get_priority(stack.Peek()) >= p1)
                                    result_stack.Push(stack.Pop().ToString());
                                else break;
                            }
                            stack.Push('+');
                        }
                        result_stack.Push(token);
                        if (complement)
                        {
                            result_stack.Push("~");
                            complement = false;
                        }
                        latest = true;
                        break;
                }
            }

            while (stack.Count > 0) result_stack.Push(stack.Pop().ToString());

            return new Stack<string>(result_stack);
        }

        public static DCInsideArchiveQueryNode make_tree(string query_string)
        {
            var stack = new Stack<DCInsideArchiveQueryNode>();
            var postfix = to_postfix(query_string);

            while (postfix.Count > 0)
            {
                string token = postfix.Pop();

                switch (token[0])
                {
                    case '(': break;

                    case '-':
                        {
                            var s1 = stack.Pop();
                            var s2 = stack.Pop();
                            stack.Push(new DCInsideArchiveQueryNode
                            {
                                combination = DCInsideArchiveQueryCombinationOption.Difference,
                                left_query = s2,
                                right_query = s1,
                                is_operator = true
                            });
                        }
                        break;

                    case '|':
                        {
                            var s1 = stack.Pop();
                            var s2 = stack.Pop();
                            stack.Push(new DCInsideArchiveQueryNode
                            {
                                combination = DCInsideArchiveQueryCombinationOption.Combination,
                                left_query = s2,
                                right_query = s1,
                                is_operator = true
                            });
                        }
                        break;

                    case '&':
                    case '+':
                        {
                            var s1 = stack.Pop();
                            var s2 = stack.Pop();
                            stack.Push(new DCInsideArchiveQueryNode
                            {
                                combination = DCInsideArchiveQueryCombinationOption.Default,
                                left_query = s2,
                                right_query = s1,
                                is_operator = true
                            });
                        }
                        break;

                    case '~':
                        {
                            var s = stack.Pop();
                            s.option = DCInsideArchiveQueryTokenOption.Complement;
                            stack.Push(s);
                        }
                        break;

                    default:
                        var query_node = new DCInsideArchiveQueryNode
                        {
                            token = token,
                            token_type = DCInsideArchiveQueryTokenType.Common,
                            is_operator = false
                        };
                        if (token.Contains('['))
                        {
                            if (token_dic.ContainsKey(token.Split('[')[0]))
                                query_node.token_type = token_dic[token.Split('[')[0]];
                            else
                                query_node.token_type = DCInsideArchiveQueryTokenType.None;
                            query_node.token = token.Split('[')[1].TrimEnd(']');
                        }
                        stack.Push(query_node);
                        break;
                }
            }

            return stack.Pop();
        }

        public static DCInsideArchiveQueryNode[] to_linear(DCInsideArchiveQueryNode query)
        {
            var querys = new DCInsideArchiveQueryNode[65535];
            var stack = new Stack<DCInsideArchiveQueryNode>();
            var pos = new Stack<int>();
            var max = 1;

            stack.Push(query);
            pos.Push(1);

            while (stack.Count > 0)
            {
                var pop = stack.Pop();
                var ps = pos.Pop();

                max = Math.Max(max, ps);
                querys[ps] = pop;

                if (pop.left_query != null)
                {
                    stack.Push(pop.left_query);
                    pos.Push(ps * 2);
                }
                if (pop.right_query != null)
                {
                    stack.Push(pop.right_query);
                    pos.Push(ps * 2 + 1);
                }
            }

            return querys.Take(max + 1).ToArray();
        }

        public static bool match_article(DCInsideArchiveQueryNode[] queries, DCInsideArchiveModel am)
        {
            bool[] checker = new bool[queries.Length];

            for (int i = 1; i < queries.Length; i++)
            {
                var query = queries[i];

                if (query != null && query.is_operator == false)
                {
                    var separate = query.token.Split(',').Select(x => x.Trim()).ToList();

                    switch (query.token_type)
                    {
                        case DCInsideArchiveQueryTokenType.None:
                            Logger.Instance.PushError("Query type not found!");
                            Logger.Instance.PushError(query);
                            throw new Exception($"Query system error!");

                        case DCInsideArchiveQueryTokenType.Common:
                            if (separate.All(x => am.info.title.Contains(x)))
                                checker[i] = true;
                            else if (separate.All(x => am.raw.Contains(x)))
                                checker[i] = true;
                            break;

                        ///
                        /// 단순 단어 매칭
                        /// 어떤 단어가 문장에 포함되어 있다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.BodyContainsSimple:
                            if (separate.All(x => am.raw.Contains(x)))
                                checker[i] = true;
                            break;

                        ///
                        /// 단순 단어 매칭(HTML 변환)
                        /// BodyContainsSimple과 같지만 HTML Decoding을 적용하여 검색합니다.
                        ///
                        case DCInsideArchiveQueryTokenType.BodyContainsHard:
                            {
                                var format = HttpUtility.HtmlDecode(am.raw.ToHtmlNode().InnerText);
                                if (separate.All(x => format.Contains(x)))
                                    checker[i] = true;
                            }
                            break;

                        ///
                        /// 작성자 정보 매칭
                        /// 닉네임 아이피 아이디를 비교해 매칭된다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentAuthorNick:
                            if (separate.Any(x => am.info.nick == x))
                                checker[i] = true;
                            break;
                        case DCInsideArchiveQueryTokenType.ContentAuthorIp:
                            if (separate.Any(x => am.info.ip == x))
                                checker[i] = true;
                            break;
                        case DCInsideArchiveQueryTokenType.ContentAuthorId:
                            if (am.info.uid != null && separate.Any(x => am.info.uid == x))
                                checker[i] = true;
                            break;

                        ///
                        /// 작성자 타입 매칭
                        /// 작성자가 유동닉 고정닉 반고정닉인지를 확인합니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentAuthorType:
                            if (query.token.Trim() == "0" && string.IsNullOrEmpty(am.info.uid))
                                checker[i] = true;
                            else if (query.token.Trim() == "2" && am.info.isfixed)
                                checker[i] = true;
                            else if (query.token.Trim() == "1" && !am.info.isfixed && !string.IsNullOrEmpty(am.info.uid))
                                checker[i] = true;
                            break;

                        ///
                        /// 댓글 여부
                        /// 댓글이 주어진 개수 이상 있다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentHasComment:
                            var count_comment = Convert.ToInt32(query.token);
                            if (am.comments != null && am.comments.Count >= count_comment)
                                checker[i] = true;
                            break;

                        ///
                        /// 게시글 이미지 포함 여부
                        /// 게시글에 업로드된 이미지가 주어진 개수 이상 있다면 pass입니다.
                        /// 외부에서 복사된 이미지는 포함되지 않습니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentHasImage:
                            var count_image = Convert.ToInt32(query.token);
                            if (am.datalinks != null && am.datalinks.Count >= count_image)
                                checker[i] = true;
                            break;

                        ///
                        /// 조회수 여부
                        /// 조회수가 주어진 숫자 이상 있다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentViews:
                            var count_views = Convert.ToInt32(query.token);
                            if (Convert.ToInt32(am.info.count) >= count_views)
                                checker[i] = true;
                            break;

                        ///
                        /// 추천수 여부
                        /// 추천수가 주어진 숫자 이상 있다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentUpVote:
                            var count_upvote = Convert.ToInt32(query.token);
                            if (Convert.ToInt32(am.info.recommend) >= count_upvote)
                                checker[i] = true;
                            break;

                        ///
                        /// 말머리 매칭
                        /// 말머리가 일치한다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentClass:
                            if (am.info.classify != null && separate.Any(x => am.info.classify == x))
                                checker[i] = true;
                            break;

                        ///
                        /// 제목 매칭
                        /// 제목에 내용이 포함되어 있다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.TitleContains:
                            if (separate.All(x => am.info.title.Contains(x)))
                                checker[i] = true;
                            break;

                        ///
                        /// 본문+댓글 단순 단어 매칭
                        /// 어떤 단어가 문장에 포함되어 있다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentContainsSimple:
                            if (am.raw.Contains(query.token))
                                checker[i] = true;
                            else if (am.comments != null && am.comments.Any(x => x.memo.Contains(query.token)))
                                checker[i] = true;
                            break;

                        ///
                        /// 본문+댓글 단순 단어 매칭(HTML 변환)
                        /// BodyContainsSimple과 같지만 HTML Decoding을 적용하여 검색합니다.
                        ///
                        case DCInsideArchiveQueryTokenType.ContentContainsHard:
                            if (HttpUtility.HtmlDecode(am.raw.ToHtmlNode().InnerText).Contains(query.token))
                                checker[i] = true;
                            else if (am.comments != null && am.comments.Any(x => HttpUtility.HtmlDecode(x.memo.ToHtmlNode().InnerText).Contains(query.token)))
                                checker[i] = true;
                            break;

                        ///
                        /// 댓글 단순 단어 매칭
                        /// 어떤 단어가 문장에 포함되어 있다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.CommentContainsSimple:
                            if (am.comments != null && am.comments.Any(x => separate.All(y => x.memo.Contains(y))))
                                checker[i] = true;
                            break;

                        ///
                        /// 댓글 단순 단어 매칭(HTML 변환)
                        /// BodyContainsSimple과 같지만 HTML Decoding을 적용하여 검색합니다.
                        ///
                        case DCInsideArchiveQueryTokenType.CommentContainsHard:
                            if (am.comments != null && am.comments.Any(x => separate.All(y => HttpUtility.HtmlDecode(x.memo.ToHtmlNode().InnerText).Contains(y))))
                                checker[i] = true;
                            break;

                        ///
                        /// 댓글 작성자 정보 매칭
                        /// 닉네임 아이피 아이디를 비교해 하나라도 매칭된다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.CommentAuthorNick:
                            if (am.comments != null && am.comments.Any(x => separate.Any(y => x.name == y)))
                                checker[i] = true;
                            break;
                        case DCInsideArchiveQueryTokenType.CommentAuthorIp:
                            if (am.comments != null && am.comments.Any(x => separate.Any(y => x.ip == y)))
                                checker[i] = true;
                            break;
                        case DCInsideArchiveQueryTokenType.CommentAuthorId:
                            if (am.comments != null && am.comments.Any(x => separate.Any(y => x.user_id == y)))
                                checker[i] = true;
                            break;

                        case DCInsideArchiveQueryTokenType.CommentAuthorType:
                        case DCInsideArchiveQueryTokenType.CommentIsDCCon:
                        case DCInsideArchiveQueryTokenType.CommentIsVoice:
                            throw new Exception("게시글 검색에선 지원하지 않습니다.");

                    }
                    if (query.option == DCInsideArchiveQueryTokenOption.Complement)
                        checker[i] = !checker[i];
                }
            }

            for (int i = queries.Length - 1; i > 0; i--)
            {
                var query = queries[i];
                if (query != null && query.is_operator == true)
                {
                    int s1 = i * 2;
                    int s2 = i * 2 + 1;

                    var qop = queries[i];

                    if (qop.combination == DCInsideArchiveQueryCombinationOption.Default)
                        checker[i] = checker[s1] && checker[s2];
                    else if (qop.combination == DCInsideArchiveQueryCombinationOption.Combination)
                        checker[i] = checker[s1] || checker[s2];
                    else if (qop.combination == DCInsideArchiveQueryCombinationOption.Difference)
                        checker[i] = checker[s1] && !checker[s2];

                    if (qop.option == DCInsideArchiveQueryTokenOption.Complement)
                        checker[i] = !checker[i];
                }
            }

            return checker[1];
        }

        public static bool match_comment(DCInsideArchiveQueryNode[] queries, DCInsideCommentElement ce)
        {
            bool[] checker = new bool[queries.Length];

            for (int i = 1; i < queries.Length; i++)
            {
                var query = queries[i];

                if (query != null && query.is_operator == false)
                {
                    var separate = query.token.Split(',').Select(x => x.Trim()).ToList();

                    switch (query.token_type)
                    {
                        case DCInsideArchiveQueryTokenType.None:
                            Logger.Instance.PushError("Query type not found!");
                            Logger.Instance.PushError(query);
                            throw new Exception($"Query system error!");

                        ///
                        /// 댓글 단순 단어 매칭
                        /// 어떤 단어가 문장에 포함되어 있다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.Common:
                        case DCInsideArchiveQueryTokenType.CommentContainsSimple:
                            if (separate.All(x => ce.memo.Contains(x)))
                                checker[i] = true;
                            break;

                        ///
                        /// 댓글 단순 단어 매칭(HTML 변환)
                        /// BodyContainsSimple과 같지만 HTML Decoding을 적용하여 검색합니다.
                        ///
                        case DCInsideArchiveQueryTokenType.CommentContainsHard:
                            if (separate.All(x => HttpUtility.HtmlDecode(ce.memo).Contains(x)))
                                checker[i] = true;
                            break;

                        ///
                        /// 댓글 작성자 정보 매칭
                        /// 닉네임 아이피 아이디를 비교해 매칭된다면 pass입니다.
                        ///
                        case DCInsideArchiveQueryTokenType.CommentAuthorNick:
                            if (separate.Any(x => ce.name == x))
                                checker[i] = true;
                            break;
                        case DCInsideArchiveQueryTokenType.CommentAuthorIp:
                            if (separate.Any(x => ce.ip == x))
                                checker[i] = true;
                            break;
                        case DCInsideArchiveQueryTokenType.CommentAuthorId:
                            if (separate.Any(x => ce.user_id == x))
                                checker[i] = true;
                            break;

                        case DCInsideArchiveQueryTokenType.CommentAuthorType:
                        case DCInsideArchiveQueryTokenType.CommentIsDCCon:
                        case DCInsideArchiveQueryTokenType.CommentIsVoice:
                            break;

                        case DCInsideArchiveQueryTokenType.BodyContainsSimple:
                        case DCInsideArchiveQueryTokenType.BodyContainsHard:
                        case DCInsideArchiveQueryTokenType.ContentAuthorNick:
                        case DCInsideArchiveQueryTokenType.ContentAuthorIp:
                        case DCInsideArchiveQueryTokenType.ContentAuthorId:
                        case DCInsideArchiveQueryTokenType.ContentAuthorType:
                        case DCInsideArchiveQueryTokenType.ContentHasComment:
                        case DCInsideArchiveQueryTokenType.ContentHasImage:
                        case DCInsideArchiveQueryTokenType.ContentViews:
                        case DCInsideArchiveQueryTokenType.ContentUpVote:
                        case DCInsideArchiveQueryTokenType.ContentClass:
                        case DCInsideArchiveQueryTokenType.TitleContains:
                        case DCInsideArchiveQueryTokenType.ContentContainsSimple:
                        case DCInsideArchiveQueryTokenType.ContentContainsHard:
                            throw new Exception("댓글 검색에선 지원하지 않습니다.");

                    }
                    if (query.option == DCInsideArchiveQueryTokenOption.Complement)
                        checker[i] = !checker[i];
                }
            }

            for (int i = queries.Length - 1; i > 0; i--)
            {
                var query = queries[i];
                if (query != null && query.is_operator == true)
                {
                    int s1 = i * 2;
                    int s2 = i * 2 + 1;

                    var qop = queries[i];

                    if (qop.combination == DCInsideArchiveQueryCombinationOption.Default)
                        checker[i] = checker[s1] && checker[s2];
                    else if (qop.combination == DCInsideArchiveQueryCombinationOption.Combination)
                        checker[i] = checker[s1] || checker[s2];
                    else if (qop.combination == DCInsideArchiveQueryCombinationOption.Difference)
                        checker[i] = checker[s1] && !checker[s2];

                    if (qop.option == DCInsideArchiveQueryTokenOption.Complement)
                        checker[i] = !checker[i];
                }
            }

            return checker[1];
        }
    }

    public class ArticleColumnModel
    {
        [PrimaryKey]
        public int no { get; set; }
        public string classify { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string replay_num { get; set; }
        public string nick { get; set; }
        public string uid { get; set; }
        public string ip { get; set; }
        public bool islogined { get; set; }
        public bool isfixed { get; set; }
        public DateTime date { get; set; }
        public string count { get; set; }
        public string recommend { get; set; }
        public string raw { get; set; }
        public string datalinks { get; set; }
        public string filenames { get; set; }
    }

    public class CommentColumnModel
    {
        [PrimaryKey]
        public int no { get; set; }
        public string parent { get; set; }
        public string user_id { get; set; }
        public string name { get; set; }
        public string ip { get; set; }
        public string reg_date { get; set; }
        public string nicktype { get; set; }
        public string t_ch1 { get; set; }
        public string t_ch2 { get; set; }
        public string vr_type { get; set; }
        public string voice { get; set; }
        public string rcnt { get; set; }
        public string c_no { get; set; }
        public int depth { get; set; }
        public string del_yn { get; set; }
        public string is_delete { get; set; }
        public string memo { get; set; }
        public string my_cmt { get; set; }
        public string del_btn { get; set; }
        public string mod_btn { get; set; }
        public string a_my_cmt { get; set; }
        public string reply_w { get; set; }
        public string gallog_icon { get; set; }
        public string vr_player { get; set; }
        public string vr_player_tag { get; set; }
        public int next_type { get; set; }
    }

    public class SQLiteWrapper<T> where T : new()
    {
        object dblock = new object();
        string dbpath;

        public SQLiteWrapper(string dbpath)
        {
            this.dbpath = dbpath;
            
            var db = new SQLiteConnection(dbpath);
            var info = db.GetTableInfo(typeof(T).Name);
            if (!info.Any())
                db.CreateTable<T>();
            db.Close();
        }

        public void Add(T dbm)
        {
            lock (dblock)
            {
                var db = new SQLiteConnection(dbpath);
                db.Insert(dbm);
                db.Close();
            }
        }

        public void AddAll(List<T> dbm)
        {
            lock (dblock)
            {
                var db = new SQLiteConnection(dbpath);
                db.InsertAll(dbm);
                db.Close();
            }
        }

        public void Update(T dbm)
        {
            lock (dblock)
            {
                var db = new SQLiteConnection(dbpath);
                db.Update(dbm);
                db.Close();
            }
        }

        public List<T> QueryAll()
        {
            lock (dblock)
            {
                using (var db = new SQLiteConnection(dbpath))
                    return db.Table<T>().ToList();
            }
        }

        public List<T> Query(string where)
        {
            lock (dblock)
            {
                var db = new SQLiteConnection(dbpath);
                return db.Query<T>($"select * from {typeof(T).Name} where {where}");
            }
        }

        public List<T> Query(Func<T, bool> func)
        {
            lock (dblock)
            {
                var db = new SQLiteConnection(dbpath);
                return db.Table<T>().Where(func).ToList();
            }
        }
    }

    public class DCInsideArchiveIndexDatabase : ILazy<DCInsideArchiveIndexDatabase>
    {
        public void Build()
        {
            var db = new SQLiteWrapper<ArticleColumnModel>("archive.db");

            db.AddAll(DCInsideArchive.Instance.Model.Select(x =>
            {
                return new ArticleColumnModel
                {
                    no = x.info.no.ToInt(),
                    classify = x.info.classify,
                    type = x.info.type,
                    title = x.info.title,
                    replay_num = x.info.replay_num,
                    nick = x.info.nick,
                    uid = x.info.uid,
                    ip = x.info.ip,
                    islogined = x.info.islogined,
                    isfixed = x.info.isfixed,
                    date = x.info.date,
                    count = x.info.count,
                    recommend = x.info.recommend,
                    raw = x.raw,
                    datalinks = x.datalinks != null ? string.Join("|", x.datalinks) : "",
                    filenames = x.filenames != null ? string.Join("|", x.filenames) : "",
                };
            }).ToList());

            //var result = new List<DCInsideCommentElement>();
            //DCInsideArchive.Instance.Model.ForEach(x =>
            //{
            //    if (x.comments == null) return;
            //    foreach (var comm in x.comments)
            //        result.Add(comm);
            //});
            //
            //var db1 = new SQLiteWrapper<CommentColumnModel>("archive.db");
            //
            //db1.AddAll(result.Select(x =>
            //{
            //    return new CommentColumnModel
            //    {
            //        no = x.no.ToInt(),
            //        parent = x.parent,
            //        user_id = x.user_id,
            //        name = x.name,
            //        ip = x.ip,
            //        reg_date = x.reg_date,
            //        nicktype = x.nicktype,
            //        t_ch1 = x.t_ch1,
            //        t_ch2 = x.t_ch2,
            //        vr_type = x.vr_type,
            //        voice = x.voice,
            //        rcnt = x.rcnt,
            //        c_no = x.c_no,
            //        depth = x.depth,
            //        del_yn = x.del_yn,
            //        is_delete = x.is_delete,
            //        memo = x.memo,
            //        my_cmt = x.my_cmt,
            //        del_btn = x.del_btn,
            //        mod_btn = x.mod_btn,
            //        a_my_cmt = x.a_my_cmt,
            //        reply_w = x.reply_w,
            //        gallog_icon = x.gallog_icon,
            //        vr_player = x.vr_player,
            //        vr_player_tag = x.vr_player_tag,
            //        next_type = x.next_type,
            //    };
            //}).ToList());
        }
    }

    /// <summary>
    /// 검색을 위한 인덱스 데이터베이스를 생성합니다.
    /// </summary>
    public class DCInsideArchiveIndex : ILazy<DCInsideArchiveIndex>
    {
        /// <summary>
        /// Aho Corasick
        /// 
        /// 모든 단어를 영어로 변환합니다.
        /// Word Tree를 생성합니다.
        /// </summary>
        public TrieArticle article_index = new TrieArticle();
        public TrieComment comment_index = new TrieComment();

#if true
        [MessagePackObject]
        public class TrieArticle
        {
            [Key(0)]
            public Dictionary<int, TrieArticle> Warp;
            [Key(1)]
            public HashSet<int> Index = new HashSet<int>();

            public TrieArticle of(char ch)
            {
                if (Warp == null)
                    Warp = new Dictionary<int, TrieArticle>();
                if (!Warp.ContainsKey(ch))
                    Warp.Add(ch, new TrieArticle());
                return Warp[ch];
            }
        }

        [MessagePackObject]
        public class TrieComment
        {
            [Key(0)]
            public Dictionary<int, TrieComment> Warp;
            [Key(1)]
            public List<(int, int)> Index = new List<(int, int)>();

            public TrieComment of(char ch)
            {
                if (Warp == null)
                    Warp = new Dictionary<int, TrieComment>();
                if (!Warp.ContainsKey(ch))
                    Warp.Add(ch, new TrieComment());
                return Warp[ch];
            }
        }

        static readonly string[] cc = new[] { "r", "R", "rt", "s", "sw", "sg", "e", "E", "f", "fr", "fa", "fq", "ft", "fe",
            "fv", "fg", "a", "q", "Q", "qt", "t", "T", "d", "w", "W", "c", "z", "e", "v", "g", "k", "o", "i", "O",
            "j", "p", "u", "P", "h", "hk", "ho", "hl", "y", "n", "nj", "np", "nl", "b", "m", "ml", "l", " ", "ss",
            "se", "st", " ", "frt", "fe", "fqt", " ", "fg", "aq", "at", " ", " ", "qr", "qe", "qtr", "qte", "qw",
            "qe", " ", " ", "tr", "ts", "te", "tq", "tw", " ", "dd", "d", "dt", " ", " ", "gg", " ", "yi", "yO", "yl", "bu", "bP", "bl" };
        static readonly char[] cc1 = new[] { 'r', 'R', 's', 'e', 'E', 'f', 'a', 'q', 'Q', 't', 'T', 'd', 'w', 'W', 'c', 'z', 'x', 'v', 'g' };
        static readonly string[] cc2 = new[] { "k", "o", "i", "O", "j", "p", "u", "P", "h", "hk", "ho", "hl", "y", "n", "nj", "np", "nl", "b", "m", "ml", "l" };
        static readonly string[] cc3 = new[] { "", "r", "R", "rt", "s", "sw", "sg", "e", "f", "fr", "fa", "fq", "ft", "fx", "fv", "fg", "a", "q", "qt", "t", "T", "d", "w", "c", "z", "x", "v", "g" };

        public static string hangul_disassembly(char letter)
        {
            if (0xAC00 <= letter && letter <= 0xD7FB)
            {
                int unis = letter - 0xAC00;
                return cc1[unis / (21 * 28)] + cc2[(unis % (21 * 28)) / 28] + cc3[(unis % (21 * 28)) % 28];
            }
            else if (0x3131 <= letter && letter <= 0x3163)
            {
                int unis = letter;
                return cc[unis - 0x3131];
            }
            else
            {
                return letter.ToString();
            }
        }

        public static string disasm(string word)
        {
            return string.Join("", word.Select(y => hangul_disassembly(y)));
        }

        public void ProcessComment(DCInsideCommentElement comm)
        {
            var word_lists = comm.memo.ToHtmlNode().InnerText.Split(' ').Select(x => disasm(x)).ToList();
            var no = comm.parent.ToInt();
            var nn = comm.no.ToInt();

            foreach (var word in word_lists)
            {
                if (word == "")
                    continue;
                TrieComment trie = comment_index.of(word[0]);
                trie.Index.Add((no, nn));
                for (int i = 1; i < word.Length; i++)
                {
                    trie = trie.of(word[i]);
                    trie.Index.Add((no, nn));
                }
            }
        }

        public void ProcessBody(DCInsideArchiveModel article)
        {
            var word_lists = (article.raw.ToHtmlNode().InnerText + article.info.title).Trim('\r', '\n', '\t').Split(' ').Select(x => disasm(x)).ToList();
            var no = article.info.no.ToInt();

            foreach (var word in word_lists)
            {
                if (word == "")
                    continue;
                TrieArticle trie = article_index.of(word[0]);
                trie.Index.Add(no);
                for (int i = 1; i < word.Length; i++)
                {
                    trie = trie.of(word[i]);
                    trie.Index.Add(no);
                }
            }
            if (article.comments == null || article.comments.Count == 0)
                return;

            for (int i = 0; i < article.comments.Count; i++)
                ProcessComment(article.comments[i]);
        }

        /// <summary>
        /// 새로운 트리를 생성합니다.
        /// </summary>
        public void Build()
        {
            for (int i = 0; i < DCInsideArchive.Instance.Model.Count; i++)
            {
                ProcessBody(DCInsideArchive.Instance.Model[i]);
                Console.Instance.WriteLine($"[{++i}/{DCInsideArchive.Instance.Model.Count}]");
            }

            var bbb = MessagePackSerializer.Serialize(article_index);
            using (FileStream fsStream = new FileStream($"article-index-ii.json", FileMode.Create))
            using (BinaryWriter sw = new BinaryWriter(fsStream))
            {
                sw.Write(bbb);
            }

            var bbb1 = MessagePackSerializer.Serialize(comment_index);
            using (FileStream fsStream = new FileStream($"comment-index-ii.json", FileMode.Create))
            using (BinaryWriter sw = new BinaryWriter(fsStream))
            {
                sw.Write(bbb1);
            }
        }

        public void Load(string prefix)
        {
            article_index = MessagePackSerializer.Deserialize<TrieArticle>(File.ReadAllBytes(prefix + "-ai.json"));
            //comment_index = MessagePackSerializer.Deserialize<TrieComment>(File.ReadAllBytes(prefix + "-ci.json"));
        }

        public HashSet<int> of_article(string word)
        {
            var dd = disasm(word);
            TrieArticle trie = article_index.of(dd[0]);
            for (int i = 1; i < dd.Length; i++)
                if (trie.Warp.ContainsKey(dd[i]))
                    trie = trie.Warp[dd[i]];
                else
                    return new HashSet<int>();
            return trie.Index;
        }

        public List<(int, int)> of_comment(string word)
        {
            var dd = disasm(word);
            TrieComment trie = comment_index.of(dd[0]);
            for (int i = 1; i < dd.Length; i++)
                trie = trie.Warp[dd[i]];
            return trie.Index;
        }
#else
        [MessagePackObject]
        public class TrieArticle
        {
            [Key(0)]
            public Dictionary<string, List<int>> Warp;

            public List<int> of(string str)
            {
                if (Warp == null)
                    Warp = new Dictionary<string, List<int>>();
                if (!Warp.ContainsKey(str))
                    Warp.Add(str, new List<int>());
                return Warp[str];
            }
        }

        [MessagePackObject]
        public class TrieComment
        {
            [Key(0)]
            public Dictionary<string, List<(int, int)>> Warp;

            public List<(int, int)> of(string str)
            {
                if (Warp == null)
                    Warp = new Dictionary<string, List<(int, int)>>();
                if (!Warp.ContainsKey(str))
                    Warp.Add(str, new List<(int, int)>());
                return Warp[str];
            }
        }

        static readonly string[] cc = new[] { "r", "R", "rt", "s", "sw", "sg", "e", "E", "f", "fr", "fa", "fq", "ft", "fe",
            "fv", "fg", "a", "q", "Q", "qt", "t", "T", "d", "w", "W", "c", "z", "e", "v", "g", "k", "o", "i", "O",
            "j", "p", "u", "P", "h", "hk", "ho", "hl", "y", "n", "nj", "np", "nl", "b", "m", "ml", "l", " ", "ss",
            "se", "st", " ", "frt", "fe", "fqt", " ", "fg", "aq", "at", " ", " ", "qr", "qe", "qtr", "qte", "qw",
            "qe", " ", " ", "tr", "ts", "te", "tq", "tw", " ", "dd", "d", "dt", " ", " ", "gg", " ", "yi", "yO", "yl", "bu", "bP", "bl" };
        static readonly char[] cc1 = new[] { 'r', 'R', 's', 'e', 'E', 'f', 'a', 'q', 'Q', 't', 'T', 'd', 'w', 'W', 'c', 'z', 'x', 'v', 'g' };
        static readonly string[] cc2 = new[] { "k", "o", "i", "O", "j", "p", "u", "P", "h", "hk", "ho", "hl", "y", "n", "nj", "np", "nl", "b", "m", "ml", "l" };
        static readonly string[] cc3 = new[] { "", "r", "R", "rt", "s", "sw", "sg", "e", "f", "fr", "fa", "fq", "ft", "fx", "fv", "fg", "a", "q", "qt", "t", "T", "d", "w", "c", "z", "x", "v", "g" };

        public static string hangul_disassembly(char letter)
        {
            if (0xAC00 <= letter && letter <= 0xD7FB)
            {
                int unis = letter - 0xAC00;
                return cc1[unis / (21 * 28)] + cc2[(unis % (21 * 28)) / 28] + cc3[(unis % (21 * 28)) % 28];
            }
            else if (0x3131 <= letter && letter <= 0x3163)
            {
                int unis = letter;
                return cc[unis - 0x3131];
            }
            else
            {
                return letter.ToString();
            }
        }

        public static string disasm(string word)
        {
            return string.Join("", word.Select(y => hangul_disassembly(y)));
        }

        public void ProcessComment(DCInsideCommentElement comm, int article, int comment)
        {
            var word_lists = comm.memo.ToHtmlNode().InnerText.Split(' ').Select(x => disasm(x)).ToList();
            //var no = comm.parent.ToInt();

            foreach (var word in word_lists)
            {
                if (word == "")
                    continue;
                string ww = word[0].ToString();
                comment_index.of(ww).Add((article, comment));
                for (int i = 1; i < word.Length; i++)
                {
                    ww += word[i];
                    comment_index.of(ww).Add((article, comment));
                }
            }
        }

        public void ProcessBody(DCInsideArchiveModel article, int index)
        {
            var word_lists = article.raw.ToHtmlNode().InnerText.Trim('\r', '\n', '\t').Split(' ').Select(x => disasm(x)).ToList();

            foreach (var word in word_lists)
            {
                if (word == "")
                    continue;
                string ww = word[0].ToString();
                article_index.of(ww).Add(index);
                for (int i = 1; i < word.Length; i++)
                {
                    ww += word[i];
                    article_index.of(ww).Add(index);
                }
            }
            if (article.comments == null || article.comments.Count == 0)
                return;

            for (int i = 0; i < article.comments.Count; i++)
                ProcessComment(article.comments[i], index, i);
        }

        /// <summary>
        /// 새로운 트리를 생성합니다.
        /// </summary>
        public void Build()
        {
            for (int i = 0; i < DCInsideArchive.Instance.Model.Count; i++)
            {
                ProcessBody(DCInsideArchive.Instance.Model[i], i);
                Console.Instance.WriteLine($"[{++i}/{DCInsideArchive.Instance.Model.Count}]");
            }

            var bbb = MessagePackSerializer.Serialize(article_index);
            using (FileStream fsStream = new FileStream($"article-index-ii.json", FileMode.Create))
            using (BinaryWriter sw = new BinaryWriter(fsStream))
            {
                sw.Write(bbb);
            }

            var bbb1 = MessagePackSerializer.Serialize(comment_index);
            using (FileStream fsStream = new FileStream($"comment-index-ii.json", FileMode.Create))
            using (BinaryWriter sw = new BinaryWriter(fsStream))
            {
                sw.Write(bbb1);
            }
        }

        public void Load(string prefix)
        {
            article_index = MessagePackSerializer.Deserialize<TrieArticle>(File.ReadAllBytes(prefix + "-ai.json"));
            comment_index = MessagePackSerializer.Deserialize<TrieComment>(File.ReadAllBytes(prefix + "-ci.json"));
        }

        public List<int> of_article(string word)
        {
            if (article_index.Warp.ContainsKey(word))
                return article_index.Warp[word];
            return new List<int>();
        }

        public List<(int, int)> of_comment(string word)
        {
            if (comment_index.Warp.ContainsKey(word))
                return comment_index.Warp[word];
            return new List<(int, int)>();
        }
#endif
    }
}
