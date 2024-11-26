using JobTracker.Models;
using JobTracker.Services;
using JobTracker.ViewModels;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

// this controller allows an Administrator to:
// - Edit or Delete Users
// - Add, Edit, Activate or Deactivate Initials
// - Add, Edit or Delete Roles
// - Add, Edit or Delete Job Operations

[AuthorizeRoles("administrator")]
public class AdministrationController : Controller
{
    private readonly FirestoreDb _firestoreDb;
    private readonly UserService _userService;
    private readonly ILogger<AdministrationController> _logger;

    public AdministrationController(FirestoreDb firestoreDb, UserService userService, ILogger<AdministrationController> logger){
        _firestoreDb = firestoreDb;
        _userService = userService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
    
    /// <summary>
    /// View all the Operations in the Firestore database
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>ViewAllOperations(){
        CollectionReference collection2 = _firestoreDb.Collection("JobOp");
        QuerySnapshot snapshot2 = await collection2.GetSnapshotAsync();
        var operations = snapshot2.Documents.Select(d => d.ConvertTo<JobOp>()).ToList();

        return View(operations);   
    }

    /// <summary>
    /// Create a new Operation in the Firestore database
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>AddOperation()
    {
        ViewData["HighestId"] = "No ids are in use.  Recommend starting with 1.";
        ViewData["InUse"] = "";

        // Query to get the highest Id 
        var collection = _firestoreDb.Collection("JobOp"); 
        var query = collection.OrderByDescending("Id").Limit(1); 
        var querySnapshot = await query.GetSnapshotAsync();
         
        int highestId = 0; 
        if (querySnapshot.Documents.Count > 0) 
        { 
            highestId = await GetHighestIdAsync(collection);
            ViewData["HighestId"] = "Highest number currently in use: "+ highestId; 
        }
    
        return View();
    }

    /// <summary>
    /// Add the new Operation to the database collection named "JobOp"
    /// </summary>
    /// <param name="demoJobData"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> AddOperation(AdministrationViewModel model)
    {
        try
        {
            CollectionReference collection = _firestoreDb.Collection("JobOp");

            // Check if a JobOp with the same Id already exists
            Query query = collection.WhereEqualTo("Id", model.JobOp.Id);
            QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

            if (querySnapshot.Documents.Count > 0)
            {
                // If Id already exists, show an error message
                var highestId = await GetHighestIdAsync(collection);
                ViewData["InUse"] = "That Id is already in use.  Recommend a number higher than " + highestId;
                return View(model);
            }

            // Add the new JobOp
            DocumentReference document = await collection.AddAsync(new JobOp
            {
                OpName = model.JobOp.OpName,
                Id = model.JobOp.Id,
                Active = true // true by default when first created
            });

            return RedirectToAction("AddOperation");
        }
        catch (Exception ex)
        {
            // Log the exception (alternatively, could use a logging framework here)
            Console.WriteLine($"An error occurred: {ex.Message}");

            // Add model error to display the error message to the user
            ViewData["ErrorMessage"] = "An error occurred while adding the operation. Please try again.";
            return View(model);
        }
    }

    /// <summary>
    /// private method for getting the highest Id number in use in a collection
    ///  - collection MUST have a field named 'Id'
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    private async Task<int> GetHighestIdAsync(CollectionReference collection)
    {
        var query = collection.OrderByDescending("Id").Limit(1);
        var querySnapshot = await query.GetSnapshotAsync();

        int highestId = 0;

        if (querySnapshot.Documents.Count > 0)
        {
            var highestDoc = querySnapshot.Documents[0];
            highestId = highestDoc.GetValue<int>("Id");
        }

        return highestId;
    }



    /// <summary>
    /// Edit an Operation in the Firestore database
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>EditOperation(int id)
    {
        var collection = _firestoreDb.Collection("JobOp");
        var query = collection.WhereEqualTo("Id", id);
        var snapshot = await query.GetSnapshotAsync();
        
        if(snapshot.Documents.Any())
        {
            var document = snapshot.Documents[0];
            var jobOp = document.ConvertTo<JobOp>();

            // logger information used in debugging
            // _logger.LogInformation("Document found: {DocumentPath}", document.Reference.Path);

            var model = new AdministrationViewModel
            {
                JobOp = jobOp
            };
            return View(model);
        }

        _logger.LogError("No document found with Id: {Id}", id);
        return NotFound();   
    }

    /// <summary>
    /// Edit an operation in the database collection named "JobOp"
    /// </summary>
    /// <param name="demoJobData"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult>EditOperation(JobDataViewModel model)
    {
        var collection = _firestoreDb.Collection("JobOp");
        var query = collection.WhereEqualTo("Id", model.JobOp.Id);
        var snapshot = await query.GetSnapshotAsync();

        if(!snapshot.Documents.Any())
        {
            _logger.LogError("Document not found with Id: {Id}", model.JobOp.Id);
            return NotFound();
        }

        var documentReference = snapshot.Documents[0].Reference;

        var updateData = new Dictionary<string, object>
        {
            {"Id", model.JobOp.Id},
            {"OpName", model.JobOp.OpName},
            {"Active", model.JobOp.Active}
        };

        await documentReference.SetAsync(updateData);

        return RedirectToAction("ViewAllOperations");
    }


    /// <summary>
    /// Delete an Operation in the Firestore database
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>DeleteOperation(int id)
    {
        ViewData["ErrorMessage"] = "";
        var collection = _firestoreDb.Collection("JobOp");
        var query = collection.WhereEqualTo("Id", id);
        var snapshot = await query.GetSnapshotAsync();
        
        if(snapshot.Documents.Any())
        {
            var document = snapshot.Documents[0];
            var jobOp = document.ConvertTo<JobOp>(); 

            // logger information used in debugging
            // _logger.LogInformation("Document found: {DocumentPath}", document.Reference.Path);

            var model = new AdministrationViewModel
            {
                JobOp = jobOp
            };
            return View(model);
        }

        _logger.LogError("No document found with Id: {Id}", id);
        return NotFound();   
    }

    /// <summary>
    /// Delete specific Operation in the Firestore database
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult>DeleteOperation(JobDataViewModel model)
    {
        var id = model.JobOp.Id;

        // if(string.IsNullOrEmpty(id))
        // {
        //     _logger.LogError("Invalid document ID");
        //     return BadRequest("Invalid document ID");
        // }

        var collection = _firestoreDb.Collection("JobOp");
        var query = collection.WhereEqualTo("Id",id);
        var snapshot = await query.GetSnapshotAsync();

        if(!snapshot.Documents.Any())
        {
            _logger.LogError("Document not found: {Id}", id);
            return NotFound();
        }

        // Check if the JobOp.Id is referenced in any DemoJobData documents 
        var demoJobDataCollection = _firestoreDb.Collection("DemoJobData"); 
        var demoJobDataQuery = demoJobDataCollection.WhereEqualTo("JobOpId", id); 
        var demoJobDataSnapshot = await demoJobDataQuery.GetSnapshotAsync();

        if (demoJobDataSnapshot.Documents.Any()) 
        { 
            _logger.LogError("Cannot delete JobOp with Id {Id} because it is currently in use.", id); 
            ModelState.AddModelError("Id", "Cannot delete this JobOp because it is currently in use.");
            ViewData["ErrorMessage"] = "This Operation is in use.  Delete not available.";

            // need to return an AdministrationViewModel
            var adminModel = new AdministrationViewModel
            {
                JobOp = model.JobOp
            };

            return View(adminModel);
        }

        var documentReference = snapshot.Documents.First().Reference;
        await documentReference.DeleteAsync();

        _logger.LogInformation("Document deleted: {DocumentId}", id);
        
        return RedirectToAction("ViewAllOperations");
    }

    /// <summary>
    /// View all the Roles in the Firestore database
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>ViewAllRoles(){
        CollectionReference collection2 = _firestoreDb.Collection("roles");
        QuerySnapshot snapshot2 = await collection2.GetSnapshotAsync();
        var roles = snapshot2.Documents.Select(d => d.ConvertTo<Role>()).ToList();

        return View(roles);   
    }

    /// <summary>
    /// Create a new Role in the Firestore database
    /// </summary>
    /// <returns></returns>
    public IActionResult AddRole()
    {
        return View();
    }

    /// <summary>
    /// Add the new Role to the database collection named "role"
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult>AddRole(AdministrationViewModel model)
    {
        CollectionReference collection = _firestoreDb.Collection("roles");

        // check if a role with the same RoleId already exist
        Query query = collection.WhereEqualTo("RoleId", model.Role.RoleId);
        QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

        if(querySnapshot.Documents.Count > 0)
        {
            ViewData["inUse"] = "The RoleId is already in use.  Please select another.";
            return View(model);
        }

        // Add the new Role
        DocumentReference document = await collection.AddAsync(new Role
        {
            RoleName = model.Role.RoleName,
            RoleId = model.Role.RoleId
        });
        return RedirectToAction("AddRole");
    }


    /// <summary>
    /// Edit a Role in the Firestore database
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>EditRole(string id)
    {
        var collection = _firestoreDb.Collection("roles");
        var query = collection.WhereEqualTo("RoleId", id);
        var snapshot = await query.GetSnapshotAsync();
        
        if(snapshot.Documents.Any())
        {
            var document = snapshot.Documents[0];
            var role = document.ConvertTo<Role>();

            // logger info used in debugging
            // _logger.LogInformation("Document found: {DocumentPath}", document.Reference.Path);

            var model = new AdministrationViewModel
            {
                Role = role
            };
            return View(model);
        }

        _logger.LogError("No document found with RoleId: {RoleId}", id);
        return NotFound();   
    }

    /// <summary>
    /// Edit a Role in the database collection named "roles"
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult>EditRole(AdministrationViewModel model)
    {
        var collection = _firestoreDb.Collection("roles");
        var query = collection.WhereEqualTo("RoleId", model.Role.RoleId);
        var snapshot = await query.GetSnapshotAsync();

        if(!snapshot.Documents.Any())
        {
            _logger.LogError("Document not found with RoleId: {RoleId}", model.Role.RoleId);
            return NotFound();
        }

        var documentReference = snapshot.Documents[0].Reference;


        var updateData = new Dictionary<string, object>
        {
            {"RoleId", model.Role.RoleId},
            {"RoleName", model.Role.RoleName}
        };

        await documentReference.SetAsync(updateData);

        return RedirectToAction("ViewAllRoles");
    }


    /// <summary>
    /// Delete a Role in the Firestore database collection "roles"
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>DeleteRole(string id)
    {
        var collection = _firestoreDb.Collection("roles");
        var query = collection.WhereEqualTo("RoleId", id);
        var snapshot = await query.GetSnapshotAsync();
        
        if(snapshot.Documents.Any())
        {
            var document = snapshot.Documents[0];
            var role = document.ConvertTo<Role>();

            // logger info used in debugging
            // _logger.LogInformation("Document found: {DocumentPath}", document.Reference.Path);

            var model = new AdministrationViewModel
            {
                Role = role
            };
            return View(model);
        }

        _logger.LogError("No document found with RoleId: {RoleId}", id);
        return NotFound();   
    }

    /// <summary>
    /// Delete specific Role in the Firestore database collection "roles"
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult>DeleteRole(AdministrationViewModel model)
    {
        var id = model.Role.RoleId;
        var roleName = model.Role.RoleName;

        // if(string.IsNullOrEmpty(id))
        // {
        //     _logger.LogError("Invalid document ID");
        //     return BadRequest("Invalid document ID");
        // }

        var collection = _firestoreDb.Collection("roles");
        var query = collection.WhereEqualTo("RoleId",id);
        var snapshot = await query.GetSnapshotAsync();

        if(!snapshot.Documents.Any())
        {
            _logger.LogError("Document not found: {RoleId}", id);
            return NotFound();
        }

        // Check if the Role.RoleName is referenced in any User documents 
        var usersCollection = _firestoreDb.Collection("users"); 
        var usersQuery = usersCollection.WhereArrayContains("roles", roleName); 
        var usersSnapshot = await usersQuery.GetSnapshotAsync();

        if (usersSnapshot.Documents.Any()) 
        { 
            _logger.LogError("Cannot delete Role {RoleName} because it is currently in use.", roleName); 
            ModelState.AddModelError("roleName", "Cannot delete this role because it is currently in use.");
            ViewData["ErrorMessage"] = "This Role is in use.  Delete not available.";

            // need to return an AdministrationViewModel
            var adminModel = new AdministrationViewModel
            {
                Role = model.Role
            };

            return View(adminModel);
        }

        var documentReference = snapshot.Documents.First().Reference;
        await documentReference.DeleteAsync();

        _logger.LogInformation("Document deleted: {DocumentId}", id);
        
        return RedirectToAction("ViewAllRoles");
    }

    /// <summary>
    ///  View all Users in the Firestore database collection "users"
    /// </summary>
    public async Task<IActionResult>ViewAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return View(users);
    }

    /// <summary>
    /// getter for EditUser
    /// </summary>
    [HttpGet]
    public async Task<IActionResult>EditUser(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Log user roles for debugging
        //_logger.LogInformation("User roles: {roles}", string.Join(", ", user.Roles ?? new List<string>()));

        var viewModel = new EditUserViewModel
        {
            Uid = user.Uid,
            Email = user.Email,
            Roles = user.Roles ?? new List<string>(),
            AllRoles = await GetRolesFromFirestore()
        };

        return View(viewModel);
    }

    ///<summary>
    /// post for EditUser
    ///</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>EditUser(EditUserViewModel model)
    {
        if(!ModelState.IsValid)
        {
            // Fetch roles from Firestore
            var roles = await GetRolesFromFirestore();            
            
            model.AllRoles = roles;

            // Log ModelState errors for debugging 
            foreach (var modelState in ViewData.ModelState.Values) 
            { 
                foreach (var error in modelState.Errors) 
                { 
                    _logger.LogError(error.ErrorMessage); 
                } 
            }
            return View(model);
        }

        //Check if Roles is still null and initialize if needed
        if(model.Roles == null)
        {
            model.Roles = new List<string>();
        }

        await _userService.UpdateUserEmailAsync(model.Uid, model.Email);
        await _userService.UpdateUserRolesAsync(model.Uid, model.Roles);

        return RedirectToAction("ViewAllUsers");
    }

    private async Task<List<string>> GetRolesFromFirestore()
    {
        var roles = new List<string>();
        CollectionReference rolesCollection = _firestoreDb.Collection("roles");
        QuerySnapshot snapshot = await rolesCollection.GetSnapshotAsync();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            if(document.Exists)
            {
                roles.Add(document.GetValue<string>("RoleName"));
            }           
        }
        return roles;
    }

    /// <summary>
    /// getter for DeleteUser
    /// </summary>
    [HttpGet]
    public async Task<IActionResult>DeleteUser(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    /// <summary>
    /// Post for DeleteUser
    /// </summary>
    [HttpPost]
    public async Task<IActionResult>DeleteUser(User user)
    {
        await _userService.DeleteUserAsync(user.Uid);

        return RedirectToAction("ViewAllUsers");
    }

    /// <summary>
    /// View all the Initials in the Firestore database
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>ViewAllInitials(){
        CollectionReference collection2 = _firestoreDb.Collection("initials");
        QuerySnapshot snapshot2 = await collection2.GetSnapshotAsync();
        var initials = snapshot2.Documents.Select(d => d.ConvertTo<InitialsAssigned>()).ToList();

        return View(initials);   
    }

    /// <summary>
    /// Create a new Role in the Firestore database
    /// </summary>
    /// <returns></returns>
    public IActionResult AddInitials()
    {
        return View();
    }

    /// <summary>
    /// Add the new Role to the database collection named "role"
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult>AddInitials(AdministrationViewModel model)
    {
        CollectionReference collection = _firestoreDb.Collection("initials");

        // check if initials document with the same initials already exist
        Query query = collection.WhereEqualTo("initials", model.InitialsAssigned.InitialsAssignedName);
        QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

        if(querySnapshot.Documents.Count > 0)
        {
            ViewData["inUse"] = "These Initials are already in use.  Please select another.";
            return View(model);
        }

        // Add the new Initials
        DocumentReference document = await collection.AddAsync(new InitialsAssigned
        {
            InitialsAssignedId = model.InitialsAssigned.InitialsAssignedId,
            InitialsAssignedName = model.InitialsAssigned.InitialsAssignedName,
            Active = true
        });

        // Retrieve the generated document ID
        string documentId = document.Id;

        // Update the document to include 'InitialsAssignedId' field with the document ID
        await document.UpdateAsync(new Dictionary<string, object>
        {
            { "InitialsAssignedId", documentId}
        });

        return RedirectToAction("AddInitials");
    }


    /// <summary>
    /// Edit Initials in the Firestore database
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult>EditInitials(string id)
    {
        var collection = _firestoreDb.Collection("initials");
        var query = collection.WhereEqualTo("InitialsAssignedId", id);
        var snapshot = await query.GetSnapshotAsync();
        
        if(snapshot.Documents.Any())
        {
            var document = snapshot.Documents[0];
            var initialsTemp = document.ConvertTo<InitialsAssigned>();

            // logger info used in debugging
            // _logger.LogInformation("Document found: {DocumentPath}", document.Reference.Path);

            var model = new AdministrationViewModel
            {
                InitialsAssigned = initialsTemp
            };
            return View(model);
        }

        _logger.LogError("No document found with initials: {initials}", id);
        return NotFound();   
    }

    /// <summary>
    /// Edit initials in the database collection named "initials"
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult>EditInitials(AdministrationViewModel model)
    {
        var collection = _firestoreDb.Collection("initials");
        var query = collection.WhereEqualTo("InitialsAssignedId", model.InitialsAssigned.InitialsAssignedId);
        var snapshot = await query.GetSnapshotAsync();

        if(!snapshot.Documents.Any())
        {
            _logger.LogError("Document not found with initials: {initials}", model.InitialsAssigned.InitialsAssignedId);
            return NotFound();
        }

        var documentReference = snapshot.Documents[0].Reference;


        var updateData = new Dictionary<string, object>
        {
            {"InitialsAssignedName", model.InitialsAssigned.InitialsAssignedName},
            {"InitialsAssignedId", model.InitialsAssigned.InitialsAssignedId},
            {"Active", model.InitialsAssigned.Active}
        };

        await documentReference.SetAsync(updateData);

        return RedirectToAction("ViewAllInitials");
    }
    
}