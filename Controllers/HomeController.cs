using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Models;

namespace JobTracker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if(User.Identity.IsAuthenticated)
        {
            ViewBag.Message = "User is authenticated";
        } else
        {
            ViewBag.Message = "User is NOT authenticated";
        }
        
        // this is used to debug the token during authentication issues
        var token = HttpContext.Session.GetString("token");
        if (token != null)
        {
            ViewBag.Token = token;
            ViewBag.Message2 = "";
            return View();
        }

        ViewBag.Message2 = "Token not found";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
