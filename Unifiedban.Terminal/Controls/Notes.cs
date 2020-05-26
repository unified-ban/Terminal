/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Unifiedban.Terminal.Bot;

namespace Unifiedban.Terminal.Controls
{
    public class Notes : IControl
    {
        BusinessLogic.Group.NoteLogic noteLogic = new BusinessLogic.Group.NoteLogic();

        public ControlResult DoCheck(Message message)
        {
            Models.Group.ConfigurationParameter configValue = CacheData.GroupConfigs[message.Chat.Id]
                .Where(x => x.ConfigurationParameterId == "GroupNotes")
                .SingleOrDefault();
            if (configValue != null)
                if (configValue.Value == "false")
                    return new ControlResult()
                    {
                        CheckName = "Group notes",
                        Result = IControl.ControlResultType.skipped
                    };

            Regex reg = new Regex("#[A-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            MatchCollection matchedTags = reg.Matches(message.Text);
            if (matchedTags.Count == 0)
                return new ControlResult()
                {
                    CheckName = "Group notes",
                    Result = IControl.ControlResultType.skipped
                };

            List<Models.Group.Note> notes = new List<Models.Group.Note>();
            foreach(Match match in matchedTags)
            {
                notes.AddRange(noteLogic.GetByTag(match.Value, CacheData.Groups[message.Chat.Id].GroupId));
            }

            List<Models.Group.Note> distNotes = new List<Models.Group.Note>(notes.Distinct());
            foreach(Models.Group.Note note in distNotes)
            {
                note.Message += Environment.NewLine;
                note.Message += "NoteId: " + note.NoteId;

                MessageQueueManager.EnqueueMessage(
                    new Models.ChatMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Chat = message.Chat,
                        Text = note.Message
                    });
            }

            return new ControlResult()
            {
                CheckName = "Group notes",
                Result = IControl.ControlResultType.negative
            };
        }
    }
}
