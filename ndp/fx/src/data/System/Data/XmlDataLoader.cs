//------------------------------------------------------------------------------
// <copyright file="XmlDataLoader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Data {
    using System;
    using System.Collections;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    internal sealed class XmlDataLoader {
        DataSet dataSet;
        XmlToDatasetMap nodeToSchemaMap = null;
        Hashtable       nodeToRowMap;
        Stack           childRowsStack = null;
        Hashtable       htableExcludedNS = null;
        bool    fIsXdr = false;
        internal bool    isDiffgram = false;

        DataRow topMostRow = null;

        XmlElement topMostNode = null;
        bool ignoreSchema = false;

        DataTable dataTable;
        bool isTableLevel = false;
        private bool fromInference = false;

        internal XmlDataLoader( DataSet dataset, bool IsXdr, bool ignoreSchema) {
            // Initialization
            this.dataSet = dataset;
            this.nodeToRowMap = new Hashtable();
            this.fIsXdr = IsXdr;
            this.ignoreSchema = ignoreSchema;
        }

        internal XmlDataLoader( DataSet dataset, bool IsXdr, XmlElement topNode, bool ignoreSchema) {
            // Initialization
            this.dataSet = dataset;
            this.nodeToRowMap = new Hashtable();
            this.fIsXdr = IsXdr;

            // Allocate the stack and create the mappings            
            childRowsStack = new Stack(50);

            topMostNode = topNode;
            this.ignoreSchema = ignoreSchema;
        }

        internal XmlDataLoader( DataTable datatable, bool IsXdr, bool ignoreSchema) {
            // Initialization
            this.dataSet = null;
            dataTable = datatable;
            isTableLevel = true;
            this.nodeToRowMap = new Hashtable();
            this.fIsXdr = IsXdr;
            this.ignoreSchema = ignoreSchema;
        }

        internal XmlDataLoader( DataTable datatable, bool IsXdr, XmlElement topNode, bool ignoreSchema) {
            // Initialization
            this.dataSet = null;
            dataTable = datatable;
            isTableLevel = true;
            this.nodeToRowMap = new Hashtable();
            this.fIsXdr = IsXdr;

            // Allocate the stack and create the mappings

            childRowsStack = new Stack(50);
            topMostNode = topNode;
            this.ignoreSchema = ignoreSchema;
        }

        internal bool FromInference {
            get {
                return fromInference;
            }
            set {
                fromInference = value;
            }
        }        

        // after loading, all detached DataRows are attached to their tables
        private void AttachRows( DataRow parentRow, XmlNode parentElement ) {
            if (parentElement == null)
                return;

            for (XmlNode n = parentElement.FirstChild; n != null; n = n.NextSibling) {
                if (n.NodeType == XmlNodeType.Element) {
                    XmlElement e = (XmlElement) n;
                    DataRow r = GetRowFromElement( e );
                    if (r != null && r.RowState == DataRowState.Detached) {
                        if (parentRow != null)
                            r.SetNestedParentRow( parentRow, /*setNonNested*/ false );

                        r.Table.Rows.Add( r );
                    } 
                    else if (r == null) {
                        // n is a 'sugar element'
                        AttachRows( parentRow, n );
                    }

                    // attach all detached rows
                    AttachRows( r, n );
                }
            }
        }

        private int CountNonNSAttributes (XmlNode node) {
            int count = 0;
            for (int i = 0; i < node.Attributes.Count; i++) {
                XmlAttribute attr = node.Attributes[i];
                if (!FExcludedNamespace(node.Attributes[i].NamespaceURI))
                        count++;
            }            
            return count;
        }

        private string GetValueForTextOnlyColums( XmlNode n ) {
            string value = null;

            // don't consider whitespace
            while (n != null && (n.NodeType == XmlNodeType.Whitespace || !IsTextLikeNode(n.NodeType))) {
                n = n.NextSibling;
            }
            
            if (n != null) {
                if (IsTextLikeNode( n.NodeType ) && (n.NextSibling == null || !IsTextLikeNode( n.NodeType ))) {
                    // don't use string builder if only one text node exists
                    value = n.Value;
                    n = n.NextSibling;
                }
                else {
                    StringBuilder sb = new StringBuilder();
                    while (n != null && IsTextLikeNode( n.NodeType )) {
                        sb.Append( n.Value );
                        n = n.NextSibling;
                    }
                    value = sb.ToString();
                }
            }

            if (value == null)
                value = String.Empty;

            return value;
        }

        private string GetInitialTextFromNodes( ref XmlNode n ) {
            string value = null;

            if (n != null) {
                // don't consider whitespace
                while (n.NodeType == XmlNodeType.Whitespace)
                    n = n.NextSibling;

                if (IsTextLikeNode( n.NodeType ) && (n.NextSibling == null || !IsTextLikeNode( n.NodeType ))) {
                    // don't use string builder if only one text node exists
                    value = n.Value;
                    n = n.NextSibling;
                }
                else {
                    StringBuilder sb = new StringBuilder();
                    while (n != null && IsTextLikeNode( n.NodeType )) {
                        sb.Append( n.Value );
                        n = n.NextSibling;
                    }
                    value = sb.ToString();
                }
            }

            if (value == null)
                value = String.Empty;

            return value;
        }

        private DataColumn GetTextOnlyColumn( DataRow row ) {
            DataColumnCollection columns = row.Table.Columns;
            int cCols = columns.Count;
            for (int iCol = 0; iCol < cCols; iCol++) {
                DataColumn c = columns[iCol];
                if (IsTextOnly( c ))
                    return c;
            }
            return null;
        }

        internal DataRow GetRowFromElement( XmlElement e ) {
            return(DataRow) nodeToRowMap[e];
        }        

        internal bool FColumnElement(XmlElement e) {
            if (nodeToSchemaMap.GetColumnSchema(e, FIgnoreNamespace(e)) == null)
                return false;

            if (CountNonNSAttributes(e) > 0)
                return false;

            for (XmlNode tabNode = e.FirstChild; tabNode != null; tabNode = tabNode.NextSibling)
                if (tabNode is XmlElement)
                    return false;

            return true;                          
        }
        
        private bool FExcludedNamespace(string ns) {
            if (ns.Equals(Keywords.XSD_XMLNS_NS))
                return true;
                
            if (htableExcludedNS == null)
                return false;
                
            return htableExcludedNS.Contains(ns);
        }

        private bool FIgnoreNamespace(XmlNode node) {
            XmlNode ownerNode;
            if (!fIsXdr)
                return false;
            if (node is XmlAttribute)
                ownerNode = ((XmlAttribute)node).OwnerElement;
            else
                ownerNode = node;
            if (ownerNode.NamespaceURI.StartsWith("x-schema:#", StringComparison.Ordinal))
                return true;
            else
                return false;
        }
        
        private bool FIgnoreNamespace(XmlReader node) {
            if (fIsXdr && node.NamespaceURI.StartsWith("x-schema:#", StringComparison.Ordinal))
                return true;
            else
                return false;
        }

        internal bool IsTextLikeNode( XmlNodeType n ) {
            switch (n) {
                case XmlNodeType.EntityReference:
                    throw ExceptionBuilder.FoundEntity();
                
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.CDATA:
                    return true;

                default:
                    return false;
            }
        }

        internal bool IsTextOnly( DataColumn c ) {
            if (c.ColumnMapping != MappingType.SimpleContent)
                return false;
            else 
                return true;
        }

        internal void LoadData( XmlDocument xdoc ) {
            if (xdoc.DocumentElement == null)
                return;

            bool saveEnforce;

            if (isTableLevel) {
                saveEnforce = dataTable.EnforceConstraints;
                dataTable.EnforceConstraints = false;
            }
            else {
                saveEnforce = dataSet.EnforceConstraints;
                dataSet.EnforceConstraints = false;
                dataSet.fInReadXml = true;
            }

            if (isTableLevel) {
                nodeToSchemaMap = new XmlToDatasetMap(dataTable, xdoc.NameTable);
            }
            else {
                nodeToSchemaMap = new XmlToDatasetMap(dataSet, xdoc.NameTable);
            }
/*
            // Top level table or dataset ?
            XmlElement rootElement = xdoc.DocumentElement;
            Hashtable tableAtoms = new Hashtable();
            XmlNode tabNode;
            if (CountNonNSAttributes (rootElement) > 0)
                dataSet.fTopLevelTable = true;
            else {
                for (tabNode = rootElement.FirstChild; tabNode != null; tabNode = tabNode.NextSibling) {
                    if (tabNode is XmlElement && tabNode.LocalName != Keywords.XSD_SCHEMA) {
                        object value = tableAtoms[QualifiedName (tabNode.LocalName, tabNode.NamespaceURI)];
                        if (value == null || (bool)value == false) {
                            dataSet.fTopLevelTable = true;
                            break;
                        }
                    }
                }
            }
*/
            DataRow topRow = null;
            if (isTableLevel ||(dataSet!= null && dataSet.fTopLevelTable) ){
                XmlElement e = xdoc.DocumentElement;
                DataTable topTable = (DataTable) nodeToSchemaMap.GetSchemaForNode(e, FIgnoreNamespace(e));
                if (topTable != null) {
                    topRow = topTable.CreateEmptyRow(); //Microsoft perf
                    nodeToRowMap[ e ] = topRow;

                    // get all field values.
                    LoadRowData( topRow, e );
                    topTable.Rows.Add(topRow);                              
                }
            }

            LoadRows( topRow, xdoc.DocumentElement );
            AttachRows( topRow, xdoc.DocumentElement );


            if (isTableLevel) {
                dataTable.EnforceConstraints = saveEnforce;
            }
            else {
                dataSet.fInReadXml = false;
                dataSet.EnforceConstraints = saveEnforce;
            }

        }

        private void LoadRowData(DataRow row, XmlElement rowElement) {
            XmlNode n;
            DataTable table = row.Table;
            if (FromInference)
                table.Prefix = rowElement.Prefix;

            // keep a list of all columns that get updated
            Hashtable foundColumns = new Hashtable();

            row.BeginEdit();

            // examine all children first
            n = rowElement.FirstChild;

            // Look for data to fill the TextOnly column
            DataColumn column = GetTextOnlyColumn( row );
            if (column != null) {
                foundColumns[column] = column;
                string text = GetValueForTextOnlyColums( n ) ;
                if (XMLSchema.GetBooleanAttribute(rowElement, Keywords.XSI_NIL, Keywords.XSINS, false) && Common.ADP.IsEmpty(text) )
                    row[column] = DBNull.Value;
                else
                    SetRowValueFromXmlText( row, column, text );
            }

            // Walk the region to find elements that map to columns
            while (n != null && n != rowElement) {
                if (n.NodeType == XmlNodeType.Element) {
                    XmlElement e = (XmlElement) n;

                    object schema = nodeToSchemaMap.GetSchemaForNode( e, FIgnoreNamespace(e) );
                    if (schema is DataTable) {
                        if (FColumnElement(e))
                            schema = nodeToSchemaMap.GetColumnSchema( e, FIgnoreNamespace(e) );
                    }
                    
                    // if element has its own table mapping, it is a separate region
                    if (schema == null || schema is DataColumn) {
                        // descend to examine child elements
                        n = e.FirstChild;

                        if (schema != null && schema is DataColumn) {
                            DataColumn c = (DataColumn) schema;

                            if (c.Table == row.Table && c.ColumnMapping != MappingType.Attribute && foundColumns[c] == null) {
                                foundColumns[c] = c;
                                string text = GetValueForTextOnlyColums( n ) ;
                                if (XMLSchema.GetBooleanAttribute(e, Keywords.XSI_NIL, Keywords.XSINS, false) && Common.ADP.IsEmpty(text) )
                                    row[c] = DBNull.Value;
                                else
                                    SetRowValueFromXmlText( row, c, text );
                            }
                        } 
                        else if ((schema == null) && (n!=null)) {
                            continue;
                        }


                        // nothing left down here, continue from element
                        if (n == null)
                            n = e;
                    }
                }

                // if no more siblings, ascend back toward original element (rowElement)
                while (n != rowElement && n.NextSibling == null) {
                    n = n.ParentNode;
                }

                if (n != rowElement)
                    n = n.NextSibling;
            }

            //
            // Walk the attributes to find attributes that map to columns.
            //
            foreach( XmlAttribute attr in rowElement.Attributes ) {
                object schema = nodeToSchemaMap.GetColumnSchema( attr, FIgnoreNamespace(attr) );
                if (schema != null && schema is DataColumn) {
                    DataColumn c = (DataColumn) schema;

                    if (c.ColumnMapping == MappingType.Attribute && foundColumns[c] == null) {
                        foundColumns[c] = c;
                        n = attr.FirstChild;
                        SetRowValueFromXmlText( row, c, GetInitialTextFromNodes( ref n ) );
                    }
                }
            }

            // Null all columns values that aren't represented in the tree
            foreach( DataColumn c in row.Table.Columns ) {
                if (foundColumns[c] == null && XmlToDatasetMap.IsMappedColumn(c)) {
                    if (!c.AutoIncrement) {
                        if (c.AllowDBNull)  {
                            row[c] = DBNull.Value;
                        }
                        else {
                            row[c] = c.DefaultValue;
                        }
                    }
                    else {
                        c.Init(row.tempRecord);
                    }
                }
            }       

            row.EndEdit();
        }
        

        // load all data from tree structre into datarows
        private void LoadRows( DataRow parentRow, XmlNode parentElement ) {
            if (parentElement == null)
                return;

            // Skip schema node as well
            if (parentElement.LocalName == Keywords.XSD_SCHEMA && parentElement.NamespaceURI == Keywords.XSDNS ||
                parentElement.LocalName == Keywords.SQL_SYNC   && parentElement.NamespaceURI == Keywords.UPDGNS ||                
                parentElement.LocalName == Keywords.XDR_SCHEMA && parentElement.NamespaceURI == Keywords.XDRNS)
                return;

            for (XmlNode n = parentElement.FirstChild; n != null; n = n.NextSibling) {
                if (n is XmlElement) {
                    XmlElement e = (XmlElement) n;
                    object schema = nodeToSchemaMap.GetSchemaForNode( e, FIgnoreNamespace(e) );

                    if (schema != null && schema is DataTable) {
                        DataRow r = GetRowFromElement( e );
                        if (r == null) {
                            // skip columns which has the same name as another table
                            if (parentRow != null && FColumnElement(e))
                                continue;
                                
                            r = ((DataTable)schema).CreateEmptyRow();
                            nodeToRowMap[ e ] = r;

                            // get all field values.
                            LoadRowData( r, e );
                        }

                        // recurse down to inner elements
                        LoadRows( r, n );
                    }
                    else {
                        // recurse down to inner elements
                        LoadRows( null, n );
                    }
                }
            }
        }

        private void SetRowValueFromXmlText( DataRow row, DataColumn col, string xmlText ) {
            row[col] = col.ConvertXmlToObject(xmlText);
        }

        internal void LoadTopMostRow(ref bool[] foundColumns) {
            // Attempt to load row from top node we backed up in DataSet.ReadXml()
            // In most cases it contains the DataSet name and no information

            // Check if DataSet object matches the top node (it won't in most cases)

            Object obj = nodeToSchemaMap.GetSchemaForNode(topMostNode,FIgnoreNamespace(topMostNode));
            
            if (obj is DataTable) {                         // It's a table? Load it.
                DataTable table = (DataTable) obj;

                topMostRow = table.CreateEmptyRow();
                
                foundColumns = new bool[topMostRow.Table.Columns.Count];

                //
                // Walk the attributes to find attributes that map to columns.
                //
                foreach( XmlAttribute attr in topMostNode.Attributes ) {
                    object schema = nodeToSchemaMap.GetColumnSchema( attr, FIgnoreNamespace(attr) );

                    if (schema != null && schema is DataColumn) {
                        DataColumn c = (DataColumn) schema;

                        if (c.ColumnMapping == MappingType.Attribute) {
                            XmlNode n = attr.FirstChild;
                            SetRowValueFromXmlText( topMostRow, c, GetInitialTextFromNodes( ref n ) );
                            foundColumns[c.Ordinal] = true;
                        }
                    }
                }

            }
            topMostNode = null;
        }

        private XmlReader dataReader = null;
        private object XSD_XMLNS_NS;
        private object XDR_SCHEMA;
        private object XDRNS;
        private object SQL_SYNC;
        private object UPDGNS;
        private object XSD_SCHEMA;
        private object XSDNS;

        private object DFFNS;
        private object MSDNS;
        private object DIFFID;
        private object HASCHANGES;
        private object ROWORDER;

        private void InitNameTable() {
            XmlNameTable nameTable = dataReader.NameTable;

            XSD_XMLNS_NS = nameTable.Add(Keywords.XSD_XMLNS_NS);
            XDR_SCHEMA = nameTable.Add(Keywords.XDR_SCHEMA);
            XDRNS = nameTable.Add(Keywords.XDRNS);
            SQL_SYNC = nameTable.Add(Keywords.SQL_SYNC);
            UPDGNS = nameTable.Add(Keywords.UPDGNS);
            XSD_SCHEMA = nameTable.Add(Keywords.XSD_SCHEMA);
            XSDNS = nameTable.Add(Keywords.XSDNS);

            DFFNS = nameTable.Add(Keywords.DFFNS);
            MSDNS = nameTable.Add(Keywords.MSDNS);
            DIFFID = nameTable.Add(Keywords.DIFFID);
            HASCHANGES = nameTable.Add(Keywords.HASCHANGES);
            ROWORDER = nameTable.Add(Keywords.ROWORDER);
        }

        internal void LoadData(XmlReader reader) {
            dataReader = DataTextReader.CreateReader(reader);

            int entryDepth = dataReader.Depth;                  // Store current XML element depth so we'll read
                                                                // correct portion of the XML and no more
            bool fEnforce = isTableLevel ? dataTable.EnforceConstraints : dataSet.EnforceConstraints;         
                                                                // Keep constraints status for datataset/table
            InitNameTable();                                    // Adds DataSet namespaces to reader's nametable

            if (nodeToSchemaMap == null) {                      // Create XML to dataset map
                nodeToSchemaMap = isTableLevel ? new XmlToDatasetMap(dataReader.NameTable, dataTable) :
                                                 new XmlToDatasetMap(dataReader.NameTable, dataSet);
            }

            if (isTableLevel) {
                dataTable.EnforceConstraints = false;           // Disable constraints
            }
            else {
                dataSet.EnforceConstraints = false;             // Disable constraints
                dataSet.fInReadXml = true;                      // We're in ReadXml now
            }

            if (topMostNode != null) {                          // Do we have top node?

                if (!isDiffgram && !isTableLevel) {             // Not a diffgram  and not DataSet?
                    DataTable table = nodeToSchemaMap.GetSchemaForNode(topMostNode, FIgnoreNamespace(topMostNode)) as DataTable;
                                                                // Try to match table in the dataset to this node
                    if (table != null) {                        // Got the table ?
                        LoadTopMostTable(table);                // Load top most node
                    }
                }

                topMostNode = null;                             // topMostNode is no more. Good riddance.
            }

            while( !dataReader.EOF ) {                          // Main XML parsing loop. Check for EOF just in case.
                if (dataReader.Depth < entryDepth)              // Stop if we have consumed all elements allowed
                    break;

                if ( reader.NodeType != XmlNodeType.Element ) { // Read till Element is found
                    dataReader.Read();
                    continue;
                }                
                DataTable table = nodeToSchemaMap.GetTableForNode(dataReader, FIgnoreNamespace(dataReader));
                                                                // Try to get table for node
                if (table == null) {                            // Read till table is found

                    if (!ProcessXsdSchema())                    // Check for schemas...
                        dataReader.Read();                      // Not found? Read next element.

                    continue;
                }                

                LoadTable(table,  false /* isNested */);        // Here goes -- load data for this table
                                                                // This is a root table, so it's not nested
            }

            if (isTableLevel) {
                dataTable.EnforceConstraints = fEnforce;        // Restore constraints and return
            } 
            else {
                dataSet.fInReadXml = false;                     // We're done.
                dataSet.EnforceConstraints = fEnforce;          // Restore constraints and return
            }
        }

        // Loads a top most table. 
        // This is neded because desktop is capable of loading almost anything into the dataset.
        // The top node could be a DataSet element or a Table element. To make things worse,
        // you could have a table with the same name as dataset.
        // Here's how we're going to dig into this mess:
        //
        //                                 TopNode is null ?
        //                                / No           \ Yes
        //                  Table matches TopNode ?       Current node is the table start
        //                 / No                  \ Yes (LoadTopMostTable called in this case only)
        //   Current node is the table start    DataSet name matches one of the tables ?
        //      TopNode is dataset node        / Yes                                  \ No
        //                                    /                                        TopNode is the table
        //      Current node matches column or nested table in the table ?             and current node
        //     / No                                                 \ Yes              is a column or a
        //     TopNode is DataSet                            TopNode is table          nested table
        // 
        // Yes, it is terrible and I don't like it also..
        
        private void LoadTopMostTable(DataTable table) {
           
            //        /------------------------------- This one is in topMostNode (backed up to XML DOM)
            //  <Table> /----------------------------- We are here on entrance
            //      <Column>Value</Column>
            //      <AnotherColumn>Value</AnotherColumn>
            //  </Table> ...
            //            \------------------------------ We are here on exit           


            Debug.Assert (table != null, "Table to be loaded is null on LoadTopMostTable() entry");
            Debug.Assert (topMostNode != null, "topMostNode is null on LoadTopMostTable() entry");
            Debug.Assert (!isDiffgram, "Diffgram mode is on while we have topMostNode table. This is bad." );

            bool topNodeIsTable = isTableLevel || (dataSet.DataSetName != table.TableName);
                                                                // If table name we have matches dataset
                                                                // name top node could be a DataSet OR a table.
                                                                // It's a table overwise.
            DataRow row = null;                                 // Data row we're going to add to this table

            bool matchFound = false;                            // Assume we found no matching elements

            int entryDepth = dataReader.Depth - 1;              // Store current reader depth so we know when to stop reading
                                                                // Adjust depth by one as we've read top most element 
                                                                // outside this method. 
            string textNodeValue;                               // Value of a text node we might have

            Debug.Assert (entryDepth >= 0, "Wrong entry Depth for top most element." );

            int entryChild = childRowsStack.Count;              // Memorize child stack level on entry
            
            DataColumn c;                                       // Hold column here
            DataColumnCollection collection = table.Columns;    // Hold column collectio here

            object[] foundColumns = new object[collection.Count];
                                                                // This is the columns data we might find
            XmlNode n;                                          // Need this to pass by reference

            foreach( XmlAttribute attr in topMostNode.Attributes ) {
                                                                // Check all attributes in this node

                c = nodeToSchemaMap.GetColumnSchema( attr, FIgnoreNamespace(attr)) as DataColumn;
                                                                // Try to mach attribute to column
                if ((c != null) && (c.ColumnMapping == MappingType.Attribute)) {
                                                                // If it's a column with attribute mapping
                    n = attr.FirstChild;

                    foundColumns[c.Ordinal] = c.ConvertXmlToObject(GetInitialTextFromNodes(ref n));
                                                                // Get value
                    matchFound = true;                          // and note we found a matching element
                }
            }

            // Now handle elements. This could be columns or nested tables
            // We'll skip the rest as we have no idea what to do with it.

            // Note: we do not need to read first as we're already as it has been done by caller.
            
            while (entryDepth < dataReader.Depth ) {
                switch (dataReader.NodeType) {                  // Process nodes based on type
                case XmlNodeType.Element:                       // It's an element
                    object o = nodeToSchemaMap.GetColumnSchema(table, dataReader, FIgnoreNamespace(dataReader));
                                                                // Get dataset element for this XML element
                    c = o as DataColumn;                        // Perhaps, it's a column?

                    if ( c != null ) {                          // Do we have matched column in this table?

                        // Let's load column data

                        if (foundColumns[c.Ordinal] == null) {
                                                                // If this column was not found before
                            LoadColumn (c, foundColumns);       // Get column value.
                            matchFound = true;                  // Got matched row.
                        } 
                        else {
                            dataReader.Read();                  // Advance to next element. 
                        } 
                    } 
                    else  {
                        DataTable nestedTable = o as DataTable; 
                                                                // Perhaps, it's a nested table ?
                        if ( nestedTable != null ) {            // Do we have matched table in DataSet ?
                            LoadTable (nestedTable, true /* isNested */);            
                                                                // Yes. Load nested table (recursive)
                            matchFound = true;                  // Got matched nested table 
                        } 
                        else if (ProcessXsdSchema()) {          // Check for schema. Skip or load if found.
                            continue;                           // Schema has been found. Process the next element 
                                                                // we're already at (done by schema processing).
                        }
                        else {                                  // Not a table or column in this table ?
                            if (!(matchFound || topNodeIsTable)) {
                                                                // Could top node be a DataSet?


                                return;                         // Assume top node is DataSet 
                                                                // and stop top node processing
                            }
                            dataReader.Read();                  // Continue to the next element.
                        }
                    }
                    break;
                // Oops. Not supported
                case XmlNodeType.EntityReference:               // Oops. No support for Entity Reference
                    throw ExceptionBuilder.FoundEntity();
                case XmlNodeType.Text:                          // It looks like a text.
                case XmlNodeType.Whitespace:                    // This actually could be
                case XmlNodeType.CDATA:                         // if we have XmlText in our table
                case XmlNodeType.SignificantWhitespace:
                    textNodeValue = dataReader.ReadString();    
                                                                // Get text node value.
                    c = table.xmlText;                          // Get XML Text column from our table

                    if (c != null && foundColumns[c.Ordinal] == null) {
                                                                // If XmlText Column is set
                                                                // and we do not have data already                
                        foundColumns[c.Ordinal] = c.ConvertXmlToObject(textNodeValue);
                                                                // Read and store the data
                    }

                    break;
                default:
                    dataReader.Read();                  // We don't process that, skip to the next element.
                    break;
                }
            }

            dataReader.Read();                          // Proceed to the next element.

            // It's the time to populate row with loaded data and add it to the table we'we just read to the table

            for ( int i = foundColumns.Length -1; i >= 0; --i) {
                                                            // Check all columns
                if (null == foundColumns[i]) {              // Got data for this column ?
                    c = collection[i];                      // No. Get column for this index

                    if (c.AllowDBNull && c.ColumnMapping != MappingType.Hidden && !c.AutoIncrement) {
                        foundColumns[i] = DBNull.Value;     // Assign DBNull if possible
                                                            // table.Rows.Add() below will deal
                                                            // with default values and autoincrement
                    }
                }
            }

            row = table.Rows.AddWithColumnEvents(foundColumns);             // Create, populate and add row

            while (entryChild < childRowsStack.Count) {     // Process child rows we might have
                DataRow childRow = (DataRow) childRowsStack.Pop();  
                                                            // Get row from the stack
                bool unchanged = (childRow.RowState == DataRowState.Unchanged);
                                                            // Is data the same as before?
                childRow.SetNestedParentRow(row, /*setNonNested*/ false);
                                                            // Set parent row
                if (unchanged)                              // Restore record if child row's unchanged
                    childRow.oldRecord = childRow.newRecord; 
            }
        }

        // Loads a table. 
        // Yes, I know it's a big method. This is done to avoid performance penalty of calling methods 
        // with many arguments and to keep recursion within one method only. To make code readable,
        // this method divided into 3 parts: attribute processing (including diffgram), 
        // nested elements processing and loading data. Please keep it this way.

        private void LoadTable(DataTable table, bool isNested ) {

            //  <DataSet> /--------------------------- We are here on entrance
            //      <Table>               
            //          <Column>Value</Column>
            //          <AnotherColumn>Value</AnotherColumn>
            //      </Table>    /-------------------------- We are here on exit           
            //      <AnotherTable>
            //      ...
            //      </AnotherTable>
            //      ...
            //  </DataSet> 

            Debug.Assert (table != null, "Table to be loaded is null on LoadTable() entry");

            DataRow row = null;                                 // Data row we're going to add to this table

            int entryDepth = dataReader.Depth;                  // Store current reader depth so we know when to stop reading
            int entryChild = childRowsStack.Count;              // Memorize child stack level on entry

            DataColumn c;                                       // Hold column here
            DataColumnCollection collection = table.Columns;    // Hold column collectio here

            object[] foundColumns = new object[collection.Count];
                                                                // This is the columns data we found 
            // This is used to process diffgramms

            int rowOrder = -1;                                  // Row to insert data to
            string diffId = String.Empty;                       // Diffgram ID string
            string hasChanges = null;                           // Changes string
            bool hasErrors = false;                             // Set this in case of problem

            string textNodeValue;                               // Value of a text node we might have

            // Process attributes first                         

            for ( int i = dataReader.AttributeCount -1; i >= 0; --i) {    
                                                                // Check all attributes one by one
                dataReader.MoveToAttribute(i);                  // Get this attribute

                c = nodeToSchemaMap.GetColumnSchema(table, dataReader, FIgnoreNamespace(dataReader)) as DataColumn;
                                                                // Try to get column for this attribute

                if ((c != null) && (c.ColumnMapping == MappingType.Attribute)) {
                    // Yep, it is a column mapped as attribute
                    // Get value from XML and store it in the object array
                    foundColumns[c.Ordinal] = c.ConvertXmlToObject(dataReader.Value);
                }                                               // Oops. No column for this element
                
                // 



                // else if (table.XmlText != null &&               
                //          dataReader.NamespaceURI == Keywords.XSINS && 
                //          dataReader.LocalName == Keywords.XSI_NIL ) {
                //                                                 // Got XMLText column and it's a NIL attribute?
                //     if (XmlConvert.ToBoolean(dataReader.Value)) {
                //         // If NIL attribute set to true...
                //         // Assign DBNull to XmlText column
                //         foundColumns[table.XmlText.Ordinal] = DBNull.Value;
                //     }
                // }

                if ( isDiffgram ) {                             // Now handle some diffgram attributes 
                    if ( dataReader.NamespaceURI == Keywords.DFFNS ) {
                        switch (dataReader.LocalName) {
                        case Keywords.DIFFID:                   // Is it a diffgeam ID ?
                            diffId = dataReader.Value;          // Store ID
                            break;
                        case Keywords.HASCHANGES:               // Has chages attribute ?
                            hasChanges = dataReader.Value;      // Store value
                            break;
                        case Keywords.HASERRORS:                // Has errors attribute ?
                            hasErrors = (bool)Convert.ChangeType(dataReader.Value, typeof(bool), CultureInfo.InvariantCulture);
                                                                // Store value
                            break;
                        }
                    } 
                    else if ( dataReader.NamespaceURI == Keywords.MSDNS ) {
                        if ( dataReader.LocalName == Keywords.ROWORDER ) {
                                                                // Is it a row order attribute ?
                            rowOrder = (Int32)Convert.ChangeType(dataReader.Value, typeof(Int32), CultureInfo.InvariantCulture);
                                                                // Store it
                        } else if (dataReader.LocalName.StartsWith("hidden", StringComparison.Ordinal)) {
                                                                // Hidden column ?
                            c = collection[XmlConvert.DecodeName(dataReader.LocalName.Substring(6))];
                                                                // Let's see if we have one. 
                                                                // We have to decode name before we look it up
                                                                // We could not use XmlToDataSet map as it contains
                                                                // no hidden columns
                            if (( c != null)  && (c.ColumnMapping == MappingType.Hidden)) {
                                                                // Got column and it is hidden ?
                                foundColumns[c.Ordinal] = c.ConvertXmlToObject(dataReader.Value);
                            }
                        }
                    }
                }
            }                                                   // Done with attributes

            // Now handle elements. This could be columns or nested tables.

            //  <DataSet> /------------------- We are here after dealing with attributes
            //      <Table foo="FooValue" bar="BarValue">  
            //          <Column>Value</Column>
            //          <AnotherColumn>Value</AnotherColumn>
            //      </Table>
            //  </DataSet>

            if ( dataReader.Read() && entryDepth < dataReader.Depth) {
                                                                // Read to the next element and see if we're inside
                while ( entryDepth < dataReader.Depth ) {       // Get out as soon as we've processed all nested nodes.
                    switch (dataReader.NodeType) {              // Process nodes based on type
                    case XmlNodeType.Element:                   // It's an element
                        object o = nodeToSchemaMap.GetColumnSchema(table, dataReader, FIgnoreNamespace(dataReader));
                                                                // Get dataset element for this XML element
                        c = o as DataColumn;                    // Perhaps, it's a column?

                        if ( c != null ) {                      // Do we have matched column in this table?
                                                                // Let's load column data
                            if (foundColumns[c.Ordinal] == null) {
                                                                // If this column was not found before
                                LoadColumn (c, foundColumns);            
                                                                // Get column value
                            }
                            else {
                                dataReader.Read();              // Advance to next element. 
                            } 
                        } 
                        else  {
                            DataTable nestedTable = o as DataTable; 
                                                                // Perhaps, it's a nested table ?
                            if ( nestedTable != null ) {        // Do we have matched nested table in DataSet ?
                                LoadTable (nestedTable, true /* isNested */);            
                                                                // Yes. Load nested table (recursive)
                            }                                   // Not a table nor column? Check if it's schema.
                            else if (ProcessXsdSchema()) {      // Check for schema. Skip or load if found.
                                continue;                       // Schema has been found. Process the next element 
                                                                // we're already at (done by schema processing).
                            }
                            else {                              
                                // We've got element which is not supposed to he here according to the schema.
                                // That might be a table which was misplaced. We should've thrown on that, 
                                // but we'll try to load it so we could keep compatibility.
                                // We won't try to match to columns as we have no idea 
                                // which table this potential column might belong to.
                                DataTable misplacedTable = nodeToSchemaMap.GetTableForNode(dataReader, FIgnoreNamespace(dataReader));
                                                                // Try to get table for node

                                if (misplacedTable != null) {   // Got some matching table?
                                    LoadTable (misplacedTable, false /* isNested */);                       
                                                                // While table's XML element is nested,
                                                                // the table itself is not. Load it this way.
                                }                
                                else {
                                    dataReader.Read();          // Not a table? Try next element.
                                }
                            }
                        }
                        break;
                    case XmlNodeType.EntityReference:           // Oops. No support for Entity Reference
                        throw ExceptionBuilder.FoundEntity();
                    case XmlNodeType.Text:                      // It looks like a text.
                    case XmlNodeType.Whitespace:                // This actually could be
                    case XmlNodeType.CDATA:                     // if we have XmlText in our table
                    case XmlNodeType.SignificantWhitespace:
                        textNodeValue = dataReader.ReadString();    
                                                                // Get text node value.
                        c = table.xmlText;                      // Get XML Text column from our table

                        if (c != null && foundColumns[c.Ordinal] == null) {
                                                                // If XmlText Column is set
                                                                // and we do not have data already                
                            foundColumns[c.Ordinal] = c.ConvertXmlToObject(textNodeValue);
                                                                // Read and store the data
                        }
                        break;
                    default:
                            dataReader.Read();                  // We don't process that, skip to the next element.
                        break;
                    }
                }

                dataReader.Read();                              // We're done here, proceed to the next element.
            }

            // It's the time to populate row with loaded data and add it to the table we'we just read to the table

            if (isDiffgram) {                               // In case of diffgram
                row = table.NewRow(table.NewUninitializedRecord());
                                                            // just create an empty row
                row.BeginEdit();                            // and allow it's population with data 

                for ( int i = foundColumns.Length - 1; i >= 0 ; --i) {
                                                            // Check all columns
                    c = collection[i];                      // Get column for this index

                    c[row.tempRecord] = null != foundColumns[i] ? foundColumns[i] : DBNull.Value;
                                                            // Set column to loaded value of to
                                                            // DBNull if value is missing.
                }

                row.EndEdit();                              // Done with this row

                table.Rows.DiffInsertAt(row, rowOrder);     // insert data to specific location

                                                            // And do some diff processing
                if (hasChanges == null) {                   // No changes ?
                    row.oldRecord = row.newRecord;          // Restore old record
                } 

                if ((hasChanges == Keywords.MODIFIED) || hasErrors) {
                    table.RowDiffId[diffId] = row;    
                }

            }
            else {
                for ( int i = foundColumns.Length -1; i >= 0 ; --i) {
                                                            // Check all columns
                    if (null == foundColumns[i]) {          // Got data for this column ?
                        c = collection[i];                  // No. Get column for this index

                        if (c.AllowDBNull && c.ColumnMapping != MappingType.Hidden && !c.AutoIncrement) {
                            foundColumns[i] = DBNull.Value; // Assign DBNull if possible
                                                            // table.Rows.Add() below will deal
                                                            // with default values and autoincrement
                        }
                    }
                }

                row = table.Rows.AddWithColumnEvents(foundColumns);         // Create, populate and add row

            }

            // Data is loaded into the row and row is added to the table at this point

            while (entryChild < childRowsStack.Count) {     // Process child rows we might have
                DataRow childRow = (DataRow) childRowsStack.Pop();  
                                                            // Get row from the stack
                bool unchanged = (childRow.RowState == DataRowState.Unchanged);
                                                            // Is data the same as before?
                childRow.SetNestedParentRow(row, /*setNonNested*/ false);
                                                            // Set parent row

                if (unchanged)                              // Restore record if child row's unchanged
                    childRow.oldRecord = childRow.newRecord; 
            }

            if (isNested)                                   // Got parent ?
                childRowsStack.Push(row);                   // Push row to the stack

        }

        // Returns column value
        private void LoadColumn (DataColumn column, object[] foundColumns) {

            //  <DataSet>    /--------------------------------- We are here on entrance
            //      <Table> /
            //          <Column>Value</Column>
            //          <AnotherColumn>Value</AnotherColumn>
            //      </Table>    \------------------------------ We are here on exit
            //  </DataSet>

            //  <Column>                                        If we have something like this
            //      <Foo>FooVal</Foo>                           We would grab first text-like node
            //      Value                                       In this case it would be "FooVal"
            //      <Bar>BarVal</Bar>                           And not "Value" as you might think
            //  </Column>                                       This is how desktop works

            string text = String.Empty;                         // Column text. Assume empty string
            string xsiNilString = null;                         // Possible NIL attribute string

            int entryDepth = dataReader.Depth;                  // Store depth so we won't read too much

            if (dataReader.AttributeCount > 0)                  // If have attributes
                xsiNilString = dataReader.GetAttribute(Keywords.XSI_NIL, Keywords.XSINS);
                                                                // Try to get NIL attribute
                                                                // We have to do it before we move to the next element
            if (column.IsCustomType) {                          // Custom type column
                object columnValue   = null;                    // Column value we're after. Assume no value.

                string xsiTypeString = null;                    // XSI type name from TYPE attribute
                string typeName      = null;                    // Type name from MSD_INSTANCETYPE attribute

                XmlRootAttribute xmlAttrib = null;              // Might need this attribute for XmlSerializer

                if (dataReader.AttributeCount > 0) {            // If have attributes, get attributes we'll need
                    xsiTypeString    = dataReader.GetAttribute(Keywords.TYPE, Keywords.XSINS);
                    typeName         = dataReader.GetAttribute(Keywords.MSD_INSTANCETYPE, Keywords.MSDNS);
                }

                // Check if need to use XmlSerializer. We need to do that if type does not implement IXmlSerializable.
                // We also need to do that if no polymorphism for this type allowed.

                bool useXmlSerializer = !column.ImplementsIXMLSerializable && 
                    !( (column.DataType == typeof(Object)) || (typeName != null) || (xsiTypeString != null) );

                // Check if we have an attribute telling us value is null.
    
                if ((xsiNilString != null) && XmlConvert.ToBoolean(xsiNilString)) { 
                    if (!useXmlSerializer) {                    // See if need to set typed null.
                        if (typeName != null && typeName.Length > 0) { 
                                                                // Got type name
                            columnValue = SqlUdtStorage.GetStaticNullForUdtType(DataStorage.GetType(typeName));
                        }
                    }

                    if (null == columnValue) {                  // If no value,
                        columnValue = DBNull.Value;             // change to DBNull;
                    }

                    if ( !dataReader.IsEmptyElement )           // In case element is not empty
                        while (dataReader.Read() && (entryDepth < dataReader.Depth));
                                                                // Read current elements
                    dataReader.Read();                          // And start reading next element.

                }
                else {                                          // No NIL attribute. Get value
                    bool skipped = false;

                    if (column.Table.DataSet != null && column.Table.DataSet.UdtIsWrapped) {
                        dataReader.Read(); // if UDT is wrapped, skip the wrapper
                        skipped = true;
                    }

                    if (useXmlSerializer) {                     // Create an attribute for XmlSerializer
                        if (skipped) {
                            xmlAttrib = new XmlRootAttribute(dataReader.LocalName);
                            xmlAttrib.Namespace = dataReader.NamespaceURI ;
                        }
                        else {
                            xmlAttrib = new XmlRootAttribute(column.EncodedColumnName);
                            xmlAttrib.Namespace = column.Namespace;
                        }
                    }

                    columnValue = column.ConvertXmlToObject(dataReader, xmlAttrib);
                                                                // Go get the value
                    if (skipped) {
                        dataReader.Read(); // if Wrapper is skipped, skip its end tag
                    }
                }

                foundColumns[column.Ordinal] = columnValue;     // Store value

            } 
            else {                                                  // Not a custom type. 
                if ( dataReader.Read() && entryDepth < dataReader.Depth) {
                                                                    // Read to the next element and see if we're inside.
                    while (entryDepth < dataReader.Depth) {
                        switch (dataReader.NodeType) {              // Process nodes based on type
                        case XmlNodeType.Text:                      // It looks like a text. And we need it.
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.CDATA:
                        case XmlNodeType.SignificantWhitespace:
                            if (0 == text.Length) {                 // In case we do not have value already

                                text = dataReader.Value;            // Get value.

                                // See if we have other text nodes near. In most cases this loop will not be executed. 
                                StringBuilder builder = null;
                                while ( dataReader.Read() && entryDepth < dataReader.Depth && IsTextLikeNode(dataReader.NodeType)) {
                                    if (builder == null) {
                                        builder = new StringBuilder(text);
                                    }
                                    builder.Append(dataReader.Value);  // Concatenate other sequential text like
                                                                       // nodes we might have. This is rare.
                                                                       // We're using this instead of dataReader.ReadString()
                                                                       // which would do the same thing but slower.
                                }

                                if (builder != null) {
                                    text = builder.ToString();
                                }
                            }
                            else {
                                dataReader.ReadString();            // We've got column value already. Read this one and ignore it.
                            }
                            break;
                        case XmlNodeType.Element:
                            if (ProcessXsdSchema()) {               // Check for schema. Skip or load if found.
                                continue;                           // Schema has been found. Process the next element 
                                                                    // we're already at (done by schema processing).
                            }
                            else {                              
                                // We've got element which is not supposed to he here.
                                // That might be table which was misplaced.
                                // Or it might be a column inside column (also misplaced).
                                object o = nodeToSchemaMap.GetColumnSchema(column.Table, dataReader, FIgnoreNamespace(dataReader));
                                                                    // Get dataset element for this XML element
                                DataColumn c = o as DataColumn;     // Perhaps, it's a column?
            
                                if ( c != null ) {                  // Do we have matched column in this table?

                                    // Let's load column data
                        
                                    if (foundColumns[c.Ordinal] == null) {
                                                                    // If this column was not found before
                                        LoadColumn (c, foundColumns);            
                                                                    // Get column value
                                    }       
                                    else {
                                        dataReader.Read();          // Already loaded, proceed to the next element
                                    }
                                } 
                                else  {
                                    DataTable nestedTable = o as DataTable; 
                                                                    // Perhaps, it's a nested table ?
                                    if ( nestedTable != null ) {        
                                                                    // Do we have matched table in DataSet ?
                                        LoadTable (nestedTable, true /* isNested */);            
                                                                    // Yes. Load nested table (recursive)
                                    }
                                    else {                          // Not a nested column nor nested table.    
                                                                    // Let's try other tables in the DataSet
        
                                        DataTable misplacedTable = nodeToSchemaMap.GetTableForNode(dataReader, FIgnoreNamespace(dataReader));
                                                                    // Try to get table for node
                                        if (misplacedTable != null) {   
                                                                    // Got some table to match?
                                            LoadTable (misplacedTable, false /* isNested */);                       
                                                                    // While table's XML element is nested,
                                                                    // the table itself is not. Load it this way.
                                        } 
                                        else {
                                            dataReader.Read();      // No match? Try next element
                                        }               
                                    }
                                }
                            }
                            break;
                        case XmlNodeType.EntityReference:           // Oops. No support for Entity Reference
                            throw ExceptionBuilder.FoundEntity();
                        default:
                            dataReader.Read();                      // We don't process that, skip to the next element.
                            break;
                        }
            
                    }

                    dataReader.Read();                              // We're done here. To the next element.
                }

                if (0 == text.Length && xsiNilString != null && XmlConvert.ToBoolean(xsiNilString)) {
                    foundColumns[column.Ordinal] = DBNull.Value;     
                                                                    // If no data and NIL attribute is true set value to null
                }
                else {
                    foundColumns[column.Ordinal] = column.ConvertXmlToObject(text);
                }
    
            }

        }

        // Check for schema and skips or loads XSD schema if found. Returns true if schema found.
        // DataReader would be set on the first XML element after the schema of schema was found.
        // If no schema detected, reader's position will not change.
        private bool ProcessXsdSchema () {
            if (((object)dataReader.LocalName == XSD_SCHEMA && (object)dataReader.NamespaceURI == XSDNS )) {
                                                                    // Found XSD schema
                if ( ignoreSchema ) {                               // Should ignore it?
                    dataReader.Skip();                              // Yes, skip it
                }
                else {                                              // Have to load schema.
                    if ( isTableLevel ) {                           // Loading into the DataTable ?
                        dataTable.ReadXSDSchema(dataReader, false); // Invoke ReadXSDSchema on a table
                        nodeToSchemaMap = new XmlToDatasetMap(dataReader.NameTable, dataTable);
                    }                                               // Rebuild XML to DataSet map with new schema.
                    else {                                          // Loading into the DataSet ?
                        dataSet.ReadXSDSchema(dataReader, false);   // Invoke ReadXSDSchema on a DataSet
                        nodeToSchemaMap = new XmlToDatasetMap(dataReader.NameTable, dataSet);
                    }                                               // Rebuild XML to DataSet map with new schema.
                }
            }
            else if (((object)dataReader.LocalName == XDR_SCHEMA && (object)dataReader.NamespaceURI == XDRNS ) || 
                    ((object)dataReader.LocalName == SQL_SYNC   && (object)dataReader.NamespaceURI == UPDGNS))
            {
                dataReader.Skip();                                  // Skip XDR or SQL sync 
            }
            else {
                return false;                                       // No schema found. That means reader's position 
                                                                    // is unchganged. Report that to the caller.
            }

            return true;                                            // Schema found, reader's position changed.
        }
    }
}
