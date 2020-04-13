using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class AddNote : ICommand
    {
        BusinessLogic.Group.NoteLogic noteLogic = new BusinessLogic.Group.NoteLogic();

        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Basic) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return;
            }

            if(message.ReplyToMessage != null)
            {
                if (!message.ReplyToMessage.Text.StartsWith("#"))
                {
                   MessageQueueManager.EnqueueMessage(
                       new ChatMessage()
                       {
                           Timestamp = DateTime.UtcNow,
                           Chat = message.Chat,
                           ReplyToMessageId = message.MessageId,
                           Text = CacheData.GetTranslation("en", "error_addnote_command_onreply")
                       });
                   return;
                }

                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.ReplyToMessage.MessageId);
                SaveNote(message.ReplyToMessage);
                return;
            }

            if (!message.Text.Remove(0, 9).StartsWith("#"))
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ReplyToMessageId = message.MessageId,
                        Text = CacheData.GetTranslation("en", "error_addnote_command_starttag")
                    });
                return;
            }

            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            SaveNote(message);
        }

        public void Execute(CallbackQuery callbackQuery) { }

        private void SaveNote(Message message)
        {
            if (message.Text.StartsWith("/setnote") ||
                message.Text.StartsWith("/addnote"))
                message.Text = message.Text.Remove(0, 9);

            Regex reg = new Regex("#[A-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedTags = reg.Matches(message.Text);
            if (matchedTags.Count == 0)
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "addnote_command_error_starttag")
                    });
                return;
            }

            string tagCollection = "";
            foreach(Match match in matchedTags)
            {
                tagCollection += match.Value;
            }

            message.Text += Environment.NewLine;
            message.Text += Environment.NewLine;
            message.Text += "The text of this note is set by the group administrator.";

            Models.Group.Note newNote = noteLogic.Add(CacheData.Groups.Values.Single(x => x.TelegramChatId == message.Chat.Id).GroupId,
                tagCollection, message.Text, -2);
            if(newNote == null)
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "addnote_command_error_generic")
                    });
                return;
            }

            MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "addnote_command_success"),
                        AutoDestroyTimeInSeconds = 5,
                        PostSentAction = ChatMessage.PostSentActions.Destroy
                    });

            MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = newNote.Message
                    });
        }
    }
}
