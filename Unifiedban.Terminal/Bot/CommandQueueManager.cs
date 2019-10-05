using System.Linq;
using System.Collections.Concurrent;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using System;

namespace Unifiedban.Terminal.Bot
{
    public class CommandQueueManager
    {
        static bool isInitialized = false;
        static bool isDisposing = false;

        public static ConcurrentDictionary<long, CommandMessage> CommandsWaitingReply =
            new ConcurrentDictionary<long, CommandMessage>();

        public static void Initialize()
        {
            if (CacheData.FatalError)
                return;

            isInitialized = true;
        }

        public static void Dispose()
        {
            isDisposing = true;
        }

        public static void EnqueueMessage(CommandMessage commandMessage)
        {
            if (!isInitialized || isDisposing)
                return;

            if (CommandsWaitingReply
                .ContainsKey(commandMessage.Message.MessageId))
                return;

            CommandsWaitingReply.TryAdd(commandMessage.Message.MessageId,
                commandMessage);
        }
        public static void DenqueueMessage(CommandMessage commandMessage)
        {
            if (!isInitialized || isDisposing)
                return;

            if (!CommandsWaitingReply
                .ContainsKey(commandMessage.Message.MessageId))
                return;

            CommandsWaitingReply.TryRemove(commandMessage.Message.MessageId,
                out CommandMessage removed);
        }

        public static void ReplyMessage(Message message)
        {
            if (!isInitialized || isDisposing)
                return;

            try
            {
                long realReplyToMessage = Convert.ToInt64(message.ReplyToMessage.Text.Split("[r:")[1].Split(']')[0]);
                if (!CommandsWaitingReply
                    .TryGetValue(realReplyToMessage,
                    out CommandMessage commandMessage))
                    return;

                switch (commandMessage.Command)
                {
                    case "AddTranslationKey":
                        Command.AddTranslation.AddTranslationKey(commandMessage, message);
                        break;
                    case "AddTranslationEntry":
                        Command.AddTranslation.AddTranslationEntry(commandMessage, message);
                        break;
                }
            }
            catch
            {
                return;
            }
        }
    }
}
