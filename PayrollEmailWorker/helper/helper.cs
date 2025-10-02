using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollEmailWorker.helper
{
    public static class Helper
    {
        /// <summary>
        /// Generates salary period string from 21st of last month to 20th of current/next month based on today.
        /// </summary>
        public static string GetSalaryPeriod(DateTime today)
        {
            DateTime periodStart;
            DateTime periodEnd;

            if (today.Day >= 21)
            {
                // From 21st of current month to 20th of next month
                periodStart = new DateTime(today.Year, today.Month, 21);
                periodEnd = periodStart.AddMonths(1).AddDays(-1);
            }
            else
            {
                // From 21st of previous month to 20th of current month
                periodEnd = new DateTime(today.Year, today.Month, 20);
                periodStart = periodEnd.AddMonths(-1).AddDays(1);
            }

            return $"{periodStart:dd MMMM yyyy} - {periodEnd:dd MMMM yyyy}";
        }

        public static string NumberToWords(decimal number)
        {
            long intPart = (long)Math.Floor(number);
            int fractionPart = (int)((number - intPart) * 100);

            string words = NumberToWords(intPart);

            if (fractionPart > 0)
                words += $" and {fractionPart}/100";

            return words + " only";
        }

        // Recursive method for integer part
        public static string NumberToWords(long number)
        {
            if (number == 0)
                return "zero";

            if (number < 0)
                return "minus " + NumberToWords(Math.Abs(number));

            string words = "";

            if ((number / 10000000) > 0)
            {
                words += NumberToWords(number / 10000000) + " Crore ";
                number %= 10000000;
            }

            if ((number / 100000) > 0)
            {
                words += NumberToWords(number / 100000) + " Lakh ";
                number %= 100000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                var unitsMap = new[]
                {
            "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
            "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        };
                var tensMap = new[]
                {
            "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words.Trim();
        }

    }
}
