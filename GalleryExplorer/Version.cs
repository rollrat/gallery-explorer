// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryExplorer
{
    public class Version
    {
        public const int MajorVersion = 2020;
        public const int MinorVersion = 04;
        public const int BuildVersion = 30;

        public const string Name = "디시인사이드 갤러리 탐색기";
        public static string Text { get; } = $"{MajorVersion}.{MinorVersion}.{BuildVersion}";
    }
}
