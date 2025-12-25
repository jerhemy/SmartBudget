using Microsoft.Extensions.DependencyInjection;
using SmartBudget.WinForms.Abstractions;
using SmartBudget.WinForms.Pages;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.WinForms.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly Panel _host;
    private readonly IReadOnlyDictionary<PageKey, Func<IPage>> _factories;

    private readonly Dictionary<PageKey, IPage> _cache = new();

    private IPage? _current;

    public PageKey Current { get; private set; }

    public NavigationService(Panel host, IReadOnlyDictionary<PageKey, Func<IPage>> factories)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _factories = factories ?? throw new ArgumentNullException(nameof(factories));

        // helps in general
        _host.SuspendLayout();
        _host.Controls.Clear();
        _host.ResumeLayout();
    }

    public void Navigate(PageKey key, NavigationContext? ctx = null)
    {
        ctx ??= new NavigationContext();

        // If we’re already on this page, DON’T redraw host. Just pass args.
        if (_current is not null && Current == key)
        {
            _current.OnNavigatedTo(ctx);
            return;
        }

        if (_current is not null && !_current.CanNavigateAway())
            return;

        var page = GetOrCreate(key);

        // Swap only when changing page
        _host.SuspendLayout();
        try
        {
            _host.Controls.Clear();

            var view = page.View;
            view.Dock = DockStyle.Fill;
            _host.Controls.Add(view);
        }
        finally
        {
            _host.ResumeLayout(true);
        }

        _current = page;
        Current = key;

        page.OnNavigatedTo(ctx);
    }

    private IPage GetOrCreate(PageKey key)
    {
        if (_cache.TryGetValue(key, out var existing))
            return existing;

        if (!_factories.TryGetValue(key, out var factory))
            throw new InvalidOperationException($"No page registered for {key}");

        var created = factory();
        _cache[key] = created;
        return created;
    }
}