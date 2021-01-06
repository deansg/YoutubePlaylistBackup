using CommandLine;
using System;
using YoutubePlaylistBackup.Core;

namespace YoutubePlaylistBackup.CLI
{
    public class Program
    {
        public class Options
        {
            [Option("playlistId", Default = null, HelpText = "The YouTube id of the playlist to be backed up", 
                Required = true)]
            public string PlaylistId { get; set; }

            [Option("youtubeAuthKey", 
                HelpText = "The API key provided by YouTube (needs to be obtained before running the script)", 
                Required = true)]
            public string YoutubeAuthKey { get; set; }

            [Option("outputDir", Default = null, 
                HelpText = "(Default: current working directory) The script's output directory", 
                Required = false)]
            public string OutputDirPath { get; set; }

            [Option("playlistName", Default = null, 
                HelpText = "(Default: the playlist id) The name of the playlist to back-up. Only used for generating the names of the output files", 
                Required = false)]
            public string PlaylistName { get; set; }

            [Option("areNewVideosLast", Default = true, 
                HelpText = "(Default: true) Whether new videos are added to the end of the playlist (in favorites' playlists they are added to the beginning, in regular playlists to the end)", 
                Required = false)]
            public bool? AreNewVideosLast { get; set; }
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                using (var writer = new YoutubePlaylistWriter(o.OutputDirPath ?? "", o.YoutubeAuthKey))
                {
                    writer.BackupPlaylist(o.PlaylistId, o.PlaylistName, o.AreNewVideosLast ?? true);
                }
            });
        }
    }
}