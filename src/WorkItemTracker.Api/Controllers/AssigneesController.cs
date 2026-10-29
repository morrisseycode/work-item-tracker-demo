using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Data;

namespace WorkItemTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assignees")]
public class AssigneesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<AssigneeResponse>> GetAssignees(
        CancellationToken cancellationToken)
    {
        var assignees = await db.Assignees
            .AsNoTracking()
            .OrderBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);

        return assignees.Select(AssigneeResponse.FromEntity);
    }
}
