// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GalleryExplorer.Core
{
    /// <summary>
    /// 모든 IO와 작업 현황을 보고합니다.
    /// </summary>
    public class Logger : ILazy<Logger>
    {
        /// <summary>
        /// 오브젝트를 직렬화합니다.
        /// </summary>
        /// <param name="toSerialize"></param>
        /// <returns></returns>
        public static string SerializeObject(object toSerialize)
        {
            try
            {
                return JsonConvert.SerializeObject(toSerialize, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
            }
            catch
            {
                try
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

                    using (StringWriter textWriter = new StringWriter())
                    {
                        xmlSerializer.Serialize(textWriter, toSerialize);
                        return textWriter.ToString();
                    }
                }
                catch
                {
                    return toSerialize.ToString();
                }
            }
        }

        /// <summary>
        /// 모니터 초기화
        /// </summary>
        public Logger()
        {
            if (controlEnable)
                showMonitorControl();
        }

        /// <summary>
        /// 프롬프트를 초기화합니다.
        /// </summary>
        public void Start()
        {
            Console.Instance.Start();
        }

        /// <summary>
        /// 처음시작시엔 디버그 모드에서만 모니터를 사용합니다.
        /// </summary>
#if DEBUG
        public bool controlEnable = true;
#else
        public bool controlEnable = false;
#endif
        public bool ControlEnable
        {
            get
            {
                return controlEnable;
            }
            set
            {
                if (value && !controlEnable)
                    showMonitorControl();
                else if (!value && controlEnable)
                    hideMonitorControl();
                controlEnable = value;
            }
        }

        public delegate void NotifyEvent(object sender, EventArgs e);
        event EventHandler LogCollectionChange;
        event EventHandler LogErrorCollectionChange;
        event EventHandler LogWarningCollectionChange;
        object event_lock = new object();

        /// <summary>
        /// Attach your own notify event.
        /// </summary>
        /// <param name="notify_event"></param>
        public void AddLogNotify(NotifyEvent notify_event)
        {
            LogCollectionChange += new EventHandler(notify_event);
        }

        public void ClearLogNotify()
        {
            LogCollectionChange = null;
        }

        /// <summary>
        /// Attach your own notify event.
        /// </summary>
        /// <param name="notify_event"></param>
        public void AddLogErrorNotify(NotifyEvent notify_event)
        {
            LogErrorCollectionChange += new EventHandler(notify_event);
        }

        /// <summary>
        /// Attach your own notify event.
        /// </summary>
        /// <param name="notify_event"></param>
        public void AddLogWarningNotify(NotifyEvent notify_event)
        {
            LogWarningCollectionChange += new EventHandler(notify_event);
        }

        /// <summary>
        /// Push some message to log.
        /// </summary>
        /// <param name="str"></param>
        public void Push(string str)
        {
            write_log(DateTime.Now, str);
            lock (event_lock) LogCollectionChange?.Invoke(Tuple.Create(DateTime.Now, str, false), null);
        }

        /// <summary>
        /// Push some object to log.
        /// </summary>
        /// <param name="obj"></param>
        public void Push(object obj)
        {
            write_log(DateTime.Now, obj.ToString());
            write_log(DateTime.Now, SerializeObject(obj));
            lock (event_lock)
            {
                LogCollectionChange?.Invoke(Tuple.Create(DateTime.Now, obj.ToString(), false), null);
                LogCollectionChange?.Invoke(Tuple.Create(DateTime.Now, SerializeObject(obj), true), null);
            }
        }

        /// <summary>
        /// Push some message to log.
        /// </summary>
        /// <param name="str"></param>
        public void PushError(string str)
        {
            write_error_log(DateTime.Now, str);
            lock (event_lock) LogErrorCollectionChange?.Invoke(Tuple.Create(DateTime.Now, str, false), null);
        }

        /// <summary>
        /// Push some object to log.
        /// </summary>
        /// <param name="obj"></param>
        public void PushError(object obj)
        {
            write_error_log(DateTime.Now, obj.ToString());
            write_error_log(DateTime.Now, SerializeObject(obj));
            lock (event_lock)
            {
                LogErrorCollectionChange?.Invoke(Tuple.Create(DateTime.Now, obj.ToString(), false), null);
                LogErrorCollectionChange?.Invoke(Tuple.Create(DateTime.Now, SerializeObject(obj), true), null);
            }
        }

        /// <summary>
        /// Push some message to log.
        /// </summary>
        /// <param name="str"></param>
        public void PushWarning(string str)
        {
            write_warning_log(DateTime.Now, str);
            lock (event_lock) LogWarningCollectionChange?.Invoke(Tuple.Create(DateTime.Now, str, false), null);
        }

        /// <summary>
        /// Push some object to log.
        /// </summary>
        /// <param name="obj"></param>
        public void PushWarning(object obj)
        {
            write_warning_log(DateTime.Now, obj.ToString());
            write_warning_log(DateTime.Now, SerializeObject(obj));
            lock (event_lock)
            {
                LogWarningCollectionChange?.Invoke(Tuple.Create(DateTime.Now, obj.ToString(), false), null);
                LogWarningCollectionChange?.Invoke(Tuple.Create(DateTime.Now, SerializeObject(obj), true), null);
            }
        }

        object log_lock = new object();

        private void write_log(DateTime dt, string message)
        {
            CultureInfo en = new CultureInfo("en-US");
            lock (log_lock)
            {
                File.AppendAllText("log.txt", $"[{dt.ToString(en)}] {message}\r\n");
            }
        }

        private void write_error_log(DateTime dt, string message)
        {
            CultureInfo en = new CultureInfo("en-US");
            lock (log_lock)
            {
                File.AppendAllText("log.txt", $"[{dt.ToString(en)}] [Error] {message}\r\n");
            }
        }

        private void write_warning_log(DateTime dt, string message)
        {
            CultureInfo en = new CultureInfo("en-US");
            lock (log_lock)
            {
                File.AppendAllText("log.txt", $"[{dt.ToString(en)}] [Warning] {message}\r\n");
            }
        }

        private void showMonitorControl()
        {
            Console.Instance.Show();

            AddLogNotify((s, e) =>
            {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write("info: ");
                System.Console.ResetColor();
                System.Console.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            AddLogErrorNotify((s, e) => {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Error.Write("error: ");
                System.Console.ResetColor();
                System.Console.Error.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            AddLogWarningNotify((s, e) => {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.Write("warning: ");
                System.Console.ResetColor();
                System.Console.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });
        }

        private void hideMonitorControl()
        {
            Console.Instance.Hide();
        }
    }
}
