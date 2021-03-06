//------------------------------------------------------------------------------
// <copyright file="ActivityCorrelator.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Data.Common
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Diagnostics;
    using System.Globalization;
    
    /// <summary>
    /// This class defines the data strucutre for ActvitiyId used for correlated tracing between client (bid trace event) and server (XEvent).
    /// It also includes all the APIs used to access the ActivityId. Note: ActivityId is thread based which is stored in TLS.
    /// </summary>
 
    internal static class ActivityCorrelator
    {
        internal const Bid.ApiGroup CorrelationTracePoints = Bid.ApiGroup.Correlation;

        internal class ActivityId
        {
            internal Guid Id { get; private set; }
            internal UInt32 Sequence { get; private set; }

            internal ActivityId()
            {
                this.Id = Guid.NewGuid();
                this.Sequence = 0; // the first event will start 1
            }

            // copy-constructor
            internal ActivityId(ActivityId activity)
            {
                this.Id = activity.Id;
                this.Sequence = activity.Sequence;
            }

            internal void Increment()
            {
                unchecked
                {
                    ++this.Sequence;
                }
            }

            public override string ToString()
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", this.Id, this.Sequence);
            }
        }

        // Declare the ActivityId which will be stored in TLS. The Id is unique for each thread.
        // The Sequence number will be incremented when each event happens.
        // Correlation along threads is consistent with the current XEvent mechanisam at server.
        [ThreadStaticAttribute]
        static ActivityId tlsActivity;

        /// <summary>
        /// Get the current ActivityId
        /// </summary>
        internal static ActivityId Current
        {
            get
            {
                if (tlsActivity == null)
                {
                    tlsActivity = new ActivityId();
                }

                return new ActivityId(tlsActivity);
            }
        }

        /// <summary>
        /// Increment the sequence number and generate the new ActviityId
        /// </summary>
        /// <returns>ActivityId</returns>
        internal static ActivityId Next()   
        {
            if (tlsActivity == null)
            {
                tlsActivity = new ActivityId();
            }

            tlsActivity.Increment();

            return new ActivityId(tlsActivity);
        }
    }
}
