using Microsoft.Extensions.Logging;
using t.App.Models;

namespace t.App.Service;

public class NavigationService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private readonly NavigationPage startPage;
    public delegate Task EventHandlerAsync<EventArg>(object? sender, EventArg e);
    public event EventHandlerAsync<EventArgs<object>>? AppearedEvent;
    public event EventHandlerAsync<EventArgs<object>>? DisappearedEvent;

    public NavigationService(IServiceProvider serviceProvider, ILogger logger, NavigationPage startPage)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.startPage = startPage;
    }
    public Page CurrentPage => startPage.CurrentPage;
    public object? NavigationData { get; private set; }
    public Task NavigateToAsync(Type toNavigate)
    {
        return NavigateToAsync(toNavigate, (object?)null);
    }
    public async Task NavigateToAsync<T>(Type toNavigate, T? data = null) where T : class
    {
        var viewmodel = serviceProvider.GetService(toNavigate);
        if (viewmodel is object p)
        {
            var pageTypeString = GetPageType(viewmodel);
            var pageType = Type.GetType(pageTypeString);
            if (pageType != null)
            {
                var pageInstance = serviceProvider.GetService(pageType);
                if (pageInstance is Page page)
                {
                    page.BindingContext = viewmodel;
                    page.Appearing += Page_Appearing;
                    page.Disappearing += Page_Disappearing;
                    NavigationData = data;
                    await CurrentPage.Navigation.PushAsync(page);
                }
            }
            else
            {
                logger.LogWarning("Could not resolve the ViewModel for {0}", p);
            }

        }
        else
        {
            throw new InvalidOperationException($"The resolved page for type {toNavigate} is not Type Page");
        }
    }

    private static string GetPageType(object viewmodel)
    {
        return $"{viewmodel}".Replace("viewmodel", "", StringComparison.OrdinalIgnoreCase);
    }
    private static string GetViewModelType(object page)
    {
        return $"{page}ViewModel";
    }

    private void Page_Disappearing(object? sender, EventArgs e)
    {
        if (sender is Page page)
        {
            var viewmodel = GetViewModelFromPage(page);
            DisappearedEvent?.Invoke(viewmodel ?? sender, new EventArgs<object>(NavigationData));
            page.Disappearing -= Page_Disappearing;
        }
    }

    private void Page_Appearing(object? sender, EventArgs e)
    {
        if (sender is Page page)
        {
            var viewmodel = GetViewModelFromPage(page);
            AppearedEvent?.Invoke(viewmodel ?? sender, new EventArgs<object>(NavigationData));
            page.Appearing -= Page_Appearing;
        }
    }

    private object? GetViewModelFromPage(Page page)
    {
        var s = GetViewModelType(page);
        var viewmodelType = Type.GetType(s);
        object? viewmodel = null;
        if (viewmodelType != null)
        {
            viewmodel = serviceProvider.GetService(viewmodelType);
        }
        return viewmodel;
    }
}

