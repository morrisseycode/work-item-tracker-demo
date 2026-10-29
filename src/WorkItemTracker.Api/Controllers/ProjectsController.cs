using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Auth;
using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Data;
using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController(AppDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ProjectResponse>> GetProjects(
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == userId)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        return projects.Select(ProjectResponse.FromEntity);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> CreateProject(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = new Project { Name = request.Name, OwnerId = currentUser.UserId };

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ProjectResponse.FromEntity(project));
    }
}
