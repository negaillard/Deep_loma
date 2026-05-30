using Microsoft.EntityFrameworkCore.Storage;

namespace Storage;

internal static class StorageTransactionHelper
{
	public static async Task<T> ExecuteInTransactionAsync<T>(
		StorageContext context,
		Func<Task<T>> action)
	{
		await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();
		try
		{
			T result = await action();
			await transaction.CommitAsync();
			return result;
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}
	}

	public static async Task ExecuteInTransactionAsync(
		StorageContext context,
		Func<Task> action)
	{
		await ExecuteInTransactionAsync<object?>(context, async () =>
		{
			await action();
			return null;
		});
	}
}
