/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Bot.Command
{
    public class RemoveNote : ICommand
    {
        BusinessLogic.Group.NoteLogic noteLogic = new BusinessLogic.Group.NoteLogic();

        public void Execute(Message message)
        {
            if (!Utils.BotTools.IsUserOperator(message.From.Id, Models.Operator.Levels.Basic) &&
                !Utils.ChatTools.IsUserAdmin(message.Chat.Id, message.From.Id))
            {
                return;
            }

            Message messageToCheck = message;
            if (message.ReplyToMessage != null)
                messageToCheck = message.ReplyToMessage;

            if(messageToCheck.From.Id != Manager.MyId &&
                !messageToCheck.Text.Contains("NoteId:"))
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        ReplyToMessageId = message.MessageId,
                        Text = CacheData.GetTranslation("en", "error_removenote_command_invalidmessage")
                    });
                return;
            }

            Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            string noteId = messageToCheck.Text.Split("NoteId:")[1].Trim();

            Models.Group.Note note = noteLogic.GetById(noteId);
            if (note == null)
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "error_removenote_command_invalidNoteId")
                    });
                return; 
            }

            if (note.GroupId != CacheData.Groups[messageToCheck.Chat.Id].GroupId)
            {
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "error_removenote_command_invalidOwner")
                    });
                return;
            }

            Models.SystemLog.ErrorCodes removed = noteLogic.Remove(noteId, -2);
            if(removed == Models.SystemLog.ErrorCodes.Error)
                MessageQueueManager.EnqueueMessage(
                    new ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = CacheData.GetTranslation("en", "error_removenote_command_generic")
                    });

            if(message.MessageId != messageToCheck.MessageId)
                Manager.BotClient.DeleteMessageAsync(message.Chat.Id, messageToCheck.MessageId);

            MessageQueueManager.EnqueueMessage(
                new ChatMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Chat = message.Chat,
                    Text = CacheData.GetTranslation("en", "removenote_command_success"),
                    AutoDestroyTimeInSeconds = 5,
                    PostSentAction = ChatMessage.PostSentActions.Destroy
                });
        }

        public void Execute(CallbackQuery callbackQuery) { }
    }
}
