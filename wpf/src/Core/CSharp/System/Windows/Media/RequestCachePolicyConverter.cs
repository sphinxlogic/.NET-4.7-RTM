//------------------------------------------------------------------------------
//  Microsoft Avalon
//  Copyright (c) Microsoft Corporation, 2007
//
//  File: RequesetCachePolicyConverter.cs
//------------------------------------------------------------------------------
using System;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using System.Windows.Media;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Net.Cache;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{    
    /// <summary>
    /// RequestCachePolicyConverter Parses a RequestCachePolicy.
    /// </summary>
    public sealed class RequestCachePolicyConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            if (t == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            // We can convert to an InstanceDescriptor or to a string.
            if (destinationType == typeof(InstanceDescriptor) ||
                destinationType == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ConvertFrom - attempt to convert to a RequestCachePolicy from the given object
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if the example object is null or is not a valid type
        /// which can be converted to a RequestCachePolicy.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext td, System.Globalization.CultureInfo ci, object value)
        {
            if (null == value)
            {
                throw GetConvertFromException(value);
            }

            string s = value as string;

            if (null == s)
            {
                throw new ArgumentException(SR.Get(SRID.General_BadType, "ConvertFrom"), "value");
            }

            HttpRequestCacheLevel level = (HttpRequestCacheLevel)Enum.Parse(typeof(HttpRequestCacheLevel), s, true);
            
            return new HttpRequestCachePolicy(level);
        }


        /// <summary>
        /// ConvertTo - Attempt to convert to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The policy to convert. </param>
        /// <param name="destinationType">The type to which to convert the policy. </param>
        ///<SecurityNote>
        ///     Critical: calls InstanceDescriptor ctor which LinkDemands
        ///     PublicOK: can only make an InstanceDescriptor for RequestCachePolicy/HttpRequestCachePolicy, not an arbitrary class
        ///</SecurityNote> 
        [SecurityCritical]
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext,
                                         CultureInfo cultureInfo,
                                         object value,
                                         Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            HttpRequestCachePolicy httpPolicy = value as HttpRequestCachePolicy;
            if(httpPolicy != null)
            {
                if (destinationType == typeof(string))
                {
                    return httpPolicy.Level.ToString();
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(HttpRequestCachePolicy).GetConstructor(new Type[] { typeof(HttpRequestCachePolicy) });
                    return new InstanceDescriptor(ci, new object[] { httpPolicy.Level });
                }
            }

            //if it's not an HttpRequestCachePolicy, try a regular RequestCachePolicy
            RequestCachePolicy policy = value as RequestCachePolicy;
            if (policy != null)
            {
                if (destinationType == typeof(string))
                {
                    return policy.Level.ToString();
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(RequestCachePolicy).GetConstructor(new Type[] { typeof(RequestCachePolicy) });
                    return new InstanceDescriptor(ci, new object[] { policy.Level });
                }
            }

            throw GetConvertToException(value, destinationType);
        }
    }
}
