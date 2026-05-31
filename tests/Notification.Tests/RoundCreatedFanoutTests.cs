using Notification.Application.Notifications.Abstractions;
using Notification.Application.Notifications.Services;
using TournamentPlatform.Contracts.Events;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Tests;

public sealed class RoundCreatedFanoutTests
{
    [Fact]
    public async Task Fanout_CreatesOneNotificationPerMemberOfBothTeams()
    {
        var repository = new InMemoryNotificationRepository();
        var fanout = new RoundCreatedFanout(repository);

        var teamA = CreateTeam("Team A", playerIds: ["a1", "a2"]);
        var teamB = CreateTeam("Team B", playerIds: ["b1", "b2"]);
        var match = CreateMatch(matchNumber: 1, teamA, teamB);
        var integrationEvent = CreateRoundCreatedEvent(roundNumber: 1, [teamA, teamB], [match]);

        var created = await fanout.FanoutAsync(integrationEvent);

        Assert.Equal(4, created);
        Assert.Equal(4, repository.Notifications.Count);
        Assert.Contains(repository.Notifications, n => n.RecipientUserId == teamA.Members.First().UserId);
        Assert.Contains(repository.Notifications, n => n.RecipientUserId == teamB.Members.First().UserId);
        Assert.All(repository.Notifications, n => Assert.Equal(integrationEvent.EventId, n.SourceEventId));
    }

    [Fact]
    public async Task Fanout_OneEightTeamRound_CreatesEightNotifications()
    {
        var repository = new InMemoryNotificationRepository();
        var fanout = new RoundCreatedFanout(repository);

        var teams = Enumerable.Range(1, 8)
            .Select(i => CreateTeam($"Team{i}", playerIds: [$"p{i}"]))
            .ToArray();
        var matches = Enumerable.Range(0, 4)
            .Select(i => CreateMatch(matchNumber: i + 1, teams[i * 2], teams[i * 2 + 1]))
            .ToArray();
        var integrationEvent = CreateRoundCreatedEvent(roundNumber: 1, teams, matches);

        var created = await fanout.FanoutAsync(integrationEvent);

        Assert.Equal(8, created);
        Assert.Equal(8, repository.Notifications.Select(n => n.RecipientUserId).Distinct().Count());
    }

    [Fact]
    public async Task Fanout_SkipsByeMatchesWithoutOpponent()
    {
        var repository = new InMemoryNotificationRepository();
        var fanout = new RoundCreatedFanout(repository);

        var teamA = CreateTeam("Team A", playerIds: ["a1"]);
        var teamB = CreateTeam("Team B", playerIds: ["b1"]);
        var realMatch = CreateMatch(matchNumber: 1, teamA, teamB);
        var byeMatch = new EventMatchDto
        {
            MatchId = Guid.NewGuid(),
            MatchNumber = 2,
            TeamAId = teamA.TeamId,
            TeamBId = null
        };
        var integrationEvent = CreateRoundCreatedEvent(roundNumber: 1, [teamA, teamB], [realMatch, byeMatch]);

        var created = await fanout.FanoutAsync(integrationEvent);

        // Only the real match should produce notifications; the bye match
        // (no opponent) is ignored on purpose so we don't ask a player to
        // arrange a meeting that doesn't exist.
        Assert.Equal(2, created);
    }

    [Fact]
    public async Task Fanout_CalledTwiceWithSameEvent_IsIdempotent()
    {
        var repository = new InMemoryNotificationRepository();
        var fanout = new RoundCreatedFanout(repository);

        var teamA = CreateTeam("Team A", playerIds: ["a1", "a2"]);
        var teamB = CreateTeam("Team B", playerIds: ["b1", "b2"]);
        var match = CreateMatch(matchNumber: 1, teamA, teamB);
        var integrationEvent = CreateRoundCreatedEvent(roundNumber: 1, [teamA, teamB], [match]);

        var firstRun = await fanout.FanoutAsync(integrationEvent);
        var secondRun = await fanout.FanoutAsync(integrationEvent);

        Assert.Equal(4, firstRun);
        Assert.Equal(0, secondRun);
        Assert.Equal(4, repository.Notifications.Count);
    }

    [Fact]
    public async Task Fanout_BodyAndLinkPointToCorrectMatch()
    {
        var repository = new InMemoryNotificationRepository();
        var fanout = new RoundCreatedFanout(repository);

        var teamA = CreateTeam("Лучшие", playerIds: ["a1"]);
        var teamB = CreateTeam("Соперник", playerIds: ["b1"]);
        var match = CreateMatch(matchNumber: 1, teamA, teamB);
        var integrationEvent = CreateRoundCreatedEvent(roundNumber: 3, [teamA, teamB], [match]);

        await fanout.FanoutAsync(integrationEvent);

        var aliceNotification = repository.Notifications.Single(n => n.RecipientUserId == teamA.Members.First().UserId);
        Assert.Contains("Соперник", aliceNotification.Body);
        Assert.Contains("3", aliceNotification.Body);
        Assert.Equal(
            $"/tournaments/{integrationEvent.TournamentId}/matches/{match.MatchId}",
            aliceNotification.LinkUrl);
    }

    private static EventTeamDto CreateTeam(string name, string[] playerIds)
    {
        var members = playerIds.Select((nick, index) => new EventTeamMemberDto
        {
            UserId = Guid.NewGuid(),
            Nickname = nick,
            Elo = 1000 + index,
            IsCaptain = index == 0,
        }).ToArray();

        return new EventTeamDto
        {
            TeamId = Guid.NewGuid(),
            Name = name,
            CaptainUserId = members[0].UserId,
            Members = members,
        };
    }

    private static EventMatchDto CreateMatch(int matchNumber, EventTeamDto teamA, EventTeamDto teamB)
    {
        return new EventMatchDto
        {
            MatchId = Guid.NewGuid(),
            MatchNumber = matchNumber,
            TeamAId = teamA.TeamId,
            TeamBId = teamB.TeamId,
        };
    }

    private static RoundCreatedEvent CreateRoundCreatedEvent(
        int roundNumber,
        IReadOnlyCollection<EventTeamDto> teams,
        IReadOnlyCollection<EventMatchDto> matches)
    {
        return new RoundCreatedEvent
        {
            TournamentId = Guid.NewGuid(),
            TournamentTitle = "Test Cup",
            DisciplineCode = "CS2",
            OrganizerId = Guid.NewGuid(),
            RoundId = Guid.NewGuid(),
            RoundNumber = roundNumber,
            BracketType = "Main",
            Teams = teams,
            Matches = matches,
        };
    }

    private sealed class InMemoryNotificationRepository : INotificationRepository
    {
        public List<NotificationEntity> Notifications { get; } = [];

        public Task<IReadOnlyCollection<NotificationEntity>> GetForUserAsync(Guid recipientUserId, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<NotificationEntity>>(
                Notifications
                    .Where(n => n.RecipientUserId == recipientUserId)
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .Skip(skip)
                    .Take(take)
                    .ToArray());

        public Task<int> CountForUserAsync(Guid recipientUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(n => n.RecipientUserId == recipientUserId));

        public Task<int> CountUnreadForUserAsync(Guid recipientUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(n => n.RecipientUserId == recipientUserId && !n.IsRead));

        public Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.FirstOrDefault(n => n.Id == id));

        public Task<bool> ExistsForSourceEventAsync(Guid sourceEventId, Guid recipientUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Any(n => n.SourceEventId == sourceEventId && n.RecipientUserId == recipientUserId));

        public void Add(NotificationEntity notification) => Notifications.Add(notification);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
