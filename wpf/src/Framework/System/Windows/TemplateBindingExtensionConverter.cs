//-----------------------------------------------------------------------
//
//  Microsoft Windows Client Platform
//  Copyright (C) Microsoft Corporation, 2005
//
//  File:      ElementItem.cs
//
//  Contents:  Implements a converter to an instance descriptor for 
//             TemplateBindingExtension
//
//  Created:   04/28/2005 Microsoft
//
//------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Windows;
using System.Security;

namespace System.Windows
{
    /// <summary>
    /// Type converter to inform the serialization system how to construct a TemplateBindingExtension from
    /// an instance. It reports that Property should be used as the first parameter to the constructor.
    /// </summary>
    public class TemplateBindingExtensionConverter : TypeConverter 
    {
        /// <summary>
        /// Returns true if converting to an InstanceDescriptor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts to an InstanceDescriptor
        /// </summary>
        ///<SecurityNote>
        ///     Critical: calls InstanceDescriptor ctor which LinkDemands
        ///     PublicOK: can only make an InstanceDescriptor for TemplateBindingExtension, not an arbitrary class
        ///</SecurityNote> 
        [SecurityCritical]
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                if(value == null)
                    throw new ArgumentNullException("value");

                TemplateBindingExtension templateBinding = value as TemplateBindingExtension;

                if(templateBinding == null)
                    throw new ArgumentException(SR.Get(SRID.MustBeOfType, "value", "TemplateBindingExtension"), "value");

                return new InstanceDescriptor(typeof(TemplateBindingExtension).GetConstructor(new Type[] { typeof(DependencyProperty) }),
                    new object[] { templateBinding.Property });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
}
