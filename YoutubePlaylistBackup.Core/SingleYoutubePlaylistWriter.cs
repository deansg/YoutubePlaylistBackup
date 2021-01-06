using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace YoutubePlaylistBackup.Core
{
    public class SingleYoutubePlaylistWriter
    {
        private readonly string _outputFilePrefix;
        private readonly string _youtubeAuthKey;
        private readonly string _playlistName;
        private readonly string _playlistId;
        private readonly HttpClient _httpClient;
        private readonly string _newVersionPath;
        private readonly string _oldVersionPath;
        private readonly string _diffFilePath;
        private readonly string _diffBackupPath;
        private readonly string _oldVersionBackupPath;
        private readonly bool _areNewVideosLast;

        public SingleYoutubePlaylistWriter(string folderPath, string youtubeAuthKey, string playlistId,
            string playlistName, bool areNewVideosLast, HttpClient httpClient)
        {
            _httpClient = httpClient;
            if (playlistName == null)
            {
                playlistName = playlistId;
            }
            _outputFilePrefix = Path.Combine(folderPath, playlistName + "-");
            _youtubeAuthKey = youtubeAuthKey;
            _playlistId = playlistId;
            _playlistName = playlistName;
            _areNewVideosLast = areNewVideosLast;

            _newVersionPath = _outputFilePrefix + "YoutubeBackupNew.txt";
            _oldVersionPath = _outputFilePrefix + "YoutubeBackup.txt";
            _diffFilePath = _outputFilePrefix + "YoutubeBackupDiff.txt";
            _diffBackupPath = _outputFilePrefix + "YoutubeBackupDiffOld.txt";
            _oldVersionBackupPath = _outputFilePrefix + "YoutubeBackupOld.txt";
        }

        public void BackupPlaylist()
        {
            IReadOnlyList<string> newPlaylistTitles = RetrieveCurrentPlaylistTitles();
            IReadOnlyList<string> titlesFromFile = GetPlaylistTitlesFromBackup();
            int lengthDiff = CalculateAndValidateLenghtDiff(newPlaylistTitles, titlesFromFile);
            WriteBackup(_oldVersionPath, _oldVersionBackupPath, _diffFilePath, _diffBackupPath);
            OverrideOldFile();
            WriteNewTitles(_newVersionPath, newPlaylistTitles);
            WriteDiffFile(newPlaylistTitles, titlesFromFile, lengthDiff);
        }

        private static void WriteBackup(string oldVersionPath, string oldVersionBackupPath, string diffFilePath,
            string diffBackupPath)
        {
            PrintMsg("Creating backups");
            if (File.Exists(oldVersionPath))
                File.Copy(oldVersionPath, oldVersionBackupPath, true);
            if (File.Exists(diffFilePath))
                File.Copy(diffFilePath, diffBackupPath, true);
        }

        private IReadOnlyList<string> RetrieveCurrentPlaylistTitles()
        {
            PrintMsg($"Retrieving {_playlistName} titles from YouTube");
            return new YoutubePlaylistTitlesRetriever(_httpClient, _playlistId, _youtubeAuthKey).RetrieveTitles();
        }

        private IReadOnlyList<string> GetPlaylistTitlesFromBackup()
        {
            if (!File.Exists(_newVersionPath))
            {
                return new string[0];
            }
            return GetPlaylistTitlesFromFile(_newVersionPath);
        }

        private IReadOnlyList<string> GetPlaylistTitlesFromFile(string filePath)
        {
            PrintMsg($"Retrieving old {_playlistName} titles");
            return File.ReadAllLines(filePath).
                Select(str => str.Substring(str.IndexOf(".", StringComparison.Ordinal) + 2)).ToList();
        }

        private int CalculateAndValidateLenghtDiff(IReadOnlyList<string> newTitles,
            IReadOnlyList<string> oldTitles)
        {
            PrintMsg("Validating new titles and calculating diffs");
            int num = newTitles.Count - oldTitles.Count;
            if (num >= 0) return num;
            CreateMissingVideosFile(oldTitles, newTitles);
            throw new Exception("Some videos were removed since last time with no update!");
        }

        public void FindMissingVideos()
        {
            IReadOnlyList<string> newTitles = RetrieveCurrentPlaylistTitles();
            CreateMissingVideosFile(GetPlaylistTitlesFromFile(_oldVersionPath), newTitles);
        }

        private void CreateMissingVideosFile(IReadOnlyList<string> oldTitles, IReadOnlyList<string> newTitles)
        {
            using (StreamWriter streamWriter = new StreamWriter(_outputFilePrefix + "MissingVideos.txt"))
            {
                for (int index = 0; index < oldTitles.Count; ++index)
                {
                    if (!newTitles.Contains(oldTitles[index]))
                        streamWriter.WriteLine("{0}. {1}", index + 1, oldTitles[index]);
                }
            }
        }

        private void OverrideOldFile()
        {
            if (File.Exists(_newVersionPath))
            {
                File.Copy(_newVersionPath, _oldVersionPath, true);
            }
        }

        private void WriteNewTitles(string newVersionPath, IReadOnlyList<string> newTitles)
        {
            PrintMsg($"Writing new {_playlistName} titles to file");
            using (var streamWriter = new StreamWriter(newVersionPath))
            {
                for (int index = 0; index < newTitles.Count; ++index)
                    streamWriter.WriteLine("{0}. {1}", index + 1, newTitles[index]);
            }
        }

        private void WriteDiffFile(IReadOnlyList<string> newTitles, IReadOnlyList<string> oldTitles,
            int titlesLengthDiff)
        {
            PrintMsg("Writing diff file");
            using (var streamWriter = new StreamWriter(_diffFilePath))
            {
                if (_areNewVideosLast)
                {
                    for (int index = 0; index < oldTitles.Count; index++)
                    {
                        string oldTitle = oldTitles[index];
                        string newTitle = newTitles[index];
                        if (oldTitle != newTitle)
                        {
                            streamWriter.WriteLine("{0}. Old: {1}. New: {2}", index + 1, oldTitle, newTitle);
                        }
                    }
                }
                else
                {
                    for (int index = oldTitles.Count - 1; index >= 0; --index)
                    {
                        string oldTitle = oldTitles[index];
                        string newTitle = newTitles[index + titlesLengthDiff];
                        if (oldTitle != newTitle)
                        {
                            streamWriter.WriteLine("{0}. Old: {1}. New: {2}",
                                index + 1 + titlesLengthDiff, oldTitle, newTitle);
                        }
                    }
                }
            }
        }

        private static void PrintMsg(string msg)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: {msg}");
        }
    }
}
