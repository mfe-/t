using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.App.Service;

namespace t.App.View
{
    public class DebugPageMobileViewModel : DebugPageViewModel
    {
        public DebugPageMobileViewModel(ILogger<DebugPageViewModel> logger, NavigationService navigationService) : base(logger, navigationService)
        {
        }
    }
}
