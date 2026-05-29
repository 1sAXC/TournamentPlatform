namespace Auth.Application.Auth.Dto;

public sealed record UserLookupRequest(IReadOnlyCollection<Guid> Ids);
