# YoutubePlaylistBackup
A CLI for backing up the titles of the videos in a YouTube playlist to a text file, that can be used in case videos from the playlist become private/get deleted.

### General Instructions:
1. Get a Youtube API Key. To do so, you can follow the instructions here: https://elfsight.com/help/how-to-get-youtube-api-key/
2. Retrieve the YouTube id of the playlist you want to create a backup for. This can be done in several ways. One of them is by navigating to the playlist's main YouTube page, and copying the text after "...list=" in the url
3. Run the CLI (more on the CLI syntax is provided by running the --help command)
4. Several text files will be generated by the script in the output directory:
    1. *PLAYLIST_NAME*-YoutubeBackupNew.txt: contains the current updated lists of the titles of the provided YouTube playlist
    2. *PLAYLIST_NAME*-YoutubeBackup.txt: if a ...YoutubeBackupNew.txt file existsed in the output directory before running the script, this file will be a backup for it (in case something went wrong with running the script etc')
    3. *PLAYLIST_NAME*-YoutubeBackupDiff.txt: if a ...YoutubeBackupNew.txt file existsed in the output directory before running the script, this file will contain the numbers of the videos whose title changed. This diff can handle new videos being added. However, if some videos from the old list were deleted, it won't take it into account properly
    4. *PLAYLIST_NAME*-YoutubeBackupDiffOld.txt if a ...YoutubeBackupDiff.txt file existed in the output directory before running the script, this file will contain a backup for it

### More Technical Info:
* The code is written in .NET Core (using C#) and can therefore be compiled and executed on multiple platforms
* The code contains 2 projects. The YoutubePlaylistBackup.Core project contains most of the logic, and can be used to create more complicated scripts. The YoutubePlaylistBackup.CLI project contains only the CLI logic such as argument parsing
* When writing a custom script using the Core project, it is recommended to YoutubePlaylistWriter class directly, as done in the CLI project

You're always welcome to send me questions or comments here or by mail (deansg@gmail.com)
