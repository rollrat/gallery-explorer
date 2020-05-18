// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            {"contentauthornick",         DCInsideArchiveQueryTokenType.ContentAuthorNick},
            {"contentauthorip",           DCInsideArchiveQueryTokenType.ContentAuthorIp},
            {"contentauthorid",           DCInsideArchiveQueryTokenType.ContentAuthorId},
            {"contentauthortype",         DCInsideArchiveQueryTokenType.ContentAuthorType},
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
            {"commentauthornick",         DCInsideArchiveQueryTokenType.CommentAuthorNick},
            {"commentauthorip",           DCInsideArchiveQueryTokenType.CommentAuthorIp},
            {"commentauthorid",           DCInsideArchiveQueryTokenType.CommentAuthorId},
            {"commentauthortype",         DCInsideArchiveQueryTokenType.CommentAuthorType},
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
                    for (; i < query_string.Length && !char.IsWhiteSpace(query_string[i]); i++)
                    {
                        if ("()-+&|~".Contains(query_string[i])) break;
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

                if (query.is_operator == false)
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
                            if (am.info.classify != null && am.info.classify == query.token)
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
                if (query.is_operator == true)
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

                if (query.is_operator == false)
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
                if (query.is_operator == true)
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
}
