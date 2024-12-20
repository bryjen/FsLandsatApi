// using LandsatReflectance.Common.Models;

namespace LandsatReflectance.UI.Services;

public class CurrentUserService
{
    // public User? User { get; private set; }
    public string AuthToken { get; private set; } = string.Empty;
    
    private ILogger<CurrentUserService> m_logger;
    
    public CurrentUserService(ILogger<CurrentUserService> logger)
    {
        m_logger = logger;
    }

    /*
    public void InitCurrentUser(User user, string authToken)
    {
        User = user;
        AuthToken = authToken;
    }
     */
}