using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace YoutubePlaylistBackup.Core
{
    public class YoutubePlaylistWriter
    {
        public const string PlaylistAPI = "https://www.googleapis.com/youtube/v3/playlistItems/";

        private readonly string _folderPath;
        private readonly string _playlistName;
        private readonly string _playlistId;
        private readonly string _youtubeAuthKey;

        public readonly string NewVersionPath;
        public readonly string OldVersionPath;
        public readonly string DiffFilePath;
        public readonly string DiffBackupPath;
        public readonly string OldVersionBackupPath;

        public YoutubePlaylistWriter(string folderPath, string playlistId, string playlistName, string youtubeAuthKey)
        {
            _folderPath = folderPath;
            _playlistName = playlistName;
            _playlistId = playlistId;
            _youtubeAuthKey = youtubeAuthKey;

            NewVersionPath = $"{_folderPath}Youtube{_playlistName}AutomatedNew.txt";
            OldVersionPath = $"{_folderPath}Youtube{_playlistName}Automated.txt";
            DiffFilePath = $"{_folderPath}Youtube{_playlistName}AutomatedDiff.txt";
            DiffBackupPath = $"{_folderPath}Youtube{_playlistName}DiffOld.txt";
            OldVersionBackupPath = $"{_folderPath}Youtube{_playlistName}OldOld.txt";
        }

        public void Run()
        {
            IReadOnlyList<string> newPlaylistTitles = RetrievePlaylistTitles();
            IReadOnlyList<string> titlesFromFile = GetPlaylistTitlesFromFile(NewVersionPath);
            int lengthDiff = CalculateAndValidateLenghtDiff(newPlaylistTitles, titlesFromFile);
            WriteBackup(OldVersionPath, OldVersionBackupPath, DiffFilePath, DiffBackupPath);
            OverrideOldFile(NewVersionPath, OldVersionPath);
            WriteNewTitles(NewVersionPath, newPlaylistTitles);
            WriteDiffFile(DiffFilePath, newPlaylistTitles, titlesFromFile, lengthDiff);
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

        private IReadOnlyList<string> RetrievePlaylistTitles()
        {
            PrintMsg($"Retrieving new {_playlistName} titles");
            return RetrievePlaylistTitlesFromAPI();
        }

        private IReadOnlyList<string> RetrievePlaylistTitlesFromAPI()
        {
            PrintMsg($"Retrieving {_playlistName} titles");
            var titles = new List<string>();
            using (var client = new HttpClient())
            {
                Dictionary<string, object> parsed;
                object nextPage = null;
                do
                {
                    Thread.Sleep(200);
                    string reqUrl = GetRequestUrl((string)nextPage);
                    string res = client.GetStringAsync(reqUrl).Result;
    
                    parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(res);
                    var items = (ArrayList)parsed["items"];
                    IList<string> curTitles = items.Cast<Dictionary<string, object>>().Select(item =>
                    {
                        var snippet = (Dictionary<string, object>)item["snippet"];
                        return (string)snippet["title"];
                    }).ToList();

                    if (titles.Count > 0 && curTitles[0] == titles[titles.Count - 1])
                    {
                        throw new Exception(
                            $"YouTube API Error: video title number {titles.Count - 1} is the same as the next one: '{curTitles[0]}'");
                    }

                    titles.AddRange(curTitles);
                    Console.Write($"\rRetrieved {titles.Count} titles");
                } while (parsed.TryGetValue("nextPageToken", out nextPage));
            }
            Console.WriteLine();
            return titles;
        }

        private string GetRequestUrl(string nextPage = null)
        {
            string nextPagePart = string.IsNullOrEmpty(nextPage) ? string.Empty : "&pageToken=" + nextPage;
            return $"{PlaylistAPI}?part=snippet&maxResults=50&playlistId={_playlistId}&key={_youtubeAuthKey}{nextPagePart}";
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
            IReadOnlyList<string> newTitles = RetrievePlaylistTitles();
            CreateMissingVideosFile(GetPlaylistTitlesFromFile(OldVersionPath), newTitles);
        }

        private void CreateMissingVideosFile(IReadOnlyList<string> oldTitles, IReadOnlyList<string> newTitles)
        {
            using (StreamWriter streamWriter = new StreamWriter(_folderPath + "MissingVideos.txt"))
            {
                for (int index = 0; index < oldTitles.Count; ++index)
                {
                    if (!newTitles.Contains(oldTitles[index]))
                        streamWriter.WriteLine("{0}. {1}", index + 1, oldTitles[index]);
                }
            }
        }

        private void OverrideOldFile(string newVersionPath, string oldVersionPath)
        {
            File.Copy(newVersionPath, oldVersionPath, true);
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

        private void WriteDiffFile(string diffFilePath, IReadOnlyList<string> newTitles,
            IReadOnlyList<string> oldTitles, int titlesLengthDiff)
        {
            PrintMsg("Writing diff file");
            using (var streamWriter = new StreamWriter(diffFilePath))
            {
                for (int index = oldTitles.Count - 1; index >= 0; --index)
                {
                    string oldTitle = oldTitles[index];
                    string newTitle = newTitles[index + titlesLengthDiff];
                    if (oldTitle != newTitle)
                        streamWriter.WriteLine(
                            "{0}. Old: {1}. New: {2}", index + 1 + titlesLengthDiff, oldTitle, newTitle);
                }
            }
        }

        private static void PrintMsg(string msg)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: {msg}");
        }
    }
}
