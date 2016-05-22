﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project3
{
    [Serializable()]
    public class EarningPrices
    {
        public DateTime ReleaseDate { get; set; }

        public double Delta { get; set; }

        public List<double> MarketReaction { get; set; }
    }

    [Serializable()]
    public class EarningData
    {
        public string Symbol { get; set; }
        public int Score { get; set; }
        public List<EarningPrices> RawData { get; set; }

        public void CalculateScore()
        {
            double sum = 0;
            foreach (var d in RawData)
            {
                d.Delta = Math.Abs(d.MarketReaction[1] / d.MarketReaction[0] - 1);
                sum += d.Delta;
            }

            Score = (int)(sum * 100);
        }
    }
}