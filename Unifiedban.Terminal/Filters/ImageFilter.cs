/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Filters
{
    public class ImageFilter : IFilter
    {
        public FilterResult DoCheck(Message message)
        {
            // return compareImage(message);
            return new FilterResult
            {
                Result = IFilter.FilterResultType.skipped
            };
        }

        private FilterResult compareImage(Message message)
        {
            try
            {
                foreach (PhotoSize photoSize in message.Photo)
                {
                    var filePath = Bot.Manager.BotClient.GetFileAsync(photoSize.FileId).Result.FilePath;
                    FileStream wFile = new FileStream(@"F:\TEMP\Unifiedban_Asset\temp\" + message.MessageId +
                        "_" + photoSize.FileSize + ".jpg", FileMode.CreateNew);
                    Bot.Manager.BotClient.DownloadFileAsync(filePath, wFile).Wait();
                    wFile.Close();
                }
                Utils.ImageComparator ic = new Utils.ImageComparator();
                ic.AddPicFolderByPath(@"F:\TEMP\Unifiedban_Asset\temp");
                ic.AddPicFolderByPathToCompare(@"F:\TEMP\Unifiedban_Asset\to_compare");

                var _comparationResult = ic.FindDuplicatesWithTollerance(70);
                //int counter = 1;
                //foreach (var hashBlock in _comparationResult)
                //{
                //    Console.WriteLine($"Duplicates {counter++} Group:");

                //    foreach (var singleHash in hashBlock)
                //    {
                //        Console.WriteLine(singleHash.FilePath);
                //    }
                //}

                System.IO.File.Delete(@"F:\TEMP\Unifiedban_Asset\temp\toCompare.jpg");

                return new FilterResult
                {
                    Result = _comparationResult.Count > 0 ? 
                        IFilter.FilterResultType.positive : IFilter.FilterResultType.negative,
                    CheckName = "ImageFilter",
                    Rule = "ImageFilter"
                };
            }
            catch
            {
                return new FilterResult
                {
                    Result = IFilter.FilterResultType.skipped
                };
            }
        }
    }
}
