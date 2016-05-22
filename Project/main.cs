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
            Console.Write("Input batch number: ");
            int batch = Convert.ToInt16(Console.ReadLine());

            var reaction = new EarningReaction();
            reaction.Start(batch);

            Console.WriteLine("Done");
        }

    }
}
