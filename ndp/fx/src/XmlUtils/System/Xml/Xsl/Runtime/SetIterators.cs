//------------------------------------------------------------------------------
// <copyright file="SetIterators.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime {

    /// <summary>
    /// Set iterators (Union, Intersection, Difference) that use containment to control two nested iterators return
    /// one of the following values from MoveNext().
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum SetIteratorResult {
        NoMoreNodes,                // Iteration is complete; there are no more nodes
        InitRightIterator,          // Initialize right nested iterator
        NeedLeftNode,               // The next node needs to be fetched from the left nested iterator
        NeedRightNode,              // The next node needs to be fetched from the right nested iterator
        HaveCurrentNode,            // This iterator's Current property is set to the next node in the iteration
    };


    /// <summary>
    /// This iterator manages two sets of nodes that are already in document order with no duplicates.
    /// Using a merge sort, this operator returns the union of these sets in document order with no duplicates.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct UnionIterator {
        private XmlQueryRuntime runtime;
        private XPathNavigator navCurr, navOther;
        private IteratorState state;

        private enum IteratorState {
            InitLeft = 0,
            NeedLeft,
            NeedRight,
            LeftIsCurrent,
            RightIsCurrent,
        };

        /// <summary>
        /// Create SetIterator.
        /// </summary>
        public void Create(XmlQueryRuntime runtime) {
            this.runtime = runtime;
            this.state = IteratorState.InitLeft;
        }

        /// <summary>
        /// Position this iterator to the next node in the union.
        /// </summary>
        public SetIteratorResult MoveNext(XPathNavigator nestedNavigator) {
            switch (this.state) {
                case IteratorState.InitLeft:
                    // Fetched node from left iterator, now get initial node from right iterator
                    this.navOther = nestedNavigator;
                    this.state = IteratorState.NeedRight;
                    return SetIteratorResult.InitRightIterator;

                case IteratorState.NeedLeft:
                    this.navCurr = nestedNavigator;
                    this.state = IteratorState.LeftIsCurrent;
                    break;

                case IteratorState.NeedRight:
                    this.navCurr = nestedNavigator;
                    this.state = IteratorState.RightIsCurrent;
                    break;

                case IteratorState.LeftIsCurrent:
                    // Just returned left node as current, so get new left
                    this.state = IteratorState.NeedLeft;
                    return SetIteratorResult.NeedLeftNode;

                case IteratorState.RightIsCurrent:
                    // Just returned right node as current, so get new right
                    this.state = IteratorState.NeedRight;
                    return SetIteratorResult.NeedRightNode;
            }

            // Merge left and right nodes
            if (this.navCurr == null) {
                // If both navCurr and navOther are null, then iteration is complete
                if (this.navOther == null)
                    return SetIteratorResult.NoMoreNodes;

                Swap();
            }
            else if (this.navOther != null) {
                int order = this.runtime.ComparePosition(this.navOther, this.navCurr);

                // If navCurr is positioned to same node as navOther,
                if (order == 0) {
                    // Skip navCurr, since it is a duplicate
                    if (this.state == IteratorState.LeftIsCurrent) {
                        this.state = IteratorState.NeedLeft;
                        return SetIteratorResult.NeedLeftNode;
                    }

                    this.state = IteratorState.NeedRight;
                    return SetIteratorResult.NeedRightNode;
                }

                // If navOther is before navCurr in document order, then swap navCurr with navOther
                if (order < 0)
                    Swap();
            }

            // Return navCurr
            return SetIteratorResult.HaveCurrentNode;
        }

        /// <summary>
        /// Return the current result navigator.  This is only defined after MoveNext() has returned -1.
        /// </summary>
        public XPathNavigator Current {
            get { return this.navCurr; }
        }

        /// <summary>
        /// Swap navCurr with navOther and invert state to reflect the change.
        /// </summary>
        private void Swap() {
            XPathNavigator navTemp = this.navCurr;
            this.navCurr = this.navOther;
            this.navOther = navTemp;

            if (this.state == IteratorState.LeftIsCurrent)
                this.state = IteratorState.RightIsCurrent;
            else
                this.state = IteratorState.LeftIsCurrent;
        }
    }


    /// <summary>
    /// This iterator manages two sets of nodes that are already in document order with no duplicates.
    /// This iterator returns the intersection of these sets in document order with no duplicates.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IntersectIterator {
        private XmlQueryRuntime runtime;
        private XPathNavigator navLeft, navRight;
        private IteratorState state;

        private enum IteratorState {
            InitLeft = 0,
            NeedLeft,
            NeedRight,
            NeedLeftAndRight,
            HaveCurrent,
        };

        /// <summary>
        /// Create IntersectIterator.
        /// </summary>
        public void Create(XmlQueryRuntime runtime) {
            this.runtime = runtime;
            this.state = IteratorState.InitLeft;
        }

        /// <summary>
        /// Position this iterator to the next node in the union.
        /// </summary>
        public SetIteratorResult MoveNext(XPathNavigator nestedNavigator) {
            int order;

            switch (this.state) {
                case IteratorState.InitLeft:
                    // Fetched node from left iterator, now get initial node from right iterator
                    this.navLeft = nestedNavigator;
                    this.state = IteratorState.NeedRight;
                    return SetIteratorResult.InitRightIterator;

                case IteratorState.NeedLeft:
                    this.navLeft = nestedNavigator;
                    break;

                case IteratorState.NeedRight:
                    this.navRight = nestedNavigator;
                    break;

                case IteratorState.NeedLeftAndRight:
                    // After fetching left node, still need right node
                    this.navLeft = nestedNavigator;
                    this.state = IteratorState.NeedRight;
                    return SetIteratorResult.NeedRightNode;

                case IteratorState.HaveCurrent:
                    // Just returned left node as current, so fetch new left and right nodes
                    Debug.Assert(nestedNavigator == null, "null is passed to MoveNext after IteratorState.HaveCurrent has been returned.");
                    this.state = IteratorState.NeedLeftAndRight;
                    return SetIteratorResult.NeedLeftNode;
            }

            if (this.navLeft == null || this.navRight == null) {
                // No more nodes from either left or right iterator (or both), so iteration is complete
                return SetIteratorResult.NoMoreNodes;
            }

            // Intersect left and right sets
            order = this.runtime.ComparePosition(this.navLeft, this.navRight);

            if (order < 0) {
                // If navLeft is positioned to a node that is before navRight, skip left node
                this.state = IteratorState.NeedLeft;
                return SetIteratorResult.NeedLeftNode;
            }
            else if (order > 0) {
                // If navLeft is positioned to a node that is after navRight, so skip right node
                this.state = IteratorState.NeedRight;
                return SetIteratorResult.NeedRightNode;
            }

            // Otherwise, navLeft is positioned to the same node as navRight, so found one item in the intersection
            this.state = IteratorState.HaveCurrent;
            return SetIteratorResult.HaveCurrentNode;
        }

        /// <summary>
        /// Return the current result navigator.  This is only defined after MoveNext() has returned -1.
        /// </summary>
        public XPathNavigator Current {
            get { return this.navLeft; }
        }
    }


    /// <summary>
    /// This iterator manages two sets of nodes that are already in document order with no duplicates.
    /// This iterator returns the difference of these sets (Left - Right) in document order with no duplicates.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct DifferenceIterator {
        private XmlQueryRuntime runtime;
        private XPathNavigator navLeft, navRight;
        private IteratorState state;

        private enum IteratorState {
            InitLeft = 0,
            NeedLeft,
            NeedRight,
            NeedLeftAndRight,
            HaveCurrent,
        };

        /// <summary>
        /// Create DifferenceIterator.
        /// </summary>
        public void Create(XmlQueryRuntime runtime) {
            this.runtime = runtime;
            this.state = IteratorState.InitLeft;
        }

        /// <summary>
        /// Position this iterator to the next node in the union.
        /// </summary>
        public SetIteratorResult MoveNext(XPathNavigator nestedNavigator) {
            switch (this.state) {
                case IteratorState.InitLeft:
                    // Fetched node from left iterator, now get initial node from right iterator
                    this.navLeft = nestedNavigator;
                    this.state = IteratorState.NeedRight;
                    return SetIteratorResult.InitRightIterator;

                case IteratorState.NeedLeft:
                    this.navLeft = nestedNavigator;
                    break;

                case IteratorState.NeedRight:
                    this.navRight = nestedNavigator;
                    break;

                case IteratorState.NeedLeftAndRight:
                    // After fetching left node, still need right node
                    this.navLeft = nestedNavigator;
                    this.state = IteratorState.NeedRight;
                    return SetIteratorResult.NeedRightNode;

                case IteratorState.HaveCurrent:
                    // Just returned left node as current, so fetch new left node
                    Debug.Assert(nestedNavigator == null, "null is passed to MoveNext after IteratorState.HaveCurrent has been returned.");
                    this.state = IteratorState.NeedLeft;
                    return SetIteratorResult.NeedLeftNode;
            }

            if (this.navLeft == null) {
                // If navLeft is null, then difference operation is complete
                return SetIteratorResult.NoMoreNodes;
            }
            else if (this.navRight != null) {
                int order = this.runtime.ComparePosition(this.navLeft, this.navRight);

                // If navLeft is positioned to same node as navRight,
                if (order == 0) {
                    // Skip navLeft and navRight
                    this.state = IteratorState.NeedLeftAndRight;
                    return SetIteratorResult.NeedLeftNode;
                }

                // If navLeft is after navRight in document order, then skip navRight
                if (order > 0) {
                    this.state = IteratorState.NeedRight;
                    return SetIteratorResult.NeedRightNode;
                }
            }

            // Return navLeft
            this.state = IteratorState.HaveCurrent;
            return SetIteratorResult.HaveCurrentNode;
        }

        /// <summary>
        /// Return the current result navigator.  This is only defined after MoveNext() has returned -1.
        /// </summary>
        public XPathNavigator Current {
            get { return this.navLeft; }
        }
    }
}
