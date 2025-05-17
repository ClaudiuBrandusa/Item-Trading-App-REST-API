using Infrastructure.Common.DatabaseContextTransaction;
using Infrastructure.Common.ExecutionStrategy;
using Infrastructure.Common.Transaction;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Reflection;

namespace Infrastructure_UnitTests.CommonTests;

public class ResilientTransactionTests
{
    private readonly IDatabaseContextTransactionWrapper _databaseContextTransactionWrapper;
    private readonly Dictionary<string, bool> _transactionCommitedStatus = new Dictionary<string, bool>();

    public ResilientTransactionTests()
    {
        var databaseContextWrapperMock = new Mock<IDatabaseContextTransactionWrapper>();
        var executionStrategyMock = new Mock<IExecutionStrategyWrapper>();
        var dbContextTransactionMock = new Mock<IDbContextTransaction>();

        databaseContextWrapperMock.Setup(x => x.CreateExecutionStrategy())
            .Returns(executionStrategyMock.Object);
        databaseContextWrapperMock.Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(dbContextTransactionMock.Object);

        executionStrategyMock.Setup(x => x.ExecuteAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> func) =>
            {
                await func();
            });

        dbContextTransactionMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        dbContextTransactionMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _databaseContextTransactionWrapper = databaseContextWrapperMock.Object;
    }

    internal class MockData
    {
        public string Data { get; set; }
    }

    [Fact(DisplayName = "Execute a transaction that will be committed")]
    public async Task ExecuteAsync_ExecuteTransaction_TransactionShouldBeCommitted()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        // Act
        
        await transaction.ExecuteAsync(() =>
        {
            return Task.FromResult(true);
        });

        // Assert

        Assert.True(WasTransactionCommited(transaction));
    }

    [Fact(DisplayName = "Execute a transaction that will be rolled back")]
    public async Task ExecuteAsync_ExecuteTransactionWithFalseResult_TransactionShouldNotBeCommitted()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        // Act

        await transaction.ExecuteAsync(() =>
        {
            return Task.FromResult(false);
        });

        // Assert

        Assert.False(WasTransactionCommited(transaction));
    }

    [Fact(DisplayName = "Execute a transaction with an unhandled exception that will be rolled back")]
    public async Task ExecuteAsync_ExecuteTransactionWithException_TransactionShouldNotBeCommited()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        // Act

        await transaction.ExecuteAsync(() =>
        {
            object x = null;
            x!.ToString();
            return Task.FromResult(true);
        });

        // Assert

        Assert.False(WasTransactionCommited(transaction));
    }

    [Fact(DisplayName = "Execute a transaction without calling the SetResult method that will have the transaction to be committed")]
    public async Task ExecuteAsync_ExecuteTransactionWithoutCallingSetResult_ShouldReturnNull()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        var data = new MockData
        {
            Data = "Test data"
        };

        // Act

        var result = await transaction.ExecuteAsync<MockData>(async (taskCompletionSource) =>
        {
            return true;
        });

        // Assert

        Assert.Null(result);
        Assert.True(WasTransactionCommited(transaction));
    }

    [Fact(DisplayName = "Execute a transaction by calling the set result, the transaction should be committed and return the input mock data")]
    public async Task ExecuteAsync_ExecuteTransactionByCallingSetResult_ShouldReturnTheSampleData()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        var mockData = new MockData
        {
            Data = "Test data"
        };

        // Act

        var result = await transaction.ExecuteAsync<MockData>(async (taskCompletionSource) =>
        {
            taskCompletionSource.SetResult(mockData);
            return true;
        });

        // Assert

        Assert.NotNull(result);
        Assert.StrictEqual(mockData, result);
        Assert.True(WasTransactionCommited(transaction));
    }

    [Fact(DisplayName = "Execute the transaction but make sure that the transaction will be rolled back")]
    public async Task ExecuteAsync_ExecuteTransactionByCallingSetResultButEnsureItWillRollback_ShouldReturnTheSampleData()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        var data = new MockData
        {
            Data = "Test data"
        };

        // Act

        var result = await transaction.ExecuteAsync<MockData>(async (taskCompletionSource) =>
        {
            taskCompletionSource.SetResult(data);
            return false;
        });

        // Assert

        Assert.NotNull(result);
        Assert.StrictEqual(data, result);
        Assert.False(WasTransactionCommited(transaction));
    }

    [Fact(DisplayName = "Execute the transaction by calling the set exception method")]
    public async Task ExecuteAsync_ExecuteTransactionByCallingSetException_ShouldReturnNull()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        // Act

        var result = await transaction.ExecuteAsync<MockData>(async (taskCompletionSource) =>
        {
            taskCompletionSource.SetException(new ArgumentNullException());
            return true;
        });

        // Assert

        Assert.Null(result);
        Assert.False(WasTransactionCommited(transaction));
    }

    [Fact(DisplayName = "Execute the transaction by adding an unhandled exception")]
    public async Task ExecuteAsync_ExecuteTransactionWithAnException_ShouldReturnNull()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        // Act

        var result = await transaction.ExecuteAsync<MockData>(async (taskCompletionSource) =>
        {
            object x = null;
            x!.ToString();
            return true;
        });

        // Assert

        Assert.Null(result);
        Assert.False(WasTransactionCommited(transaction));
    }

    [Fact(DisplayName = "Execute transaction by canceling the transaction")]
    public async Task ExecuteAsync_ExecuteTransactionByCancellingTheTask_ShouldReturnNull()
    {
        // Arrange

        var transaction = ResilientTransaction.New(_databaseContextTransactionWrapper);

        // Act

        var result = await transaction.ExecuteAsync<MockData>(async (taskCompletionSource) =>
        {
            taskCompletionSource.SetCanceled();
            return true;
        });

        // Assert

        Assert.Null(result);
        Assert.False(WasTransactionCommited(transaction));
    }

    private bool WasTransactionCommited(ResilientTransaction transaction)
    {
        var field = transaction.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.FieldType == typeof(bool));
        return field!.GetValue(transaction) as bool? ?? false;
    }
}
