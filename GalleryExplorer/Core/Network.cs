// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GalleryExplorer.Core
{
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

        /* Callback Functions */

        public Action<long> SizeCallback;
        public Action<long> DownloadCallback;
        public Action StartCallback;
        public Action CompleteCallback;
        public Action<string> CompleteCallbackString;
        public Action<byte[]> CompleteCallbackBytes;
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
}
