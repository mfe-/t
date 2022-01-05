using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Service;

public class NavigationService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private readonly NavigationPage startPage;

    public NavigationService(IServiceProvider serviceProvider, ILogger logger, NavigationPage startPage)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.startPage = startPage;
    }
    public Page CurrentPage => startPage.CurrentPage;
    public async Task NavigateToAsync(Type toNavigate)
    {
        var page = serviceProvider.GetService(toNavigate);
        if (page is Page p)
        {
            var viewmodelTypeString = $"{page}ViewModel";
            var viewmodelType = Type.GetType(viewmodelTypeString);
            if (viewmodelType != null)
            {
                var viewmodelInstance = serviceProvider.GetService(viewmodelType);
                p.BindingContext = viewmodelInstance;
            }
            else
            {
                logger.LogWarning("Could not resolve the ViewModel for {0}",p);
            }

            await CurrentPage.Navigation.PushAsync(p);
        }
        else
        {
            throw new InvalidOperationException($"The resolved page for type {toNavigate} is not Type Page");
        }
    }
}

