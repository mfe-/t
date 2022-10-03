using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Service;

public class DialogService
{
    private readonly NavigationService navigationService;

    public DialogService(NavigationService navigationService)
    {
        this.navigationService = navigationService;
    }
    public Task DisplayAsync(string title, string message, string button)
    {
        return navigationService.CurrentPage.DisplayAlert(title, message, button);
    }
}
