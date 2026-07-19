using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.QuestTasks.Commands;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.QuestTasks;

public class CreateQuestTaskListCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private TestDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(opts);
    }

    private ICurrentUserService MockUser() 
    {
        var svc = Substitute.For<ICurrentUserService>();
        svc.UserId.Returns(UserId);
        return svc;
    }

    [Fact]
    public async Task Handle_CreatesListWithCorrectUserIdAndName()
    {
        using var db = CreateDb();
        var sut = new CreateQuestTaskListCommandHandler(db, MockUser());

        var id = await sut.Handle(new CreateQuestTaskListCommand("Shopping"), CancellationToken.None);

        var list = await db.QuestTaskLists.SingleAsync();
        list.Id.ShouldBe(id);
        list.Name.ShouldBe("Shopping");
        list.UserId.ShouldBe(UserId);
        list.SortOrder.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_IncrementsMaxSortOrder_WhenListsAlreadyExist()
    {
        using var db = CreateDb();
        db.QuestTaskLists.Add(new QuestTaskList { Name = "Existing", UserId = UserId, SortOrder = 5 });
        await db.SaveChangesAsync();

        var sut = new CreateQuestTaskListCommandHandler(db, MockUser());
        await sut.Handle(new CreateQuestTaskListCommand("New"), CancellationToken.None);

        var lists = await db.QuestTaskLists.ToListAsync();
        lists.Count.ShouldBe(2);
        lists.Max(l => l.SortOrder).ShouldBe(6);
    }
}
