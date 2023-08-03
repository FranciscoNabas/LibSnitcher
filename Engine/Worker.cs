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
            Worker.GetWorker(max_concurrent_tasks, OnWorkComplete, OnWriteProgress).EnqueueWork(lib_name, string.Empty, 0, DependencySource.None, Guid.NewGuid(), Guid.NewGuid());

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
        private static Worker _instance;

        private readonly List<Work> _result;
        private readonly WorkComplete CompletedCallback;
        private readonly WriteProgress ProgressCallback;
        private readonly ConcurrentDictionary<Guid, Work> _completed_unique;

        private readonly SemaphoreSlim _semaphore;

        private int _queued_count;
        private int _completed_count;

        private Worker(int max_thread_count, WorkComplete completed_callback, WriteProgress progress_callback)
        {
            _queued_count = 0;
            _completed_count = 0;

            _result = new();
            _completed_unique = new();

            _semaphore = new(max_thread_count);
            _semaphore.Release(max_thread_count);

            CompletedCallback = completed_callback;
            ProgressCallback = progress_callback;
        }

        internal static Worker GetWorker(int max_thread_count, WorkComplete complete_callback, WriteProgress progress_callback)
        {
            _instance ??= new(max_thread_count, complete_callback, progress_callback);
            return _instance;
        }

        internal void EnqueueWork(string lib_name, string parent, int depth, DependencySource source, Guid id, Guid parent_id)
        {
            Work work = new() {
                Id = id,
                ParentId = parent_id,
                Name = lib_name,
                Parent = parent,
                Depth = depth,
                Source = source,
                State = WorkState.Queued,
                Dependencies = new()
            };

            Task task = DoWork(work);
            task.Wait();
        }

        private async Task DoWork(object obj)
        {
            _instance._queued_count++;

            // Wait for the semaphore.
            _instance._semaphore.Wait();

            Work work = obj as Work;
            if (TryReadFromCompleted(work, out Work existent))
            {
                work.Path = existent.Path;
                work.Loaded = existent.Loaded;
                work.LoaderException = existent.LoaderException;
            
                lock (_instance._result)
                    _instance._result.Add(work);
            
                return;
            }
            
            TryAddToCompleted(work);

            // Getting dependency information.
            Random random = new();
            Wrapper unwrapper = new();

            LibInfo info = unwrapper.GetDependencyList(work.Name, work.Source);

            work.Name = info.Name;
            work.Path = info.Path;
            work.Loaded = info.Loaded;
            work.LoaderException = info.LoaderError;

            // Creating an id, and task for each dependency.
            if (info.Dependencies is not null && info.Dependencies.Length > 0)
            {
                work.Dependencies = new(info.Dependencies);
                foreach (DependencyEntry entry in info.Dependencies)
                {
                    Work dep_work = new() {
                        Id = Guid.NewGuid(),
                        ParentId = work.Id,
                        Name = entry.Name,
                        Depth = work.Depth + 1,
                        Dependencies = new(),
                        Source = entry.Source
                    };

                    await DoWork(dep_work);
                }
            }

            // Storing the result.
            lock (_instance._result)
                _instance._result.Add(work);

            // Checking if there are any pending tasks.
            _instance._completed_count++;
            if (_instance._completed_count == _instance._queued_count)
                _instance.CompletedCallback(_instance._result);

            // Progress test.
            ProgressCallback(0, "Listing dependency chain", $"Queued: {_instance._queued_count}; Completed: {_instance._completed_count}");

            _semaphore.Release();
        }

        private bool TryAddToCompleted(Work work)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Storing work. Lib: {work.Name}.");
            return _completed_unique.TryAdd(work.Id, work);
        }

        private bool TryReadFromCompleted(Work work, out Work output)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Reading work. Lib: {work.Name}.");
            return _completed_unique.TryGetValue(work.Id, out output);
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
}