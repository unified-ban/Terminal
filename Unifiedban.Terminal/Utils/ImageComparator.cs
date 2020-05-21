/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unifiedban.Terminal.Utils;

namespace Unifiedban.Terminal.Utils
{
    [Serializable]
    public class ImageComparator
    {
        private readonly int _hashSize;

        private readonly List<ImageHash> _hashLib = new List<ImageHash>();
        private readonly List<ImageHash> _hashLibToCompare = new List<ImageHash>();

        public ImageComparator(int hashSize = 16)
        {
            _hashSize = hashSize;
        }

        public void DeadItemsCleanUp()
        {
            for (int i = 0; i < _hashLib.Count; i++)
            {
                var path = _hashLib[i].FilePath;

                if (!File.Exists(path))
                {
                    _hashLib.RemoveAt(i);

                    i--;
                }
            }
        }

        public List<List<ImageHash>> FindDuplicatesWithTollerance(int minSimilarity = 90)
        {
            List<ImageHash> alreadyMarkedAsDupl = new List<ImageHash>();

            var duplicatesFound = new List<List<ImageHash>>();

            foreach (var hash in _hashLib)
            {
                if (alreadyMarkedAsDupl.Contains(hash) == false)
                {
                    var singleImgDuplicates = FindDuplicatesTo(hash, minSimilarity, ref alreadyMarkedAsDupl);
                    if (singleImgDuplicates.Count > 0)
                        duplicatesFound.Add(singleImgDuplicates);
                }
            }

            return duplicatesFound;
        }

        private List<ImageHash> FindDuplicatesTo(ImageHash hash, int minSimilarity, ref List<ImageHash> alreadyMarkedAsDupl)
        {
            var currHashDupl = new List<ImageHash>();

            foreach (var hashCompareWith in _hashLibToCompare)
            {
                if (hash.CompareWith(hashCompareWith) >= minSimilarity)
                {
                    if (!alreadyMarkedAsDupl.Contains(hash))
                    {
                        alreadyMarkedAsDupl.Add(hash);

                        currHashDupl.Add(hash);
                    }

                    //if (!alreadyMarkedAsDupl.Contains(hashCompareWith))
                    //{
                    //    alreadyMarkedAsDupl.Add(hashCompareWith);

                    //    currHashDupl.Add(hashCompareWith);
                    //}
                }
            }

            return currHashDupl;
        }

        //This comment can be fixed for faster duplicates search in future

        //public List<List<ImageHash>> FindDuplicatesWithTollerance(int minSimilarity = 90, bool useFastSearch = true)
        //{
        //    List<ImageHash> duplicatesFound = new List<ImageHash>();

        //    var result = new List<List<ImageHash>>();

        //    ParallelTaskRunner ptr = new ParallelTaskRunner();

        //    var ranges = ptr.CalculateTaskRanges(0, _hashLib.Count, 1);

        //    ptr.Add(new Task(() => {
        //        FindDuplicatesInRange(0, _hashLib.Count, minSimilarity, ref result, ref duplicatesFound);
        //    }));

        //    ptr.StartAllTasksAndWaitForFinish();

        //    result = result.Where(a => a.Count >= 2).ToList();

        //    return result;
        //}

        //public void FindDuplicatesInRange(int start, int end, int minSimilarity, ref List<List<ImageHash>> result, ref List<ImageHash> duplicatesFound)
        //{
        //    for (int i = start; i < end; i++)
        //    {
        //        var lastAddedLst = new List<ImageHash>();
        //        result.Add(lastAddedLst);

        //        if (!duplicatesFound.Contains(_hashLib[i]))
        //        {
        //            foreach (var anotherHash in _hashLib)
        //            {
        //                if (_hashLib[i].CompareWith(anotherHash) > minSimilarity)
        //                {
        //                    if (!duplicatesFound.Contains(_hashLib[i]))
        //                    {
        //                        duplicatesFound.Add(_hashLib[i]);
        //                        lastAddedLst.Add(_hashLib[i]);
        //                    }

        //                    if (!duplicatesFound.Contains(anotherHash))
        //                    {
        //                        duplicatesFound.Add(anotherHash);
        //                        lastAddedLst.Add(anotherHash);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        public void AddPicByPath(string path)
        {
            var hash = new ImageHash(_hashSize);
            hash.GenerateFromPath(path);
            _hashLib.Add(hash);

            //hash = new ImageHash(_hashSize);
            //hash.GenerateFromPathReversed(path);
            //_hashLib.Add(hash);
        }
        public void AddPicByPathToCompare(string path)
        {
            var hash = new ImageHash(_hashSize);
            hash.GenerateFromPath(path);
            _hashLibToCompare.Add(hash);

            hash = new ImageHash(_hashSize);
            hash.GenerateFromPathReversed(path);
            _hashLibToCompare.Add(hash);
        }

        public void AddPicFolderByPath(string folderPath, bool includeSubFolders = false)
        {
            var files = GetFilesList(folderPath, includeSubFolders, ".bmp", ".gif", ".exif", ".jpg", ".jpeg", ".png", ".tiff");

            foreach (var file in files)
            {
                AddPicByPath(file);
            }
        }
        public void AddPicFolderByPathToCompare(string folderPath, bool includeSubFolders = false)
        {
            var files = GetFilesList(folderPath, includeSubFolders, ".bmp", ".gif", ".exif", ".jpg", ".jpeg", ".png", ".tiff");

            foreach (var file in files)
            {
                AddPicByPathToCompare(file);
            }
        }

        /// <summary>
        /// Returns files from some path. Can return with subfolders files or without;
        /// </summary>
        private List<string> GetFilesList(string path, bool withSubFolders, params string[] extensions)
        {
            if (withSubFolders == false)
            {
                return GetFilesListFromSingleFolder(path, extensions);
            }

            var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

            List<string> result = new List<string>();

            foreach (var dir in dirs)
            {
                result.AddRange(GetFilesListFromSingleFolder(dir, extensions));
            }
            result.AddRange(GetFilesListFromSingleFolder(path, extensions));

            return result;
        }

        /// <summary>
        /// Return's all files from single folder WITHOUT subfolders. Do not use this method!
        /// Use GetFilesList instead!
        /// </summary>
        private List<string> GetFilesListFromSingleFolder(string path, string[] extensions)
        {
            var rezults = Directory.GetFiles(path).ToList();

            if (extensions.Length == 0)
            {
                return rezults;
            }

            for (int i = 0; i < rezults.Count; i++)
            {
                if (FilePathHaveOneOfExtensions(rezults[i], extensions) == false)
                {
                    rezults.RemoveAt(i);
                    i--;
                }
            }

            return rezults;
        }

        /// <summary>
        /// Checking if file have one of extensions that is OK for us
        /// </summary>
        private bool FilePathHaveOneOfExtensions(string value, IEnumerable<string> extensions)
        {
            value = value.ToLower();

            foreach (var suffix in extensions)
            {
                if (value.EndsWith(suffix))
                    return true;
            }
            return false;
        }

        public void Serialize(string filePath = "SavedData.hshlib")
        {
            using (Stream stream = File.Open(filePath, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                bformatter.Serialize(stream, this);
            }
        }

        public static ImageComparator DeSerialize()
        {
            if (File.Exists("SavedData.hshlib"))
            {
                using (Stream stream = File.Open("SavedData.hshlib", FileMode.Open))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    return (ImageComparator)bformatter.Deserialize(stream);
                }
            }

            return new ImageComparator();
        }
    }
}
