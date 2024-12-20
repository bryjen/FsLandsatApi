using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;

namespace LandsatReflectance.UI.Pages;

public partial class Dashboard : ComponentBase
{
    // private List<Target> m_targets = new();

    protected override async Task OnInitializedAsync()
    {
    }
    
    protected override Task OnAfterRenderAsync(bool isFirstRender)
    {
        return Task.CompletedTask;
    }
}
