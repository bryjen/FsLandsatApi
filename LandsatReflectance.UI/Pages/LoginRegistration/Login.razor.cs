using LandsatReflectance.UI.Components;
using LandsatReflectance.UI.Exceptions.Api;
using LandsatReflectance.UI.Services;
using LandsatReflectance.UI.Services.Api;
using LandsatReflectance.UI.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;

namespace LandsatReflectance.UI.Pages.LoginRegistration;

public partial class Login : ComponentBase
{
    [Inject]
    public required IWebAssemblyHostEnvironment Environment { get; set; }
    
    [Inject]
    public required NavigationManager NavigationManager { get; set; }
    
    [Inject]
    public required ISnackbar Snackbar { get; set; }
    
    [Inject]
    public required IDialogService DialogService { get; set; }
    
    [Inject]
    public required UserService UserService { get; set; }
    
    
    [CascadingParameter]
    public required FullPageLoadingOverlay FullPageLoadingOverlay { get; set; }
    
    private string m_email = string.Empty;
    private string m_password = string.Empty;
    private bool m_isProcessing = false;
    
    
#region Password Text Field
    private InputType m_passwordFieldInputType = InputType.Password;

    private string PasswordFieldInputIcon =>
        m_passwordFieldInputType is InputType.Password
            ? Icons.Material.Filled.Visibility
            : Icons.Material.Filled.VisibilityOff;

    private void OnPasswordVisibilityButtonClicked()
    {
        m_passwordFieldInputType = m_passwordFieldInputType is InputType.Password 
            ? InputType.Text 
            : InputType.Password;
    }
#endregion



#region RegistrationPanel
    private string m_registrationEmail = string.Empty;

    private async Task GoToRegistrationPage()
    {
        var workFunc = async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(Rand.GeneratePageSwitchDelayTime()));
        };
        
        var onWorkFinishedCallback = () =>
        {
            string emailQueryString = string.IsNullOrWhiteSpace(m_registrationEmail)
                ? string.Empty
                : "?email=" + Uri.EscapeDataString(m_registrationEmail);
            
            Snackbar.Clear();
            NavigationManager.NavigateTo($"/Register{emailQueryString}");
            return Task.CompletedTask;
        };

        await FullPageLoadingOverlay.ExecuteWithOverlay(workFunc, onWorkFinishedCallback);
    }
#endregion


    private async Task TryLogin()
    {
        m_isProcessing = true;
        StateHasChanged();
        
        try
        {
            await UserService.LoginAsync(m_email, m_password);
            
            var workFunc = async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(Rand.GeneratePageSwitchDelayTime()));
            };
            
            var onWorkFinishedCallback = () =>
            {
                NavigationManager.NavigateTo("/");
                Snackbar.Add("Successfully logged in.", Severity.Info);
                return Task.CompletedTask;
            };
            
            m_isProcessing = false;
            StateHasChanged();

            await FullPageLoadingOverlay.ExecuteWithOverlay(workFunc, onWorkFinishedCallback);
        }
        catch (BadRequestException badRequestException)
        {
            Snackbar.Add(badRequestException.Message, Severity.Error);
        }
        catch (ServerRequestException serverRequestException)
        {
            Snackbar.Add(serverRequestException.Message, Severity.Error, options =>
            {
                if (Environment.IsDevelopment())
                {
                    options.Action = "More information";
                    options.ActionVariant = Variant.Text;
                    options.Onclick = _ =>
                    {
                        var parameters = new DialogParameters<ExceptionDetailsDialog>
                        {
                            { dialog => dialog.Exception, serverRequestException }
                        };

                        var dialogOptions = new DialogOptions
                        {
                            BackdropClick = false,
                            MaxWidth = MaxWidth.Large,
                            FullWidth = true
                        };

                        DialogService.Show<ExceptionDetailsDialog>(null, parameters, dialogOptions);
                        return Task.CompletedTask;
                    };
                }
            });
        }
    }
}