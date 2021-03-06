//------------------------------------------------------------------------------
// <copyright file="XsdDuration.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>                                                                
//------------------------------------------------------------------------------

namespace System.Xml.Schema {
    using System;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// This structure holds components of an Xsd Duration.  It is used internally to support Xsd durations without loss
    /// of fidelity.  XsdDuration structures are immutable once they've been created.
    /// </summary>
#if SILVERLIGHT    
    [System.Runtime.CompilerServices.FriendAccessAllowed] // used by System.Runtime.Serialization.dll
#endif
    internal struct XsdDuration {
        private int years;
        private int months;
        private int days;
        private int hours;
        private int minutes;
        private int seconds;
        private uint nanoseconds;       // High bit is used to indicate whether duration is negative

        private const uint NegativeBit = 0x80000000;

        private enum Parts {
            HasNone = 0,
            HasYears = 1,
            HasMonths = 2,
            HasDays = 4,
            HasHours = 8,
            HasMinutes = 16,
            HasSeconds = 32,
        }

        public enum DurationType {
            Duration,
            YearMonthDuration,
            DayTimeDuration,
        };

        /// <summary>
        /// Construct an XsdDuration from component parts.
        /// </summary>
        public XsdDuration(bool isNegative, int years, int months, int days, int hours, int minutes, int seconds, int nanoseconds) {
            if (years < 0) throw new ArgumentOutOfRangeException("years");
            if (months < 0) throw new ArgumentOutOfRangeException("months");
            if (days < 0) throw new ArgumentOutOfRangeException("days");
            if (hours < 0) throw new ArgumentOutOfRangeException("hours");
            if (minutes < 0) throw new ArgumentOutOfRangeException("minutes");
            if (seconds < 0) throw new ArgumentOutOfRangeException("seconds");
            if (nanoseconds < 0 || nanoseconds > 999999999) throw new ArgumentOutOfRangeException("nanoseconds");

            this.years = years;
            this.months = months;
            this.days = days;
            this.hours = hours;
            this.minutes = minutes;
            this.seconds = seconds;
            this.nanoseconds = (uint) nanoseconds;

            if (isNegative)
                this.nanoseconds |= NegativeBit;
        }

        /// <summary>
        /// Construct an XsdDuration from a TimeSpan value.
        /// </summary>
        public XsdDuration(TimeSpan timeSpan) : this(timeSpan, DurationType.Duration) {
        }

        /// <summary>
        /// Construct an XsdDuration from a TimeSpan value that represents an xsd:duration, an xdt:dayTimeDuration, or
        /// an xdt:yearMonthDuration.
        /// </summary>
        public XsdDuration(TimeSpan timeSpan, DurationType durationType) {
            long ticks = timeSpan.Ticks;
            ulong ticksPos;
            bool isNegative;

            if (ticks < 0) {
                // Note that (ulong) -Int64.MinValue = Int64.MaxValue + 1, which is what we want for that special case
                isNegative = true;
                ticksPos = (ulong) -ticks;
            }
            else {
                isNegative = false;
                ticksPos = (ulong) ticks;
            }

            if (durationType == DurationType.YearMonthDuration) {
                int years = (int) (ticksPos / ((ulong) TimeSpan.TicksPerDay * 365));
                int months = (int) ((ticksPos % ((ulong) TimeSpan.TicksPerDay * 365)) / ((ulong) TimeSpan.TicksPerDay * 30));

                if (months == 12) {
                    // If remaining days >= 360 and < 365, then round off to year
                    years++;
                    months = 0;
                }

                this = new XsdDuration(isNegative, years, months, 0, 0, 0, 0, 0);
            }
            else {
                Debug.Assert(durationType == DurationType.Duration || durationType == DurationType.DayTimeDuration);

                // Tick count is expressed in 100 nanosecond intervals
                this.nanoseconds = (uint) (ticksPos % 10000000) * 100;
                if (isNegative)
                    this.nanoseconds |= NegativeBit;

                this.years = 0;
                this.months = 0;
                this.days = (int) (ticksPos / (ulong) TimeSpan.TicksPerDay);
                this.hours = (int) ((ticksPos / (ulong) TimeSpan.TicksPerHour) % 24);
                this.minutes = (int) ((ticksPos / (ulong) TimeSpan.TicksPerMinute) % 60);
                this.seconds = (int) ((ticksPos / (ulong) TimeSpan.TicksPerSecond) % 60);
            }
        }

        /// <summary>
        /// Constructs an XsdDuration from a string in the xsd:duration format.  Components are stored with loss
        /// of fidelity (except in the case of overflow).
        /// </summary>
        public XsdDuration(string s) : this(s, DurationType.Duration) {
        }

        /// <summary>
        /// Constructs an XsdDuration from a string in the xsd:duration format.  Components are stored without loss
        /// of fidelity (except in the case of overflow).
        /// </summary>
        public XsdDuration(string s, DurationType durationType) {
            XsdDuration result;
            Exception exception = TryParse(s, durationType, out result);
            if (exception != null) {
                throw exception;
            }
            this.years = result.Years;
            this.months = result.Months;
            this.days = result.Days;
            this.hours = result.Hours;
            this.minutes = result.Minutes;
            this.seconds = result.Seconds;
            this.nanoseconds = (uint)result.Nanoseconds;
            if (result.IsNegative) {
                this.nanoseconds |= NegativeBit;
            }
            return;
        }

        /// <summary>
        /// Return true if this duration is negative.
        /// </summary>
        public bool IsNegative {
            get { return (this.nanoseconds & NegativeBit) != 0; }
        }

        /// <summary>
        /// Return number of years in this duration (stored in 31 bits).
        /// </summary>
        public int Years {
            get { return this.years; }
        }

        /// <summary>
        /// Return number of months in this duration (stored in 31 bits).
        /// </summary>
        public int Months {
            get { return this.months; }
        }

        /// <summary>
        /// Return number of days in this duration (stored in 31 bits).
        /// </summary>
        public int Days {
            get { return this.days; }
        }

        /// <summary>
        /// Return number of hours in this duration (stored in 31 bits).
        /// </summary>
        public int Hours {
            get { return this.hours; }
        }

        /// <summary>
        /// Return number of minutes in this duration (stored in 31 bits).
        /// </summary>
        public int Minutes {
            get { return this.minutes; }
        }

        /// <summary>
        /// Return number of seconds in this duration (stored in 31 bits).
        /// </summary>
        public int Seconds {
            get { return this.seconds; }
        }

        /// <summary>
        /// Return number of nanoseconds in this duration.
        /// </summary>
        public int Nanoseconds {
            get { return (int) (this.nanoseconds & ~NegativeBit); }
        }

#if !SILVERLIGHT
        /// <summary>
        /// Return number of microseconds in this duration.
        /// </summary>
        public int Microseconds {
            get { return Nanoseconds / 1000; }
        }

        /// <summary>
        /// Return number of milliseconds in this duration.
        /// </summary>
        public int Milliseconds {
            get { return Nanoseconds / 1000000; }
        }

        /// <summary>
        /// Normalize year-month part and day-time part so that month < 12, hour < 24, minute < 60, and second < 60.
        /// </summary>
        public XsdDuration Normalize() {
            int years = Years;
            int months = Months;
            int days = Days;
            int hours = Hours;
            int minutes = Minutes;
            int seconds = Seconds;

            try {
                checked {
                    if (months >= 12) {
                        years += months / 12;
                        months %= 12;
                    }

                    if (seconds >= 60) {
                        minutes += seconds / 60;
                        seconds %= 60;
                    }

                    if (minutes >= 60) {
                        hours += minutes / 60;
                        minutes %= 60;
                    }

                    if (hours >= 24) {
                        days += hours / 24;
                        hours %= 24;
                    }
                }
            }
            catch (OverflowException) {
                throw new OverflowException(Res.GetString(Res.XmlConvert_Overflow, ToString(), "Duration"));
            }

            return new XsdDuration(IsNegative, years, months, days, hours, minutes, seconds, Nanoseconds);
        }
#endif

        /// <summary>
        /// Internal helper method that converts an Xsd duration to a TimeSpan value.  This code uses the estimate
        /// that there are 365 days in the year and 30 days in a month.
        /// </summary>
        public TimeSpan ToTimeSpan() {
            return ToTimeSpan(DurationType.Duration);
        }

        /// <summary>
        /// Internal helper method that converts an Xsd duration to a TimeSpan value.  This code uses the estimate
        /// that there are 365 days in the year and 30 days in a month.
        /// </summary>
        public TimeSpan ToTimeSpan(DurationType durationType) {
            TimeSpan result;
            Exception exception = TryToTimeSpan(durationType, out result);
            if (exception != null) {
                throw exception;
            }
            return result;
        }

#if !SILVERLIGHT
        internal Exception TryToTimeSpan(out TimeSpan result) {
            return TryToTimeSpan(DurationType.Duration, out result);
        }
#endif

        internal Exception TryToTimeSpan(DurationType durationType, out TimeSpan result) {
            Exception exception = null; 
            ulong ticks = 0;

            // Throw error if result cannot fit into a long
            try {
                checked {
                    // Discard year and month parts if constructing TimeSpan for DayTimeDuration
                    if (durationType != DurationType.DayTimeDuration) {
                        ticks += ((ulong) this.years + (ulong) this.months / 12) * 365;
                        ticks += ((ulong) this.months % 12) * 30;
                    }

                    // Discard day and time parts if constructing TimeSpan for YearMonthDuration
                    if (durationType != DurationType.YearMonthDuration) {
                        ticks += (ulong) this.days;

                        ticks *= 24;
                        ticks += (ulong) this.hours;

                        ticks *= 60;
                        ticks += (ulong) this.minutes;

                        ticks *= 60;
                        ticks += (ulong) this.seconds;

                        // Tick count interval is in 100 nanosecond intervals (7 digits)
                        ticks *= (ulong) TimeSpan.TicksPerSecond;
                        ticks += (ulong) Nanoseconds / 100;
                    }
                    else {
                        // Multiply YearMonth duration by number of ticks per day
                        ticks *= (ulong) TimeSpan.TicksPerDay;
                    }

                    if (IsNegative) {
                        // Handle special case of Int64.MaxValue + 1 before negation, since it would otherwise overflow
                        if (ticks == (ulong) Int64.MaxValue + 1) {
                            result = new TimeSpan(Int64.MinValue);
                        }
                        else {
                            result = new TimeSpan(-((long) ticks));
                        }
                    }
                    else {
                        result = new TimeSpan((long) ticks);
                    }
                    return null;
                }
            }
            catch (OverflowException) {
                result = TimeSpan.MinValue;
                exception = new OverflowException(Res.GetString(Res.XmlConvert_Overflow, durationType, "TimeSpan"));
            }
            return exception;
        }

        /// <summary>
        /// Return the string representation of this Xsd duration.
        /// </summary>
        public override string ToString() {
            return ToString(DurationType.Duration);
        }

        /// <summary>
        /// Return the string representation according to xsd:duration rules, xdt:dayTimeDuration rules, or
        /// xdt:yearMonthDuration rules.
        /// </summary>
        internal string ToString(DurationType durationType) {
            StringBuilder sb = new StringBuilder(20);
            int nanoseconds, digit, zeroIdx, len;

            if (IsNegative)
                sb.Append('-');

            sb.Append('P');

            if (durationType != DurationType.DayTimeDuration) {
                
                if (this.years != 0) {
                    sb.Append(XmlConvert.ToString(this.years));
                    sb.Append('Y');
                }

                if (this.months != 0) {
                    sb.Append(XmlConvert.ToString(this.months));
                    sb.Append('M');
                }
            }

            if (durationType != DurationType.YearMonthDuration) {
                if (this.days != 0) {
                    sb.Append(XmlConvert.ToString(this.days));
                    sb.Append('D');
                }

                if (this.hours != 0 || this.minutes != 0 || this.seconds != 0 || Nanoseconds != 0) {
                    sb.Append('T');
                    if (this.hours != 0) {
                        sb.Append(XmlConvert.ToString(this.hours));
                        sb.Append('H');
                    }

                    if (this.minutes != 0) {
                        sb.Append(XmlConvert.ToString(this.minutes));
                        sb.Append('M');
                    }

                    nanoseconds = Nanoseconds;
                    if (this.seconds != 0 || nanoseconds != 0) {
                        sb.Append(XmlConvert.ToString(this.seconds));
                        if (nanoseconds != 0) {
                            sb.Append('.');

                            len = sb.Length;
                            sb.Length += 9;
                            zeroIdx = sb.Length - 1;

                            for (int idx = zeroIdx; idx >= len; idx--) {
                                digit = nanoseconds % 10;
                                sb[idx] = (char) (digit + '0');

                                if (zeroIdx == idx && digit == 0)
                                    zeroIdx--;

                                nanoseconds /= 10;
                            }

                            sb.Length = zeroIdx + 1;
                        }
                        sb.Append('S');
                    }
                }

                // Zero is represented as "PT0S"
                if (sb[sb.Length - 1] == 'P')
                    sb.Append("T0S");
            }
            else {
                // Zero is represented as "T0M"
                if (sb[sb.Length - 1] == 'P')
                    sb.Append("0M");
            }

            return sb.ToString();
        }

#if !SILVERLIGHT
        internal static Exception TryParse(string s, out XsdDuration result) {
            return TryParse(s, DurationType.Duration, out result);
        }
#endif

        internal static Exception TryParse(string s, DurationType durationType, out XsdDuration result) {
            string errorCode; 
            int length;
            int value, pos, numDigits;
            Parts parts = Parts.HasNone;

            result = new XsdDuration();

            s = s.Trim();
            length = s.Length;

            pos = 0;
            numDigits = 0;

            if (pos >= length) goto InvalidFormat;

            if (s[pos] == '-') {
                pos++;
                result.nanoseconds = NegativeBit;
            }
            else {
                result.nanoseconds = 0;
            }

            if (pos >= length) goto InvalidFormat;

            if (s[pos++] != 'P') goto InvalidFormat;

            errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
            if (errorCode != null) goto Error;

            if (pos >= length) goto InvalidFormat;

            if (s[pos] == 'Y') {
                if (numDigits == 0) goto InvalidFormat;

                parts |= Parts.HasYears;
                result.years = value;
                if (++pos == length) goto Done;

                errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                if (errorCode != null) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'M') {
                if (numDigits == 0) goto InvalidFormat;

                parts |= Parts.HasMonths;
                result.months = value;
                if (++pos == length) goto Done;

                errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                if (errorCode != null) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'D') {
                if (numDigits == 0) goto InvalidFormat;

                parts |= Parts.HasDays;
                result.days = value;
                if (++pos == length) goto Done;

                errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                if (errorCode != null) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'T') {
                if (numDigits != 0) goto InvalidFormat;

                pos++;
                errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                if (errorCode != null) goto Error;

                if (pos >= length) goto InvalidFormat;

                if (s[pos] == 'H') {
                    if (numDigits == 0) goto InvalidFormat;

                    parts |= Parts.HasHours;
                    result.hours = value;
                    if (++pos == length) goto Done;

                    errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                    if (errorCode != null) goto Error;

                    if (pos >= length) goto InvalidFormat;
                }

                if (s[pos] == 'M') {
                    if (numDigits == 0) goto InvalidFormat;

                    parts |= Parts.HasMinutes;
                    result.minutes = value;
                    if (++pos == length) goto Done;

                    errorCode = TryParseDigits(s, ref pos, false, out value, out numDigits);
                    if (errorCode != null) goto Error;

                    if (pos >= length) goto InvalidFormat;
                }

                if (s[pos] == '.') {
                    pos++;

                    parts |= Parts.HasSeconds;
                    result.seconds = value;

                    errorCode = TryParseDigits(s, ref pos, true, out value, out numDigits);
                    if (errorCode != null) goto Error;

                    if (numDigits == 0) { //If there are no digits after the decimal point, assume 0
                        value = 0;
                    }
                    // Normalize to nanosecond intervals
                    for (; numDigits > 9; numDigits--)
                        value /= 10;

                    for (; numDigits < 9; numDigits++)
                        value *= 10;

                    result.nanoseconds |= (uint) value;

                    if (pos >= length) goto InvalidFormat;

                    if (s[pos] != 'S') goto InvalidFormat;
                    if (++pos == length) goto Done;
                }
                else if (s[pos] == 'S') {
                    if (numDigits == 0) goto InvalidFormat;

                    parts |= Parts.HasSeconds;
                    result.seconds = value;
                    if (++pos == length) goto Done;
                }
            }

            // Duration cannot end with digits
            if (numDigits != 0) goto InvalidFormat;

            // No further characters are allowed
            if (pos != length) goto InvalidFormat;

        Done:
            // At least one part must be defined
            if (parts == Parts.HasNone) goto InvalidFormat;

            if (durationType == DurationType.DayTimeDuration) {
                if ((parts & (Parts.HasYears | Parts.HasMonths)) != 0)
                    goto InvalidFormat;
            }
            else if (durationType == DurationType.YearMonthDuration) {
                if ((parts & ~(XsdDuration.Parts.HasYears | XsdDuration.Parts.HasMonths)) != 0)
                    goto InvalidFormat;
            }
            return null;

        InvalidFormat:
            return new FormatException(Res.GetString(Res.XmlConvert_BadFormat, s, durationType));

        Error:
            return new OverflowException(Res.GetString(Res.XmlConvert_Overflow, s, durationType));
        }

        /// Helper method that constructs an integer from leading digits starting at s[offset].  "offset" is
        /// updated to contain an offset just beyond the last digit.  The number of digits consumed is returned in
        /// cntDigits.  The integer is returned (0 if no digits).  If the digits cannot fit into an Int32:
        ///   1. If eatDigits is true, then additional digits will be silently discarded (don't count towards numDigits)
        ///   2. If eatDigits is false, an overflow exception is thrown
        private static string TryParseDigits(string s, ref int offset, bool eatDigits, out int result, out int numDigits) {
            int offsetStart = offset;
            int offsetEnd = s.Length;
            int digit;

            result = 0;
            numDigits = 0;

            while (offset < offsetEnd && s[offset] >= '0' && s[offset] <= '9') {
                digit = s[offset] - '0';

                if (result > (Int32.MaxValue - digit) / 10) {
                    if (!eatDigits) {
                        return Res.XmlConvert_Overflow;
                    }

                    // Skip past any remaining digits
                    numDigits = offset - offsetStart;

                    while (offset < offsetEnd && s[offset] >= '0' && s[offset] <= '9') {
                        offset++;
                    }

                    return null;
                }

                result = result * 10 + digit;
                offset++;
            }

            numDigits = offset - offsetStart;
            return null;
        }
    }
}
