//------------------------------------------------------------------------------
// <copyright file="XMLDiffLoader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
// <owner current="false" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Data {
    using System;
    using System.Runtime.Serialization.Formatters;
    using System.Configuration.Assemblies;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.IO;
    using System.Collections;
    using System.Globalization;
    using Microsoft.Win32;
    using System.ComponentModel;
    using System.Xml;
    using System.Xml.Serialization;
    
    internal sealed class XMLDiffLoader {
        ArrayList tables;
        DataSet dataSet = null;
        DataTable dataTable = null;

        internal void LoadDiffGram(DataSet ds, XmlReader dataTextReader) {
            XmlReader reader = DataTextReader.CreateReader(dataTextReader);
            dataSet = ds;
            while (reader.LocalName == Keywords.SQL_BEFORE && reader.NamespaceURI==Keywords.DFFNS)  {
                ProcessDiffs(ds, reader);
                reader.Read(); // now the reader points to the error section
            }

            while (reader.LocalName == Keywords.MSD_ERRORS && reader.NamespaceURI==Keywords.DFFNS) {
                ProcessErrors(ds, reader);
                Debug.Assert(reader.LocalName == Keywords.MSD_ERRORS && reader.NamespaceURI==Keywords.DFFNS, "something fishy");
                reader.Read(); // pass the end of errors tag
            }
        }


        private void CreateTablesHierarchy(DataTable dt) {
            foreach( DataRelation r in dt.ChildRelations ) {
                if (! tables.Contains((DataTable)r.ChildTable)) {
                    tables.Add((DataTable)r.ChildTable);
                    CreateTablesHierarchy(r.ChildTable)     ;
                }
            }
        }
            
        internal void LoadDiffGram(DataTable dt, XmlReader dataTextReader) {
            XmlReader reader = DataTextReader.CreateReader(dataTextReader);
            dataTable = dt;
            tables = new ArrayList();
            tables.Add(dt);
            CreateTablesHierarchy(dt);
                
            while (reader.LocalName == Keywords.SQL_BEFORE && reader.NamespaceURI==Keywords.DFFNS)  {
                ProcessDiffs(tables, reader);
                reader.Read(); // now the reader points to the error section
            }

            while (reader.LocalName == Keywords.MSD_ERRORS && reader.NamespaceURI==Keywords.DFFNS) {
                ProcessErrors(tables, reader);
                Debug.Assert(reader.LocalName == Keywords.MSD_ERRORS && reader.NamespaceURI==Keywords.DFFNS, "something fishy");
                reader.Read(); // pass the end of errors tag
            }

        }

        internal void ProcessDiffs(DataSet ds, XmlReader ssync) {
            DataTable tableBefore;
            DataRow row;
            int oldRowRecord;
            int pos = -1;

            int iSsyncDepth = ssync.Depth; 
            ssync.Read(); // pass the before node.

            SkipWhitespaces(ssync);

            while (iSsyncDepth < ssync.Depth) {
                tableBefore = null;
                string diffId = null;

                oldRowRecord = -1;

                // the diffgramm always contains sql:before and sql:after pairs

                int iTempDepth = ssync.Depth;

                diffId = ssync.GetAttribute(Keywords.DIFFID, Keywords.DFFNS);
                bool hasErrors = (bool) (ssync.GetAttribute(Keywords.HASERRORS, Keywords.DFFNS) == Keywords.TRUE);
                oldRowRecord = ReadOldRowData(ds, ref tableBefore, ref pos, ssync);
                if (oldRowRecord == -1)
                    continue;
 
                if (tableBefore == null) 
                    throw ExceptionBuilder.DiffgramMissingSQL();

                row = (DataRow)tableBefore.RowDiffId[diffId];
                if (row != null) {
                    row.oldRecord = oldRowRecord ;
                    tableBefore.recordManager[oldRowRecord] = row;
                } else {
                    row = tableBefore.NewEmptyRow();
                    tableBefore.recordManager[oldRowRecord] = row;
                    row.oldRecord = oldRowRecord;
                    row.newRecord = oldRowRecord;
                    tableBefore.Rows.DiffInsertAt(row, pos);
                    row.Delete();
                    if (hasErrors)
                        tableBefore.RowDiffId[diffId] = row;
                }
            }

            return; 
        }
        internal void ProcessDiffs(ArrayList tableList, XmlReader ssync) {
            DataTable tableBefore;
            DataRow row;
            int oldRowRecord;
            int pos = -1;

            int iSsyncDepth = ssync.Depth; 
            ssync.Read(); // pass the before node.

            //SkipWhitespaces(ssync); for given scenario does not require this change, but in fact we should do it.

            while (iSsyncDepth < ssync.Depth) {
                tableBefore = null;
                string diffId = null;

                oldRowRecord = -1;

                // the diffgramm always contains sql:before and sql:after pairs

                int iTempDepth = ssync.Depth;

                diffId = ssync.GetAttribute(Keywords.DIFFID, Keywords.DFFNS);
                bool hasErrors = (bool) (ssync.GetAttribute(Keywords.HASERRORS, Keywords.DFFNS) == Keywords.TRUE);
                oldRowRecord = ReadOldRowData(dataSet, ref tableBefore, ref pos, ssync);
                if (oldRowRecord == -1)
                    continue;
 
                if (tableBefore == null) 
                    throw ExceptionBuilder.DiffgramMissingSQL();

                row = (DataRow)tableBefore.RowDiffId[diffId];

                if (row != null) {
                    row.oldRecord = oldRowRecord ;
                    tableBefore.recordManager[oldRowRecord] = row;
                } else {
                    row = tableBefore.NewEmptyRow();
                    tableBefore.recordManager[oldRowRecord] = row;
                    row.oldRecord = oldRowRecord;
                    row.newRecord = oldRowRecord;
                    tableBefore.Rows.DiffInsertAt(row, pos);
                    row.Delete();
                    if (hasErrors)
                        tableBefore.RowDiffId[diffId] = row;
                }
            }

            return; 

        }

        
        internal void ProcessErrors(DataSet ds, XmlReader ssync) {
            DataTable table;

            int iSsyncDepth = ssync.Depth;
            ssync.Read(); // pass the before node.

            while (iSsyncDepth < ssync.Depth) {
                table = ds.Tables.GetTable(XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI);
                if (table == null) 
                    throw ExceptionBuilder.DiffgramMissingSQL();
                string diffId = ssync.GetAttribute(Keywords.DIFFID, Keywords.DFFNS);
                DataRow row = (DataRow)table.RowDiffId[diffId];
                string rowError = ssync.GetAttribute(Keywords.MSD_ERROR, Keywords.DFFNS);
                if (rowError != null)
                    row.RowError = rowError;
                int iRowDepth = ssync.Depth;
                ssync.Read(); // we may be inside a column
                while (iRowDepth < ssync.Depth) {
                    if (XmlNodeType.Element == ssync.NodeType) {
                        DataColumn col = table.Columns[XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI];
                        //if (col == null)
                        // throw exception here
                        string colError = ssync.GetAttribute(Keywords.MSD_ERROR, Keywords.DFFNS);
                        row.SetColumnError(col, colError);
                    }

                    ssync.Read();
                }
                while ((ssync.NodeType == XmlNodeType.EndElement) && (iSsyncDepth < ssync.Depth) )
                    ssync.Read();

            }

            return; 
        }

        internal void ProcessErrors(ArrayList dt, XmlReader ssync) {
            DataTable table;

            int iSsyncDepth = ssync.Depth;
            ssync.Read(); // pass the before node.

            while (iSsyncDepth < ssync.Depth) {
                table = GetTable(XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI);
                if (table == null) 
                    throw ExceptionBuilder.DiffgramMissingSQL();

                string diffId = ssync.GetAttribute(Keywords.DIFFID, Keywords.DFFNS);

                DataRow row = (DataRow)table.RowDiffId[diffId];
                if (row  == null) {
                    for(int i = 0; i < dt.Count; i++) {
                        row = (DataRow)((DataTable)dt[i]).RowDiffId[diffId];
                        if (row != null) {
                            table = row.Table;
                            break;
                        }                       
                    }
                }
                string rowError = ssync.GetAttribute(Keywords.MSD_ERROR, Keywords.DFFNS);
                if (rowError != null)
                    row.RowError = rowError;
                int iRowDepth = ssync.Depth;
                ssync.Read(); // we may be inside a column

                while (iRowDepth < ssync.Depth) {
                    if (XmlNodeType.Element == ssync.NodeType) {
                        DataColumn col = table.Columns[XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI];
                        //if (col == null)
                        // throw exception here
                        string colError = ssync.GetAttribute(Keywords.MSD_ERROR, Keywords.DFFNS);
                        row.SetColumnError(col, colError);
                    }
                    ssync.Read();
                }
                while ((ssync.NodeType == XmlNodeType.EndElement) && (iSsyncDepth < ssync.Depth) )
                    ssync.Read();

            }

            return; 
        }
        private DataTable GetTable(string tableName, string ns) {
            if (tables == null)
                return dataSet.Tables.GetTable(tableName, ns);

            if (tables.Count == 0)
                return (DataTable)tables[0];
            
            for(int i = 0; i < tables.Count; i++) {
                DataTable dt = (DataTable)tables[i];
                if ((string.Compare(dt.TableName, tableName, StringComparison.Ordinal) == 0)
                    && (string.Compare(dt.Namespace, ns, StringComparison.Ordinal) == 0))
                    return dt;
            }
            return null;
        }
        
        private int ReadOldRowData(DataSet ds, ref DataTable table, ref int pos, XmlReader row) {
            // read table information
            if (ds != null) {
                table = ds.Tables.GetTable(XmlConvert.DecodeName(row.LocalName), row.NamespaceURI);
            }
            else {
                table = GetTable(XmlConvert.DecodeName(row.LocalName), row.NamespaceURI);
            }

            if (table == null) {
                row.Skip(); // need to skip this element if we dont know about it, before returning -1
                return -1;
            }
            
            int iRowDepth = row.Depth;
            string value = null;

            if (table == null)
                throw ExceptionBuilder.DiffgramMissingTable(XmlConvert.DecodeName(row.LocalName));

            
            value = row.GetAttribute(Keywords.ROWORDER, Keywords.MSDNS);
            if (!Common.ADP.IsEmpty(value)) {
                pos = (Int32) Convert.ChangeType(value, typeof(Int32), null);
            }

            int record = table.NewRecord();
            foreach (DataColumn col in table.Columns) {
                col[record] = DBNull.Value;
            }

            foreach (DataColumn col in table.Columns) {
                if ((col.ColumnMapping == MappingType.Element) ||
                    (col.ColumnMapping == MappingType.SimpleContent))
                    continue;

                if (col.ColumnMapping == MappingType.Hidden) {
                    value = row.GetAttribute("hidden"+col.EncodedColumnName, Keywords.MSDNS);
                }
                else {
                    value = row.GetAttribute(col.EncodedColumnName, col.Namespace);
                }

                if (value == null) {
                    continue;
                }

                col[record] = col.ConvertXmlToObject(value);
            }

            row.Read();
            SkipWhitespaces(row);

            int currentDepth = row.Depth;
            if (currentDepth <= iRowDepth) {
                // the node is empty
                if (currentDepth == iRowDepth && row.NodeType == XmlNodeType.EndElement) {
                    // VSTFDEVDIV 764390: read past the EndElement of the current row
                    // note: (currentDepth == iRowDepth) check is needed so we do not skip elements on parent rows.
                    row.Read();
                    SkipWhitespaces(row);
                }
                return record;
            }

            if (table.XmlText != null) {
                DataColumn col = table.XmlText;
                col[record] = col.ConvertXmlToObject(row.ReadString());
            }
            else {
                while (row.Depth > iRowDepth)  {
                    String ln =XmlConvert.DecodeName( row.LocalName) ;
                    String ns = row.NamespaceURI;
                    DataColumn column = table.Columns[ln, ns];

                    if (column == null) {
                        while((row.NodeType != XmlNodeType.EndElement) && (row.LocalName!=ln) && (row.NamespaceURI!=ns))
                            row.Read(); // consume the current node
                        row.Read(); // now points to the next column
                        //SkipWhitespaces(row); seems no need, just in case if we see other issue , this will be here as hint
                        continue;// add a read here!
                    }

                    if (column.IsCustomType) {
                        // if column's type is object or column type does not implement IXmlSerializable
                        bool isPolymorphism = (column.DataType == typeof(Object)|| (row.GetAttribute(Keywords.MSD_INSTANCETYPE, Keywords.MSDNS) != null) ||
                        (row.GetAttribute(Keywords.TYPE, Keywords.XSINS) != null)) ;

                        bool skipped = false;
                        if (column.Table.DataSet != null && column.Table.DataSet.UdtIsWrapped) {
                            row.Read(); // if UDT is wrapped, skip the wrapper
                            skipped = true;
                        }

                        XmlRootAttribute xmlAttrib = null;

                        if (!isPolymorphism && !column.ImplementsIXMLSerializable) { // THIS 
                            // if does not implement IXLSerializable, need to go with XmlSerializer: pass XmlRootAttribute
                            if (skipped) {
                                xmlAttrib = new XmlRootAttribute(row.LocalName);
                                xmlAttrib.Namespace = row.NamespaceURI ;
                            }
                            else {
                                xmlAttrib = new XmlRootAttribute(column.EncodedColumnName);
                                xmlAttrib.Namespace = column.Namespace;
                            }
                        }
                        // for else case xmlAttrib MUST be null
                        column[record] = column.ConvertXmlToObject(row, xmlAttrib); // you need to pass null XmlAttib here

                        
                        if (skipped) {
                            row.Read(); // if Wrapper is skipped, skip its end tag
                        }
                    }
                    else {
                        int iColumnDepth = row.Depth;
                        row.Read();
                        
                        // SkipWhitespaces(row);seems no need, just in case if we see other issue , this will be here as hint
                        if (row.Depth > iColumnDepth) { //we are inside the column
                            if (row.NodeType == XmlNodeType.Text || row.NodeType == XmlNodeType.Whitespace || row.NodeType == XmlNodeType.SignificantWhitespace) {
                                String text = row.ReadString();
                                column[record] = column.ConvertXmlToObject(text);

                                row.Read(); // now points to the next column
                            }
                        }
                        else {
                            // <element></element> case
                            if (column.DataType == typeof(string))
                                column[record] = string.Empty;
                        }
                    }
                }
            }
            row.Read(); //now it should point to next row
            SkipWhitespaces(row);
            return record;
        }

        internal void SkipWhitespaces(XmlReader reader) {
            while (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace) {
                reader.Read();
            }    
        }
    }
}
