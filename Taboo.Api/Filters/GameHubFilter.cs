using Microsoft.AspNetCore.SignalR;

namespace Taboo.Api.Filters;

public class GameHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            var logger = invocationContext.ServiceProvider
                .GetRequiredService<ILogger<GameHubFilter>>();

            logger.LogError(ex, "Erro não tratado no Hub ao executar {MethodName}",
                invocationContext.HubMethodName);

            if (invocationContext.HubMethod.ReturnType == typeof(Task<bool>))
            {
                return false;
            }

            throw;
        }
    }
}
