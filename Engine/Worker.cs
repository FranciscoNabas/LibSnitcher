using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Management.Automation;
using System.Collections.Concurrent;
using LibSnitcher.Core;

namespace LibSnitcher
{
    internal delegate void WriteProgress(int id, string activity, string status);
    internal delegate void WorkCompleted(List<Work> all_work);

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
            Worker worker = new(max_concurrent_tasks, OnWriteProgress, OnWorkComplete);
            worker.TriggerChainListing(lib_name);

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
        private readonly Wrapper _unwrapper;
        private readonly ConcurrentBag<Task> _tasks;
        private readonly ConcurrentDictionary<string, Work> _result;
        private readonly List<Tuple<int, Work>> _queue;

        private readonly WriteProgress ProgressCallback;
        private readonly WorkCompleted CompletedCallback;

        private int _queued_count;
        private int _completed_count;

        internal Worker(int max_concurrent_task, WriteProgress progress_callback, WorkCompleted completed_callback)
        {
            ProgressCallback = progress_callback;
            CompletedCallback = completed_callback;

            _queued_count = 0;
            _completed_count = 0;

            _queue = new();
            _tasks = new();
            _result = new();
            _unwrapper = new();

            ThreadPool.SetMaxThreads(max_concurrent_task + 2, max_concurrent_task + 2);
        }

        internal void TriggerChainListing(string module_name)
        {
            _tasks.Add(Task.Run(() => { DoWork(new(Guid.NewGuid(), Guid.Empty, module_name, string.Empty, DependencySource.None, 0)); }));
            Task.WhenAll(_tasks);
        }

        private void QueueManager()
        {
            var queue_copy = _queue;
            bool first = true;
            int current_depth = 0;
            foreach (Tuple<int, Work> position in queue_copy.OrderBy(p => p.Item1))
            {
                if (first)
                {
                    current_depth = position.Item1;
                }
            }
        }

        private void DoWork(Work work)
        {
            _queued_count++;
            LibInfo info = _unwrapper.GetDependencyList(work.Name, work.Source);
            
            work.Name = info.Name;
            work.Path = info.Path;
            work.Loaded = info.Loaded;
            work.LoaderException = info.LoaderError;

            _result.TryAdd(work.Name, work);

            if (info.Dependencies is not null)
            {
                foreach (DependencyEntry entry in info.Dependencies)
                {
                    if (_result.TryGetValue(entry.Name, out Work existent))
                    {
                        _queued_count++;
                        existent.Recurrences.Add(new ModuleRecurrence(work.Id, work.Depth + 1));
                        _completed_count++;

                        // string dep_text = string.Concat(Enumerable.Repeat("  ", work.Depth + 1));
                        // Console.WriteLine(string.Join("", dep_text, $"{existent.Name} (Id: {existent.Id};Loaded: {existent.Loaded}; Parent Id: {existent.ParentId};Parent: {existent.Parent}): {existent.Path}"));
                        ProgressCallback(0, "Listing dependency chain", $"Queued: {_queued_count}. Completed: {_completed_count}.");
                        continue;
                    }

                    DoWork(new(Guid.NewGuid(), work.Id, entry.Name, work.Name, entry.Source, work.Depth + 1));
                    // _tasks.Add(Task.Run(() => { DoWork(new(Guid.NewGuid(), work.Id, entry.Name, work.Name, entry.Source, work.Depth + 1)); }));
                }
            }

            _completed_count++;
            
            // string text = string.Concat(Enumerable.Repeat("  ", work.Depth));
            // Console.WriteLine(string.Join("", text, $"{work.Name} (Id: {work.Id};Loaded: {work.Loaded}; Parent Id: {work.ParentId};Parent: {work.Parent}): {work.Path}"));

            ProgressCallback(0, "Listing dependency chain", $"Queued: {_queued_count}. Completed: {_completed_count}.");
        }
    }
    
    internal class Work
    {
        internal Guid Id { get; }
        internal Guid ParentId { get; }

        internal string Name { get; set; }
        internal string Parent { get; }
        internal DependencySource Source { get; }
        internal int Depth { get; }

        internal string Path { get; set; }
        internal bool Loaded { get; set; }
        internal Exception LoaderException { get; set; }

        internal List<ModuleRecurrence> Recurrences { get; }

        internal Work(Guid id, Guid parent_id, string name, string parent, DependencySource source, int depth)
            => (Id, ParentId, Name, Parent, Source, Depth, Recurrences) = (id, parent_id, name, parent, source, depth, new());
    }

    internal class ModuleRecurrence
    {
        internal Guid ParentId { get; }
        internal int Depth { get; }

        internal ModuleRecurrence(Guid parent_id, int depth)
            => (ParentId, Depth) = (parent_id, depth);
    }
}