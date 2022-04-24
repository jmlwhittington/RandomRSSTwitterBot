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
    class RandomRSS
    {
        private static string path = Directory.GetCurrentDirectory();
        private static string logsPath = path + "/logs/";
        private static string startTime = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        private static List<string> titles = new List<string>();
        private static List<string> links = new List<string>();
        private static int urlNum;
        private static int i;
        private static bool failure = false;
        private static int attempts = 0;
        private static int hour = DateTime.Now.Hour;
        private static int value;
        private static bool test = false;
        private static bool queueRun = false;
        private static string url;
        private static int r = 0;
        private static Random rand = new Random();
        static async Task Main()
        {
            if (!Directory.Exists(path + "/logs"))
            {
                Directory.CreateDirectory(path + "/logs");
            }
            File.WriteAllText(logsPath + startTime + ".txt", "");
            Console.WriteLine(DateTime.Now + ": RandomRSSTwitterBot booting up!" + Environment.NewLine);
            File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": RandomRSSTwitterBot booting up!" + Environment.NewLine + Environment.NewLine);
            if (!File.Exists(path + "/keys.txt"))
            {
                File.WriteAllText(path + "/keys.txt", "<CONSUMER_KEY>        = " + Environment.NewLine + "<CONSUMER_SECRET>     = " + Environment.NewLine + "<ACCESS_TOKEN>        = " + Environment.NewLine + "<ACCESS_TOKEN_SECRET> = ");
                Console.WriteLine(DateTime.Now + ": You need to input your authentication keys into keys.txt!" + Environment.NewLine + "Press any key to exit...");
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": You need to input your authentication keys into keys.txt!" + Environment.NewLine + "Press any key to exit..." + Environment.NewLine);
                Console.ReadKey();
                Environment.Exit(0);
            }
            if (!File.Exists(path + "/sources.txt"))
            {
                Console.WriteLine(DateTime.Now + ": Creating sources.txt file..." + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Creating sources.txt file..." + Environment.NewLine + Environment.NewLine);
                File.WriteAllText(path + "/sources.txt", "https://news.google.com/rss/topics/CAAqJggKIiBDQkFTRWdvSUwyMHZNRGRqTVhZU0FtVnVHZ0pWVXlnQVAB");
            }
            if (!File.Exists(path + "/frequency.txt"))
            {
                Console.WriteLine(DateTime.Now + ": Creating frequency.txt file..." + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Creating frequency.txt file..." + Environment.NewLine + Environment.NewLine);
                File.WriteAllText(path + "/frequency.txt", "0");
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
            else
            {
                UpdateFrequency();
            }
            if (!File.Exists(path + "/postings.txt"))
            {
                Console.WriteLine(DateTime.Now + ": Creating postings.txt file..." + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Creating postings.txt file..." + Environment.NewLine + Environment.NewLine);
                File.WriteAllText(path + "/postings.txt", "");
            }
            if (!File.Exists(path + "/queue.txt"))
            {
                Console.WriteLine(DateTime.Now + ": Creating queue.txt file..." + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Creating queue.txt file..." + Environment.NewLine + Environment.NewLine);
                File.WriteAllText(path + "/queue.txt", "");
            }
            if (!Directory.Exists(path + "/mult"))
            {
                Directory.CreateDirectory(path + "/mult");
            }
            Console.WriteLine(DateTime.Now + ": How frequent in hours do you want this bot to run? Type 0 to test bot:");
            value = Console.Read();
            Console.WriteLine(Environment.NewLine);
            value = Int32.Parse(Convert.ToChar(value).ToString());
            if (value == 0)
            {
                test = true;
                await Seek(-1);
            }
            else
            {
                hour = hour + value - 1;
                if (hour >= 24)
                {
                    hour = hour - 24;
                }
                Console.WriteLine(DateTime.Now + ": Running bot every " + value + " hour(s)!" + Environment.NewLine);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Running bot every " + value + " hour(s)!" + Environment.NewLine + Environment.NewLine);
                while (true)
                {
                    await Timer();
                }
            }
        }
        static async Task Seek(int seekNum)
        {
            UpdateFrequency();
            try
            {
                if (seekNum == -1)
                {
                    Console.WriteLine(DateTime.Now + ": Seeking article to post...");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Seeking article to post..." + Environment.NewLine);
                }
                string[] queue = File.ReadAllLines(path + "/queue.txt");
                if (queue.Length > 1)
                {
                    titles.Add(queue[0]);
                    url = queue[1];
                    queueRun = true;
                    File.WriteAllText(path + "/queue.txt", "");
                    bool runOnce = false;
                    for (r = 2; r < queue.Length; r++)
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
                    r = 0;
                    await Tweet();
                }
                else
                {
                    string[] sources = File.ReadAllLines(path + "/sources.txt");
                    if (seekNum == -1)
                    {
                        urlNum = rand.Next(0, sources.Length);
                        url = sources[urlNum];
                        CheckMult(seekNum);
                    }
                    else
                    {
                        urlNum = seekNum;
                        url = sources[urlNum];
                        CheckMult(seekNum);
                    }
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
                    if (z >= 1)
                    {
                        Console.WriteLine(DateTime.Now + ": Source too frequent, choosing again...");
                        File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Source too frequent, choosing again..." + Environment.NewLine);
                        while (urlNum == urlCurrent)
                        {
                            urlNum = rand.Next(0, sources.Length);
                            url = sources[urlNum];
                            z = ((double)Int32.Parse(freq[urlNum]) - avg) / sd;
                            if (z >= 1)
                            {
                                urlNum = urlCurrent;
                            }
                        }
                        CheckMult(-1);
                    }
                    XmlReader read = XmlReader.Create(url);
                    SyndicationFeed rss = SyndicationFeed.Load(read);
                    read.Close();
                    foreach (SyndicationItem item in rss.Items)
                    {
                        titles.Add(item.Title.Text);
                        links.Add(item.Links[0].Uri.ToString());
                    }
                    i = rand.Next(0, titles.Count);
                    foreach (string item in File.ReadAllLines(path + "/postings.txt"))
                    {
                        if (titles[i] == item)
                        {
                            failure = true;
                        }
                    }
                    if (failure == false)
                    {
                        await Tweet();
                        read.Dispose();
                    }
                    else
                    {
                        if (attempts < 10)
                        {
                            failure = false;
                            attempts = attempts++;
                            titles.Clear();
                            links.Clear();
                            Console.WriteLine(DateTime.Now + ": Duplicate chosen, trying again... (Attempt number: " + attempts + ")");
                            File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Duplicate chosen, trying again... (Attempt number: " + attempts + ")" + Environment.NewLine);
                            read.Dispose();
                            await Seek(urlNum);
                        }
                        else
                        {
                            Console.WriteLine(DateTime.Now + ": Failure to find suitable article within 10 attempts, press any key to exit...");
                            File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Failure to find suitable article within 10 attempts, will try again later..." + Environment.NewLine);
                            attempts = 0;
                            failure = false;
                            titles.Clear();
                            links.Clear();
                            read.Dispose();
                            return;
                        }
                    }
                }
            }
            catch (IOException error)
            {
                Console.Error.WriteLine(error);
            }
        }
        static async Task Tweet()
        {
            if (queueRun == true)
            {
                Console.WriteLine(DateTime.Now + ": Tweeting from queue!" + Environment.NewLine + titles[0] + Environment.NewLine + url);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Tweeting from queue!" + Environment.NewLine + titles[0] + Environment.NewLine + url + Environment.NewLine);
            }
            else
            {
                Console.WriteLine(DateTime.Now + ": Suitable article found!" + Environment.NewLine + titles[i] + Environment.NewLine + links[i]);
                File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Suitable article found!" + Environment.NewLine + titles[i] + Environment.NewLine + links[i] + Environment.NewLine);
                File.AppendAllText(path + "/postings.txt", titles[i] + Environment.NewLine);
            }
            Console.WriteLine(DateTime.Now + ": Tweeting article...");
            File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Tweeting article..." + Environment.NewLine);
            string[] keys = File.ReadAllLines(path + "/keys.txt");
            keys[0] = keys[0].Remove(0, 24);
            keys[1] = keys[1].Remove(0, 24);
            keys[2] = keys[2].Remove(0, 24);
            keys[3] = keys[3].Remove(0, 24);
            try
            {
                if (queueRun == true)
                {
                    var userClient = new TwitterClient(keys[0], keys[1], keys[2], keys[3]);
                    var user = await userClient.Users.GetAuthenticatedUserAsync();
                    var tweet = await userClient.Tweets.PublishTweetAsync(titles[0] + "\n" + url);
                }
                else
                {
                    var userClient = new TwitterClient(keys[0], keys[1], keys[2], keys[3]);
                    var user = await userClient.Users.GetAuthenticatedUserAsync();
                    var tweet = await userClient.Tweets.PublishTweetAsync(titles[i] + "\n" + links[i]);
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
                attempts = 0;
                failure = false;
                queueRun = false;
                titles.Clear();
                links.Clear();
                if (test == true)
                {
                    Console.WriteLine(Environment.NewLine + DateTime.Now + ": Press any key to exit...");
                    File.AppendAllText(logsPath + startTime + ".txt", Environment.NewLine + DateTime.Now + ": Press any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(0);
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
        static async Task Timer()
        {
            try
            {
                Thread.Sleep(30000);
                if (hour < DateTime.Now.Hour || (hour <= 23 && DateTime.Now.Hour == hour + value - 24))
                {
                    hour = DateTime.Now.Hour + value - 1; ;
                    if (hour >= 24)
                    {
                        hour = hour - 24;
                    }
                    await Seek(-1);
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
                    r = 0;
                }
            }
            catch (IOException error)
            {
                Console.Error.WriteLine(error);
            }
        }
        static void CheckMult(int checkNum)
        {
            try
            {
                if (File.Exists(path + "/mult/" + urlNum + ".txt"))
                {
                    Console.WriteLine(DateTime.Now + ": Source has multiple feeds, choosing one...");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Source has multiple feeds, choosing one..." + Environment.NewLine);
                    string[] multSources = File.ReadAllLines(path + "/mult/" + urlNum + ".txt");
                    int urlNumMult = rand.Next(0, multSources.Length);
                    url = multSources[urlNumMult];
                    Console.WriteLine(DateTime.Now + ": Chose source feed #" + urlNumMult + " (" + url + ")!");
                    File.AppendAllText(logsPath + startTime + ".txt", DateTime.Now + ": Chose source feed #" + urlNumMult + " (" + url + ")!" + Environment.NewLine);
                }
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
    }
}
