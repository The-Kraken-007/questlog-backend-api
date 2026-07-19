using Microsoft.EntityFrameworkCore;
using QuestLog.Application.QuestTasks.Commands;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.QuestTasks;

public class DeleteQuestTaskListCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private TestDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(opts);
    }

    private async Task<int> SeedList(TestDbContext db, string name = "Shopping")
    {
        var list = new QuestTaskList { Name = name, UserId = UserId, SortOrder = 1 };
        db.QuestTaskLists.Add(list);
        await db.SaveChangesAsync();
        return list.Id;
    }

    [Fact]
    public async Task Handle_DeletesList_WhenItExists()
    {
        using var db = CreateDb();
        var listId = await SeedList(db);
        var sut = new DeleteQuestTaskListCommandHandler(db);

        await sut.Handle(new DeleteQuestTaskListCommand(listId), CancellationToken.None);

        var remaining = await db.QuestTaskLists.CountAsync();
        remaining.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperation_WhenListNotFound()
    {
        using var db = CreateDb();
        var sut = new DeleteQuestTaskListCommandHandler(db);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.Handle(new DeleteQuestTaskListCommand(999), CancellationToken.None)
        );
        ex.Message.ShouldContain("Task List not found");
    }
}
