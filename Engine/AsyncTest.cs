using System;
using System.Collections.Concurrent;
using System.Threading;

#nullable enable

namespace LibSnitcher;

public class TestWorker
{
    private readonly ConcurrentDictionary<Guid, Work> _completed;
    private int _panic;
    private int _repetition;

    public TestWorker()
    {
        _completed = new();
        _panic = 0;
    }

    public void EnqueueWork(string name, Guid id)
    {
        if (_panic >= 10)
            Environment.Exit(666);

        Work work = new()
        {
            Name = name,
            Id = id
        };

        ThreadPool.QueueUserWorkItem(DoWork, work);
    }

    public void DoWork(object? obj)
    {
        Work work = obj as Work;
        if (_completed.TryGetValue(work.Id, out Work existing))
        {
            Thread.Sleep(1000);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Found work: {existing.Name} Id: {existing.Id}");
            Console.ResetColor();
            if (_repetition >= 2)
            {
                EnqueueWork($"{work.Name}+", Guid.NewGuid());
                _repetition = 0;
            }
            else
            {
                EnqueueWork(work.Name, work.Id);
                _repetition++;
            }
            _panic++;
            return;
        }

        if (_completed.TryAdd(work.Id, work))
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Added work: {work.Name} Id: {work.Id}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine($"Normal processing: {work.Name} Id: {work.Id}");
        Console.ResetColor();

        for (int i = 0; i < 10; i++)
        {
            if (i == 1)
                EnqueueWork(work.Name, work.Id);
            else
                EnqueueWork($"{work.Name}+", Guid.NewGuid());
        }

        Thread.Sleep(1000);
        if (_repetition >= 2)
        {
            EnqueueWork($"{work.Name}+", Guid.NewGuid());
            _repetition = 0;
        }
        else
        {
            EnqueueWork(work.Name, work.Id);
            _repetition++;
        }
        _panic++;
    }
}

public class TestWork
{
    public string Name;
    public int Id;
}