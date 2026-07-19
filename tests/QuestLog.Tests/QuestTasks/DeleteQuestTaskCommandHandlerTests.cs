using Microsoft.EntityFrameworkCore;
using QuestLog.Application.QuestTasks.Commands;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.QuestTasks;

public class DeleteQuestTaskCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private TestDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(opts);
    }

    private async Task<(int listId, int taskId)> SeedTask(TestDbContext db)
    {
        var list = new QuestTaskList { Name = "Life", UserId = UserId, SortOrder = 1 };
        db.QuestTaskLists.Add(list);
        await db.SaveChangesAsync();

        var task = new QuestTask { QuestTaskListId = list.Id, Name = "Call dentist" };
        db.QuestTasks.Add(task);
        await db.SaveChangesAsync();
        return (list.Id, task.Id);
    }

    [Fact]
    public async Task Handle_DeletesTask_WhenItExists()
    {
        using var db = CreateDb();
        var (_, taskId) = await SeedTask(db);
        var sut = new DeleteQuestTaskCommandHandler(db);

        await sut.Handle(new DeleteQuestTaskCommand(taskId), CancellationToken.None);

        var remaining = await db.QuestTasks.CountAsync();
        remaining.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperation_WhenTaskNotFound()
    {
        using var db = CreateDb();
        var sut = new DeleteQuestTaskCommandHandler(db);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.Handle(new DeleteQuestTaskCommand(999), CancellationToken.None)
        );
        ex.Message.ShouldContain("Task not found");
    }
}
