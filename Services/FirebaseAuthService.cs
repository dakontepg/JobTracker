using Firebase.Auth;
using FirebaseAdmin.Auth;

namespace JobTracker.Services
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly FirebaseAuthClient _firebaseAuth;

        public FirebaseAuthService(FirebaseAuthClient firebaseAuth)
        {
            _firebaseAuth = firebaseAuth;
        }

        public async Task<string?>SignUp(string email, string password)
        { 
            var userCredentials = await _firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password); 
            if (userCredentials != null) 
            { 
                await AddCustomClaims(userCredentials.User.Uid, new List<string> { "operator" }); 
                var token = await userCredentials.User.GetIdTokenAsync(true); // Force refresh to include custom claims 
                return token; 
            } 
            return null;
        }

        public async Task<string?>Login(string email, string password)
        {
            var userCredentials = await _firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
            if (userCredentials != null) 
            { 
                // Re-fetch the user to ensure the token has updated claims 
                var token = await userCredentials.User.GetIdTokenAsync(true); // Force refresh 
                return token; 
            }
            return null;
        }

        public void SignOut() => _firebaseAuth.SignOut();

        // Method to add custom claims to a user 
        public async Task AddCustomClaims(string userId, List<string> roles) 
        { 
            var claims = new Dictionary<string, object> 
            { 
                { "roles", roles } 
            }; 
            
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(userId, claims); 
        }

        public async Task<Dictionary<string, object>> GetCustomClaims(string userId)
        {
            var user = await FirebaseAuth.DefaultInstance.GetUserAsync(userId);
            return user.CustomClaims.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }


        //method to get user ID from token
        public async Task<string> GetUserIdFromTokenAsync(string token)
        {
            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                return decodedToken.Uid;
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException)
            {
                // Handle error (e.g., token invalid or expired)
                return null;
            }
        }
    }
}