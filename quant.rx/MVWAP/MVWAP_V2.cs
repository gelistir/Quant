﻿using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using quant.common;

namespace quant.rx
{
    class MVWAP_V2 : IObservable<double>
    {
        readonly IObservable<QTY_PX> _source;
        readonly IObservable<double> _offset;
        readonly uint WND_SIZE;
        // variables
        LinkedList<QTY_PX> que = new LinkedList<QTY_PX>();
        double  pxVol   = 0;
        uint    Vol     = 0;
        #region ctor
        public MVWAP_V2(IObservable<QTY_PX> source, uint period, IObservable<double> offset) {
            _source = source;
            WND_SIZE = period;
            _offset = offset;
        }
        #endregion
        void OnVal(QTY_PX val, IObserver<double> obsvr)
        {
            que.AddLast(val);    // add to the end                    
            pxVol += val.PxVol;  // add to the total sum and volume
            Vol += val.QTY;

            // if volume exceeded the limit
            while (Vol > WND_SIZE)
            {
                // remove  old value
                var oldTck = que.First.Value;
                que.RemoveFirst();
                // if removed quantity is a lot more than needed
                if (oldTck.QTY + WND_SIZE > Vol) {
                    // find amount to reduce
                    uint diff = Vol - WND_SIZE;
                    // add back the difference
                    que.AddFirst(new QTY_PX(oldTck.QTY - diff, oldTck.PX));
                    // reduce the aggregate amounts
                    pxVol -= oldTck.PX * diff;
                    Vol -= diff;
                }
                else {
                    // reduce the aggregate amounts
                    pxVol -= oldTck.PxVol;
                    Vol -= oldTck.QTY;
                }
            }
            // count matches window size
            if (Vol >= WND_SIZE) {
                obsvr.OnNext(pxVol / WND_SIZE);
            }
        }
        #region IObservable
        public IDisposable Subscribe(IObserver<double> obsvr)
        {
            var ret = new CompositeDisposable();
            if (_offset != null) {
                // empty
            }
            ret.Add(_source.Subscribe(val => OnVal(val, obsvr), obsvr.OnError, obsvr.OnCompleted));
            return ret;
        }
        #endregion
    }
}
