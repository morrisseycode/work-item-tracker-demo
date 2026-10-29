using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WorkItemTracker.Api.Auth;
using WorkItemTracker.Api.Contracts;
using WorkItemTracker.Api.Data;
using WorkItemTracker.Api.Domain;

namespace WorkItemTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workitems")]
public class WorkItemsController(
    AppDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ControllerBase
{
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<IEnumerable<WorkItemResponse>> GetWorkItems(
        int? projectId,
        WorkItemStatus? status,
        DateOnly? dueBefore,
        [Range(1, int.MaxValue / MaxPageSize)] int page = 1,
        [Range(1, MaxPageSize)] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = OwnedWorkItems().AsNoTracking();

        if (projectId is not null)
        {
            query = query.Where(w => w.ProjectId == projectId);
        }

        if (status is not null)
        {
            query = query.Where(w => w.Status == status);
        }

        if (dueBefore is not null)
        {
            query = query.Where(w => w.DueDate < dueBefore);
        }

        var workItems = await query
            .OrderBy(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return workItems.Select(WorkItemResponse.FromEntity);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkItemResponse>> GetWorkItem(
        int id,
        CancellationToken cancellationToken)
    {
        var workItem = await OwnedWorkItems()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        return workItem is null ? NotFound() : WorkItemResponse.FromEntity(workItem);
    }

    [HttpPost]
    public async Task<ActionResult<WorkItemResponse>> CreateWorkItem(
        CreateWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var ownsProject = await db.Projects.AnyAsync(
            p => p.Id == request.ProjectId && p.OwnerId == userId, cancellationToken);
        if (!ownsProject)
        {
            return NotFound();
        }

        if (!await AssigneeExistsAsync(request.AssigneeId, cancellationToken))
        {
            return UnknownAssignee();
        }

        var workItem = NewWorkItem(request, userId);
        db.WorkItems.Add(workItem);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(workItem).Reference(w => w.Assignee).LoadAsync(cancellationToken);

        var response = WorkItemResponse.FromEntity(workItem);
        return CreatedAtAction(nameof(GetWorkItem), new { id = workItem.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkItemResponse>> UpdateWorkItem(
        int id,
        UpdateWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await OwnedWorkItems()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (workItem is null)
        {
            return NotFound();
        }

        if (!await AssigneeExistsAsync(request.AssigneeId, cancellationToken))
        {
            return UnknownAssignee();
        }

        workItem.Title = request.Title;
        workItem.Description = request.Description;
        workItem.Priority = request.Priority;
        workItem.DueDate = request.DueDate;
        workItem.AssigneeId = request.AssigneeId;
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(workItem).Reference(w => w.Assignee).LoadAsync(cancellationToken);

        return WorkItemResponse.FromEntity(workItem);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<WorkItemResponse>> UpdateWorkItemStatus(
        int id,
        UpdateWorkItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await OwnedWorkItems()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (workItem is null)
        {
            return NotFound();
        }

        var status = request.Status!.Value;
        if (status != WorkItemStatus.Completed)
        {
            workItem.CompletedAt = null;
        }
        else if (workItem.Status != WorkItemStatus.Completed)
        {
            workItem.CompletedAt = timeProvider.GetUtcNow();
        }

        workItem.Status = status;
        await db.SaveChangesAsync(cancellationToken);

        return WorkItemResponse.FromEntity(workItem);
    }

    [HttpPut("{id:int}/assignee")]
    public async Task<ActionResult<WorkItemAssigneeChangeResponse>> UpdateWorkItemAssignee(
        int id,
        UpdateWorkItemAssigneeRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await OwnedWorkItems()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (workItem is null)
        {
            return NotFound();
        }

        if (!await AssigneeExistsAsync(request.AssigneeId, cancellationToken))
        {
            return UnknownAssignee();
        }

        var previousAssigneeName = workItem.Assignee.DisplayName;
        workItem.AssigneeId = request.AssigneeId;
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(workItem).Reference(w => w.Assignee).LoadAsync(cancellationToken);

        return new WorkItemAssigneeChangeResponse(
            WorkItemResponse.FromEntity(workItem), previousAssigneeName);
    }

    [HttpGet("overdue")]
    public async Task<IEnumerable<OverdueWorkItemResponse>> GetOverdueWorkItems(
        int projectId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var workItems = await db.WorkItems
            .AsNoTracking()
            .Where(w => w.ProjectId == projectId && w.DueDate < today)
            .OrderByDescending(w => w.DueDate)
            .ToListAsync(cancellationToken);

        var overdue = new List<OverdueWorkItemResponse>();
        foreach (var workItem in workItems)
        {
            var status = workItem.Status.ToString();
            if (status == "Completed" || status == "Cancelled")
            {
                continue;
            }

            var assignee = await db.Assignees
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == workItem.AssigneeId, cancellationToken);

            var dueDate = workItem.DueDate!.Value;
            overdue.Add(new OverdueWorkItemResponse(
                workItem.Id,
                workItem.Title,
                dueDate,
                today.DayNumber - dueDate.DayNumber,
                assignee?.DisplayName));
        }

        return overdue;
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<WorkItemResponse>> PatchWorkItem(
        int id,
        JsonPatchDocument<WorkItem> patch,
        CancellationToken cancellationToken)
    {
        var workItem = await OwnedWorkItems()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (workItem is null)
        {
            return NotFound();
        }

        patch.ApplyTo(workItem, error =>
            ModelState.AddModelError(string.Empty, error.ErrorMessage));
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await db.SaveChangesAsync(cancellationToken);

        return WorkItemResponse.FromEntity(workItem);
    }

    private WorkItem NewWorkItem(CreateWorkItemRequest request, string ownerId) => new()
    {
        ProjectId = request.ProjectId!.Value,
        Title = request.Title,
        Description = request.Description,
        Priority = request.Priority,
        DueDate = request.DueDate,
        AssigneeId = request.AssigneeId,
        OwnerId = ownerId,
        CreatedAt = timeProvider.GetUtcNow(),
    };

    private async Task<bool> AssigneeExistsAsync(
        int? assigneeId,
        CancellationToken cancellationToken) =>
        assigneeId is null
        || await db.Assignees.AnyAsync(a => a.Id == assigneeId, cancellationToken);

    private ActionResult UnknownAssignee()
    {
        ModelState.AddModelError(
            nameof(CreateWorkItemRequest.AssigneeId), "No assignee has this id.");
        return ValidationProblem(ModelState);
    }

    private IQueryable<WorkItem> OwnedWorkItems()
    {
        var userId = currentUser.UserId;

        return db.WorkItems
            .Include(w => w.Assignee)
            .Where(w => w.OwnerId == userId);
    }
}
