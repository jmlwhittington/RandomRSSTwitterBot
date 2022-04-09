# RandomRSSTwitterBot
A Twitter bot written in C# using [Tweetinvi](https://github.com/linvi/tweetinvi) and [System.ServiceModel.Syndication](https://www.nuget.org/packages/System.ServiceModel.Syndication/) to select a random article from a custom list of RSS feeds then tweet it out.

To install the bot, download the latest release, drag the folder out of the .RAR archive, and place it wherever you intend to store the bot. Run it once to produce a keys.txt file, then input your keys there. Run the bot again, and it will create a few more files (a logs folder where logs are stored and titled by Unix timestamp, a sources.txt file, and a postings.txt file). If the bot ran successfully, it will have also posted something from the default feed (Google News' Technology topic). Check out the bot's feed to see if the post was successful!

From here, you can add in your sources' RSS feeds in the sources.txt file (one per line). The bot will automatically add titles of posts to the postings.txt file, and will search for another suitable article up to 100 times before giving up. You can clear out the logs folder and postings.txt file periodically per your needs. A future release may see the bot clearing out those two periodically on its own.

Currently, the intended way to use this bot is to set it up in Windows Task Scheduler at the frequency you wish to use. Make sure you configure the "Start in" field on the action to list the path to the directory of the root folder of the bot itself, or you may run into issues. A future release may see the bot running continuously.

If you have any questions, feel free to raise an issue!
