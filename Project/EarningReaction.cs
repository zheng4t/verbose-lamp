using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System.Threading;


namespace Project3
{
    class EarningReaction
    {
        private string earningURL = "http://www.nasdaq.com/earnings/report/";

        private List<EarningData> dataCollection;

        private List<string> errorMessages = new List<string>();

        public void GenerateFinalReport()
        {
            dataCollection = new List<EarningData>();
            string[] dirs = Directory.GetFiles(@"c:\", "result?.xml");

            foreach (string dir in dirs)
            {
                var data = new List<EarningData>();
                var x = new System.Xml.Serialization.XmlSerializer(typeof(List<EarningData>));
                using (var fileStream = new FileStream(dir, FileMode.Open))
                {
                    data = (List<EarningData>)x.Deserialize(fileStream);
                }

                dataCollection.AddRange(data);
            }

            dataCollection.Sort((x, y) => x.Score.CompareTo(y.Score));
            SerializeToFile(@"C:\finalresult.xml");
        }

        private void SerializeToFile(string filePath)
        {
            var file = System.IO.File.Create(filePath);

            var x = new System.Xml.Serialization.XmlSerializer(dataCollection.GetType());
            x.Serialize(file, dataCollection);
            file.Close();
        }

        public void Process(int batch)
        {
            dataCollection = new List<EarningData>();

            int numberPerPatch = 400;
            int batchStart = (batch - 1) * numberPerPatch + 1;
            int batchEnd = batch * numberPerPatch;

            int n = 0;
            string[] readText = File.ReadAllLines(@"c:\russell 1000.txt");
            foreach (string s in readText)
            {
                n++;
                if (n >= batchStart && n <= batchEnd)
                    dataCollection.Add(GetMarketReaction(s));
            }

            SerializeToFile(@"C:\result" + batch + ".xml");

            using (StreamWriter outputFile = new StreamWriter(@"c:\errorLog.txt"))
            {
                foreach (string line in errorMessages)
                    outputFile.WriteLine(line);
            }
        }

        private EarningData GetMarketReaction(string symbol)
        {
            var data = new EarningData()
            {
                Symbol = symbol,
                RawData = new List<EarningPrices>()
            };

            List<DateTime> dates = GetEarningReleaseDates(symbol);
            foreach (var d in dates)
            {
                try
                {
                    var ep = new EarningPrices();
                    ep.ReleaseDate = d;

                    var prices = GetPrice(symbol, d);
                    ep.PriceBefore = prices.Item1;
                    ep.PriceAfter = prices.Item2;

                    data.RawData.Add(ep);
                }
                catch (Exception e)
                {
                    string error = symbol + " " + e.ToString();
                    errorMessages.Add(error);
                    Console.WriteLine("Error getting prices for " + error);
                }
            }

            data.CalculateScore();

            return data;
        }

        private List<DateTime> GetEarningReleaseDates(string symbol)
        {
            Console.WriteLine(symbol);
            var dates = new List<DateTime>();
            DateTime nextDate;

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument document = htmlWeb.Load(earningURL + symbol);

                nextDate = GetNextReleaseDate(document, symbol);

                HtmlNode node = document.GetElementbyId("showdata-div");
                var divTable = node.Descendants().Where
                    (d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("genTable")).First();
                var table = divTable.ChildNodes.Where(d => d.Name.Contains("table")).First();
                foreach (var row in table.SelectNodes("th|tr"))
                {
                    dates.Add(DateTime.ParseExact(row.SelectNodes("td").ElementAt(1).InnerText, "d", CultureInfo.InvariantCulture));
                }
            }
            catch
            {
                Console.WriteLine("No release dates for " + symbol);
            }
            return dates;
        }

        private DateTime GetNextReleaseDate(HtmlDocument document, string symbol)
        {
            var v = document.DocumentNode.SelectNodes("//body//h2").First();
            int posStart = v.InnerText.IndexOf(":") + 1;
            int posEnd = v.InnerText.IndexOf("\n", posStart);
            string s = v.InnerText.Substring(posStart, posEnd - posStart).Trim();

            return Convert.ToDateTime(s);  
        }

        private Tuple<double, double> GetPrice(string s, DateTime d)
        {
            string page = "https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.historicaldata%20where%20symbol%20%3D%20%22" +
                s.Replace('.', '-') + "%22%20and%20startDate%20%3D%20%22" + d.AddDays(-1).ToString("yyyy-MM-dd") +
                "%22%20and%20endDate%20%3D%20%22" + d.AddDays(10).ToString("yyyy-MM-dd") +
                "%22&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys";

            XDocument doc = XDocument.Load(page);
            var quotes = doc.Root.Element("results").Elements("quote");

            string beforePrice = quotes.Last().Element("Adj_Close").Value;
            string afterPrice = quotes.ElementAt(quotes.Count() - 3).Element("Adj_Close").Value;

            return new Tuple<double, double>(Convert.ToDouble(beforePrice), Convert.ToDouble(afterPrice));
        }
    }
}
