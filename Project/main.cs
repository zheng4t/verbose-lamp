using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project3
{
    class Program
    {
        static void Main()
        {
            var reaction = new EarningReaction();
            var candidate = new EarningCandidate();
            Console.Write("Input command: 'r' for report, 'c' for candidate, 'm' for modify, numbers (1, 2, or 3) for groups ");

            string command = Console.ReadLine();
            if (command == "r")
                reaction.GenerateFinalReport();
            else if (command == "c")
                candidate.GetCandidate();
            else if (command == "m")
                reaction.UpdatePrice();
            else
            {
                int batch = Convert.ToInt16(command);
                reaction.Process(batch);
            }

        }

    }
}
