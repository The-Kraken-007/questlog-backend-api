using Microsoft.EntityFrameworkCore;
using QuestLog.Application.QuestTasks.Commands;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.QuestTasks;

public class CreateQuestTaskCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private TestDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(opts);
    }

    private async Task<int> SeedList(TestDbContext db, string name = "Work")
    {
        var list = new QuestTaskList { Name = name, UserId = UserId, SortOrder = 1 };
        db.QuestTaskLists.Add(list);
        await db.SaveChangesAsync();
        return list.Id;
    }

    [Fact]
    public async Task Handle_CreatesTaskInCorrectList()
    {
        using var db = CreateDb();
        var listId = await SeedList(db);
        var sut = new CreateQuestTaskCommandHandler(db);

        var dueDate = new DateTime(2025, 8, 1);
        var id = await sut.Handle(new CreateQuestTaskCommand(listId, "Buy milk", dueDate), CancellationToken.None);

        var task = await db.QuestTasks.SingleAsync();
        task.Id.ShouldBe(id);
        task.Name.ShouldBe("Buy milk");
        task.QuestTaskListId.ShouldBe(listId);
        task.DueDate.ShouldBe(dueDate);
        task.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_CreatesTaskWithNullDueDate_WhenNotProvided()
    {
        using var db = CreateDb();
        var listId = await SeedList(db);
        var sut = new CreateQuestTaskCommandHandler(db);

        await sut.Handle(new CreateQuestTaskCommand(listId, "No deadline task", null), CancellationToken.None);

        var task = await db.QuestTasks.SingleAsync();
        task.DueDate.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperation_WhenListDoesNotExist()
    {
        using var db = CreateDb();
        var sut = new CreateQuestTaskCommandHandler(db);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.Handle(new CreateQuestTaskCommand(999, "Orphan task", null), CancellationToken.None)
        );
        ex.Message.ShouldContain("Task List not found");
    }
}
