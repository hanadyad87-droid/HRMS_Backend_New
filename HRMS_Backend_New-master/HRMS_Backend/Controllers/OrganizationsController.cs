using HRMS_Backend.Data;
using HRMS_Backend.Models;
using Microsoft.AspNetCore.Mvc;

[Route("api/organizations")]
[ApiController]
public class OrganizationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrganizationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET ALL
    [HttpGet]
    public IActionResult GetAll()
    {
        var data = _context.Organizations
            .Select(x => new
            {
                x.Id,
                x.Name,
               
            })
            .ToList();

        return Ok(data);
    }

    // ADD
    [HttpPost]
    public IActionResult Add(Organization model)
    {
        _context.Organizations.Add(model);
        _context.SaveChanges();
        return Ok(model);
    }
}