using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagnitudeConcavityPeakFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            var peakFinder = new PeakDetector();

            var dataFilePath = @"..\..\Examples\Scan12543.txt";

            peakFinder.TestPeakFinder(dataFilePath);

            dataFilePath = @"..\..\Examples\Scan7478.txt";
            peakFinder.TestPeakFinder(dataFilePath);

        }
    }
}
