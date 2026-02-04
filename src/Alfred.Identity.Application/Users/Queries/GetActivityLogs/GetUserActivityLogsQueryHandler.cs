using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Users.Queries.GetActivityLogs;

public record GetUserActivityLogsQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<ActivityLogPageResult>;

public record ActivityLogPageResult(List<ActivityLogDto> Items, int TotalCount, int Page, int PageSize);

public class GetUserActivityLogsQueryHandler : IRequestHandler<GetUserActivityLogsQuery, ActivityLogPageResult>
{
    private readonly IUserActivityLogRepository _activityLogRepository;

    public GetUserActivityLogsQueryHandler(IUserActivityLogRepository activityLogRepository)
    {
        _activityLogRepository = activityLogRepository;
    }

    public async Task<ActivityLogPageResult> Handle(GetUserActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _activityLogRepository.GetPagedAsync(
            request.UserId, 
            request.Page, 
            request.PageSize, 
            cancellationToken);

        var dtos = items.Select(l => ActivityLogDto.FromEntity(l)).ToList();

        return new ActivityLogPageResult(dtos, totalCount, request.Page, request.PageSize);
    }
}

