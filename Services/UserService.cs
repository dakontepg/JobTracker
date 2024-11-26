using JobTracker.Models;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;

namespace JobTracker.Services
{
    public class UserService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<UserService> _logger;
        private readonly IFirebaseAuthService _firebaseAuthService;
        
        public UserService(FirestoreDb firestoreDb, ILogger<UserService> logger, IFirebaseAuthService firebaseAuthService)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
            _firebaseAuthService = firebaseAuthService;
        }

        public async Task AddUserAsync(User user)
        {
            var userDoc = new Dictionary<string, object>
            {
                {"uid", user.Uid},
                {"email", user.Email},
                {"roles", user.Roles}
            };
            await _firestoreDb.Collection("users").Document(user.Uid).SetAsync(userDoc);
            await _firebaseAuthService.AddCustomClaims(user.Uid, user.Roles);
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            var docRef = _firestoreDb.Collection("users").Document(userId);
            var snapshot = await docRef.GetSnapshotAsync();
            if(snapshot.Exists)
            {
                var user = snapshot.ConvertTo<User>();// this convert is returning an empty user
                user.Uid = snapshot.Id;
                user.Email = snapshot.GetValue<string>("email");
                user.Roles = snapshot.GetValue<List<object>>("roles")?.Select(r => r.ToString()).ToList() ?? new List<string?>();
                return user;
            }
            return null;
        }

        public async Task AssignRoleToUserAsync(string userId, string role)
        {
            var docRef = _firestoreDb.Collection("users").Document(userId);
            var snapshot = await docRef.GetSnapshotAsync();
            if(snapshot.Exists)
            {
                var roles = snapshot.GetValue<List<string>>("roles") ?? new List<string>();
                if(!roles.Contains(role))
                {
                    roles.Add(role);
                    await docRef.UpdateAsync("roles", roles);
                    await _firebaseAuthService.AddCustomClaims(userId, roles);
                }
            }
        }

        public async Task UpdateUserEmailAsync(string userId, string newEmail)
        {
            var docRef = _firestoreDb.Collection("users").Document(userId);
            await docRef.UpdateAsync("email", newEmail);
        }

        public async Task UpdateUserRolesAsync(string userId, List<string> roles)
        {
            var docRef = _firestoreDb.Collection("users").Document(userId);
            await docRef.UpdateAsync("roles", roles);
            await _firebaseAuthService.AddCustomClaims(userId, roles);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var usersCollection = _firestoreDb.Collection("users");
            var snapshot = await usersCollection.GetSnapshotAsync();

            if(snapshot != null && snapshot.Documents != null)
            {
                var users = new List<User>();

                foreach (var document in snapshot.Documents)
                {
                    // logger info used in debugging
                    // _logger.LogInformation($"Document ID: {document.Id}");

                    var data = document.ToDictionary();

                    // logger info use in debugging
                    // foreach(var kvp in data)
                    // {
                    //     _logger.LogInformation($"Key: {kvp.Key}, Value: {kvp.Value}");
                    // }
                    try
                    {
                        var user = new User
                        {
                            Uid = document.GetValue<string>("uid"),
                            Email = document.GetValue<string>("email"),
                            Roles = document.GetValue<List<object>>("roles")?.Select(r => r.ToString()).ToList() ?? new List<string?>()
                        };
                        users.Add(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error converting document {document.Id} to User: {ex.Message}");
                    }
                }
                return users;
            }
            return new List<User>();
        }

        public async Task DeleteUserAsync(string userId)
        {
            // Delete user from Firebase Authentication
            await DeleteUserFromFirebaseAsync(userId);

            // Delete user from Firestore User Collection
            await DeleteUserDataFromFirestoreAsync(userId);
        }

        private async Task DeleteUserFromFirebaseAsync(string userId)
        {
            try 
            { 
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(userId); 
            } 
            catch (FirebaseAuthException ex) 
            { 
                _logger.LogError($"Failed to delete user from Firebase Authentication: {ex.Message}"); 
            }
        }

        private async Task DeleteUserDataFromFirestoreAsync(string userId)
        {
            try 
            { 
                var userDocument = _firestoreDb.Collection("users").Document(userId); 
                await userDocument.DeleteAsync(); 
            } 
            catch (Exception ex) 
            { 
                _logger.LogError($"An unexpected error occurred: {ex.Message}");
            }
        }

    }
}