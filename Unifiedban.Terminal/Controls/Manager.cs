/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Unifiedban.Models;
using Unifiedban.Terminal.Bot;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Controls
{
    public class Manager
    {
        static List<IControl> controls = new List<IControl>();
        static List<Filters.IFilter> filters = new List<Filters.IFilter>();

        public static void Initialize()
        {
            controls.Add(new SpamNameControl());
            controls.Add(new FloodControl());
            controls.Add(new SafeGroupControl());
            controls.Add(new Notes());

            filters.Add(new Filters.BadWordFilter());
            filters.Add(new Filters.NonLatinFilter());
            filters.Add(new Filters.ScamFilter());
            filters.Add(new Filters.RTLNameFilter());

            Data.Utils.Logging.AddLog(new Models.SystemLog()
            {
                LoggerName = CacheData.LoggerName,
                Date = DateTime.Now,
                Function = "Unifiedban Terminal Startup",
                Level = Models.SystemLog.Levels.Info,
                Message = "Controls and filters initialized",
                UserId = -2
            });
        }

        public static void DoCheck(Message message)
        {
            foreach (var plugin in CacheData.PreControlsPlugins)
            {
                if (!plugin.Execute(message, MessageQueueManager.EnqueueMessage))
                {
                    return;
                }
            }
            
            foreach (IControl control in controls)
            {
                ControlResult result = control.DoCheck(message);
                if (result.Result == IControl.ControlResultType.positive)
                {
                    RemoveMessageForPositiveControl(message, result);
                    return;
                }
            }
            
            foreach (var plugin in CacheData.PostControlsPlugins)
            {
                if (!plugin.Execute(message, MessageQueueManager.EnqueueMessage))
                {
                    return;
                }
            }
            
            foreach (var plugin in CacheData.PreFiltersPlugins)
            {
                if (!plugin.Execute(message, MessageQueueManager.EnqueueMessage))
                {
                    return;
                }
            }

            foreach (Filters.IFilter filter in filters)
            {
                Filters.FilterResult result = filter.DoCheck(message);
                if(result.Result == Filters.IFilter.FilterResultType.positive)
                {
                    RemoveMessageForPositiveFilter(message, result);
                    return;
                }
            }
            
            foreach (var plugin in CacheData.PostFiltersPlugins)
            {
                if (!plugin.Execute(message, MessageQueueManager.EnqueueMessage))
                {
                    return;
                }
            }
        }

        public static void DoMediaCheck(Message message)
        {
            // TODO !
        }

        private static void RemoveMessageForPositiveControl(Message message, Controls.ControlResult result)
        {
            Bot.Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            string author = message.From.Username == null
                ? message.From.FirstName + " " + message.From.LastName
                : "@" + message.From.Username;
            Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "Message deleted due to control *{0}*.\n" +
                        "⚠ do not open links you don't know ⚠\n" +
                        "\nChat: `{1}`" +
                        "\nAuthor: `{3}`" +
                        "\nUserId: `{4}`` " +
                        "\nOriginal message:\n```{2}```" +
                        "\n\n*hash_code:* #UB{5}-{6}",
                        result.CheckName,
                        message.Chat.Title,
                        message.Text,
                        author,
                        message.From.Id,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid()),
                    disableWebPagePreview: true
                );
        }

        private static void RemoveMessageForPositiveFilter(Message message, Filters.FilterResult result)
        {
            Bot.Manager.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            string author = message.From.Username == null
                ? message.From.FirstName + " " + message.From.LastName
                : "@" + message.From.Username;
            Bot.Manager.BotClient.SendTextMessageAsync(
                    chatId: CacheData.ControlChatId,
                    parseMode: ParseMode.Markdown,
                    text: String.Format(
                        "*[Report]*\n" +
                        "Message deleted due to filter *{0}* provided positive result.\n" +
                        "⚠ do not open links you don't know ⚠\n" +
                        "\nChat: `{1}`" +
                        "\nAuthor: `{3}`" +
                        "\nUserId: `{4}`" +
                        "\nOriginal message:\n```{2}```" +
                        "\n\n*hash_code:* #UB{5}-{6}",
                        result.CheckName,
                        message.Chat.Title,
                        message.Text,
                        author,
                        message.From.Id,
                        message.Chat.Id.ToString().Replace("-",""),
                        Guid.NewGuid()),
                    disableWebPagePreview: true
                );
        }

        public static bool IsTelegramLink(string siteUri)
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                string htmlCode = "";
                try
                {
                    htmlCode = client.DownloadString(siteUri);
                }
                catch
                {
                    Data.Utils.Logging.AddLog(new Models.SystemLog()
                    {
                        LoggerName = CacheData.LoggerName,
                        Date = DateTime.Now,
                        Function = "Unifiedban.Terminal.Controls.IsTelegramLink",
                        Level = Models.SystemLog.Levels.Error,
                        Message = "Error loading siteUri: " + siteUri,
                        UserId = -2
                    });
                }

                if (htmlCode.Contains("tgme_page_extra"))
                    return true;

                return false;
            }
        }
    }

    public class WebClientWithTimeout : System.Net.WebClient
    {
        protected override System.Net.WebRequest GetWebRequest(Uri address)
        {
            System.Net.WebRequest wr = base.GetWebRequest(address);
            wr.Timeout = 5000; // timeout in milliseconds (ms)
            return wr;
        }
    }
}
