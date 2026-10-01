using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ConfidraApi.Controllers;
[ApiController,Route("api/stats"),Authorize(Roles="Operations")]
public sealed class StatsController:ControllerBase { [HttpGet] public IActionResult Get()=>Ok(new{message="Public patient and clinician counts are not published."}); }
