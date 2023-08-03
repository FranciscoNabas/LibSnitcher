using System;
using System.Collections.Concurrent;
using System.Threading;

#nullable enable

namespace LibSnitcher;

public class TestWork
{
    ConcurrentDictionary<int, object> _completed;

    public void ContinueWith(Action<TestWork> action)
    {
        
    }
}