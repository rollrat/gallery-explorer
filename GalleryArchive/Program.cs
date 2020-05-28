// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using GalleryExplorer.Core;
using System;
using System.Globalization;

namespace GalleryArchive
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Instance.AddLogNotify((s, e) =>
            {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("info: ");
                Console.ResetColor();
                Console.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            Logger.Instance.AddLogErrorNotify((s, e) => {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write("error: ");
                Console.ResetColor();
                Console.Error.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            Logger.Instance.AddLogWarningNotify((s, e) => {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("warning: ");
                Console.ResetColor();
                Console.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            Console.WriteLine("디시인사이드 아카이브 검색도구 V1.2 - 2020.05.19");
            Console.WriteLine("저격 기능 제거 버전");
            Console.WriteLine("For 툴리우스 갤러리 - 툴리우스 갤러리 이외의 갤러니나 다른 웹 사이트에 재배포 및 판매 금지");
            Console.WriteLine("");

            Console.WriteLine("데이터 로딩중...");
            DCInsideArchive.Instance.Load(@"툴갤 아카이브-index.json");

            while (true)
            {
                Console.WriteLine("1: 글 검색, 2: 댓글 검색, 3: 명령어 보기");
                Console.Write("> ");
                var x = Console.ReadLine();

                try
                {

                    if (x.Trim() == "1")
                    {
                        Console.WriteLine("검색어 입력 (글 검색)");
                        Console.Write("> ");
                        var y = Console.ReadLine();

                        var query = DCInsideArchiveQueryHelper.to_linear(DCInsideArchiveQueryHelper.make_tree(y));
                        var result = DCInsideArchive.Instance.Query.Query(query);
                        result.Results.Sort((x, y) => x.info.no.ToInt().CompareTo(y.info.no.ToInt()));
                        result.Save();
                        foreach (var rr in result.Results)
                            Console.WriteLine($"[{rr.info.no}] {rr.info.title}");
                    }
                    else if (x.Trim() == "2")
                    {
                        Console.WriteLine("검색어 입력 (댓글 검색)");
                        Console.Write("> ");
                        var y = Console.ReadLine();

                        var query = DCInsideArchiveQueryHelper.to_linear(DCInsideArchiveQueryHelper.make_tree(y));
                        var result = DCInsideArchive.Instance.Query.QueryComment(query);
                        result.Results.Sort((x, y) => x.no.ToInt().CompareTo(y.no.ToInt()));
                        result.Results.Sort((x, y) => x.parent.ToInt().CompareTo(y.parent.ToInt()));
                        result.Save();
                        foreach (var rr in result.Results)
                            Console.WriteLine($"[{rr.parent}] {rr.name}({rr.ip}{rr.user_id}): {rr.memo}");
                    }
                    else if (x.Trim() == "3")
                    {
                        Console.WriteLine("게시글");
                        Console.WriteLine("BodyContainsSimple: 글 내용에 단어 포함(HTML 전체)");
                        Console.WriteLine("BodyContainsHard: 글 내용에 단어 포함");
                        Console.WriteLine("TitleContains: 제목에 단어 포함");
#if DEBUG
                        Console.WriteLine("ContentAuthorNick: 작성자 닉네임");
                        Console.WriteLine("ContentAuthorIp: 작성자 아이피");
                        Console.WriteLine("ContentAuthorId: 작성자 아이디");
                        Console.WriteLine("ContentAuthorType: 작성자 종류(0: 유동, 1:반고정, 2:고정)");
#endif
                        Console.WriteLine("ContentHasComment: 최소 댓글 개수");
                        Console.WriteLine("ContentHasImage: 최소 이미지 개수");
                        Console.WriteLine("ContentViews: 최소 조회수");
                        Console.WriteLine("ContentUpVote: 최소 추천수");
                        Console.WriteLine("ContentClass: 말머리 지정");
                        Console.WriteLine("");
                        Console.WriteLine("댓글");
                        Console.WriteLine("CommentContainsSimple: 댓글 내용에 단어 포함(HTML 전체)");
                        Console.WriteLine("CommentContainsHard: 댓글 내용에 단어 포함");
#if DEBUG
                        Console.WriteLine("CommentAuthorNick: 댓글 작성자 닉네임");
                        Console.WriteLine("CommentAuthorIp: 댓글 작성자 아이피");
                        Console.WriteLine("CommentAuthorId: 댓글 작성자 아이디");
#endif
                        Console.WriteLine("");
                        Console.WriteLine("댓글검색 전용");
                        Console.WriteLine("CommentAuthorType: 지원안함");
                        Console.WriteLine("CommentIsDCCon: 지원안함");
                        Console.WriteLine("CommentIsVoice: 지원안함");
                        Console.WriteLine("");
                        Console.WriteLine("글+댓글");
                        Console.WriteLine("ContentContainsSimple: 글+댓글 내용에 단어 포함(HTML 전체)");
                        Console.WriteLine("ContentContainsHard: 글+댓글 내용에 단어 포함");
                    }
                    else
                    {
                        Console.WriteLine("ㅗ");
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.PushError($"{e.Message}\r\n{e.StackTrace}");
                }
            }
        }
    }
}
