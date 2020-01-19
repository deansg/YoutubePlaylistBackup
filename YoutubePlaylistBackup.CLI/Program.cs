using CommandLine;
using System;
using YoutubePlaylistBackup.Core;

namespace YoutubePlaylistBackup.CLI
{
    public class Program
    {
        public class Options
        {
            [Option("playlistId", Default = null, HelpText = "The YouTube id of the playlist to be backed up", Required = true)]
            public string PlaylistId { get; set; }

            [Option("youtubeAuthKey", HelpText = "", Required = true)]
            public string YoutubeAuthKey { get; set; }

            [Option("outputDir", Default = "", HelpText = "The script's output directory", Required = false)]
            public string OutputDirPath { get; set; }

            [Option("playlistName", Default = null, HelpText = "", Required = false)]
            public string PlaylistName { get; set; }
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                using (var writer = new YoutubePlaylistWriter(o.OutputDirPath, o.YoutubeAuthKey))
                {
                    writer.BackupPlaylist(o.PlaylistId, o.PlaylistName);
                }
            });
        }
    }
}