using System;
using System.Collections.Generic;
using Dalamud.Logging;

namespace PullLogger;

/**
 * Dirt simple service container that only fixes my lazyness.
 */
public class Container
{
    private readonly Dictionary<Type, object> _container = new();
    private readonly List<IDisposable> _disposables = new();

    public void Register<TRegister>(TRegister instance) where TRegister : class
    {
        _container.Add(typeof(TRegister), instance);
    }

    public void RegisterDisposable<TRegister>(TRegister instance) where TRegister : IDisposable
    {
        _container.Add(typeof(TRegister), instance);
        _disposables.Add(instance);
    }

    public TRegister Resolve<TRegister>() where TRegister : class
    {
        return _container[typeof(TRegister)] as TRegister ?? throw new InvalidOperationException();
    }

    public void DoDispose()
    {
        _disposables.Reverse();
        foreach (var disposable in _disposables)
        {
            PluginLog.Information("dispose " + disposable.GetType());
            disposable.Dispose();
        }
    }
}