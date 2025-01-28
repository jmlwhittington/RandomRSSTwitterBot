# **RandomRSSTwitterBot has been archived and is effectively dead *for reasons*.**

-----

# RandomRSSTwitterBot (v0.5.3)
A Twitter bot written in C# using [Tweetinvi](https://github.com/linvi/tweetinvi) and [System.ServiceModel.Syndication](https://www.nuget.org/packages/System.ServiceModel.Syndication/) to tweet a random article from a custom list of RSS feeds at a custom frequency in hours.

If you have any questions, feel free to create a discussion or raise an issue!

## Installation and setup
To install the bot, download the latest release, drag the folder out of the *.RAR* archive, and place it wherever you intend to store the bot. Run it once to produce a *config.txt* file, then input your keys there and tweak the other settings such as how frequently it posts (in hours). Run the bot again, and it will create a few more files (a *logs* folder where logs are stored and titled by Unix timestamp, a *sources.txt* file, a *mult* folder where sources with multiple feeds may be utilized, a *postings.txt* file, a *frequency.txt*, and a *queue.txt* file). Test the bot by changing the frequency in the config to 0. If the bot ran successfully, it will have posted something from the default feed (the Technology topic on Google News).

## Adding/removing sources
From here, you can add in your sources' RSS feeds in the *sources.txt* file (one per line). The bot will automatically add titles of posts to the *postings.txt* file, and will search for another suitable article up to the amount of times specified in the *config.txt* file (default 10) before giving up until the next cycle. You can clear out the *logs* folder and *postings.txt* file periodically per your needs (just make sure to leave the last log up if the bot is running!).

If you decide to remove a source, make sure you remove its frequency entry from the *frequency.txt* file! You can do this by checking the line number of the source in the *sources.txt* file in Notepad, then removing that same line number from both files. If you do not do this, the program will incorrectly calculate the frequency of each souce! You do not have to do the same when adding sources, as it will automatically add in new lines with a value of 0 for you. Do note that if you do not add a new source in at the last line available, the bot will add the new lines in the *frequency.txt* file at the bottom and end up calculating the frequency incorrectly!

Finally, if you are using sources with multiple feeds and remove a source, ensure the name of the *.txt* file in the *mult* folder is changed if necessary as well!

## Sources with multiple feeds
For sources with multiple feeds, you can add a *.txt* file in the *mult* folder named after the source's line number **minus one** in the *sources.txt* file. You may, from here, change the source in the *sources.txt* file to whatever you want (such as the name of the source), then add the link of each feed to the newly created *[source_line_number_minus_one].txt* file (one per line). The bot will automatically check for this file each time it selects the source and choose from these feeds, but the frequency will be calculated based on the original source, so that one source does not overwhelm others despite the number of its feed.

## Queueing up posts
To queue up posts manually selected by you, open the *queue.txt* file and add first the title of the article, then the link to it. You must always respect this "every other" pattern (titles on odd-numbered lines, links on even-numbered lines), or else it will not work properly. The bot tweets from the queue top-to-bottom, so the first item in the queue is on lines 1 and 2. Once the bot tweets from the queue, it will remove the first item but leave any others in the queue, moving them up automatically for you. If the queue is emptied, the bot returns to tweeting from the *sources.txt* file as normal on its next cycle of the timer.

You can change the relevant entry in the *config.txt* file to ensure that the queue does not unload exclusively when putting in more than one item. When tweaked, it runs after n gaps (default 0, so every cycle; 1 would be every other, 2 every third, etc.) in the bot's cycles. This may be preferred for people who like to queue up multiple items.

Do note that posts tweeted from the queue do not get added to the *postings.txt* file, so if selecting posts from the feeds in the *sources.txt* file, you may end up with duplicates or even crashing of the bot (if the post is selected too frequently after). This feature is mostly meant for articles applicable to your bot, but not from sources normally applicable to your bot.

## Errors
If there is an error, you will see some reasoning in the console output, but not the log output unfortunately. You can use the timestamp to look at Event Viewer under Windows Logs > Application, however, to see the same output for later if it was related to a failure to tweet.
