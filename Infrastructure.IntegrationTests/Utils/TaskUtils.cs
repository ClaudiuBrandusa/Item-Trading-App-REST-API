namespace Infrastructure.IntegrationTests.Utils;

public static class TaskUtils
{
    public static Task TimeoutAfterMilliseconds<T>(this TaskCompletionSource<T> tcs, int milliseconds)
    {
        return Task.Run(async () =>
        {
            await Task.Delay(milliseconds);

            if (tcs.Task.IsCompleted)
                return;

            tcs.TrySetCanceled();
        });
    }
}
