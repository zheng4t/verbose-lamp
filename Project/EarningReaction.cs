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

        public void UpdatePrice()
        {
            dataCollection = new List<EarningData>();
            var x = new System.Xml.Serialization.XmlSerializer(typeof(List<EarningData>));
            using (var fileStream = new FileStream(@"C:\finalresult.xml", FileMode.Open))
            {
                dataCollection = (List<EarningData>)x.Deserialize(fileStream);
            }

            foreach (var item in dataCollection)
            {
                foreach (var data in item.RawData)
                {
                    try
                    {
                        var prices = GetPrice(item.Symbol, data.ReleaseDate);
                        data.PriceBefore = prices.Item1;
                        data.PriceAfter = prices.Item2;
                    }
                    catch (Exception e)
                    {
                        string error = item.Symbol + " " + e.ToString();
                        errorMessages.Add(error);
                        Console.WriteLine("Error getting prices for " + error);
                    }
                }
            }

            SerializeToFile(@"C:\finalresultHL.xml");
        }

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

            GenerateSummary(@"C:\summary.csv");
        }

        private void SerializeToFile(string filePath)
        {
            var file = System.IO.File.Create(filePath);

            var x = new System.Xml.Serialization.XmlSerializer(dataCollection.GetType());
            x.Serialize(file, dataCollection);
            file.Close();
        }

        private void GenerateSummary(string filePath)
        {
            var summary = from data in dataCollection
                          where data.RawData.Count > 0
                          select data.Symbol + "," + Convert.ToString(data.Score) + "," + data.NextRelease;
            
            File.WriteAllLines(filePath, summary);
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
            Console.WriteLine(symbol);

            var data = new EarningData();
            data.Symbol = symbol;
            data.RawData = new List<EarningPrices>();

            var dates = GetEarningReleaseDates(symbol);
            data.NextRelease = dates.Item2;

            foreach (var d in dates.Item1)
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

        private Tuple<List<DateTime>, string> GetEarningReleaseDates(string symbol)
        {
            var dates = new List<DateTime>();
            string nextDate = "";

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument document = htmlWeb.Load(earningURL + symbol);

                nextDate = GetNextReleaseDate(document, symbol);
                dates = GetHistoricalReleaseDate(document, symbol);
            }
            catch
            {
                Console.WriteLine("No release dates for " + symbol);
            }

            return new Tuple<List<DateTime>, string>(dates, nextDate);
        }

        private string GetNextReleaseDate(HtmlDocument document, string symbol)
        {
            var v = document.DocumentNode.SelectNodes("//body//h2").First();
            int posStart = v.InnerText.IndexOf(":") + 1;
            int posEnd = v.InnerText.IndexOf("\n", posStart);

            return v.InnerText.Substring(posStart, posEnd - posStart).Trim(); 
        }

        private List<DateTime> GetHistoricalReleaseDate(HtmlDocument document, string symbol)
        {
            var dates = new List<DateTime>();

            HtmlNode node = document.GetElementbyId("showdata-div");
            var divTable = node.Descendants().Where
                (d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("genTable")).First();
            var table = divTable.ChildNodes.Where(d => d.Name.Contains("table")).First();
            foreach (var row in table.SelectNodes("th|tr"))
            {
                dates.Add(DateTime.ParseExact(row.SelectNodes("td").ElementAt(1).InnerText, "d", CultureInfo.InvariantCulture));
            }

            return dates;
        }

        private Tuple<double, double> GetPrice(string s, DateTime d)
        {
            string page = "https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.historicaldata%20where%20symbol%20%3D%20%22" +
                s.Replace('.', '-') + "%22%20and%20startDate%20%3D%20%22" + d.AddDays(-1).ToString("yyyy-MM-dd") +
                "%22%20and%20endDate%20%3D%20%22" + d.AddDays(10).ToString("yyyy-MM-dd") +
                "%22&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys";

            XDocument doc = XDocument.Load(page);
            var quotes = doc.Root.Element("results").Elements("quote");

            double beforePrice = Convert.ToDouble(quotes.Last().Element("Adj_Close").Value);
            double afterPrice = GetAfterPrice(d, quotes, beforePrice);

            return new Tuple<double, double>(beforePrice, afterPrice);
        }

        private double GetAfterPrice(DateTime d, IEnumerable<XElement> quotes, double beforePrice)
        {
            double afterPrice1 = GetAfterOneDay(quotes.ElementAt(quotes.Count() - 3), beforePrice);
            double afterPrice2 = GetAfterOneDay(quotes.ElementAt(quotes.Count() - 2), beforePrice);

            if (Math.Abs(beforePrice - afterPrice1) > Math.Abs(beforePrice - afterPrice2))
                return afterPrice1;
            else
                return afterPrice2;

        }

        private double GetAfterOneDay(XElement x, double beforePrice) 
        {
            double high = Convert.ToDouble(x.Element("High").Value);
            double low = Convert.ToDouble(x.Element("Low").Value);

            if (Math.Abs(beforePrice - high) > Math.Abs(beforePrice - low))
                return high;
            else
                return low;
        }
    }
}
