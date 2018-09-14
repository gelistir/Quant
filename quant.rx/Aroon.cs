﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace quant.rx
{
    /// <summary>
    /// https://chartpatterns.files.wordpress.com/2011/11/excel-indicators.xls
    /// </summary>
    
    internal static class ArronExt
    {
        internal static IObservable<double> AroonDown_V1(this IObservable<double> source, int period)
        {
            return source.Buffer(period, 1).Where(x => x.Count == period)
                .Select(lt => {
                    var y = lt.Min();
                    var idx = period - (lt.IndexOf(y) + 1);
                    return (period - idx) * (100.0 / period);
                });
        }

        internal static IObservable<double> AroonUp_V1(this IObservable<double> source, int period)
        {
            return source.Buffer(period, 1).Where(x => x.Count == period)
                .Select(lt => {
                    var y = lt.Max();
                    var idx = period - (lt.IndexOf(y) + 1);
                    return (period - idx) * (100.0 / period);
                });
        }
        internal static IObservable<double> AroonDown_V2(this IObservable<double> source, int period)
        {
            return source.Buffer(period, 1).Where(x => x.Count == period)
                .Select(lt => {
                    var y = lt.Min();
                    var idx = period - (lt.IndexOf(y) + 1);
                    return (period - idx) * (100.0 / period);
                });
        }

        internal static IObservable<double> AroonUp_V2(this IObservable<double> source, int period)
        {
            return source.Buffer(period, 1).Where(x => x.Count == period)
                .Select(lt => {
                    var y = lt.Max();
                    var idx = period - (lt.IndexOf(y) + 1);
                    return (period - idx) * (100.0 / period);
                });
        }

    }

    public static partial class QuantExt
    {
        public static IObservable<double> AroonDown(this IObservable<double> source, int period)
        {
            return source.AroonDown_V2(period);
        }

        public static IObservable<double> AroonUp(this IObservable<double> source, int period)
        {
            return source.AroonUp_V2(period);
        }
    }
}
