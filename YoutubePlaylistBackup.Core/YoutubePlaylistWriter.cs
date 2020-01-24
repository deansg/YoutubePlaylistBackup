using System;
using System.Net.Http;

namespace YoutubePlaylistBackup.Core
{
    public class YoutubePlaylistWriter : IDisposable
    {
        private readonly string _folderPath;
        private readonly string _youtubeAuthKey;
        private readonly HttpClient _httpClient;

        public YoutubePlaylistWriter(string folderPath, string youtubeAuthKey, HttpClient httpClient = null)
        {
            _folderPath = folderPath;
            _youtubeAuthKey = youtubeAuthKey;
            _httpClient = httpClient ?? new HttpClient();
        }

        public void BackupPlaylist(string playlistId, string playlistName = null)
        {
            new SingleYoutubePlaylistWriter(_folderPath, _youtubeAuthKey, playlistId, playlistName, _httpClient).BackupPlaylist();
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }
    }
}
