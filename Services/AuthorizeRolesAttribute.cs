using System.Security.Claims;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JobTracker.Services
{
    public class AuthorizeRolesAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _roles;
        public AuthorizeRolesAttribute(params string[] roles)
        {
            _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<AuthorizeRolesAttribute>>();
            if (logger == null) 
            { 
                context.Result = new UnauthorizedResult(); 
                return; 
            }

            var user = context.HttpContext?.User;
            if (user == null) 
            { 
                logger.LogWarning("HttpContext or User is null. Unauthorized access attempted."); 
                context.Result = new UnauthorizedResult(); 
                return; 
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(userId == null)
            {
                logger.LogWarning("User ID not found.  Unauthorized access attempted");
                context.Result = new UnauthorizedResult();
                return;
            }

            var configuration = context.HttpContext.RequestServices.GetService<IConfiguration>(); 
            if (configuration == null) 
            { 
                logger.LogWarning("Configuration service is not available. Unauthorized access attempted."); context.Result = new UnauthorizedResult(); 
                return; 
            } 
            
            var projectId = configuration["project_id"]; 
            if (string.IsNullOrEmpty(projectId)) 
            { 
                logger.LogWarning("Firebase project ID is not configured. Unauthorized access attempted."); 
                context.Result = new UnauthorizedResult(); 
                return; 
            }

            var firestore = FirestoreDb.Create(projectId);
            var docRef = firestore.Collection("users").Document(userId);
            var snapshot = docRef.GetSnapshotAsync().Result;

            if(snapshot.Exists)
            {
                var roles = snapshot.GetValue<List<string>>("roles");

                // Logging roles for debugging purposes
                // logger.LogInformation("User ID: {UserId}, Roles:  {Roles}", userId, string.Join(", ", roles));

                if(roles != null && roles.Any(role => _roles.Contains(role)))
                {
                    return; //allow access if any role matches
                }
                logger.LogWarning("User Id: {UserId} does not have required roles.", userId);
                context.Result = new ForbidResult(); // forbid access if no roles match
            }
            else
            {
                logger.LogWarning("Snapshot does not exist.");
                context.Result = new ForbidResult(); // Forbid access if user document doesnt exist
            }
        }
    }
}