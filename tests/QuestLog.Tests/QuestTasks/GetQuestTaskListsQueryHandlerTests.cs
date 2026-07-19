using Microsoft.EntityFrameworkCore;
using QuestLog.Application.QuestTasks.Queries;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.QuestTasks;

public class GetQuestTaskListsQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    private TestDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(opts);
    }

    [Fact]
    public async Task Handle_ReturnsAllLists_OrderedBySortOrder()
    {
        using var db = CreateDb();
        db.QuestTaskLists.AddRange(
            new QuestTaskList { Name = "Life", UserId = UserId, SortOrder = 2 },
            new QuestTaskList { Name = "Work", UserId = UserId, SortOrder = 1 }
        );
        await db.SaveChangesAsync();

        var sut = new GetQuestTaskListsQueryHandler(db);
        var result = await sut.Handle(new GetQuestTaskListsQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Work");  // SortOrder = 1 first
        result[1].Name.ShouldBe("Life");
    }

    [Fact]
    public async Task Handle_IncludesTasksInsideEachList()
    {
        using var db = CreateDb();
        var list = new QuestTaskList { Name = "Shopping", UserId = UserId, SortOrder = 1 };
        db.QuestTaskLists.Add(list);
        await db.SaveChangesAsync();

        db.QuestTasks.AddRange(
            new QuestTask { QuestTaskListId = list.Id, Name = "Buy apples", IsCompleted = false },
            new QuestTask { QuestTaskListId = list.Id, Name = "Buy bread", IsCompleted = true }
        );
        await db.SaveChangesAsync();

        var sut = new GetQuestTaskListsQueryHandler(db);
        var result = await sut.Handle(new GetQuestTaskListsQuery(), CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Tasks.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoListsExist()
    {
        using var db = CreateDb();
        var sut = new GetQuestTaskListsQueryHandler(db);

        var result = await sut.Handle(new GetQuestTaskListsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
