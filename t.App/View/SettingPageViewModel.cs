using Microsoft.Extensions.Logging;

namespace t.App.View;

internal class SettingPageViewModel : BaseViewModel
{
    public SettingPageViewModel(ILogger<SettingPageViewModel> logger) : base(logger)
    {
    }
    public String Title { get; set; } = "Settings";
}
