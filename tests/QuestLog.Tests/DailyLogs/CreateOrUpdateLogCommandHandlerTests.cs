using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.DailyLogs.Commands.CreateOrUpdateLog;
using QuestLog.Domain.Entities;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.DailyLogs;

public class CreateOrUpdateLogCommandHandlerTests
{
    private readonly IDailyLogRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly CreateOrUpdateLogCommandHandler _sut;

    public CreateOrUpdateLogCommandHandlerTests()
    {
        _repository = Substitute.For<IDailyLogRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        _sut = new CreateOrUpdateLogCommandHandler(_repository, _currentUserService);
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
            l.UserId == Guid.Parse("33333333-3333-3333-3333-333333333333")));

        result.Content.ShouldBe("My new log");
        result.Date.ShouldBe(date);
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
        
        result.Content.ShouldBe("Updated content");
        result.Id.ShouldBe(1);
    }
}
