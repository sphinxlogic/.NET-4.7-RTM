//---------------------------------------------------------------------------
//
// File: DataStreams.cs
//
// Description:
// Holds the data for Avalon BindProducts in the journal
//
//
// History:
//  7/30/2001:  t-ypchen        Created
//  5/20/2003:  kusumav         Ported from to WCP_Dev
// 11/14/2005:  Microsoft         Refactoring as part of the "Island Frame" implementation; 
//                              introduced the internal IJournalState interface.
//
// Copyright (C) 2003 by Microsoft Corporation.  All rights reserved.
// 
//---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;
using MS.Internal.AppModel;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MS.Internal.AppModel
{
    #region SubStream struct
    [Serializable]
    internal struct SubStream
    {
        internal SubStream(string propertyName, byte[] dataBytes)
        {
            _propertyName = propertyName;
            _data = dataBytes;
        }

        internal string _propertyName;
        internal byte[] _data;
    }
    #endregion SubStream struct
    
    #region DataStreams class
    [Serializable]
    internal class DataStreams
    {
        /// <SecurityNote>
        /// Critical - as the constructor initializes the Critical member _subStreams.
        /// Safe - as the initialization is to a safe value.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal DataStreams()
        {
            // Dummy constructor to keep FxCop Critical rules happy.
        }

        /// <SecurityNote>
        /// Critical - as this refers to Critical member _subStreams.
        /// Safe - as this doesn't expose the data
        /// </SecurityNote>
        internal bool HasAnyData
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get 
            {
                return _subStreams != null && _subStreams.Count > 0
                    || _customJournaledObjects != null && _customJournaledObjects.Count > 0;
            }
        }

        /// <SecurityNote>
        /// Critical - as this refers to Critical member _subStreams.
        /// Safe - as this doesn't expose the data.  Returning if a stream exists
        ///        is not Critical information, the content of the stream is.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private bool HasSubStreams(object key)
        {
            return _subStreams != null && _subStreams.Contains(key);
        }

        /// <SecurityNote>
        /// Critical - as this returns Critical data in _subStreams.
        /// </SecurityNote>
        [SecurityCritical]
        private ArrayList GetSubStreams(object key)
        {
            ArrayList subStreams = (ArrayList) _subStreams[key];
            if (subStreams == null)
            {
                subStreams = new ArrayList(3);
                _subStreams[key] = subStreams;
            }

            return subStreams;
        }

        private delegate void NodeOperation(object node);

        /// <summary>
        /// Build an ArrayList of SubStreams where each SubStream represents one of the
        /// node's journalable DPs.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>The ArrayList of SubStreams. May be null.</returns>
        /// <SecurityNote>
        /// Critical - as this calls Formatter.Serialize under an elevation and 
        ///            returns the value so obtained.
        /// </SecurityNote>
        [SecurityCritical]
        private ArrayList SaveSubStreams(UIElement element)
        {
            ArrayList subStreams = null;

#pragma warning disable 618
            if ((element != null) && (element.PersistId != 0))
#pragma warning restore 618
            {
                LocalValueEnumerator dpEnumerator = element.GetLocalValueEnumerator();

                while (dpEnumerator.MoveNext())
                {
                    LocalValueEntry localValueEntry = (LocalValueEntry)dpEnumerator.Current;
                    FrameworkPropertyMetadata metadata = localValueEntry.Property.GetMetadata(element.DependencyObjectType) as FrameworkPropertyMetadata;
                                        
                    if (metadata == null)
                    {
                        continue;
                    }

                    // To be saved, a DP should have the correct metadata and NOT be an expression or data bound.
                    // Since Bind inherits from Expression, the test for Expression will suffice.
                    // NOTE: we do not journal expression. So we should let parser restore it in BamlRecordReader.SetDependencyValue.
                    // Please see Windows OS bug # 1852349 for details.
                    if (metadata.Journal && (!(localValueEntry.Value is Expression)))
                    {
                        // These properties should not be journaled.
                        // 

                        if (object.ReferenceEquals(localValueEntry.Property, Frame.SourceProperty))
                            continue;

                        if (subStreams == null)
                        {
                            subStreams = new ArrayList(3);
                        }

                        object currentValue = element.GetValue(localValueEntry.Property);
                        byte[] bytes = null;

                        if ((currentValue != null) && !(currentValue is Uri))
                        {
                            // Convert the value of the DP into a byte array
                            MemoryStream byteStream = new MemoryStream();
                            new SecurityPermission(SecurityPermissionFlag.SerializationFormatter).Assert();
                            try
                            {
                                this.Formatter.Serialize(byteStream, currentValue);
                            }
                            finally
                            {
                                SecurityPermission.RevertAssert();
                            }
                            
                            bytes = byteStream.ToArray();
                            // Dispose the stream
                            ((IDisposable)byteStream).Dispose( );
                        }

                        // Save the byte array by the property name
                        subStreams.Add(new SubStream(localValueEntry.Property.Name, bytes));
                    }
                }
            }

            return subStreams;
        }

        /// <SecurityNote>
        /// Critical - as this invokes Critical function SaveSubStreams and sets data
        ///            in Critical member _subStreams.
        /// Safe - as this doesn't return the Critical data, so any private members that
        ///        are saved from Serializing the object are protected.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void SaveState(object node)
        {
            UIElement element = node as UIElement;
            if (element == null)
            {
                return;
            }

            // Due to bug 1282529, PersistId can be null. Only XAML/BAML-loaded elements have it.
            // Besides for PageFunctions journaled by type, the PersistId check below is needed
            // because elements might have been added to the tree after loading from XAML/BAML.
#pragma warning disable 618
            int persistId = element.PersistId;
#pragma warning restore 618

            if (persistId != 0)
            {
                ArrayList subStreams = this.SaveSubStreams(element);
                if (subStreams != null)
                {
                    //
                    // If one element in the tree is replaced with a new element which is created
                    // from a xaml/baml stream programatically, this new element and all its descendent nodes 
                    // would have another set of PersistId starting from 1, it would end up with two or more 
                    // elements in the same page share the same PersistId.  We cannot guarantee to restore 
                    // journaldata for the newly added elements when doing the journaling navigation.
                    //
                    // So to do some level protection to avoid application crash, the code doesn't update the 
                    // journaldata for that element id if the data was set already.
                    //
                    // 



                    if (!_subStreams.Contains(persistId))
                    {
                        _subStreams[persistId] = subStreams;
                    }
                }

                IJournalState customJournalingObject = node as IJournalState;
                if (customJournalingObject != null)
                {
                    object customState = customJournalingObject.GetJournalState(JournalReason.NewContentNavigation);
                    if (customState != null)
                    {
                        if (_customJournaledObjects == null)
                        {
                            _customJournaledObjects = new HybridDictionary(2);
                        }

                        //
                        // Again, We cannot guarantee the PeristId of all elements in the same page are unique.
                        // Some IJouralState aware node such as Frame, FlowDocumentPageViewer could be added 
                        // programatically after the page is created from baml stream,  the new added node could also 
                        // be created from baml/xaml stream by Parser.
                        //
                        if (!_customJournaledObjects.Contains(persistId))
                        {
                            _customJournaledObjects[persistId] = customState;
                        }
                    }
                }
            }
        }

        internal void PrepareForSerialization()
        {
            if (_customJournaledObjects != null)
            {
                foreach (DictionaryEntry entry in _customJournaledObjects)
                {
                    CustomJournalStateInternal cjs = (CustomJournalStateInternal)entry.Value;
                    cjs.PrepareForSerialization();
                }
            }

            // Everything in _subStreams is already binary-serialized.
        }

        /// <SecurityNote>
        /// Critical - as this invokes Deserialize under an elevation on a random stream passed in.
        /// Safe - Demands SerializationFormatter permissions
        /// </SecurityNote>
        [SecuritySafeCritical]
        private void LoadSubStreams(UIElement element, ArrayList subStreams)
        {
            for (int subStreamIndex = 0; subStreamIndex < subStreams.Count; ++subStreamIndex)
            {
                SubStream subStream = (SubStream)subStreams[subStreamIndex];

                // Restore the value of an individual DP
                DependencyProperty dp = DependencyProperty.FromName(subStream._propertyName, element.GetType());
                // If the dp cannot be found it may mean that we navigated back to a loose file that has been changed.
                if (dp != null)
                {
                    object newValue = null;
                    if (subStream._data != null)
                    {
                        try
                        {
	                        new SecurityPermission(SecurityPermissionFlag.SerializationFormatter).Demand(); // prevent any journal metadata de-serialization in partial trust
                            newValue = this.Formatter.Deserialize(new MemoryStream(subStream._data));
                        }
                        catch (SecurityException)
                        {
                            newValue = DependencyProperty.UnsetValue;
                        }
                    }
                    element.SetValue(dp, newValue);
                }
            }
        }

        /// <SecurityNote>
        /// Critical - as this calls Critical functions GetSubStreams and LoadSubStreams.
        /// Safe - this does not return data obtained from GetSubStreams.  The call to
        ///        LoadSubStreams won't cause de-serialization of random data.  It'd only
        ///        cause deserializaton of data returned by GetSubStreams which returns an
        ///        object from _subStreams array which is Critical and thus is tracked.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void LoadState(object node)
        {
            UIElement element = node as UIElement;
            if (element == null)
            {
                return;
            }

#pragma warning disable 618
            int persistId = element.PersistId;
#pragma warning restore 618

            // Due to bug 1282529, PersistId can be null. Only XAML/BAML-loaded elements have it.
            if (persistId != 0)
            {
                if (this.HasSubStreams(persistId))
                {
                    // Get the properties to restore
                    ArrayList properties = this.GetSubStreams(persistId);
                    LoadSubStreams(element, properties);
                }

                if (_customJournaledObjects != null && _customJournaledObjects.Contains(persistId))
                {
                    CustomJournalStateInternal state = 
                        (CustomJournalStateInternal)_customJournaledObjects[persistId];
                    Debug.Assert(state != null);
                    IJournalState customJournalingObject = node as IJournalState;

                    //
                    // For below two scenarios, JournalData cannot be restored successfully. For now, we just
                    // simply ignore it and don't throw exception.
                    //
                    //  A. After the tree was created from xaml/baml stream,  some elements might be replaced 
                    //     programatically with new elements which could be created from other xaml/baml by Parser.
                    //
                    //  B. If the loose xaml file has been changed since the journal data was created
                    //
                    // 

                    if (customJournalingObject != null)
                    {
                        customJournalingObject.RestoreJournalState(state);
                    }
                }
            }
        }

        /// <summary>
        /// Walk the logical tree, and perform an operation on all nodes and children of nodes.
        /// This is slightly more complex than it might otherwise be because it makes no assumptions
        /// that the Logical Tree is strongly typed. It is the responsibility of the operation to make sure
        /// that it can handle the node it receives.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="operation"></param>
        private void WalkLogicalTree(object node, NodeOperation operation)
        {
            if (node != null)
            {
                operation(node);
            }

            DependencyObject treeNode = node as DependencyObject;
            if (treeNode == null)
            {
                return;
            }

            IEnumerator e = LogicalTreeHelper.GetChildren(treeNode).GetEnumerator();
            if (e == null)
            {
                return;
            }

            while (e.MoveNext())
            {
                WalkLogicalTree(e.Current, operation);
            }
        }

        /// <SecurityNote>
        /// Critical - as this modifies Critical data member _subStreams.
        /// Safe - as this just initializes it to a safe value.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal void Save(Object root)
        {
            if (_subStreams == null)
            {
                _subStreams = new HybridDictionary(3);
            }
            else
            {
                _subStreams.Clear();
            }
            WalkLogicalTree(root, new NodeOperation(this.SaveState));            
        }

        internal void Load(Object root)
        {
            if (this.HasAnyData)
            {
                WalkLogicalTree(root, new NodeOperation(this.LoadState));               
            }
        }

        /// <SecurityNote>
        /// Critical - as this refers to Critical member _subStreams.
        /// Safe - as this doesn't expose the data
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal void Clear()
        {
            _subStreams = null;
            _customJournaledObjects = null; 
        }

        #region Private and internal fields and properties
        private BinaryFormatter Formatter
        {
            get
            {
                if (_formatter == null)
                {
                    _formatter = new BinaryFormatter();
                }

                return _formatter;
            }
        }

        [ThreadStatic]
        static private BinaryFormatter _formatter;

        /// <SecurityNote>
        /// Critical - This data is critical for two reasons.  One is that this holds
        ///            data obtained by invoking Serialization under elevation.  The
        ///            other is that we call DeSerialization on this data under elevation.
        /// </SecurityNote>
        [SecurityCritical]
        private HybridDictionary _subStreams = new HybridDictionary(3);

        /// <summary>
        /// PersistId->CustomJournalStateInternal map
        /// <see cref="SaveState"/>.
        /// </summary>
        private HybridDictionary _customJournaledObjects;

        #endregion Private and internal fields and properties
    }
    #endregion DataStreams class
    
}
