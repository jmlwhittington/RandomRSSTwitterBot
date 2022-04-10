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

namespace RandomRSSTwitterBot
{
    class RandomRSS
    {
        private static string path = Directory.GetCurrentDirectory();
        private static string startTime = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        private static List<string> titles = new List<string>();
        private static List<Uri> links = new List<Uri>();
        private static int i;
        private static int urlNum;
        private static bool failure = false;
        private static int attempts = 0;
        private static int hour = DateTime.Now.Hour;
        private static int value;
        private static bool test = false;
        private static Random rand = new Random();
        static async Task Main()
        {
            if (!Directory.Exists(path + "/logs"))
            {
                Directory.CreateDirectory(path + "/logs");
            }
            File.WriteAllText(path + "/logs/" + startTime + ".txt", "");
            Console.WriteLine(DateTime.Now + ": RandomRSSTwitterBot booting up!" + Environment.NewLine);
            File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": RandomRSSTwitterBot booting up!" + Environment.NewLine + Environment.NewLine);
            if (!File.Exists(path + "/keys.txt"))
            {
                File.WriteAllText(path + "/keys.txt", "<CONSUMER_KEY>        = " + Environment.NewLine + "<CONSUMER_SECRET>     = " + Environment.NewLine + "<ACCESS_TOKEN>        = " + Environment.NewLine + "<ACCESS_TOKEN_SECRET> = ");
                Console.WriteLine(DateTime.Now + ": You need to input your authentication keys into keys.txt!" + Environment.NewLine + "Press any key to exit...");
                File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": You need to input your authentication keys into keys.txt!" + Environment.NewLine + "Press any key to exit..." + Environment.NewLine);
                Console.Read();
                Environment.Exit(0);
            }
            if (!File.Exists(path + "/sources.txt"))
            {
                Console.WriteLine(DateTime.Now + ": Creating sources.txt file..." + Environment.NewLine);
                File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": Creating sources.txt file..." + Environment.NewLine + Environment.NewLine);
                File.WriteAllText(path + "/sources.txt", "https://news.google.com/rss/topics/CAAqJggKIiBDQkFTRWdvSUwyMHZNRGRqTVhZU0FtVnVHZ0pWVXlnQVAB");
            }
            if (!File.Exists(path + "/postings.txt"))
            {
                File.WriteAllText(path + "/postings.txt", "");
            }
            Console.WriteLine(DateTime.Now + ": How frequent in hours do you want this bot to run? Type 0 to test bot:");
            value = Console.Read();
            Console.WriteLine(Environment.NewLine);
            value = Int32.Parse(Convert.ToChar(value).ToString());
            if (value == 0)
            {
                test = true;
                Console.WriteLine(DateTime.Now + ": Seeking article to post...");
                File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": Seeking article to post..." + Environment.NewLine);
                await Seek();
            }
            else
            {
                hour = hour + value - 1;
                if (hour >= 24)
                {
                    hour = hour - 24;
                }
                Console.WriteLine(DateTime.Now + ": Running bot every " + value + " hour(s)!" + Environment.NewLine);
                File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": Running bot every " + value + " hour(s)!" + Environment.NewLine + Environment.NewLine);
                await Timer();
            }
        }
        static async Task Seek()
        {
            string[] sources = File.ReadAllLines(path + "/sources.txt");
            urlNum = rand.Next(0, sources.Length);
            string url = sources[urlNum];
            XmlReader read = XmlReader.Create(url);
            SyndicationFeed rss = SyndicationFeed.Load(read);
            read.Close();
            foreach (SyndicationItem item in rss.Items)
            {
                titles.Add(item.Title.Text);
                links.Add(new Uri(item.Links[0].Uri.ToString()));
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
            }
            else
            {
                if (attempts < 10)
                {
                    failure = false;
                    attempts = attempts + 1;
                    Console.WriteLine(DateTime.Now + ": Duplicate chosen, trying again... (Attempt number: " + attempts + ")");
                    File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": Duplicate chosen, trying again... (Attempt number: " + attempts + ")" + Environment.NewLine);
                    await Seek();
                }
                else
                {
                    Console.WriteLine(DateTime.Now + ": Failure to find suitable article within 10 attempts, press any key to exit...");
                    File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": Failure to find suitable article within 10 attempts, will try again later..." + Environment.NewLine);
                    attempts = 0;
                    failure = false;
                    GC.Collect();
                    await Timer();
                }
            }
            read.Dispose();
        }
        static async Task Tweet()
        {
            Console.WriteLine(DateTime.Now + ": Suitable article found!" + Environment.NewLine + titles[i] + Environment.NewLine + links[i]);
            File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": Suitable article found!" + Environment.NewLine + titles[i] + Environment.NewLine + links[i] + Environment.NewLine);
            File.AppendAllText(path + "/postings.txt", titles[i] + Environment.NewLine);
            Console.WriteLine(DateTime.Now + ": Tweeting article..." + Environment.NewLine);
            File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": Tweeting article..." + Environment.NewLine + Environment.NewLine);
            string[] keys = File.ReadAllLines(path + "/keys.txt");
            keys[0] = keys[0].Remove(0, 24);
            keys[1] = keys[1].Remove(0, 24);
            keys[2] = keys[2].Remove(0, 24);
            keys[3] = keys[3].Remove(0, 24);
            var userClient = new TwitterClient(keys[0], keys[1], keys[2], keys[3]);
            var user = await userClient.Users.GetAuthenticatedUserAsync();
            var tweet = await userClient.Tweets.PublishTweetAsync(titles[i] + "\n" + links[i]);
            attempts = 0;
            failure = false;
            GC.Collect();
            if (test == true)
            {
                Console.WriteLine(DateTime.Now + "Press any key to exit...");
                File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + "Press any key to exit...");
                Console.Read();
                Environment.Exit(0);
            }
            else
            {
                await Timer();
            }
        }
        static async Task Timer()
        {
            Thread.Sleep(60000);
            if (hour < DateTime.Now.Hour || (hour <= 23 && DateTime.Now.Hour == hour + value - 24))
            {
                hour = DateTime.Now.Hour + value - 1; ;
                if (hour >= 24)
                {
                    hour = hour - 24;
                }
                Console.WriteLine(DateTime.Now + ": Seeking article to post...");
                File.AppendAllText(path + "/logs/" + startTime + ".txt", DateTime.Now + ": Seeking article to post..." + Environment.NewLine);
                await Seek();
            }
            else
            {
                await Timer();
            }
        }
    }
}
