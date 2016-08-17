using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Xml.Linq;
using System.IO;


namespace Project3
{
    public class EarningCandidate
    {
        private string calendarURL = "http://www.nasdaq.com/earnings/earnings-calendar.aspx?date=";

        public class Candidate
        {
            public string symbol;
            public string value;
        }

        public List<Candidate> CandidateList;
        private List<string> Weeklys;

        public void GetCandidate()
        {
            GetWeeklyOptions();

            CandidateList = new List<Candidate>();
            string date = "2016-Aug-17";
            GetCandidate(false, date);
            GetCandidate(true, "2016-Aug-18");

            var file = System.IO.File.Create(Constants.ArtifactPath + @"\candidate.xml");

            var x = new System.Xml.Serialization.XmlSerializer(CandidateList.GetType());
            x.Serialize(file, CandidateList);
            file.Close();
        }

        private void GetWeeklyOptions()
        {
            Weeklys = new List<string>();
            string[] readText = File.ReadAllLines(Constants.ArtifactPath + @"\weeklysmf.csv");
            foreach (string s in readText)
                Weeklys.Add(s.Substring(0, s.IndexOf(',')));
        }

        private void GetCandidate(bool preopen, string date)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument document = htmlWeb.Load(calendarURL + date);
            HtmlNode table = document.GetElementbyId("ECCompaniesTable");
            foreach (var row in table.SelectNodes("tr"))
            {
                string t;
                try
                {
                    t = row.SelectNodes("td").ElementAt(0).ChildNodes[1].Attributes[1].Value;
                }
                catch
                {
                    continue;
                }

                if ((preopen && !t.Equals("Pre-market Quotes", StringComparison.InvariantCultureIgnoreCase)) ||
                    (!preopen && !t.Equals("After Hours Quotes", StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                string s = row.SelectNodes("td").ElementAt(1).InnerText;
                int b = s.LastIndexOf('(') + 1;
                int e = s.LastIndexOf(')');

                Candidate c = new Candidate();
                c.symbol = s.Substring(b, e - b);

                b = s.IndexOf('$') + 1;
                c.value = s.Substring(b).Trim();

                if (c.value.EndsWith("B") && Convert.ToDouble(c.value.Substring(0, c.value.Length - 1)) > 10)
                {
           //         if (Weeklys.Contains(c.symbol))
                        CandidateList.Add(c);
                }
            }
        }
    }
}
