using MatchTFE.Client.Resources;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace MatchTFE.Client.Localization;

public class AppMudLocalizer : MudLocalizer
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    public AppMudLocalizer(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
    }

    public override LocalizedString this[string key]
    {
        get
        {
            var result = _localizer[key];
            return result.ResourceNotFound ? base[key] : result;
        }
    }
}
