//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace System.Runtime.Serialization.Json
{
    using System.Xml;
    using System.ServiceModel;
    using System.Runtime.Serialization;
    using System.Globalization;

    class JsonObjectDataContract : JsonDataContract
    {
        public JsonObjectDataContract(DataContract traditionalDataContract)
            : base(traditionalDataContract)
        {
        }

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
        {
            object obj;
            string contentMode = jsonReader.GetAttribute(JsonGlobals.typeString);

            switch (contentMode)
            {
                case JsonGlobals.nullString:
                    jsonReader.Skip();
                    obj = null;
                    break;
                case JsonGlobals.booleanString:
                    obj = jsonReader.ReadElementContentAsBoolean();
                    break;
                case JsonGlobals.stringString:
                case null:
                    obj = jsonReader.ReadElementContentAsString();
                    break;
                case JsonGlobals.numberString:
                    obj = ParseJsonNumber(jsonReader.ReadElementContentAsString());
                    break;
                case JsonGlobals.objectString:
                    jsonReader.Skip();
                    obj = new object();
                    break;
                case JsonGlobals.arrayString:
                    // Read as object array
                    return DataContractJsonSerializer.ReadJsonValue(DataContract.GetDataContract(Globals.TypeOfObjectArray), jsonReader, context);
                default:
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        XmlObjectSerializer.CreateSerializationException(SR.GetString(SR.JsonUnexpectedAttributeValue, contentMode)));
            }

            if (context != null)
            {
                context.AddNewObject(obj);
            }
            return obj;
        }

        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
        {
            jsonWriter.WriteAttributeString(null, JsonGlobals.typeString, null, JsonGlobals.objectString);
        }

        internal static object ParseJsonNumber(string value, out TypeCode objectTypeCode)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(System.Runtime.Serialization.SR.GetString(System.Runtime.Serialization.SR.XmlInvalidConversion, value, Globals.TypeOfInt)));
            }

            if (value.IndexOfAny(JsonGlobals.floatingPointCharacters) == -1)
            {
                int intValue;
                if (Int32.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out intValue))
                {
                    objectTypeCode = TypeCode.Int32;
                    return intValue;
                }

                long longValue;
                if (Int64.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out longValue))
                {
                    objectTypeCode = TypeCode.Int64;
                    return longValue;
                }
            }

            decimal decimalValue;
            if (Decimal.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out decimalValue))
            {
                objectTypeCode = TypeCode.Decimal;

                //check for decimal underflow
                if (decimalValue == Decimal.Zero)
                {
                    double doubleValue = XmlConverter.ToDouble(value);
                    if (doubleValue != 0.0)
                    {
                        objectTypeCode = TypeCode.Double;
                        return doubleValue;
                    }
                }
                return decimalValue;
            }

            objectTypeCode = TypeCode.Double;
            return XmlConverter.ToDouble(value);
        }

        static object ParseJsonNumber(string value)
        {
            TypeCode unusedTypeCode;
            return ParseJsonNumber(value, out unusedTypeCode);
        }
    }
}
