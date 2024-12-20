using System.Text.Encodings.Web;
using System.Text.Json;
using Blazored.LocalStorage;
using GoogleMapsComponents;
// using LandsatReflectance.Common.Converters;
// using LandsatReflectance.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LandsatReflectance.UI;
using LandsatReflectance.UI.Services;
using LandsatReflectance.UI.Services.Api;
using MudBlazor.Services;
using ProtoBuf.Meta;


// Pre-compiling 'Wrs2Area' class for faster de-serialization
var model = RuntimeTypeModel.Default;
// model.Add(typeof(Wrs2Area), true);
model.CompileInPlace();


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Fuck no easy way to hide this thing in wasm
// TODO: Change & Restrict before making repo public
builder.Services.AddBlazorGoogleMaps("AIzaSyCDocP3wfMPBO_0YWpaxlZiISEHwgStdQU");

builder.Services.AddSingleton(_ =>
{
    var jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // To prevent escaping of '&'
    };
    // jsonSerializerOptions.Converters.Add(new UserWithTokenConverter());
    return jsonSerializerOptions;
});

builder.Services.AddScoped(sp =>
{
    const string proxyServerBaseUri = "https://localhost:7280/";
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(proxyServerBaseUri);

    return httpClient;
});

builder.Services.AddScoped<Wrs2AreasService>();
builder.Services.AddScoped<UserInterfaceService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CurrentUserService>();

builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
