using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Models;
using OpsFlow.Application.Rules;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Auditing;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.Api.Endpoints;

public static class KnowledgeEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/articles").WithTags("Knowledge Base").RequireAuthorization();

        group.MapGet("/", async (OpsFlowDbContext db, string? search, string? tag, Guid? serviceId,
            ArticleStatus? status, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        {
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
            var query = db.KnowledgeArticles.AsNoTracking().Where(x => !x.IsArchived);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Title.Contains(search) || x.Summary.Contains(search) || x.ProblemSymptoms.Contains(search));
            if (!string.IsNullOrWhiteSpace(tag)) query = query.Where(x => x.Tags.Contains(tag));
            if (serviceId is not null) query = query.Where(x => x.ServiceId == serviceId);
            if (status is not null) query = query.Where(x => x.Status == status);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(x => x.Status == ArticleStatus.Published)
                .ThenByDescending(x => x.LastReviewedDate).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.ArticleNumber, x.Title, x.Summary, x.Author, x.Tags, x.Status,
                    x.LastReviewedDate, x.ServiceId, Service = x.Service != null ? x.Service.Name : null, x.IncidentId,
                    Helpful = x.Feedback.Count(feedback => feedback.WasHelpful),
                    NotHelpful = x.Feedback.Count(feedback => !feedback.WasHelpful) })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { Items = items, Page = page, PageSize = pageSize, TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
        });

        group.MapGet("/{id:guid}", async (Guid id, OpsFlowDbContext db, CancellationToken cancellationToken) =>
        {
            var article = await db.KnowledgeArticles.AsNoTracking().Include(x => x.Service).Include(x => x.Incident)
                .Include(x => x.Feedback).SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            return article is null ? Results.NotFound() : Results.Ok(article);
        });

        group.MapPost("/", async (ArticleRequest request, HttpContext context, OpsFlowDbContext db,
            AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            var article = Map(request, $"KB-{(await db.KnowledgeArticles.CountAsync(cancellationToken) + 86):0000}");
            db.KnowledgeArticles.Add(article);
            audit.Record(context.User, "Created knowledge draft", "Article", article.ArticleNumber, current: request);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/articles/{article.Id}", article);
        }).RequireAuthorization("KnowledgeWrite");

        group.MapPost("/from-incident/{incidentId:guid}", async (Guid incidentId, HttpContext context,
            OpsFlowDbContext db, AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            var incident = await db.Incidents.Include(x => x.IncidentServices)
                .SingleOrDefaultAsync(x => x.Id == incidentId && !x.IsArchived, cancellationToken);
            if (incident is null) return Results.NotFound();
            if (incident.Status is not IncidentStatus.Resolved and not IncidentStatus.Closed)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["incident"] = ["Only resolved incidents can become articles."] });
            var article = new KnowledgeArticle
            {
                ArticleNumber = $"KB-{(await db.KnowledgeArticles.CountAsync(cancellationToken) + 86):0000}",
                Title = $"Resolution: {incident.Title}", Summary = incident.ResolutionSummary ?? incident.Description,
                ProblemSymptoms = incident.Description, RootCause = "Add the verified root cause.",
                ResolutionSteps = incident.ResolutionSummary ?? "Add the verified resolution steps.",
                Author = context.User.Identity?.Name ?? "Unknown", IncidentId = incident.Id,
                ServiceId = incident.IncidentServices.Select(x => (Guid?)x.ServiceId).FirstOrDefault(), Status = ArticleStatus.Draft
            };
            db.KnowledgeArticles.Add(article);
            audit.Record(context.User, "Converted incident to knowledge draft", "Article", article.ArticleNumber,
                current: new { Incident = incident.IncidentNumber });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/articles/{article.Id}", article);
        }).RequireAuthorization("KnowledgeWrite");

        group.MapPost("/{id:guid}/publish", async (Guid id, HttpContext context, OpsFlowDbContext db,
            AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            var article = await db.KnowledgeArticles.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (article is null) return Results.NotFound();
            ArticleRules.ValidateForPublication(article.Title, article.Summary, article.ProblemSymptoms, article.RootCause, article.ResolutionSteps);
            article.Status = ArticleStatus.Published; article.LastReviewedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            article.UpdatedAt = DateTimeOffset.UtcNow;
            audit.Record(context.User, "Published article", "Article", article.ArticleNumber);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(article);
        }).RequireAuthorization("KnowledgeWrite");

        group.MapPost("/{id:guid}/feedback", async (Guid id, ArticleFeedbackRequest request, OpsFlowDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!await db.KnowledgeArticles.AnyAsync(x => x.Id == id && x.Status == ArticleStatus.Published, cancellationToken))
                return Results.NotFound();
            var feedback = new ArticleFeedback { ArticleId = id, WasHelpful = request.WasHelpful, Comment = request.Comment };
            db.ArticleFeedback.Add(feedback);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Accepted($"/api/articles/{id}", new { feedback.Id });
        });

        return app;
    }

    private static KnowledgeArticle Map(ArticleRequest request, string number) => new()
    {
        ArticleNumber = number, Title = request.Title, Summary = request.Summary,
        ProblemSymptoms = request.ProblemSymptoms, RootCause = request.RootCause,
        ResolutionSteps = request.ResolutionSteps, Author = request.Author, Tags = request.Tags,
        ServiceId = request.ServiceId, IncidentId = request.IncidentId, Status = ArticleStatus.Draft
    };
}
