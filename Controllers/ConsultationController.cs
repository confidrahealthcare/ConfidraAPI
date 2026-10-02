using Microsoft.AspNetCore.Mvc;
namespace ConfidraApi.Controllers;
[ApiController,Route("api/consultations")]
public sealed class ConsultationController:ControllerBase { [HttpPost] public IActionResult Create()=>StatusCode(410,new{message="Use the care-team scheduling page. This endpoint no longer collects public enquiries."}); }
