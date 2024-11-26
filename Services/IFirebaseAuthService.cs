namespace JobTracker.Services
{
    public interface IFirebaseAuthService
    {
        Task<string?>SignUp(string email, string password);

        Task<string?>Login(string email, string password);

        void SignOut();

        Task AddCustomClaims(string userId, List<string> roles);

        Task<string>GetUserIdFromTokenAsync(string token);
        
        Task<Dictionary<string, object>> GetCustomClaims(string userId);

    }
}