using Microsoft.AspNetCore.Components;

namespace LandsatReflectance.UI.Components;

public partial class FullPageLoadingOverlay : ComponentBase
{
    private string? m_overlayMessage;
    private bool m_isVisible = false;

    public async Task ExecuteWithOverlay(Func<Task> work, Func<Task> onFinishedCallback)
    {
        m_isVisible = true;
        StateHasChanged();
        
        await work();
        m_isVisible = false;
        
        StateHasChanged();
        await onFinishedCallback();
    }
    
    public async Task ExecuteWithOverlay(string overlayMessage, Func<Task> work, Func<Task> onFinishedCallback)
    {
        m_overlayMessage = overlayMessage;
        m_isVisible = true;
        StateHasChanged();
        
        await work();
        m_overlayMessage = null;
        m_isVisible = false;
        
        StateHasChanged();
        await onFinishedCallback();
    }
}