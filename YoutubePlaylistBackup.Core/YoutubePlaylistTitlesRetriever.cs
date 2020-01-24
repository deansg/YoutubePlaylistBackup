using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace YoutubePlaylistBackup.Core
{
    public class YoutubePlaylistTitlesRetriever
    {
        public const string PlaylistAPI = "https://www.googleapis.com/youtube/v3/playlistItems/";

        private readonly HttpClient _httpClient;
        private readonly string _playlistId;
        private readonly string _youtubeAuthKey;
        private readonly List<string> _titles = new List<string>();
        private Dictionary<string, object> _curParsedResponse;
        private object _nextPage;

        public YoutubePlaylistTitlesRetriever(HttpClient httpClient, string playlistId, string youtubeAuthKey)
        {
            _httpClient = httpClient;
            _playlistId = playlistId;
            _youtubeAuthKey = youtubeAuthKey;
        }

        public IReadOnlyList<string> RetrieveTitles()
        {
            do
            {
                Thread.Sleep(200); // Without sleep sometimes irregularities in the API response pop up
                IList<string> curTitles = RetrieveTitlesBulk();

                if (_titles.Count > 0 && curTitles[0] == _titles[_titles.Count - 1])
                {
                    throw new Exception(
                        $"YouTube API Error: video title number {_titles.Count - 1} is the same as the next one: '{curTitles[0]}'");
                }

                _titles.AddRange(curTitles);
                Console.Write($"\rRetrieved {_titles.Count} titles");
            } while (_curParsedResponse.TryGetValue("nextPageToken", out _nextPage));
            Console.WriteLine();
            return _titles;
        }

        private IList<string> RetrieveTitlesBulk()
        {
            string reqUrl = GetRequestUrl((string)_nextPage);
            string res = _httpClient.GetStringAsync(reqUrl).Result;

            _curParsedResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(res);
            var items = (JArray)_curParsedResponse["items"];
            return items.Cast<JObject>().Select(item =>
            {
                var snippet = (JObject)item["snippet"];
                return (string)snippet["title"];
            }).ToList();
        }

        private string GetRequestUrl(string nextPage = null)
        {
            string nextPagePart = string.IsNullOrEmpty(nextPage) ? string.Empty : "&pageToken=" + nextPage;
            return $"{PlaylistAPI}?part=snippet&maxResults=50&playlistId={_playlistId}&key={_youtubeAuthKey}{nextPagePart}";
        }
    }
}
