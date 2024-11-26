using Google.Cloud.Firestore;
using JobTracker.Services;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Models;
using System.Diagnostics;

// this controller is for the User activities of Signing Up, Logging In, and Logging Out

public class AccountController : Controller
{
    private readonly IFirebaseAuthService _authService;
    private readonly UserService _userService;

    public AccountController(IFirebaseAuthService authService, UserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    
    /// <summary>
    ///  getter for the Signup page
    /// </summary>
    public IActionResult Signup()
    {
        return View();
    }
    
    /// <summary>
    /// Post the Signup information
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Signup(SignUpUserDto model)
    {
        try
        {
            // Create user with Firebase Authentication
            var token = await _authService.SignUp(model.Email, model.Password);
            if (token == null)
            {
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            // Get user Id from the token
            var userId = await _authService.GetUserIdFromTokenAsync(token);

            // Store user data in Firestore
            var newUser = new User
            {
                Uid = userId,
                Email = model.Email,
                Roles = new List<string> {"operator"} // default role, adjust as needed
            };

            await _userService.AddUserAsync(newUser);

            // store the token in a cookie
            HttpContext.Response.Cookies.Append("authToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true
            });

            return RedirectToAction("Login");
        }
        catch (Exception)
        {
            // Handle exception (e.g. display error message)
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }

    /// <summary>
    ///  getter for Login
    /// </summary>
    public IActionResult Login ()
    {
        ViewData["LoginErrorMessage"] = "";
        return View();
    }

    /// <summary
    /// Post the Login
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Login(UserDto userDto)
    {
        try
        {
            if(!ModelState.IsValid)
            {
                ViewData["LoginErrorMessage"] = "Email address or password not found.  Please retry";
                return RedirectToAction("Login");
            }

            var token = await _authService.Login(userDto.Email, userDto.Password);

            if(token is not null)
            {
                HttpContext.Session.SetString("token", token);
                return RedirectToAction("Index","Home");
            }
            
            // if token IS null, not an authorized user or the connection to Firebase is not available
            ViewData["LoginErrorMessage"] = "Login not authorized.  Please see Supervisor.";
            return RedirectToAction("Login");

        }
        catch (Exception)
        {
            ViewData["LoginErrorMessage"] = "Email address or password not found.  Please retry";
            return View(userDto);
        }
    }

    ///<summary>
    /// Logout the user
    ///</summary>
    public IActionResult Logout()
    {
        _authService.SignOut();
        HttpContext.Session.Remove("token");
        return View();
    }

    ///<summary>
    /// display a page for an unauthenticated user
    ///</summary>
    public IActionResult Unauth()
    {
        return View();
    }

}