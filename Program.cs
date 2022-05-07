using System;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using System.IO;
using System.Threading;
using System.Linq;

namespace RandomRSSTwitterBot
{
    class RRTB
    {
        // Variables for use throughout
        private static string path = Directory.GetCurrentDirectory();
        private static string logsPath = path + "/logs/";
        private static string startTime = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        private static List<string> titles = new List<string>();
        private static List<string> links = new List<string>();
        private static int urlNum;
        private static int i;
        private static bool failure = false;
        private static int attempts = 0;
        private static int attemptMax;
        private static bool test = false;
        private static bool queueRun = false;
        private static int queueAttempt = 0;
        private static int queueGap;
        private static string url;
        private static int hour = DateTime.Now.Hour;
        private static int value;
        private static int timerMS;
        private static double threshold;
        private static string[] keys = { "", "", "", "" };
        private static Random rand = new Random();
        
        static async Task Main()
        {
            // Checking if log folder exists, creating it if not
            if (!Directory.Exists(path + "/logs"))
            {
                Directory.CreateDirectory(path + "/logs");
            }
            File.WriteAllText(logsPath + startTime + ".txt", "");

            // Boot message
            Console.WriteLine(DateTime.Now + ": RandomRSSTwitterBot booting up!" + Environment.NewLine);
            File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": RandomRSSTwitterBot booting up!" + Environment.NewLine + Environment.NewLine);

            // Checking if config file exists, creating it if not and exiting to allow user to input keys
            if (!File.Exists(path + "/config.txt"))
            {
                File.WriteAllText(path + "/config.txt",
                    "INPUT ALL ENTRIES ON NEXT LINE" + Environment.NewLine + Environment.NewLine + Environment.NewLine +
                    "===== GENERAL =====" + Environment.NewLine + Environment.NewLine +
                    "POST HOW OFTEN (IN HOURS)? 0 TO TEST ONCE AND EXIT" + Environment.NewLine + "[DEFAULT: 1] [INTEGER]" + Environment.NewLine + "1" + Environment.NewLine + Environment.NewLine +
                    "CHECK TIMER HOW OFTEN (IN MILLISECONDS)?" + Environment.NewLine + "[DEFAULT: 30000] [INTEGER]" + Environment.NewLine + "30000" + Environment.NewLine + Environment.NewLine +
                    "HOW MUCH OF GAP BETWEEN CYCLES TO CHECK QUEUE (1 = EVERY OTHER CYCLE, 2 = EVERY THIRD, ETC.)?" + Environment.NewLine + "[DEFAULT: 0] [INTEGER]" + Environment.NewLine + "0" + Environment.NewLine + Environment.NewLine +
                    "NUMBER OF TIMES TO RUN DUPLICATE PREVENTION BEFORE GIVING UP UNTIL NEXT CYCLE?" + Environment.NewLine + "[DEFAULT: 10] [INTEGER]" + Environment.NewLine + "10" + Environment.NewLine + Environment.NewLine +
                    "STATISTICAL STANDARD SCORE THRESHOLD FOR FREQUENCY CHECKS? WORKS ONLY POSITIVE, NOT NEGATIVE (FOR MORE INFO: https://en.wikipedia.org/wiki/Standard_score)" + Environment.NewLine + "[DEFAULT: 1] [DOUBLE (DECIMAL UP TO 15 DIGITS)]" + Environment.NewLine + "1" + Environment.NewLine + Environment.NewLine + Environment.NewLine +
                    "===== KEYS =====" + Environment.NewLine + Environment.NewLine +
                    "CONSUMER KEY" + Environment.NewLine + "<INPUT KEY HERE>" + Environment.NewLine + Environment.NewLine +
                    "CONSUMER SECRET" + Environment.NewLine + "<INPUT KEY HERE>" + Environment.NewLine + Environment.NewLine +
                    "ACCESS TOKEN" + Environment.NewLine + "<INPUT KEY HERE>" + Environment.NewLine + Environment.NewLine +
                    "ACCESS TOKEN SECRET" + Environment.NewLine + "<INPUT KEY HERE>"
                );
                Console.WriteLine(DateTime.Now + ": You need to input your authentication keys into config.txt!" + Environment.NewLine + "Press any key to exit...");
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": You need to input your authentication keys into config.txt!" + Environment.NewLine + "Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(0);
            }

            // Interprets config file for later use
            else
            {
                string[] config = File.ReadAllLines(path + "/config.txt");
                foreach (string item in config)
                {
                    value = Int32.Parse(config[7]);
                    timerMS = Int32.Parse(config[11]);
                    queueGap = Int32.Parse(config[15]);
                    attemptMax = Int32.Parse(config[19]);
                    threshold = Double.Parse(config[23]);
                    keys[0] = config[29];
                    keys[1] = config[32];
                    keys[2] = config[35];
                    keys[3] = config[38];
                }
            }

            // Checking if other required files and mult folder exist, creating them if not
            if (!File.Exists(path + "/sources.txt"))
            {
                CreateFile("sources", "https://news.google.com/rss/topics/CAAqJggKIiBDQkFTRWdvSUwyMHZNRGRqTVhZU0FtVnVHZ0pWVXlnQVAB");
            }
            if (!File.Exists(path + "/postings.txt"))
            {
                CreateFile("postings", "");
            }
            if (!File.Exists(path + "/queue.txt"))
            {
                CreateFile("queue", "");
            }
            if (!Directory.Exists(path + "/mult"))
            {
                Directory.CreateDirectory(path + "/mult");
            }
            if (!File.Exists(path + "/frequency.txt"))
            {
                CreateFile("frequency", "0");

                // Adding new entries for all sources at start, in case file was deleted
                bool runOnce = false;
                foreach (string item in File.ReadAllLines(path + "/sources.txt"))
                {
                    if (runOnce == false)
                    {
                        runOnce = true;
                    }
                    else
                    {
                        File.AppendAllText(path + "/frequency.txt", Environment.NewLine + "0");
                    }
                }
            }

            // Cleaning up frequency.txt in case new sources were added, to prevent crashing
            else
            {
                UpdateFrequency();
            }

            // Running bot test if config specified
            if (value == 0)
            {
                Console.WriteLine(DateTime.Now + ": Running bot test!" + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Running bot test!" + Environment.NewLine + Environment.NewLine);
                test = true;
                await Seek();
            }

            // Running bot as normal if no test being ran
            else
            {
                // Properly formatting hour for use
                hour = hour + value - 1;
                if (hour >= 24)
                {
                    hour = hour - 24;
                }

                // Initiation message
                Console.WriteLine(DateTime.Now + ": Running bot every " + value + " hour(s)!" + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Running bot every " + value + " hour(s)!" + Environment.NewLine + Environment.NewLine);

                // Checking change of hour per timer length as defined in config, aligning with frequency in hours to run and initiate next cycle
                while (true)
                {
                    await Timer();

                    // Cleaning up for next cycle, to preserve variables used in multiple places and keep memory usage low
                    attempts = 0;
                    failure = false;
                    queueRun = false;
                    titles.Clear();
                    links.Clear();
                }
            }
        }

        // Seeking article to tweet
        static async Task Seek(int seekNum = -1)
        {
            // Cleaning up frequency.txt in case new sources were added, to prevent crashing
            UpdateFrequency();

            try
            {
                // Ensures method is being ran first time in cycle, as opposed to if duplicate was found
                if (seekNum == -1)
                {
                    Console.WriteLine(DateTime.Now + ": Seeking article to post...");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Seeking article to post..." + Environment.NewLine);
                }

                // Checking if queue has any entries in it and using first one if so then bypassing rest of method
                string[] queue = File.ReadAllLines(path + "/queue.txt");
                if (queue.Length > 1 && seekNum == -1)
                {
                    // Checking if enough cycles have passed since last queue pull
                    if (queueAttempt >= queueGap)
                    {
                        queueAttempt = 0;
                        titles.Add(queue[0]);
                        url = queue[1];
                        queueRun = true;

                        // Removing first item from queue and moving everything else up behind it
                        File.WriteAllText(path + "/queue.txt", "");
                        bool runOnce = false;
                        for (int r = 2; r < queue.Length; r++)
                        {
                            if (runOnce == false)
                            {
                                File.AppendAllText(path + "/queue.txt", queue[r]);
                                runOnce = true;
                            }
                            else
                            {
                                File.AppendAllText(path + "/queue.txt", Environment.NewLine + queue[r]);
                            }
                        }

                        // Tweeting from queue, bypassing rest of method
                        await Tweet();
                        return;
                    }

                    // What happens if enough cycles have not passed since last queue pull
                    else
                    {
                        queueAttempt = queueAttempt + 1;
                        Console.WriteLine(DateTime.Now + ": Bot will use queue in " + (queueGap - queueAttempt) + "cycle(s)!");
                        File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Bot will use queue in " + (queueGap - queueAttempt) + "cycle(s)!" + Environment.NewLine);
                    }
                }

                // Reset queue gap counter if queue is empty, in case queue is manually cleared
                else
                {
                    queueAttempt = 0;
                    Console.WriteLine(DateTime.Now + ": Queue is empty, resetting queue attempts...");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Queue is empty, resetting queue attempts..." + Environment.NewLine);
                }

                // What happens if queue is empty
                // Finding random source if method is being ran first time in cycle
                string[] sources = File.ReadAllLines(path + "/sources.txt");
                if (seekNum == -1)
                {
                    urlNum = rand.Next(0, sources.Length);
                    url = sources[urlNum];

                    // Checking if source has multiple feeds
                    CheckMult(seekNum);
                }

                // Using source chosen in first cycle if duplicate was found
                else
                {
                    urlNum = seekNum;
                    url = sources[urlNum];

                    // Checking if source has multiple feeds
                    CheckMult(seekNum);
                }

                // Calculating frequency of source via statistical standard score
                string[] freq = File.ReadAllLines(path + "/frequency.txt");
                List<double> freqD = new List<double>();
                foreach (string item in freq)
                {
                    freqD.Add((double)Int32.Parse(item));
                }
                double avg = freqD.Average();
                double sum = freqD.Sum(d => Math.Pow(d - avg, 2));
                double sd = Math.Sqrt((sum) / freqD.Count());
                double z = ((double)Int32.Parse(freq[urlNum]) - avg) / sd;
                freqD.Clear();
                int urlCurrent = urlNum;

                // What happens if source is too frequent
                if (z >= threshold)
                {
                    Console.WriteLine(DateTime.Now + ": Source too frequent, choosing again...");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Source too frequent, choosing again..." + Environment.NewLine);

                    // Sifting through other sources until one is found that is not same as first chosen or another that is also too frequent
                    while (urlNum == urlCurrent)
                    {
                        urlNum = rand.Next(0, sources.Length);
                        url = sources[urlNum];
                        z = ((double)Int32.Parse(freq[urlNum]) - avg) / sd;
                        if (z >= threshold)
                        {
                            urlNum = urlCurrent;
                        }
                    }

                    // Checking if source has multiple feeds
                    CheckMult();
                }

                // Pulling feed from source
                XmlReader read = XmlReader.Create(url);
                SyndicationFeed rss = SyndicationFeed.Load(read);
                read.Close();

                // Extracting only relevant info from feed for use
                foreach (SyndicationItem item in rss.Items)
                {
                    titles.Add(item.Title.Text);
                    links.Add(item.Links[0].Uri.ToString());
                }

                // Finding random item from feed
                i = rand.Next(0, titles.Count);

                // Ensuring item has not been posted prior
                foreach (string item in File.ReadAllLines(path + "/postings.txt"))
                {
                    if (titles[i] == item)
                    {
                        failure = true;
                    }
                }

                // Tweeting item if not duplicate
                if (failure == false)
                {
                    await Tweet();
                    read.Dispose();
                }

                // What happens if item is duplicate
                else
                {
                    // Attempts to find new item in same source up to ten times before giving up until next cycle
                    if (attempts < attemptMax)
                    {
                        failure = false;
                        attempts = attempts + 1;
                        titles.Clear();
                        links.Clear();
                        Console.WriteLine(DateTime.Now + ": Duplicate chosen, trying again... (Attempt number: " + attempts + ")");
                        File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Duplicate chosen, trying again... (Attempt number: " + attempts + ")" + Environment.NewLine);
                        read.Dispose();
                        await Seek(urlNum);
                    }

                    // What happens when finding new item in same source is unsuccessful after ten attempts and bot has given up until next cycle
                    else
                    {
                        Console.WriteLine(DateTime.Now + ": Failure to find suitable article within 10 attempts, press any key to exit..." + Environment.NewLine);
                        File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Failure to find suitable article within 10 attempts, will try again later..." + Environment.NewLine + Environment.NewLine);
                    }
                }
            }
            catch (IOException error)
            {
                Console.Error.WriteLine(error);
            }
        }
        
        // Tweeting found article
        static async Task Tweet()
        {
            // Checks if tweeting from queue, uses relevant message if so
            if (queueRun == true)
            {
                Console.WriteLine(DateTime.Now + ": Tweeting from queue!" + Environment.NewLine + titles[0] + Environment.NewLine + url);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Tweeting from queue!" + Environment.NewLine + titles[0] + Environment.NewLine + url + Environment.NewLine);
            }

            // Relevant message for tweeting from random source
            else
            {
                Console.WriteLine(DateTime.Now + ": Suitable article found!" + Environment.NewLine + titles[i] + Environment.NewLine + links[i]);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Suitable article found!" + Environment.NewLine + titles[i] + Environment.NewLine + links[i] + Environment.NewLine);
                File.AppendAllText(path + "/postings.txt", titles[i] + Environment.NewLine);
            }

            // Tweeting
            try
            {
                Console.WriteLine(DateTime.Now + ": Tweeting article..." + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Tweeting article..." + Environment.NewLine + Environment.NewLine);

                // Authenticating account
                var userClient = new TwitterClient(keys[0], keys[1], keys[2], keys[3]);
                var user = await userClient.Users.GetAuthenticatedUserAsync();

                // Tweeting from queue
                if (queueRun == true)
                {
                    var tweet = await userClient.Tweets.PublishTweetAsync(titles[0] + "\n" + url);
                }

                // Tweeting from random source
                else
                {
                    var tweet = await userClient.Tweets.PublishTweetAsync(titles[i] + "\n" + links[i]);

                    // Updating frequency.txt
                    string[] freq = File.ReadAllLines(path + "/frequency.txt");
                    freq[urlNum] = (Int32.Parse(freq[urlNum]) + 1).ToString();
                    File.WriteAllText(path + "/frequency.txt", "");
                    bool runOnce = false;
                    foreach (string item in freq)
                    {
                        if (runOnce == false)
                        {
                            File.AppendAllText(path + "/frequency.txt", item);
                            runOnce = true;
                        }
                        else
                        {
                            File.AppendAllText(path + "/frequency.txt", Environment.NewLine + item);
                        }
                    }
                }

                // Checking if bot was being tested, exiting if so
                if (test == true)
                {
                    Console.WriteLine(DateTime.Now + ": Press any key to exit...");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Press any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(0);
                }

                // Returning to beginning for timer to check for next cycle initiation
                else
                {
                    return;
                }
            }
            catch (IOException error)
            {
                Console.Error.WriteLine(error);
            }
        }

        // Method for checking change of hour per timer length as defined in config, aligning with frequency in hours to run and initiate next cycle
        static async Task Timer()
        {
            try
            {
                Thread.Sleep(timerMS);
                if (hour < DateTime.Now.Hour || (hour <= 23 && DateTime.Now.Hour == hour + value - 24))
                {
                    hour = DateTime.Now.Hour + value - 1;
                    if (hour >= 24)
                    {
                        hour = hour - 24;
                    }
                    await Seek();
                }
                else
                {
                    return;
                }
            }
            catch (IOException error)
            {
                Console.Error.WriteLine(error);
            }
        }

        // Method for cleaning up frequency.txt in case new sources were added, to prevent crashing
        static void UpdateFrequency()
        {
            try
            {
                bool runOnce = false;
                string[] freq = File.ReadAllLines(path + "/frequency.txt");
                string[] sources = File.ReadAllLines(path + "/sources.txt");
                if (sources.Length != freq.Length)
                {
                    Console.WriteLine(DateTime.Now + ": Adding new lines for new sources to frequency.txt file..." + Environment.NewLine);
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Adding new lines for new sources to frequency.txt file..." + Environment.NewLine + Environment.NewLine);
                    File.WriteAllText(path + "/frequency.txt", "");
                    int r = 0;
                    foreach (string item in sources)
                    {
                        if (runOnce == false)
                        {
                            File.AppendAllText(path + "/frequency.txt", freq[r]);
                            runOnce = true;
                        }
                        else if (r < freq.Length)
                        {
                            File.AppendAllText(path + "/frequency.txt", Environment.NewLine + freq[r]);
                        }
                        else
                        {
                            File.AppendAllText(path + "/frequency.txt", Environment.NewLine + "0");
                        }
                        r = r++;
                    }
                }
            }
            catch (IOException error)
            {
                Console.Error.WriteLine(error);
            }
        }

        // Method for checking if source has multiple feeds
        static void CheckMult(int checkNum = -1)
        {
            try
            {
                // Checking if source has file with multiple feeds specified
                if (File.Exists(path + "/mult/" + urlNum + ".txt"))
                {
                    Console.WriteLine(DateTime.Now + ": Chose source #" + urlNum + " (" + url + ")!");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Chose source #" + urlNum + " (" + url + ")!" + Environment.NewLine);
                    Console.WriteLine(DateTime.Now + ": Source has multiple feeds, choosing one...");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Source has multiple feeds, choosing one..." + Environment.NewLine);
                    string[] multSources = File.ReadAllLines(path + "/mult/" + urlNum + ".txt");
                    int urlNumMult = rand.Next(0, multSources.Length);
                    url = multSources[urlNumMult];
                    Console.WriteLine(DateTime.Now + ": Chose source feed #" + urlNumMult + " (" + url + ")!");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Chose source feed #" + urlNumMult + " (" + url + ")!" + Environment.NewLine);
                }

                // What happens if no file exists specifying multiple feeds
                else if (checkNum == -1)
                {
                    Console.WriteLine(DateTime.Now + ": Chose source #" + urlNum + " (" + url + ")!");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Chose source #" + urlNum + " (" + url + ")!" + Environment.NewLine);
                }
            }
            catch (IOException error)
            {
                Console.Error.WriteLine(error);
            }
        }

        // Method for creating required files if missing
        static void CreateFile(string fileName, string extraInfo)
        {
            try
            {
                Console.WriteLine(DateTime.Now + ": Creating " + fileName + ".txt file..." + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Creating " + fileName + ".txt file..." + Environment.NewLine + Environment.NewLine);
                File.WriteAllText(path + "/" + fileName + ".txt", extraInfo);
            }
            catch (IOException error)
            {
                Console.Error.WriteLine(error);
            }
        }
    }
}
