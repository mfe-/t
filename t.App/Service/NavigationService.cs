using Microsoft.Extensions.Logging;
using t.App.Models;

namespace t.App.Service;

public class NavigationService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private readonly Shell startPage;
    public delegate Task EventHandlerAsync<in EventArg>(object? sender, EventArg e);
    public event EventHandlerAsync<EventArgs<object>>? AppearedEvent;
    public event EventHandlerAsync<EventArgs<object>>? PageLeftEvent;

    public NavigationService(IServiceProvider serviceProvider, ILogger logger, Shell startPage)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.startPage = startPage;
    }
    /// <summary>
    /// Gets the current page which is displayed or navigated
    /// </summary>
    public Page CurrentPage => startPage.CurrentPage;
    /// <summary>
    /// Get or sets the data which is used by <see cref="NavigateToAsync(Type)"/>, <see cref="Page_Appearing(object?, EventArgs)"/> 
    /// and <see cref="Page_Appearing(object?, EventArgs)"/>
    /// </summary>
    private object? NavigationData { get; set; }
    /// <summary>
    /// Navigates to the proper page, by looking up which page is "connected" by the <paramref name="viewmodelTypeToNavigate"/> type.
    /// </summary>
    /// <param name="viewmodelTypeToNavigate">The corresponding viewmodel of the page which should be navigated to</param>
    /// <returns>A task that indicates the state of the computation of the executed function</returns>
    public Task NavigateToAsync(Type viewmodelTypeToNavigate)
    {
        return NavigateToAsync(viewmodelTypeToNavigate, (object?)null);
    }
    /// <summary>
    /// Navigates to the proper page, by looking up which page is "connected" by the <paramref name="viewmodelTypeToNavigate"/> type.
    /// </summary>
    /// <typeparam name="T">The type which is used for <paramref name="data"/></typeparam>
    /// <param name="viewmodelTypeToNavigate">The corresponding viewmodel of the page which should be navigated to</param>
    /// <param name="data">The data which should be passed to the next page</param>
    /// <returns>A task that indicates the state of the computation of the executed function</returns>
    /// <exception cref="InvalidOperationException">If no proper viewmodel instance can be resolved using <see cref="IServiceProvider.GetService(Type)"/></exception>
    public async Task NavigateToAsync<T>(Type viewmodelTypeToNavigate, T? data = null) where T : class
    {
        var viewmodel = serviceProvider.GetService(viewmodelTypeToNavigate);
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
                    LeavePage(CurrentPage);
                    NavigationData = data;

                    //if (CurrentPage is Shell shell)
                    //{
                    //    //not supported yet
                    //}
                    //else if (CurrentPage is Page currentPage)
                    //{
                        await CurrentPage.Navigation.PushAsync(page);
                    //}

                }
            }
            else
            {
                logger.LogWarning("Could not resolve the ViewModel for {0}", p);
            }

        }
        else
        {
            throw new InvalidOperationException($"Could not resolve the viewmodel instance of type {viewmodelTypeToNavigate}");
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

    internal void LeavePage(Page? page)
    {
        if (page == null) return;
        var viewmodel = GetViewModelFromPage(page);
        PageLeftEvent?.Invoke(viewmodel ?? page, new EventArgs<object>(NavigationData));
    }

    internal void Page_Appearing(object? sender, EventArgs e)
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

