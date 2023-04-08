public class BaseSocketClient
{
    #region MessageDeleted

    public void HookMessageDeleted(BaseSocketClient client)
    {
        client.MessageDeleted += HandleMessageDelete;
    }

    public Task HandleMessageDelete(Cacheable<IMessage, ulong> cachedMessage, ISocketMessageChannel channel)
    {
        // check if the message exists in cache; if not, we cannot report what was removed
        if (!cachedMessage.HasValue) return;
        var message = cachedMessage.Value;
        Console.WriteLine($"A message ({message.Id}) from {message.Author} was removed from the channel {channel.Name} ({channel.Id}):"
            + Environment.NewLine
            + message.Content);
        return Task.CompletedTask;
    }

    #endregion
}