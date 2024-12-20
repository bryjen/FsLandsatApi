using System.Text;
using System.Text.Json;
// using LandsatReflectance.Common.Models;
// using LandsatReflectance.Common.Models.Request;
// using LandsatReflectance.Common.Models.ResponseModels;
using LandsatReflectance.UI.Exceptions;
using LandsatReflectance.UI.Exceptions.Api;
using LandsatReflectance.UI.Pages.LoginRegistration;

namespace LandsatReflectance.UI.Services.Api;

public class UserService
{
    private readonly JsonSerializerOptions m_jsonSerializerOptions;
    private readonly ILogger<UserService> m_logger;
    private readonly HttpClient m_httpClient;
    private readonly CurrentUserService m_currentUserService;
    
    public UserService(
        JsonSerializerOptions jsonSerializerOptions, 
        ILogger<UserService> logger, 
        HttpClient httpClient, 
        CurrentUserService currentUserService)
    {
        m_jsonSerializerOptions = jsonSerializerOptions;
        m_logger = logger;
        m_httpClient = httpClient;
        m_currentUserService = currentUserService;
    }

    public async Task LoginAsync(string email, string password)
    {
        /*
        var userLoginRequest = new UserLoginInfo
        {
            Email = email,
            Password = password
        };

        try
        {
            using var contents = new StringContent(JsonSerializer.Serialize(userLoginRequest, m_jsonSerializerOptions), Encoding.UTF8, "application/json");
            var response = await m_httpClient.PostAsync("Authentication/Login", contents);
            
            var stream = await response.Content.ReadAsStreamAsync();
            var registerResponse = await JsonSerializer.DeserializeAsync<ResponseBase<UserWithToken>>(stream, m_jsonSerializerOptions);
            
            if (registerResponse is null)
            {
                var innerException = new Exception("The deserialized response from the server is null.");
                throw new ServerRequestException(innerException);
            }

            if (registerResponse.ErrorMessage is not null)
            {
                throw new BadRequestException(registerResponse.ErrorMessage);
            }

            if (registerResponse.Data is null)
            {
                var innerException = new NoResponseException("The deserialized response from the server is null.");
                throw new ServerRequestException(innerException);
            }

            var userAndToken = registerResponse.Data;
            m_currentUserService.InitCurrentUser(userAndToken.User, userAndToken.Token);
        }
        catch (ServerRequestException)
        {
            throw;
        }
        catch (BadRequestException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Wrap as 'ServerRequestException' by default
            throw new ServerRequestException(exception);
        }
    }

    public async Task RegisterAsync(UserModel userModel)
    {
        var userLoginRequest = new UserLoginInfo
        {
            Email = userModel.Email,
            Password = userModel.Password
        };

        try
        {
            using var contents = new StringContent(JsonSerializer.Serialize(userLoginRequest, m_jsonSerializerOptions), Encoding.UTF8, "application/json");
            var response = await m_httpClient.PostAsync("Authentication/Register", contents);
            
            var stream = await response.Content.ReadAsStreamAsync();
            var registerResponse = await JsonSerializer.DeserializeAsync<ResponseBase<UserWithToken>>(stream, m_jsonSerializerOptions);
            
            if (registerResponse is null)
            {
                var innerException = new Exception("The deserialized response from the server is null.");
                throw new ServerRequestException(innerException);
            }

            if (registerResponse.ErrorMessage is not null)
            {
                throw new BadRequestException(registerResponse.ErrorMessage);
            }

            if (registerResponse.Data is null)
            {
                var innerException = new NoResponseException("The deserialized response from the server is null.");
                throw new ServerRequestException(innerException);
            }

            var userAndToken = registerResponse.Data;
            m_currentUserService.InitCurrentUser(userAndToken.User, userAndToken.Token);
        }
        catch (ServerRequestException)
        {
            throw;
        }
        catch (BadRequestException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Wrap as 'ServerRequestException' by default
            throw new ServerRequestException(exception);
        }
         */
    }
}