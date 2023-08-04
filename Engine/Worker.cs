using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Management.Automation;
using System.Collections.Concurrent;
using LibSnitcher.Core;

namespace LibSnitcher
{
    internal delegate void WriteProgress(int id, string activity, string status);
    internal delegate void WorkComplete(List<Work> all_work);

    public class Helper
    {
        private readonly PSCmdlet _context;
        private readonly HashSet<Tuple<string, Guid>> _printed;

        private List<Work> _dep_chain;
        private bool _completed;

        private int _id;
        private string _activity;
        private string _status;

        public Helper(PSCmdlet context)
        {
            _context = context;
            _dep_chain = new();
            _printed = new();
            _completed = false;

            _id = 0;
            _activity = "Listing dependency chain";
            _status = "Initializing...";

            // Console.WriteLine("Helper constructor");
        }

        public void GetDependencyChainList(string lib_name, int max_concurrent_tasks)
        {
            // Console.WriteLine("Helper.GetDependencyChainList");
            Worker worker = new(max_concurrent_tasks, OnWorkComplete, OnWriteProgress);
            worker.EnqueueWork(lib_name, string.Empty, 0, DependencySource.None, Guid.NewGuid(), Guid.NewGuid());

            //int dot_count = 1;
            int progress_time = 0;
            do
            {
                // if (progress_time >= 750)
                // {
                //     if (dot_count >= 3)
                //         dot_count = 1;
                //     else
                //         dot_count++;

                //     progress_time = 0;
                // }

                // StringBuilder buffer = new();
                // buffer.Append($"Listing dependency chain");
                // buffer.Append('.', dot_count);
                _context.WriteProgress(new(_id, _activity, _status));

                Thread.Sleep(10);
                progress_time += 10;

            } while (!_completed);

            GetTextListFromWorkList(_dep_chain);
        }

        private void GetTextListFromWorkList(List<Work> work_list)
        {
            foreach (Work work in from w in work_list orderby w.Depth select w)
            {
                Tuple<string, Guid> current = new(work.Name, work.Id);
                if (_printed.Contains(current))
                    continue;

                // Printing the parent.
                PrintWork(work);
                _printed.Add(current);

                // Printing the children.
                List<Work> children = work_list.Where(w => w.ParentId == work.Id).ToList();
                GetTextListFromWorkList(children);
            }
        }

        private void PrintWork(Work work)
        {
            string text = string.Concat(Enumerable.Repeat("  ", work.Depth));
            text = string.Join("", text, $"{work.Name} (Id: {work.Id};Loaded: {work.Loaded}; Parent Id: {work.ParentId};Parent: {work.Parent}): {work.Path}");
            // text = string.Join("", text, $"{work.Name} (Loaded: {work.Loaded}; Parent: {work.Parent}): {work.Path}");

            _context.WriteObject(text);
        }

        private void OnWorkComplete(List<Work> dep_chain)
            => (_completed, _dep_chain) = (true, dep_chain);

        private void OnWriteProgress(int id, string activity, string status)
            => (_id, _activity, _status) = (id, activity, status);
    }

    internal class Worker
    {
        private readonly List<Work> _result;
        private readonly WorkComplete CompletedCallback;
        private readonly WriteProgress ProgressCallback;
        private readonly ConcurrentDictionary<string, Work> _completed_unique;
        private readonly List<string> _active;
        private readonly List<Work> _orphans;

        private readonly SemaphoreSlim _semaphore;

        private int _queued_count;
        private int _completed_count;

        internal Worker(int max_thread_count, WorkComplete completed_callback, WriteProgress progress_callback)
        {
            _queued_count = 0;
            _completed_count = 0;

            _result = new();
            _active = new();
            _orphans = new();
            _completed_unique = new(StringComparer.Ordinal);

            _semaphore = new(max_thread_count);
            _semaphore.Release(max_thread_count);

            CompletedCallback = completed_callback;
            ProgressCallback = progress_callback;

            Task.Run(() => {
                lock (_orphans)
                {
                    if (_orphans.Count > 0)
                    {
                        foreach (Work orphan in _orphans)
                            if (_completed_unique.TryGetValue(orphan.Name, out Work father))
                            {
                                orphan.Path = father.Path;
                                orphan.Loaded = father.Loaded;
                                orphan.LoaderException = father.LoaderException;

                                lock (_result)
                                    _result.Add(orphan);

                                _orphans.Remove(orphan);
                            }
                    }
                }

                Task.Delay(1000);
            });
        }

        internal void EnqueueWork(string lib_name, string parent, int depth, DependencySource source, Guid id, Guid parent_id)
        {
            Work work = new()
            {
                Id = id,
                ParentId = parent_id,
                Name = lib_name,
                Parent = parent,
                Depth = depth,
                Source = source,
                State = WorkState.Queued,
                Dependencies = new()
            };

            lock (_active)
            {
                if (_active.Contains(work.Name))
                {
                    if (_completed_unique.TryGetValue(work.Name, out Work existent))
                    {
                        work.Path = existent.Path;
                        work.Loaded = existent.Loaded;
                        work.LoaderException = existent.LoaderException;

                        lock (_result)
                            _result.Add(work);
                    }
                    else
                        lock (_orphans)
                            _orphans.Add(work);

                    return;
                }

                _active.Add(work.Name);
            }

            _queued_count++;
            ThreadPool.QueueUserWorkItem(DoWork, work);
        }

        private void DoWork(object obj)
        {
            // Wait for the semaphore.
            _semaphore.Wait();

            Work work = obj as Work;

            // Getting dependency information.
            Random random = new();
            Wrapper unwrapper = new();

            LibInfo info = unwrapper.GetDependencyList(work.Name, work.Source);

            work.Name = info.Name;
            work.Path = info.Path;
            work.Loaded = info.Loaded;
            work.LoaderException = info.LoaderError;

            _completed_unique.TryAdd(work.Name, work);

            // Creating an id, and task for each dependency.
            if (info.Dependencies is not null && info.Dependencies.Length > 0)
            {
                work.Dependencies = new(info.Dependencies);
                foreach (DependencyEntry entry in info.Dependencies)
                    EnqueueWork(entry.Name, work.Name, work.Depth + 1, entry.Source, Guid.NewGuid(), work.Id);
            }

            // Storing the result.
            lock (_result)
                _result.Add(work);

            // Checking if there are any pending tasks.
            _completed_count++;
            if (_completed_count == _queued_count)
                CompletedCallback(_result);

            // Progress test.
            ProgressCallback(0, "Listing dependency chain", $"Queued: {_queued_count}; Completed: {_completed_count}");

            _semaphore.Release();
        }
    }

    public class Work
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int Depth { get; set; }
        public string Parent { get; set; }
        public bool Loaded { get; set; }
        public Exception LoaderException { get; set; }
        public List<DependencyEntry> Dependencies { get; set; }
        public WorkState State { get; set; }
        public DependencySource Source { get; set; }
    }

    public enum WorkState
    {
        Queued,
        Running,
        Completed
    }

    public class GuidComparer : IComparer<Guid>, IEqualityComparer<Guid>
    {
        internal static readonly GuidComparer Instance = new();

        public int Compare(Guid x, Guid y)
        {
            if (x == y)
                return 0;

            return -1;
        }

        public bool Equals(Guid x, Guid y)
            => x == y;

        public int GetHashCode(Guid obj)
            => obj.GetHashCode();
    }
}