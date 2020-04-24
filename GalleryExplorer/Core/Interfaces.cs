// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryExplorer.Core
{
    /// <summary>
    /// 콘솔 최상위 명령의 인터페이스입니다.
    /// </summary>
    public interface IConsole
    {
        /// <summary>
        /// 리다이렉트 함수입니다.
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>리다이렉트가 성공적으로 수행되었는지의 여부입니다.</returns>
        bool Redirect(string[] arguments, string contents);
    }

    /// <summary>
    /// 반드시 포함되어야하는 콘솔 옵션입니다.
    /// </summary>
    public class IConsoleOption
    {
        public bool Error;
        public string ErrorMessage;
        public string HelpMessage;
    }

    public class InstanceMonitor
    {
        public static Dictionary<string, object> Instances = new Dictionary<string, object>();
    }

    /// <summary>
    /// Lazy구현을 쉽게 해주는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ILazy<T>
        where T : new()
    {
        private static readonly Lazy<T> instance = new Lazy<T>(() =>
        {
            T instance = new T();
            InstanceMonitor.Instances.Add(instance.GetType().Name.ToLower(), instance);
            return instance;
        });
        public static T Instance => instance.Value;
        public static bool IsValueCreated => instance.IsValueCreated;
    }
}
