using Microsoft.EntityFrameworkCore;
using QuestLog.Application.QuestTasks.Commands;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.QuestTasks;

public class ToggleQuestTaskCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private TestDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(opts);
    }

    private async Task<(int listId, int taskId)> SeedTask(TestDbContext db, bool isCompleted = false)
    {
        var list = new QuestTaskList { Name = "Work", UserId = UserId, SortOrder = 1 };
        db.QuestTaskLists.Add(list);
        await db.SaveChangesAsync();

        var task = new QuestTask { QuestTaskListId = list.Id, Name = "Write report", IsCompleted = isCompleted };
        db.QuestTasks.Add(task);
        await db.SaveChangesAsync();
        return (list.Id, task.Id);
    }

    [Fact]
    public async Task Handle_TogglesTask_FromFalseToTrue()
    {
        using var db = CreateDb();
        var (_, taskId) = await SeedTask(db, isCompleted: false);
        var sut = new ToggleQuestTaskCommandHandler(db);

        await sut.Handle(new ToggleQuestTaskCommand(taskId), CancellationToken.None);

        var task = await db.QuestTasks.FindAsync(taskId);
        task!.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_TogglesTask_FromTrueToFalse()
    {
        using var db = CreateDb();
        var (_, taskId) = await SeedTask(db, isCompleted: true);
        var sut = new ToggleQuestTaskCommandHandler(db);

        await sut.Handle(new ToggleQuestTaskCommand(taskId), CancellationToken.None);

        var task = await db.QuestTasks.FindAsync(taskId);
        task!.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperation_WhenTaskNotFound()
    {
        using var db = CreateDb();
        var sut = new ToggleQuestTaskCommandHandler(db);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.Handle(new ToggleQuestTaskCommand(999), CancellationToken.None)
        );
        ex.Message.ShouldContain("Task not found");
    }
}
