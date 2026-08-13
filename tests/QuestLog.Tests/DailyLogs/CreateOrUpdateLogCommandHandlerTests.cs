using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.DailyLogs.Commands.CreateOrUpdateLog;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.DailyLogs;

public class CreateOrUpdateLogCommandHandlerTests
{
    private readonly IDailyLogRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IXpAwardService _xpAwardService;
    private readonly IAchievementChecker _achievementChecker;
    private readonly CreateOrUpdateLogCommandHandler _sut;

    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public CreateOrUpdateLogCommandHandlerTests()
    {
        _repository = Substitute.For<IDailyLogRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(UserId);

        _xpAwardService = Substitute.For<IXpAwardService>();
        _xpAwardService.AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new XpAwardResult(0, null, null, false, false));

        _achievementChecker = Substitute.For<IAchievementChecker>();
        _achievementChecker.CheckAndUnlockAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Achievement>());

        _sut = new CreateOrUpdateLogCommandHandler(_repository, _currentUserService, _xpAwardService, _achievementChecker);
    }

    [Fact]
    public async Task Handle_CreatesNewLog_WhenNoneExistsForDate()
    {
        var date = new DateOnly(2025, 1, 1);
        _repository.GetByDateAsync(date, Arg.Any<CancellationToken>()).ReturnsNull();

        var command = new CreateOrUpdateLogCommand(date, "  My new log  ");
        var result = await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).Add(Arg.Is<DailyLog>(l =>
            l.Date == date &&
            l.Content == "My new log" &&
            l.UserId == UserId));

        result.Data.Content.ShouldBe("My new log");
        result.Data.Date.ShouldBe(date);
    }

    [Fact]
    public async Task Handle_UpdatesExistingLog_WhenLogExistsForDate()
    {
        var date = new DateOnly(2025, 1, 1);
        var existingLog = new DailyLog { Id = 1, Date = date, Content = "Old content", UpdatedAt = DateTime.UtcNow.AddHours(-1) };
        _repository.GetByDateAsync(date, Arg.Any<CancellationToken>()).Returns(Task.FromResult(existingLog)!);

        var command = new CreateOrUpdateLogCommand(date, "  Updated content  ");
        var result = await _sut.Handle(command, CancellationToken.None);

        _repository.DidNotReceive().Add(Arg.Any<DailyLog>());

        existingLog.Content.ShouldBe("Updated content");

        result.Data.Content.ShouldBe("Updated content");
        result.Data.Id.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_Awards15Xp_OnCreate()
    {
        var date = new DateOnly(2025, 1, 1);
        _repository.GetByDateAsync(date, Arg.Any<CancellationToken>()).ReturnsNull();

        await _sut.Handle(new CreateOrUpdateLogCommand(date, "New log"), CancellationToken.None);

        await _xpAwardService.Received(1).AwardXpAsync(
            UserId, 15, XpSource.DailyLog, "2025-01-01", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotAwardXp_OnUpdate()
    {
        var date = new DateOnly(2025, 1, 1);
        var existing = new DailyLog { Id = 1, Date = date, Content = "Old", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _repository.GetByDateAsync(date, Arg.Any<CancellationToken>()).Returns(Task.FromResult(existing)!);

        await _sut.Handle(new CreateOrUpdateLogCommand(date, "Updated content"), CancellationToken.None);

        await _xpAwardService.DidNotReceive().AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RunsAchievementChecker_OnCreate()
    {
        var date = new DateOnly(2025, 1, 1);
        _repository.GetByDateAsync(date, Arg.Any<CancellationToken>()).ReturnsNull();

        await _sut.Handle(new CreateOrUpdateLogCommand(date, "New log"), CancellationToken.None);

        await _achievementChecker.Received(1).CheckAndUnlockAsync(Arg.Any<CancellationToken>());
    }
}