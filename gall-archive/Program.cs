// This source code is a part of Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using HtmlAgilityPack;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;

namespace gall_archive
{
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

    public class SettingModel
    {
        public class NetworkSetting
        {
            public bool TimeoutInfinite;
            public int TimeoutMillisecond;
            public int DownloadBufferSize;
            public int RetryCount;
            public string Proxy;
            public bool UsingProxyList;
        }

        public NetworkSetting NetworkSettings;

        /// <summary>
        /// Scheduler Thread Count
        /// </summary>
        public int ThreadCount;

        /// <summary>
        /// Postprocessor Scheduler Thread Count
        /// </summary>
        public int PostprocessorThreadCount;

        /// <summary>
        /// Provider Language
        /// </summary>
        public string Language;

        /// <summary>
        /// Parent Path for Downloading
        /// </summary>
        public string SuperPath;
    }

    public class Settings : ILazy<Settings>
    {
        public const string Name = "settings.json";

        public SettingModel Model { get; set; }
        public SettingModel.NetworkSetting Network { get { return Model.NetworkSettings; } }

        public Settings()
        {
            var full_path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Name);
            if (File.Exists(full_path))
                Model = JsonConvert.DeserializeObject<SettingModel>(File.ReadAllText(full_path));

            if (Model == null)
            {
                Model = new SettingModel
                {
                    Language = GetLanguageKey(),
                    ThreadCount = 3,
                    //ThreadCount = Environment.ProcessorCount,
                    PostprocessorThreadCount = 3,

                    NetworkSettings = new SettingModel.NetworkSetting
                    {
                        TimeoutInfinite = false,
                        TimeoutMillisecond = 10000,
                        DownloadBufferSize = 131072,
                        RetryCount = 10,
                        UsingProxyList = false,
                    },
                };
            }
            //Save();
        }

        public static string GetLanguageKey()
        {
            var lang = Thread.CurrentThread.CurrentCulture.ToString();
            var language = "all";
            switch (lang)
            {
                case "ko-KR":
                    language = "korean";
                    break;

                case "ja-JP":
                    language = "japanese";
                    break;

                case "en-US":
                    language = "english";
                    break;
            }
            return language;
        }

        public void Save()
        {
            var full_path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Name);
            var json = JsonConvert.SerializeObject(Model, Formatting.Indented);
            using (var fs = new StreamWriter(new FileStream(full_path, FileMode.Create, FileAccess.Write)))
            {
                fs.Write(json);
            }
        }
    }

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
    }

    public enum NetPriorityType
    {
        // ex) Download and save file, large data
        Low = 0,
        // ex) Download metadata, html file ...
        Trivial = 1,
        // Pause all processing and force download
        Emergency = 2,
    }

    public class NetPriority : IComparable<NetPriority>
    {
        [JsonProperty]
        public NetPriorityType Type { get; set; }
        [JsonProperty]
        public int TaskPriority { get; set; }

        public int CompareTo(NetPriority pp)
        {
            if (Type > pp.Type) return 1;
            else if (Type < pp.Type) return -1;

            return pp.TaskPriority.CompareTo(TaskPriority);
        }
    }

    public class ProxyList : ILazy<ProxyList>
    {
        public const string Name = "proxy.txt";

        public List<string> List { get; set; } = new List<string>();

        public ProxyList()
        {
            var full_path = Path.Combine(Directory.GetCurrentDirectory(), Name);
            if (!File.Exists(full_path))
            {
                Logger.Instance.PushError("[Proxy] 'proxy.txt' not found!");
                return;
            }
            var txt = File.ReadAllLines(full_path);
            foreach (var line in txt)
            {
                if (string.IsNullOrEmpty(line))
                    break;
                List.Add(line.Split(' ')[0].Trim());
            }
        }

        public string RandomPick()
            => List[new Random().Next(List.Count)];
    }

    /// <summary>
    /// Information of what download for
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class NetTask : ISchedulerContents<NetTask, NetPriority>
    {
        public static NetTask MakeDefault(string url, string cookie = "")
            => new NetTask
            {
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.139 Safari/537.36",
                TimeoutInfinite = Settings.Instance.Network.TimeoutInfinite,
                TimeoutMillisecond = Settings.Instance.Network.TimeoutMillisecond,
                AutoRedirection = true,
                RetryWhenFail = true,
                RetryCount = Settings.Instance.Network.RetryCount,
                DownloadBufferSize = Settings.Instance.Network.DownloadBufferSize,
                Priority = new NetPriority() { Type = NetPriorityType.Trivial },
                Proxy = !string.IsNullOrEmpty(Settings.Instance.Network.Proxy) ? new WebProxy(Settings.Instance.Network.Proxy) :
                    Settings.Instance.Network.UsingProxyList ? new WebProxy(ProxyList.Instance.RandomPick()) : null,
                Cookie = cookie,
                Url = url
            };

        public static NetTask MakeDefaultMobile(string url, string cookie = "")
            => new NetTask
            {
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8",
                UserAgent = "Mozilla/5.0 (Android 7.0; Mobile; rv:54.0) Gecko/54.0 Firefox/54.0 AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.125 Mobile Safari/603.2.4",
                TimeoutInfinite = Settings.Instance.Network.TimeoutInfinite,
                TimeoutMillisecond = Settings.Instance.Network.TimeoutMillisecond,
                AutoRedirection = true,
                RetryWhenFail = true,
                RetryCount = Settings.Instance.Network.RetryCount,
                DownloadBufferSize = Settings.Instance.Network.DownloadBufferSize,
                Priority = new NetPriority() { Type = NetPriorityType.Trivial },
                Proxy = !string.IsNullOrEmpty(Settings.Instance.Network.Proxy) ? new WebProxy(Settings.Instance.Network.Proxy) :
                    Settings.Instance.Network.UsingProxyList ? new WebProxy(ProxyList.Instance.RandomPick()) : null,
                Cookie = cookie,
                Url = url
            };

        public enum NetError
        {
            Unhandled = 0,
            CannotContinueByCriticalError,
            UnknowError, // Check DPI Blocker
            UriFormatError,
            Aborted,
            ManyRetry,
        }

        /* Task Information */

        [JsonProperty]
        public int Index { get; set; }

        /* Http Information */

        [JsonProperty]
        public string Url { get; set; }
        [JsonProperty]
        public List<string> FailUrls { get; set; }
        [JsonProperty]
        public string Accept { get; set; }
        [JsonProperty]
        public string Referer { get; set; }
        [JsonProperty]
        public string UserAgent { get; set; }
        [JsonProperty]
        public string Cookie { get; set; }
        [JsonProperty]
        public Dictionary<string, string> Headers { get; set; }
        [JsonProperty]
        public Dictionary<string, string> Query { get; set; }
        [JsonProperty]
        public IWebProxy Proxy { get; set; }

        /* Detail Information */

        /// <summary>
        /// Text Encoding Information
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Set if you want to download and save file to your own device.
        /// </summary>
        [JsonProperty]
        public bool SaveFile { get; set; }
        [JsonProperty]
        public string Filename { get; set; }

        /// <summary>
        /// Set if needing only string datas.
        /// </summary>
        [JsonProperty]
        public bool DownloadString { get; set; }

        /// <summary>
        /// Download data to temporary directory on your device.
        /// </summary>
        [JsonProperty]
        public bool DriveCache { get; set; }

        /// <summary>
        /// Download data to memory.
        /// </summary>
        [JsonProperty]
        public bool MemoryCache { get; set; }

        /// <summary>
        /// Retry download when fail to download.
        /// </summary>
        [JsonProperty]
        public bool RetryWhenFail { get; set; }
        [JsonProperty]
        public int RetryCount { get; set; }

        /// <summary>
        /// Timeout settings
        /// </summary>
        [JsonProperty]
        public bool TimeoutInfinite { get; set; }
        [JsonProperty]
        public int TimeoutMillisecond { get; set; }

        [JsonProperty]
        public int DownloadBufferSize { get; set; }

        [JsonProperty]
        public bool AutoRedirection { get; set; }

        [JsonProperty]
        public bool NotifyOnlySize { get; set; }

        [JsonProperty]
        public bool GetStream { get; set; }

        /* Callback Functions */

        public Action<long> SizeCallback;
        public Action<long> DownloadCallback;
        public Action StartCallback;
        public Action CompleteCallback;
        public Action<string> CompleteCallbackString;
        public Action<byte[]> CompleteCallbackBytes;
        public Action<Stream> CompleteCallbackStream;
        public Action<CookieCollection> CookieReceive;
        public Action<string> HeaderReceive;
        public Action CancleCallback;

        /// <summary>
        /// Return total downloaded size
        /// </summary>
        public Action<int> RetryCallback;
        public Action<NetError> ErrorCallback;

        /* For NetField */

        public bool Aborted;
        public HttpWebRequest Request;
        public CancellationToken Cancel;

        /* Post Processor */

        public Action StartPostprocessorCallback;
    }


    public class UpdatableHeapElements<T> : IComparable<T>
        where T : IComparable<T>
    {
        public T data;
        public int index;
        public static UpdatableHeapElements<T> Create(T data, int index)
            => new UpdatableHeapElements<T> { data = data, index = index };
        public int CompareTo(T obj)
            => data.CompareTo(obj);
    }

    public class UpdatableHeap<S, T, C>
        where S : IComparable<S>
        where T : UpdatableHeapElements<S>, IComparable<S>
        where C : IComparer<S>, new()
    {
        List<T> heap;
        C comp;

        public UpdatableHeap(int capacity = 256)
        {
            heap = new List<T>(capacity);
            comp = new C();
        }

        public T Push(S d)
        {
            var dd = (T)UpdatableHeapElements<S>.Create(d, heap.Count - 1);
            heap.Add(dd);
            top_down(heap.Count - 1);
            return dd;
        }

        public void Pop()
        {
            heap[0] = heap[heap.Count - 1];
            heap[0].index = 0;
            heap.RemoveAt(heap.Count - 1);
            bottom_up();
        }

        public void Update(T d)
        {
            int p = (d.index - 1) >> 1;
            if (p == d.index)
                bottom_up();
            else
            {
                if (comp.Compare(heap[p].data, heap[d.index].data) > 0)
                    top_down(d.index);
                else
                    bottom_up(d.index);
            }
        }

        public S Front => heap[0].data;

        public int Count { get { return heap.Count; } }

        private void bottom_up(int x = 0)
        {
            int l = heap.Count - 1;
            while (x < l)
            {
                int c1 = x * 2 + 1;
                int c2 = c1 + 1;

                //
                //      x
                //     / \
                //    /   \
                //   c1   c2
                //

                int c = c1;
                if (c2 < l && comp.Compare(heap[c2].data, heap[c1].data) > 0)
                    c = c2;

                if (c < l && comp.Compare(heap[c].data, heap[x].data) > 0)
                {
                    swap(c, x);
                    x = c;
                }
                else
                {
                    break;
                }
            }
        }

        private void top_down(int x)
        {
            while (x > 0)
            {
                int p = (x - 1) >> 1;
                if (comp.Compare(heap[x].data, heap[p].data) > 0)
                {
                    swap(p, x);
                    x = p;
                }
                else
                    break;
            }
        }

        private void swap(int i, int j)
        {
            T t = heap[i];
            heap[i] = heap[j];
            heap[j] = t;

            int tt = heap[i].index;
            heap[i].index = heap[j].index;
            heap[j].index = tt;
        }
    }

    public class DefaultHeapComparer<T> : Comparer<T> where T : IComparable<T>
    {
        public override int Compare(T x, T y)
            => x.CompareTo(y);
    }

    public class UpdatableHeap<T> : UpdatableHeap<T, UpdatableHeapElements<T>, DefaultHeapComparer<T>> where T : IComparable<T> { }

    public interface IScheduler<T>
        where T : IComparable<T>
    {
        void update(UpdatableHeapElements<T> elem);
    }

    public class ISchedulerContents<T, P>
        : IComparable<ISchedulerContents<T, P>>
        where T : IComparable<T>
        where P : IComparable<P>
    {
        /* Scheduler Information */
        P priority;

        [JsonProperty]
        public P Priority { get { return priority; } set { priority = value; if (scheduler != null) scheduler.update(heap_elements); } }
        public int CompareTo(ISchedulerContents<T, P> other)
            => Priority.CompareTo(other.Priority);

        public UpdatableHeapElements<T> heap_elements;
        public IScheduler<T> scheduler;
    }

    public abstract class IField<T, P>
        where T : ISchedulerContents<T, P>
        where P : IComparable<P>
    {
        public abstract void Main(T content);
        public ManualResetEvent interrupt = new ManualResetEvent(true);
    }

    /// <summary>
    /// Scheduler Interface
    /// </summary>
    /// <typeparam name="T">Task type</typeparam>
    /// <typeparam name="P">Priority type</typeparam>
    /// <typeparam name="F">Field type</typeparam>
    public class Scheduler<T, P, F>
        : IScheduler<T>
        where T : ISchedulerContents<T, P>
        where P : IComparable<P>
        where F : IField<T, P>, new()
    {
        public UpdatableHeap<T> queue = new UpdatableHeap<T>();

        public void update(UpdatableHeapElements<T> elem)
        {
            queue.Update(elem);
        }

        public int thread_count = 0;
        public int busy_thread = 0;
        public int capacity = 0;

        public P LatestPriority;

        public List<Thread> threads = new List<Thread>();
        public List<ManualResetEvent> interrupt = new List<ManualResetEvent>();
        public List<F> field = new List<F>();

        object notify_lock = new object();

        public Scheduler(int capacity = 0, bool use_emergency_thread = false)
        {
            this.capacity = capacity;

            if (this.capacity == 0)
                this.capacity = Environment.ProcessorCount;

            thread_count = this.capacity;

            if (use_emergency_thread)
                thread_count += 1;

            for (int i = 0; i < this.capacity; i++)
            {
                interrupt.Add(new ManualResetEvent(false));
                threads.Add(new Thread(new ParameterizedThreadStart(remote_thread_handler)));
                threads.Last().Start(i);
            }

            for (int i = 0; i < this.capacity; i++)
            {
                field.Add(new F());
            }
        }

        private void remote_thread_handler(object i)
        {
            int index = (int)i;

            while (true)
            {
                interrupt[index].WaitOne();

                T task;

                lock (queue)
                {
                    if (queue.Count > 0)
                    {
                        task = queue.Front;
                        queue.Pop();
                    }
                    else
                    {
                        interrupt[index].Reset();
                        continue;
                    }
                }

                Interlocked.Increment(ref busy_thread);

                LatestPriority = task.Priority;

                field[index].Main(task);

                Interlocked.Decrement(ref busy_thread);
            }
        }

        public void Pause()
        {
            field.ForEach(x => x.interrupt.Reset());
        }

        public void Resume()
        {
            field.ForEach(x => x.interrupt.Set());
        }

        public void Notify()
        {
            interrupt.ForEach(x => x.Set());
        }

        public UpdatableHeapElements<T> Add(T task)
        {
            task.scheduler = this;
            UpdatableHeapElements<T> e;
            lock (queue) e = queue.Push(task);
            lock (notify_lock) Notify();
            return e;
        }
    }

    /// <summary>
    /// Implementaion of real download procedure
    /// </summary>
    public class NetField : IField<NetTask, NetPriority>
    {
        public override void Main(NetTask content)
        {
            var retry_count = 0;

        RETRY_PROCEDURE:

            interrupt.WaitOne();
            if (content.Cancel != null && content.Cancel.IsCancellationRequested)
            {
                content.CancleCallback();
                return;
            }

            interrupt.WaitOne();

            if (content.DownloadString)
                Logger.Instance.Push("[NetField] Start download string... " + content.Url);
            else if (content.MemoryCache)
                Logger.Instance.Push("[NetField] Start download to memory... " + content.Url);
            else if (content.SaveFile)
                Logger.Instance.Push("[NetField] Start download file... " + content.Url + " to " + content.Filename);

            REDIRECTION:

            interrupt.WaitOne();
            if (content.Cancel != null && content.Cancel.IsCancellationRequested)
            {
                content.CancleCallback();
                return;
            }

            content.StartCallback?.Invoke();

            try
            {
                //
                //  Initialize http-web-request
                //

                var request = (HttpWebRequest)WebRequest.Create(content.Url);
                content.Request = request;

                request.Accept = content.Accept;
                request.UserAgent = content.UserAgent;

                if (content.Referer != null)
                    request.Referer = content.Referer;
                else
                    request.Referer = (content.Url.StartsWith("https://") ? "https://" : (content.Url.Split(':')[0] + "//")) + request.RequestUri.Host;

                if (content.Cookie != null)
                    request.Headers.Add(HttpRequestHeader.Cookie, content.Cookie);

                if (content.Headers != null)
                    content.Headers.ToList().ForEach(p => request.Headers.Add(p.Key, p.Value));

                if (content.Proxy != null)
                    request.Proxy = content.Proxy;

                if (content.TimeoutInfinite)
                    request.Timeout = Timeout.Infinite;
                else
                    request.Timeout = content.TimeoutMillisecond;

                request.AllowAutoRedirect = content.AutoRedirection;

                //
                //  POST Data
                //

                if (content.Query != null)
                {
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";

                    var request_stream = new StreamWriter(request.GetRequestStream());
                    var query = string.Join("&", content.Query.ToList().Select(x => $"{x.Key}={x.Value}"));
                    request_stream.Write(query);
                    request_stream.Close();

                    interrupt.WaitOne();
                    if (content.Cancel != null && content.Cancel.IsCancellationRequested)
                    {
                        content.CancleCallback();
                        return;
                    }
                }

                //
                //  Wait request
                //

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.NotFound ||
                        response.StatusCode == HttpStatusCode.Forbidden ||
                        response.StatusCode == HttpStatusCode.Unauthorized ||
                        response.StatusCode == HttpStatusCode.BadRequest ||
                        response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        //
                        //  Cannot continue
                        //

                        content.ErrorCallback?.Invoke(NetTask.NetError.CannotContinueByCriticalError);
                        return;
                    }
                    else if (response.StatusCode == HttpStatusCode.Moved ||
                             response.StatusCode == HttpStatusCode.Redirect)
                    {
                        if (content.AutoRedirection)
                        {
                            var old = content.Url;
                            content.Url = response.Headers.Get("Location");
                            Logger.Instance.Push("[NetField] Redirection " + old + " to " + content.Url);
                            goto REDIRECTION;
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.OK)
                    {
                        interrupt.WaitOne();
                        if (content.Cancel != null && content.Cancel.IsCancellationRequested)
                        {
                            content.CancleCallback();
                            return;
                        }

                        content.HeaderReceive?.Invoke(response.Headers.ToString());
                        content.CookieReceive?.Invoke(response.Cookies);

                        Stream istream = response.GetResponseStream();
                        Stream ostream = null;

                        if (content.GetStream)
                        {
                            content.CompleteCallbackStream?.Invoke(istream);
                            return;
                        }

                        if (content.DownloadString || content.MemoryCache)
                        {
                            ostream = new MemoryStream();
                        }
                        else if (content.DriveCache)
                        {
                            // TODO:
                        }
                        else
                        {
                            ostream = File.OpenWrite(content.Filename);
                        }

                        content.SizeCallback?.Invoke(response.ContentLength);

                        if (content.NotifyOnlySize)
                        {
                            ostream.Close();
                            istream.Close();
                            return;
                        }

                        interrupt.WaitOne();
                        if (content.Cancel != null && content.Cancel.IsCancellationRequested)
                        {
                            content.CancleCallback();
                            return;
                        }

                        byte[] buffer = new byte[content.DownloadBufferSize];
                        long byte_read = 0;

                        //
                        //  Download loop
                        //

                        do
                        {
                            interrupt.WaitOne();
                            if (content.Cancel != null && content.Cancel.IsCancellationRequested)
                            {
                                content.CancleCallback();
                                return;
                            }

                            byte_read = istream.Read(buffer, 0, buffer.Length);
                            ostream.Write(buffer, 0, (int)byte_read);

                            interrupt.WaitOne();
                            if (content.Cancel != null && content.Cancel.IsCancellationRequested)
                            {
                                content.CancleCallback();
                                return;
                            }

                            content.DownloadCallback?.Invoke(byte_read);

                        } while (byte_read != 0);

                        //
                        //  Notify Complete
                        //

                        if (content.DownloadString)
                        {
                            if (content.Encoding == null)
                                content.CompleteCallbackString(Encoding.UTF8.GetString(((MemoryStream)ostream).ToArray()));
                            else
                                content.CompleteCallbackString(content.Encoding.GetString(((MemoryStream)ostream).ToArray()));
                        }
                        else if (content.MemoryCache)
                        {
                            content.CompleteCallbackBytes(((MemoryStream)ostream).ToArray());
                        }
                        else
                        {
                            content.CompleteCallback?.Invoke();
                        }

                        ostream.Close();
                        istream.Close();

                        return;
                    }
                }
            }
            catch (WebException e)
            {
                var response = (HttpWebResponse)e.Response;

                if (response != null && response.StatusCode == HttpStatusCode.Moved)
                {
                    if (content.AutoRedirection)
                    {
                        var old = content.Url;
                        content.Url = response.Headers.Get("Location");
                        Logger.Instance.Push("[NetField] Redirection " + old + " to " + content.Url);
                        goto REDIRECTION;
                    }
                }

                lock (Logger.Instance)
                {
                    Logger.Instance.PushError("[NetField] Web Excpetion - " + e.Message + "\r\n" + e.StackTrace);
                    Logger.Instance.PushError(content);
                }

                if (content.FailUrls != null && retry_count < content.FailUrls.Count)
                {
                    content.Url = content.FailUrls[retry_count++];
                    content.RetryCallback?.Invoke(retry_count);

                    lock (Logger.Instance)
                    {
                        Logger.Instance.Push($"[NetField] Retry [{retry_count}/{content.RetryCount}]");
                        Logger.Instance.Push(content);
                    }
                    goto RETRY_PROCEDURE;
                }

                if ((response != null && (
                    response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.InternalServerError)) ||
                    e.Status == WebExceptionStatus.NameResolutionFailure ||
                    e.Status == WebExceptionStatus.UnknownError)
                {
                    if (response != null && response.StatusCode == HttpStatusCode.Forbidden && response.Cookies != null)
                    {
                        content.CookieReceive?.Invoke(response.Cookies);
                        return;
                    }

                    //
                    //  Cannot continue
                    //

                    if (e.Status == WebExceptionStatus.UnknownError)
                    {
                        lock (Logger.Instance)
                        {
                            Logger.Instance.PushError("[NetField] Check your Firewall, Router or DPI settings.");
                            Logger.Instance.PushError("[NetField] If you continue to receive this error, please contact developer.");
                        }

                        content.ErrorCallback?.Invoke(NetTask.NetError.UnknowError);
                    }
                    else
                    {
                        content.ErrorCallback?.Invoke(NetTask.NetError.CannotContinueByCriticalError);
                    }

                    return;
                }
            }
            catch (UriFormatException e)
            {
                lock (Logger.Instance)
                {
                    Logger.Instance.PushError("[NetField] URI Exception - " + e.Message + "\r\n" + e.StackTrace);
                    Logger.Instance.PushError(content);
                }

                //
                //  Cannot continue
                //

                content.ErrorCallback?.Invoke(NetTask.NetError.UriFormatError);
                return;
            }
            catch (Exception e)
            {
                lock (Logger.Instance)
                {
                    Logger.Instance.PushError("[NetField] Unhandled Excpetion - " + e.Message + "\r\n" + e.StackTrace);
                    Logger.Instance.PushError(content);
                }
            }

            //
            //  Request Aborted
            //

            if (content.Aborted)
            {
                content.ErrorCallback?.Invoke(NetTask.NetError.Aborted);
                return;
            }

            //
            //  Retry
            //

            if (content.FailUrls != null && retry_count < content.FailUrls.Count)
            {
                content.Url = content.FailUrls[retry_count++];
                content.RetryCallback?.Invoke(retry_count);

                lock (Logger.Instance)
                {
                    Logger.Instance.Push($"[NetField] Retry [{retry_count}/{content.RetryCount}]");
                    Logger.Instance.Push(content);
                }
                goto RETRY_PROCEDURE;
            }

            if (content.RetryWhenFail)
            {
                if (content.RetryCount > retry_count)
                {
                    retry_count += 1;

                    content.RetryCallback?.Invoke(retry_count);

                    lock (Logger.Instance)
                    {
                        Logger.Instance.Push($"[NetField] Retry [{retry_count}/{content.RetryCount}]");
                        Logger.Instance.Push(content);
                    }
                    goto RETRY_PROCEDURE;
                }

                //
                //  Many retry
                //

                lock (Logger.Instance)
                {
                    Logger.Instance.Push($"[NetField] Many Retry");
                    Logger.Instance.Push(content);
                }
                content.ErrorCallback?.Invoke(NetTask.NetError.ManyRetry);
            }

            content.ErrorCallback?.Invoke(NetTask.NetError.Unhandled);
        }
    }

    public class NetScheduler : Scheduler<NetTask, NetPriority, NetField>
    {
        public NetScheduler(int capacity = 0, bool use_emergency_thread = false)
            : base(capacity, use_emergency_thread) { }
    }

    public class NetTools
    {
        public static NetScheduler Scheduler = new NetScheduler(Settings.Instance.Model.ThreadCount);

        public static List<string> DownloadStrings(List<string> urls, string cookie = "", Action complete = null)
        {
            var interrupt = new ManualResetEvent(false);
            var result = new string[urls.Count];
            var count = urls.Count;
            int iter = 0;

            foreach (var url in urls)
            {
                var itertmp = iter;
                var task = NetTask.MakeDefault(url);
                task.DownloadString = true;
                task.CompleteCallbackString = (str) =>
                {
                    result[itertmp] = str;
                    if (Interlocked.Decrement(ref count) == 0)
                        interrupt.Set();
                    complete?.Invoke();
                };
                task.ErrorCallback = (code) =>
                {
                    if (Interlocked.Decrement(ref count) == 0)
                        interrupt.Set();
                };
                task.Cookie = cookie;
                Scheduler.Add(task);
                iter++;
            }

            interrupt.WaitOne();

            return result.ToList();
        }

        public static List<string> DownloadStrings(List<NetTask> tasks, string cookie = "", Action complete = null)
        {
            var interrupt = new ManualResetEvent(false);
            var result = new string[tasks.Count];
            var count = tasks.Count;
            int iter = 0;

            foreach (var task in tasks)
            {
                var itertmp = iter;
                task.DownloadString = true;
                task.CompleteCallbackString = (str) =>
                {
                    result[itertmp] = str;
                    if (Interlocked.Decrement(ref count) == 0)
                        interrupt.Set();
                    complete?.Invoke();
                };
                task.ErrorCallback = (code) =>
                {
                    if (Interlocked.Decrement(ref count) == 0)
                        interrupt.Set();
                };
                task.Cookie = cookie;
                Scheduler.Add(task);
                iter++;
            }

            interrupt.WaitOne();

            return result.ToList();
        }

        public static string DownloadString(string url)
        {
            return DownloadStringAsync(NetTask.MakeDefault(url)).Result;
        }

        public static string DownloadString(NetTask task)
        {
            return DownloadStringAsync(task).Result;
        }

        public static async Task<string> DownloadStringAsync(NetTask task)
        {
            return await Task.Run(() =>
            {
                var interrupt = new ManualResetEvent(false);
                string result = null;

                task.DownloadString = true;
                task.CompleteCallbackString = (string str) =>
                {
                    result = str;
                    interrupt.Set();
                };

                task.ErrorCallback = (code) =>
                {
                    task.ErrorCallback = null;
                    interrupt.Set();
                };

                Scheduler.Add(task);

                interrupt.WaitOne();

                return result;
            }).ConfigureAwait(false);
        }

        public static List<string> DownloadFiles(List<(string, string)> url_path, string cookie = "", Action<long> download = null, Action complete = null)
        {
            var interrupt = new ManualResetEvent(false);
            var result = new string[url_path.Count];
            var count = url_path.Count;
            int iter = 0;

            foreach (var up in url_path)
            {
                var itertmp = iter;
                var task = NetTask.MakeDefault(up.Item1);
                task.SaveFile = true;
                task.Filename = up.Item2;
                task.DownloadCallback = (sz) =>
                {
                    download?.Invoke(sz);
                };
                task.CompleteCallback = () =>
                {
                    if (Interlocked.Decrement(ref count) == 0)
                        interrupt.Set();
                    complete?.Invoke();
                };
                task.ErrorCallback = (code) =>
                {
                    if (Interlocked.Decrement(ref count) == 0)
                        interrupt.Set();
                };
                task.Cookie = cookie;
                Scheduler.Add(task);
                iter++;
            }

            interrupt.WaitOne();

            return result.ToList();
        }

        public static void DownloadFile(string url, string filename)
        {
            var task = NetTask.MakeDefault(url);
            task.SaveFile = true;
            task.Filename = filename;
            task.Priority = new NetPriority { Type = NetPriorityType.Low };
            DownloadFileAsync(task).Wait();
        }

        public static void DownloadFile(NetTask task)
        {
            DownloadFileAsync(task).Wait();
        }

        public static async Task DownloadFileAsync(NetTask task)
        {
            await Task.Run(() =>
            {
                var interrupt = new ManualResetEvent(false);

                task.SaveFile = true;
                task.CompleteCallback = () =>
                {
                    interrupt.Set();
                };

                task.ErrorCallback = (code) =>
                {
                    task.ErrorCallback = null;
                    interrupt.Set();
                };

                Scheduler.Add(task);

                interrupt.WaitOne();
            }).ConfigureAwait(false);
        }

        public static byte[] DownloadData(string url)
        {
            return DownloadDataAsync(NetTask.MakeDefault(url)).Result;
        }

        public static byte[] DownloadData(NetTask task)
        {
            return DownloadDataAsync(task).Result;
        }

        public static async Task<byte[]> DownloadDataAsync(NetTask task)
        {
            return await Task.Run(() =>
            {
                var interrupt = new ManualResetEvent(false);
                byte[] result = null;

                task.MemoryCache = true;
                task.CompleteCallbackBytes = (byte[] bytes) =>
                {
                    result = bytes;
                    interrupt.Set();
                };

                task.ErrorCallback = (code) =>
                {
                    task.ErrorCallback = null;
                    interrupt.Set();
                };

                Scheduler.Add(task);

                interrupt.WaitOne();

                return result;
            }).ConfigureAwait(false);
        }

        public static Stream RequestStream(string url)
        {
            return RequestStreamAsync(NetTask.MakeDefault(url)).Result;
        }

        public static async Task<Stream> RequestStreamAsync(NetTask task)
        {
            return await Task.Run(() =>
            {
                var interrupt = new ManualResetEvent(false);
                Stream result = null;

                task.GetStream = true;
                task.CompleteCallbackStream = (Stream stream) =>
                {
                    result = stream;
                    interrupt.Set();
                };

                task.ErrorCallback = (code) =>
                {
                    task.ErrorCallback = null;
                    interrupt.Set();
                };

                Scheduler.Add(task);

                interrupt.WaitOne();

                return result;
            }).ConfigureAwait(false);
        }
    }

    public class NetCommon
    {
        public static WebClient GetDefaultClient()
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.139 Safari/537.36");
            return wc;
        }

        public static string DownloadString(string url)
        {
            Logger.Instance.Push($"Download string: {url}");
            return GetDefaultClient().DownloadString(url);
        }

    }

    public class DCInsideArticle
    {
        public string Id { get; set; }
        public string GalleryName { get; set; }
        public string OriginalGalleryName { get; set; }
        public string Thumbnail { get; set; }
        public string Class { get; set; }
        public string Title { get; set; }
        public string Contents { get; set; }
        public List<string> ImagesLink { get; set; }
        public List<string> FilesName { get; set; }
        public string Archive { get; set; }
        public string ESNO { get; set; }
        public string Views { get; set; }
        public string ReplyCount { get; set; }
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

    public class DCInsideGallery
    {
        public string id;
        public string name;
        public string esno;
        public string cur_page;
        public string max_page;
        public DCInsidePageArticle[] articles;
    }

    public class DCInsideCommentElement
    {
        public string no;
        public string parent;
        public string user_id;
        public string name;
        public string ip;
        public string reg_date;
        public string nicktype;
        public string t_ch1;
        public string t_ch2;
        public string vr_type;
        public string voice;
        public string rcnt;
        public string c_no;
        public int depth;
        public string del_yn;
        public string is_delete;
        public string memo;
        public string my_cmt;
        public string del_btn;
        public string mod_btn;
        public string a_my_cmt;
        public string reply_w;
        public string gallog_icon;
        public string vr_player;
        public string vr_player_tag;
        public int next_type;
    }

    public class DCInsideComment
    {
        public int total_cnt;
        public int comment_cnt;
        public List<DCInsideCommentElement> comments;
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

    public class DCInsideManager : ILazy<DCInsideManager>
    {
        public string ESNO { get; set; }

        public DCInsideManager()
        {
            ESNO = DCInsideUtils.ParseGallery(NetTools.DownloadString("https://gall.dcinside.com/board/lists?id=hit")).esno;
        }
    }

    public class DCInsideUtils
    {
        public static async Task<DCInsideComment> GetComments(DCInsideArticle article, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", article.OriginalGalleryName },
                { "no", article.Id },
                { "cmt_id", article.OriginalGalleryName },
                { "cmt_no", article.Id },
                { "e_s_n_o", article.ESNO },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<DCInsideComment> GetComments(DCInsideGallery g, DCInsidePageArticle article, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", g.id },
                { "no", article.no },
                { "cmt_id", g.id },
                { "cmt_no", article.no },
                { "e_s_n_o", g.esno },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<DCInsideComment> GetComments(string gall_id, string article_id, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", gall_id },
                { "no", article_id },
                { "cmt_id", gall_id },
                { "cmt_no", article_id },
                { "e_s_n_o", DCInsideManager.Instance.ESNO },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<List<DCInsideCommentElement>> GetAllComments(string gall_id, string article_id)
        {
            var first = await GetComments(gall_id, article_id, "1");
            TidyComments(ref first);
            if (first.comments.Count == first.total_cnt)
                return first.comments.ToList();
            var cur = first.comments.Count;
            int iter = 2;
            while (cur < first.total_cnt)
            {
                var ll = await GetComments(gall_id, article_id, iter++.ToString());
                TidyComments(ref ll);
                cur += ll.comments.Count;
                first.comments.AddRange(ll.comments);
            }
            return first.comments;
        }

        public static void TidyComments(ref DCInsideComment comment)
        {
            for (int i = 0; i < comment.comments.Count; i++)
                if (comment.comments[i].nicktype == "COMMENT_BOY")
                    comment.comments.RemoveAt(i--);
        }

        /// <summary>
        /// 일반 갤러리의 리스트를 가져옵니다.
        /// </summary>
        /// <returns></returns>
        public static SortedDictionary<string, string> GetGalleryList()
        {
            var dic = new SortedDictionary<string, string>();
            var src = NetTools.DownloadString("http://wstatic.dcinside.com/gallery/gallindex_iframe_new_gallery.html");

            var parse = new List<Match>();
            parse.AddRange(Regex.Matches(src, @"onmouseover=""gallery_view\('(\w+)'\);""\>[\s\S]*?\<.*?\>([\w\s]+)\<").Cast<Match>().ToList());
            parse.AddRange(Regex.Matches(src, @"onmouseover\=""gallery_view\('(\w+)'\);""\>\s*([\w\s]+)\<").Cast<Match>().ToList());

            foreach (var match in parse)
            {
                var identification = match.Groups[1].Value;
                var name = match.Groups[2].Value.Trim();

                if (!string.IsNullOrEmpty(name))
                {
                    if (name[0] == '-')
                        name = name.Remove(0, 1).Trim();
                    if (!dic.ContainsKey(name))
                        dic.Add(name, identification);
                }
            }

            return dic;
        }

        public static SortedDictionary<string, string> GetMinorGalleryList()
        {
            var dic = new SortedDictionary<string, string>();
            var html = NetTools.DownloadString("https://gall.dcinside.com/m");

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            foreach (var a in document.DocumentNode.SelectNodes("//a[@onmouseout='thumb_hide();']"))
                dic.Add(a.InnerText.Trim(), a.GetAttributeValue("href", "").Split('=').Last());

            var under_name = new List<string>();
            foreach (var b in document.DocumentNode.SelectNodes("//button[@class='btn_cate_more']"))
                under_name.Add(b.GetAttributeValue("data-lyr", ""));

            int count = 1;
            foreach (var un in under_name)
            {
            RETRY:
                //var wc = NetCommon.GetDefaultClient();
                //wc.Headers.Add("X-Requested-With", "XMLHttpRequest");
                //wc.QueryString.Add("under_name", un);
                //var subhtml = Encoding.UTF8.GetString(wc.UploadValues("https://gall.dcinside.com/ajax/minor_ajax/get_under_gall", "POST", wc.QueryString));
                var subhtml = NetTools.DownloadString($"https://wstatic.dcinside.com/gallery/mgallindex_underground/{un}.html");
                if (subhtml.Trim() == "")
                {
                    Console.WriteLine($"[{count}/{under_name.Count}] Retry {un}...");
                    goto RETRY;
                }

                HtmlDocument document2 = new HtmlDocument();
                document2.LoadHtml(subhtml);
                foreach (var c in document2.DocumentNode.SelectNodes("//a[@class='list_title']"))
                    if (!dic.ContainsKey(c.InnerText.Trim()))
                        dic.Add(c.InnerText.Trim(), c.GetAttributeValue("href", "").Split('=').Last());
                Console.WriteLine($"[{count++}/{under_name.Count}] Complete {un}");
            }

            return dic;
        }

        public static DCInsideArticle ParseBoardView(string html, bool is_minor = false)
        {
            DCInsideArticle article = new DCInsideArticle();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//div[@class='view_content_wrap']")[0];

            article.Id = Regex.Match(html, @"name=""gallery_no"" value=""(\d+)""").Groups[1].Value;
            article.GalleryName = Regex.Match(html, @"<h4 class=""block_gallname"">\[(.*?) ").Groups[1].Value;
            article.OriginalGalleryName = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            if (is_minor)
                article.Class = node.SelectSingleNode("//span[@class='title_headtext']").InnerText;
            article.Contents = node.SelectSingleNode("//div[@class='writing_view_box']").InnerHtml;
            article.Title = HttpUtility.HtmlDecode(node.SelectSingleNode("//span[@class='title_subject']").InnerText);
            article.Views = node.SelectSingleNode("//span[@class='gall_count']").InnerText.Trim().Split(' ').Last().Replace(",", "");
            article.ReplyCount = node.SelectSingleNode("//span[@class='gall_reply_num']").InnerText.Trim().Split(' ').Last().Replace(",", "");
            article.CommentCount = node.SelectSingleNode("//span[@class='gall_comment']").InnerText.Trim().Split(' ').Last().Replace(",", "");
            try
            {
                article.ImagesLink = node.SelectNodes("//ul[@class='appending_file']/li").Select(x => x.SelectSingleNode("./a").GetAttributeValue("href", "")).ToList();
                article.FilesName = node.SelectNodes("//ul[@class='appending_file']/li").Select(x => x.SelectSingleNode("./a").InnerText).ToList();
            }
            catch { }
            article.ESNO = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");

            return article;
        }

        public static DCInsideGallery ParseGallery(string html)
        {
            var gall = new DCInsideGallery();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//tbody")[0];

            gall.id = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            gall.name = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", "");
            gall.esno = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");
            gall.cur_page = document.DocumentNode.SelectSingleNode("//div[@class='bottom_paging_box']/em").InnerText;
            try { gall.max_page = document.DocumentNode.SelectSingleNode("//a[@class='page_end']").GetAttributeValue("href", "").Split('=').Last(); } catch { }

            var pas = new List<DCInsidePageArticle>();

            foreach (var tr in node.SelectNodes("./tr"))
            {
                var gall_num = tr.SelectSingleNode("./td[1]").InnerText;
                int v;
                if (!int.TryParse(gall_num, out v)) continue;

                var pa = new DCInsidePageArticle();
                pa.no = gall_num;
                pa.type = tr.SelectSingleNode("./td[2]/a/em").GetAttributeValue("class", "").Split(' ')[1];
                pa.title = HttpUtility.HtmlDecode(tr.SelectSingleNode("./td[2]/a").InnerText);
                try { pa.replay_num = tr.SelectSingleNode(".//span[@class='reply_num']").InnerText; } catch { }
                pa.nick = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-nick", "");
                pa.uid = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-uid", "");
                pa.ip = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-ip", "");
                if (pa.ip == "")
                {
                    pa.islogined = true;
                    if (tr.SelectSingleNode("./td[3]/a/img") != null && tr.SelectSingleNode("./td[3]/a/img").GetAttributeValue("src", "").Contains("fix_nik.gif"))
                        pa.isfixed = true;
                }
                pa.date = DateTime.Parse(tr.SelectSingleNode("./td[4]").GetAttributeValue("title", ""));
                pa.count = tr.SelectSingleNode("./td[5]").InnerText;
                pa.recommend = tr.SelectSingleNode("./td[6]").InnerText;

                pas.Add(pa);
            }

            gall.articles = pas.ToArray();

            return gall;
        }

        public static DCInsideGallery ParseMinorGallery(string html)
        {
            var gall = new DCInsideGallery();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//tbody")[0];

            gall.id = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            gall.name = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", "");
            gall.esno = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");
            gall.cur_page = document.DocumentNode.SelectSingleNode("//div[@class='bottom_paging_box']/em").InnerText;
            try { gall.max_page = document.DocumentNode.SelectSingleNode("//a[@class='page_end']").GetAttributeValue("href", "").Split('=').Last(); } catch { }

            List<DCInsidePageArticle> pas = new List<DCInsidePageArticle>();

            foreach (var tr in node.SelectNodes("./tr"))
            {
                try
                {
                    var gall_num = tr.SelectSingleNode("./td[1]").InnerText;
                    int v;
                    if (!int.TryParse(gall_num, out v)) continue;

                    var pa = new DCInsidePageArticle();
                    pa.no = gall_num;
                    pa.classify = tr.SelectSingleNode("./td[2]").InnerText;
                    pa.type = tr.SelectSingleNode("./td[3]/a/em").GetAttributeValue("class", "").Split(' ')[1];
                    pa.title = HttpUtility.HtmlDecode(tr.SelectSingleNode("./td[3]/a").InnerText);
                    try { pa.replay_num = tr.SelectSingleNode(".//span[@class='reply_num']").InnerText; } catch { }
                    pa.nick = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-nick", "");
                    pa.uid = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-uid", "");
                    pa.ip = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-ip", "");
                    if (pa.ip == "")
                    {
                        pa.islogined = true;
                        if (tr.SelectSingleNode("./td[4]/a/img") != null && tr.SelectSingleNode("./td[4]/a/img").GetAttributeValue("src", "").Contains("fix_nik.gif"))
                            pa.isfixed = true;
                    }
                    pa.date = DateTime.Parse(tr.SelectSingleNode("./td[5]").GetAttributeValue("title", ""));
                    pa.count = tr.SelectSingleNode("./td[6]").InnerText;
                    pa.recommend = tr.SelectSingleNode("./td[7]").InnerText;

                    pas.Add(pa);
                }
                catch { }
            }

            gall.articles = pas.ToArray();

            return gall;
        }
    }

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

    class Program
    {
        static void Main(string[] args)
        {
            DCGalleryAnalyzer.Instance.Open("툴리우스갤 데이터.txt");

            var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var sp = Path.Combine(Directory.GetCurrentDirectory(), "Archive",
                $"{DCGalleryAnalyzer.Instance.Model.gallery_name} ({DCGalleryAnalyzer.Instance.Model.gallery_id})");

            Console.WriteLine(sp);

            Directory.CreateDirectory(sp);
            int i = 0;

            for (; Convert.ToInt32(DCGalleryAnalyzer.Instance.Articles[i].no) > 400681; i++)
            {
                var article = DCGalleryAnalyzer.Instance.Articles[i];
                var ttitle = $"{article.title}";
                foreach (char c in invalid)
                    ttitle = ttitle.Replace(c.ToString(), "");

                string url;

                if (DCGalleryAnalyzer.Instance.Model.is_minor_gallery)
                    url = $"https://gall.dcinside.com/mgallery/board/view/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&no={article.no}";
                else
                    url = $"https://gall.dcinside.com/board/view/?id={DCGalleryAnalyzer.Instance.Model.gallery_id}&no={article.no}";

                var html = NetTools.DownloadString(url);
                if (string.IsNullOrEmpty(html))
                    continue;
                var info = DCInsideUtils.ParseBoardView(html, DCGalleryAnalyzer.Instance.Model.is_minor_gallery);

                File.WriteAllText(Path.Combine(sp, $"[{article.no}]-body-{ttitle}.json"), JsonConvert.SerializeObject(info, Formatting.Indented));

                int com;
                if (int.TryParse(info.CommentCount.Replace(",", ""), out com) && com > 0)
                {
                    try
                    {
                        var comments = DCInsideUtils.GetAllComments(DCGalleryAnalyzer.Instance.Model.gallery_id, article.no).Result;
                        File.WriteAllText(Path.Combine(sp, $"[{article.no}]-comments-{ttitle}.json"), JsonConvert.SerializeObject(comments, Formatting.Indented));
                    }
                    catch { }
                }

                Thread.Sleep(700);
            }
        }
    }
}
