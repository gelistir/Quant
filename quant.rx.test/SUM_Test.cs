﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace quant.rx.test
{
    [TestClass]
    public class SUMTest
    {
        [TestMethod]
        public void SUM_Test_1()
        {
            /// <summary>
            /// Data from
            /// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_averages
            /// </summary>
            double[] items = { 22.2734, 22.1940, 22.0847, 22.1741, 22.1840, 22.1344,
                22.2337, 22.4323, 22.2436, 22.2933, 22.1542, 22.3926, 22.3816, 22.6109,
                23.3558, 24.0519, 23.7530, 23.8324, 23.9516, 23.6338, 23.8225, 23.8722,
                23.6537, 23.1870, 23.0976, 23.3260, 22.6805, 23.0976, 22.4025, 22.1725
            };

            string sum1 = null;
            string sum2 = null;
            string sum3 = null;
            items.ToObservable().Publish(sr => {
                sr.SUM_V1(10).Subscribe(x => sum1 = x.ToString("0.00"));
                sr.SUM_V2(10).Subscribe(x => sum2 = x.ToString("0.00"));
                // validate using SMA 
                sr.SMA_V2(10).Subscribe(x => sum3 = (x * 10).ToString("0.00"));
                return sr;
            }).Subscribe(val => {
                Debug.Assert(sum1 == sum2 && sum1 == sum3);
                Trace.WriteLine($"{val.ToString("0.00")}\t{sum1}\t{sum2}\t{sum3}");
            });

        }
        [TestMethod]
        public void Perf_Test()
        {
            Random rnd = new Random();
            var data = new List<double>();
            for (int itr = 0; itr < 1000000; itr++)
            {
                data.Add(rnd.Next(1, 5));
                data.Add(rnd.Next(5, 10));
                data.Add(rnd.Next(10, 15));
                data.Add(rnd.Next(15, 20));
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var cnt = data.ToObservable().SUM_V1(100).Count().Wait();
            sw.Stop();
            Trace.WriteLine($"{sw.ElapsedMilliseconds}\t{cnt}");
            sw = new Stopwatch();
            sw.Start();
            cnt = data.ToObservable().SUM_V2(100).Count().Wait();
            sw.Stop();
            Trace.WriteLine($"{sw.ElapsedMilliseconds}\t{cnt}");
        }
    }
}
