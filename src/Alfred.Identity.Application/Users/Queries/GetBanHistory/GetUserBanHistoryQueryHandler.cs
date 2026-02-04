using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Users.Queries.GetBanHistory;

public record GetUserBanHistoryQuery(Guid UserId) : IRequest<List<BanDto>>;

public class GetUserBanHistoryQueryHandler : IRequestHandler<GetUserBanHistoryQuery, List<BanDto>>
{
    private readonly IUserBanRepository _userBanRepository;

    public GetUserBanHistoryQueryHandler(IUserBanRepository userBanRepository)
    {
        _userBanRepository = userBanRepository;
    }

    public async Task<List<BanDto>> Handle(GetUserBanHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _userBanRepository.GetHistoryByUserIdAsync(request.UserId, cancellationToken);
        return history.Select(b => BanDto.FromEntity(b)).ToList();
    }
}

