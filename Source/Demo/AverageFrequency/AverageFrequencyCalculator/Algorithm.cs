using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AverageFrequencyCalculator.Framework;
using AverageFrequencyCalculator.Model.Test;

namespace AverageFrequencyCalculator
{
    static class Algorithm
    {
		public static AverageFrequency Execute(FrequencyCollection input)
		{
			AverageFrequency output = new AverageFrequency();

            output.Value = input.Frequencies.DefaultIfEmpty(double.NaN).Average();
            string avg = output.Value.ToString("0.000").PadLeft(10);
            MainWindow.WriteMessage($"Avg: {avg}");

			return output;
		}
    }
}
