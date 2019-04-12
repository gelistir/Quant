﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;

namespace quant.core
{
    /// <summary>
    /// OHLC = Open High Low Close Ticks
    /// bar can be on Interval, VWAP, Volume, Price 
    /// Open.Security != Close.Security is possible because of Roll
    /// Offset represents the Offset between the roll of the securities
    /// Offset == 0 if Open.Security == Close.Security
    /// </summary>
    public class OHLC
    {
        /// <summary>
        /// update count, volume and pxVol
        /// </summary>
        /// <param name="tck"></param>
        void updateStats(Tick tck) {
            Count++;
            Volume += tck.Quantity;
            PxVol += tck.PxVol;
        }

        #region ctor
        /// <summary>
        /// need a tick to start the bar
        /// </summary>
        public OHLC(Tick tck) {
            High = Low = Open = Close = tck;
            updateStats(tck);
        }
        #endregion

        #region enum
        public enum Color { Black, Red, NA };
        public enum PriceType { OPEN, HIGH, LOW, CLOSE, HL, HLC, OHLC };
        #endregion

        #region properties
        public Tick     Open    { get; private set; } = null;
        public Tick     Close   { get; private set; } = null;
        public Tick     High    { get; private set; } = null;
        public Tick     Low     { get; private set; } = null;
        public uint     Volume  { get; private set; } = 0;
        public uint     Count   { get; private set; } = 0;
        public double   PxVol   { get; private set; } = 0;
        public int      Offset  { get; private set; } = 0;

        public double   VWAP    => PxVol / Volume;
        public uint     Range   => High.Price - Low.Price;
        public DateTime Seed { get; set; }
        public Color FillColor => (Close.Price == Open.Price) ? Color.NA : (Close.Price > Open.Price) ? Color.Black : Color.Red;
        #endregion
        public (int hr, int tr, int lr) DM(OHLC prev)
        {
            // To do adjust for Roll
            return ((int)(this.High.Price - prev.High.Price), (int)TR(prev), (int)(prev.Low.Price - this.Low.Price));
        }
        public long TR(OHLC prev)
        {
            // To do check the logic
            // adjusted for Roll.
            var adj_prevClose = prev.Close.Price + Offset;
            var high_prevclose = Math.Abs(this.High.Price - adj_prevClose);
            var low_prevclose = Math.Abs(this.Low.Price - adj_prevClose);
            return Math.Max(this.Range, Math.Max(low_prevclose, high_prevclose));
        }
        public void Add(Tick tck) {
            // update High and Low
            if (High.Price < tck.Price) 
                High = tck;
            else if (Low.Price > tck.Price) 
                Low = tck;

            // security roll
            if(tck.Security != Close.Security) {
                // find the offset
                var diff = (int)(tck.Price - Close.Price);
                // increment offset if there are multiple rolls
                Offset += diff;
                // adjust pxVol to reflect continuous pricing
                PxVol += Volume * diff;
            }
            // update Close
            Close = tck;
            // update stats
            updateStats(tck);
        }
        public int get_Offset(OHLC old) {

            int retVal = Offset;
            // roll happened at end of bar and bar includes multiple contracts 
            if (old != null && old.Close.Security != this.Open.Security)
                retVal += (int)(this.Open.Price - old.Close.Price);
            return retVal;
        }
        public int get_Offset(Tick old) {
            int retVal = Offset;
            // roll happened at end of bar and bar includes multiple contracts 
            if (old != null && old.Security != this.Open.Security)
                retVal += (int)(this.Open.Price - old.Price);
            return retVal;
        }
        #region Object
        public override string ToString() {
            var opn = Open.TradedAt.ToString("MM/dd/yyyy HH:mm:ss.fff");
            var cls = Close.TradedAt.ToString("MM/dd/yyyy HH:mm:ss.fff");
            return ($"OHLC:\t{Close.Security}\t[{opn} : {cls}]\t[O:{Open.Price} H:{High.Price} L:{Low.Price} C:{Close.Price} V:{Volume}]");
        }
        #endregion
    }
    public static class OHLCExt
    {
        /// <summary>
        /// OHLC Generation from source till OnComplete is called
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IObservable<OHLC> OHLC(this IObservable<Tick> source) {
            return source.Aggregate((OHLC)null, 
                (oh, tk) => {
                    // update or create (upsert) OHLC
                    if (oh != null)
                        oh.Add(tk);
                    else
                        oh = new OHLC(tk);
                    return oh;
                });
        }
        /// <summary>
        /// generate stream of OHLC on continuous pricing source
        /// </summary>
        /// <param name="source"></param>
        /// <param name="durationSelector"></param>
        /// <returns></returns>
        internal static IObservable<OHLC> OHLC(this IObservable<Tick> source, Func<OHLC, Tick, bool> durationSelector) {
            return Observable.Create<OHLC>(obs => {
                OHLC ohlc = null;
                return source.Subscribe((tck) => {
                    if (ohlc == null || durationSelector(ohlc, tck)) {
                        if(ohlc != null)
                            obs.OnNext(ohlc);
                        ohlc = new OHLC(tck);
                    }
                    else
                        ohlc.Add(tck);
                }, obs.OnError, obs.OnCompleted);
            });
        }
        /// <summary>
        /// Utility function : can be moved where Bucket() code exists
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IObservable<IList<OHLC>> OHLC(this IObservable<IObservable<Tick>> source)  {
            return source.SelectMany(x => x.OHLC()).ToList();
        }
        /// <summary>
        /// replaced by above function
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IObservable<IList<OHLC>> OHLC_OLD(this IObservable<IObservable<Tick>> source)
        {
            return source.SelectMany(p => p.Aggregate((OHLC)null,
                (ohlc, td) => {
                    if (ohlc == null)
                        ohlc = new OHLC(td);
                    else
                        ohlc.Add(td);
                    return ohlc;
                })).ToList();
        }
    }
}
