using JobTracker.Models;
using JobTracker.Services;
using JobTracker.ViewModels;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


public class FirestoreController : Controller
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<FirestoreController> _logger;

    public FirestoreController(FirestoreDb firestoreDb, ILogger<FirestoreController> logger){
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    /// <summary>
    /// View all the data in the Firestore database
    /// </summary>
    /// <returns></returns>
        [AuthorizeRoles("supervisor","administrator")]
        public async Task<IActionResult>ViewAllData(){
        CollectionReference collection = _firestoreDb.Collection("DemoJobData");
        QuerySnapshot snapshot = await collection.GetSnapshotAsync();
        var documents = snapshot.Documents.Select(d => d.ConvertTo<DemoJobData>()).ToList();

        CollectionReference collection2 = _firestoreDb.Collection("JobOp");
        QuerySnapshot snapshot2 = await collection2.GetSnapshotAsync();
        var operations = snapshot2.Documents.Select(d => d.ConvertTo<JobOp>()).ToList();

        // Retrieve InitialsAssigned collection 
        CollectionReference collection3 = _firestoreDb.Collection("initials"); 
        QuerySnapshot snapshot3 = await collection3.GetSnapshotAsync(); 
        var initialsAssigned = snapshot3.Documents.Select(d => d.ConvertTo<InitialsAssigned>()).ToList();

        var result = from d in documents 
                        join o in operations on d.JobOpId equals o.Id
                        join i in initialsAssigned on d.InitialsAssignedId equals i.InitialsAssignedId 
                        select new JobDataViewModel
                            {
                                DemoJobData = d,
                                JobOp = o,
                                InitialsAssigned = i
                            };
                            

        return View(result.ToList().AsEnumerable());   
    }

    /// <summary>
    /// Present the view for entering new Data
    /// </summary>
    /// <returns></returns>
    [AuthorizeRoles("operator", "supervisor","administrator")]
    public async Task<IActionResult> AddData(){
        CollectionReference collection = _firestoreDb.Collection("JobOp");
        Query query = collection.WhereEqualTo("Active", true); // only want the active JobOps for adding data
        QuerySnapshot snapshot = await query.GetSnapshotAsync();
        var operations = snapshot.Documents.Select(d => d.ConvertTo<JobOp>()).ToList();
        ViewData["JobOps"] = new SelectList(operations, "Id", "OpName");

        CollectionReference collection2 = _firestoreDb.Collection("initials");
        Query query2 = collection2.WhereEqualTo("Active", true); // only want the active initials for adding data
        QuerySnapshot snapshot2= await query2.GetSnapshotAsync(); 
        var initialsAssigned = snapshot2.Documents.Select(d => d.ConvertTo<InitialsAssigned>()).ToList();
        ViewData["InitialsAssigned"] = new SelectList(initialsAssigned, "InitialsAssignedId", "InitialsAssignedName");

        return View();
    }

    /// <summary>
    /// Add the new data to the database collection named "DemoJobData"
    /// </summary>
    /// <param name="demoJobData"></param>
    /// <returns></returns>
    [HttpPost]
    [AuthorizeRoles("operator", "supervisor","administrator")]
    public async Task<IActionResult>AddData(JobDataViewModel model)
    {
        model.DemoJobData.JobDataId = Guid.NewGuid().ToString();

        DateTime utcDate = model.DemoJobData.DateToday.Date.ToUniversalTime();

        DateTime dateTime1 = DateTime.ParseExact(model.DemoJobData.StartTime, "HH:mm", null);
        DateTime dateTime2 = DateTime.ParseExact(model.DemoJobData.EndTime, "HH:mm", null);
        TimeSpan difference = dateTime2 - dateTime1;
        
        if(difference.TotalMinutes < 0)
        {
            ModelState.AddModelError(string.Empty, "End time must be after start time");
                CollectionReference refresh = _firestoreDb.Collection("JobOp");
                QuerySnapshot snapshot = await refresh.GetSnapshotAsync();
                var operations = snapshot.Documents.Select(d => d.ConvertTo<JobOp>()).ToList();
                ViewData["JobOps"] = new SelectList(operations, "Id", "OpName");
            return View(model);
        }
        
        CollectionReference collection = _firestoreDb.Collection("DemoJobData");
        DocumentReference document = await collection.AddAsync(new DemoJobData
        {
            JobDataId = model.DemoJobData.JobDataId,
            JobNum = model.DemoJobData.JobNum,
            StartTime = model.DemoJobData.StartTime,
            EndTime = model.DemoJobData.EndTime,
            Quantity = model.DemoJobData.Quantity,
            DateToday = utcDate,
            Minutes = difference.TotalMinutes,
            JobOp = model.JobOp,
            JobOpId = model.JobOp.Id,
            InitialsAssigned = model.InitialsAssigned,
            InitialsAssignedId = model.InitialsAssigned.InitialsAssignedId
        });
        return RedirectToAction("AddData");
    }

    /// <summary>
    /// Edit the data in the Firestore database
    /// </summary>
    /// <returns></returns>
    [AuthorizeRoles("supervisor","administrator")]
    public async Task<IActionResult>EditData(string id)
    {
        CollectionReference collection = _firestoreDb.Collection("DemoJobData");
        QuerySnapshot snapshot = await collection.GetSnapshotAsync();
        var documents = snapshot.Documents.Select(d => d.ConvertTo<DemoJobData>()).ToList();

        CollectionReference collection2 = _firestoreDb.Collection("JobOp");
        Query query2 = collection2.WhereEqualTo("Active", true); // only want the active JobOps for editing data
        QuerySnapshot snapshot2 = await query2.GetSnapshotAsync();
        var operations = snapshot2.Documents.Select(d => d.ConvertTo<JobOp>()).ToList();
        ViewData["JobOps"] = new SelectList(operations, "Id", "OpName");

        // Retrieve InitialsAssigned collection 
        CollectionReference collection3 = _firestoreDb.Collection("initials");
        Query query3 = collection3.WhereEqualTo("Active", true); // only want the active initials for adding data 
        QuerySnapshot snapshot3 = await query3.GetSnapshotAsync(); 
        var initialsAssigned = snapshot3.Documents.Select(d => d.ConvertTo<InitialsAssigned>()).ToList();
        ViewData["InitialsAssigned"] = new SelectList(initialsAssigned, "InitialsAssignedId", "InitialsAssignedName");

        var result = from d in documents 
                        join o in operations on d.JobOpId equals o.Id
                        join i in initialsAssigned on d.InitialsAssignedId equals i.InitialsAssignedId
                        where d.JobDataId == id
                        select new JobDataViewModel
                            {
                                DemoJobData = d,
                                JobOp = o,
                                InitialsAssigned = i
                            };

        var specificDocumentModel = result.FirstOrDefault();

        return View(specificDocumentModel);   
    }

    /// <summary>
    /// Edit a document in the database collection named "DemoJobData"
    /// </summary>
    /// <param name="demoJobData"></param>
    /// <returns></returns>
    [HttpPost]
    [AuthorizeRoles("supervisor","administrator")]
    public async Task<IActionResult>EditData(JobDataViewModel model)
    {
        var jobDataId = model.DemoJobData.JobDataId ?? Guid.NewGuid().ToString(); // coalesce expression checking for null
        DateTime utcDate = model.DemoJobData.DateToday.Date.ToUniversalTime();

        DateTime dateTime1 = DateTime.ParseExact(model.DemoJobData.StartTime, "HH:mm", null);
        DateTime dateTime2 = DateTime.ParseExact(model.DemoJobData.EndTime, "HH:mm", null);
        TimeSpan difference = dateTime2 - dateTime1;

        if(difference.TotalMinutes < 0)
        {
            ModelState.AddModelError(string.Empty, "End time must be after start time");
                CollectionReference refresh = _firestoreDb.Collection("JobOp");
                QuerySnapshot snapshotAnew = await refresh.GetSnapshotAsync();
                var operations = snapshotAnew.Documents.Select(d => d.ConvertTo<JobOp>()).ToList();
                ViewData["JobOps"] = new SelectList(operations, "Id", "OpName");
            return View(model);
        }
        
        var collection = _firestoreDb.Collection("DemoJobData");
        var query = collection.WhereEqualTo("JobDataId", model.DemoJobData.JobDataId);
        var snapshot = await query.GetSnapshotAsync();

        if(!snapshot.Documents.Any())
        {
            _logger.LogError("Document not found with JobDataID: {JobDataId}", model.DemoJobData.JobDataId);
            return NotFound();
        }

        var documentReference = snapshot.Documents[0].Reference;


        var updateData = new Dictionary<string, object>
        {
            {"JobDataId", jobDataId},
            {"JobNum", model.DemoJobData.JobNum},
            {"StartTime", model.DemoJobData.StartTime},
            {"EndTime", model.DemoJobData.EndTime},
            {"Quantity", model.DemoJobData.Quantity},
            {"DateToday", utcDate},
            {"Minutes", difference.TotalMinutes},
            {"JobOpId", model.JobOp.Id},
            {"InitialsAssignedId", model.InitialsAssigned.InitialsAssignedId}
        };

        await documentReference.SetAsync(updateData);

        return RedirectToAction("ViewAllData");
    }

    /// <summary>
    /// Delete specific data document in the Firestore database
    /// </summary>
    /// <returns></returns>
    [AuthorizeRoles("administrator")]
    public async Task<IActionResult>DeleteData(string id)
    {
        CollectionReference collection = _firestoreDb.Collection("DemoJobData");
        QuerySnapshot snapshot = await collection.GetSnapshotAsync();
        var documents = snapshot.Documents.Select(d => d.ConvertTo<DemoJobData>()).ToList();

        CollectionReference collection2 = _firestoreDb.Collection("JobOp");
        QuerySnapshot snapshot2 = await collection2.GetSnapshotAsync();
        var operations = snapshot2.Documents.Select(d => d.ConvertTo<JobOp>()).ToList();
        ViewData["JobOps"] = new SelectList(operations, "Id", "OpName");

        CollectionReference collection3 = _firestoreDb.Collection("initials"); 
        QuerySnapshot snapshot3 = await collection3.GetSnapshotAsync(); 
        var initialsAssigned = snapshot3.Documents.Select(d => d.ConvertTo<InitialsAssigned>()).ToList();
        ViewData["InitialsAssigned"] = new SelectList(initialsAssigned, "InitialsAssignedId", "InitialsAssignedName");

        var result = from d in documents 
                        join o in operations on d.JobOpId equals o.Id
                        join i in initialsAssigned on d.InitialsAssignedId equals i.InitialsAssignedId
                        where d.JobDataId == id
                        select new JobDataViewModel
                            {
                                DemoJobData = d,
                                JobOp = o,
                                InitialsAssigned = i
                            };

        var specificDocumentModel = result.FirstOrDefault();

        return View(specificDocumentModel);   
    }

    /// <summary>
    /// Delete specific data document in the Firestore database
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [AuthorizeRoles("administrator")]
    public async Task<IActionResult>DeleteData(JobDataViewModel model)
    {
        var id = model.DemoJobData.JobDataId;

        if(string.IsNullOrEmpty(id))
        {
            _logger.LogError("Invalid document ID");
            return BadRequest("Invalid document ID");
        }

        var collection = _firestoreDb.Collection("DemoJobData");
        var query = collection.WhereEqualTo("JobDataId",id);
        var snapshot = await query.GetSnapshotAsync();

        if(!snapshot.Documents.Any())
        {
            _logger.LogError("Document not found: {JobDataId}", id);
            return NotFound();
        }

        var documentReference = snapshot.Documents.First().Reference;
        await documentReference.DeleteAsync();
        _logger.LogInformation("Document deleted: {DocumentId}", id);
        
        return RedirectToAction("ViewAllData");
    }


}
