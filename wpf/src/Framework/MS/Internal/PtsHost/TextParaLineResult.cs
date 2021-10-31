//---------------------------------------------------------------------------
//
// <copyright file="TextParaLineResult.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
// 
// Description: Access to calculated information of a line of text created
//              by TextParagraph. 
//
// History:  
//  04/25/2003 : Microsoft - Moving from Avalon branch.
//  06/25/2004 : Microsoft - Performance work.
//
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using MS.Internal.Documents;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Provides access to calculated information of a line of text created
    /// by TextParagraph
    /// </summary>
    internal sealed class TextParaLineResult : LineResult
    {
        //-------------------------------------------------------------------
        //
        //  LineResult Methods
        //
        //-------------------------------------------------------------------

        #region LineResult Methods

        /// <summary>
        /// Get text position corresponding to given distance from start of line
        /// </summary>
        /// <param name="distance">
        /// Distance from start of line.
        /// </param>
        internal override ITextPointer GetTextPositionFromDistance(double distance)
        {
            return _owner.GetTextPositionFromDistance(_dcp, distance);
        }

        /// <summary>
        /// Returns true if given position is at a caret unit boundary and false if not.
        /// Not presently implemented.
        /// </summary>
        /// <param name="position">
        /// TextPointer representing position to check for unit boundary
        /// </param>
        internal override bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            Debug.Assert(false);
            return false;
        }

        /// <summary>
        /// Return next caret unit position from the specified position in the given direction.
        /// Not presently implemented.
        /// </summary>
        /// <param name="position">
        /// TextPointer for the current position
        /// </param>
        /// <param name="direction">
        /// LogicalDirection in which next caret unit position is desired
        /// </param>
        internal override ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Return next caret unit position from the specified position.
        /// Not presently implemented.
        /// </summary>
        /// <param name="position">
        /// TextPointer for the current position
        /// </param>
        internal override ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Return GlyphRun collection for the specified range of positions.
        /// Not presently implemented.
        /// </summary>
        /// <param name="start">
        /// ITextPointer marking start of range.
        /// </param>
        /// <param name="end">
        /// ITextPointer marking end of range
        /// </param>
        internal override ReadOnlyCollection<GlyphRun> GetGlyphRuns(ITextPointer start, ITextPointer end)
        {
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Returns the position after last content character of the line, 
        /// not including any line breaks.
        /// </summary>
        internal override ITextPointer GetContentEndPosition()
        {
            EnsureComplexData();
            return _owner.GetTextPosition(_dcp + _cchContent, LogicalDirection.Backward);
        }

        /// <summary>
        /// Returns the position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </summary>
        internal override ITextPointer GetEllipsesPosition()
        {
            EnsureComplexData();
            if (_cchEllipses != 0)
            {
                return _owner.GetTextPosition(_dcp + _cch - _cchEllipses, LogicalDirection.Forward);
            }
            return null;
        }

        /// <summary>
        /// Retrieves the position after last content character of the line, 
        /// not including any line breaks.
        /// </summary>
        /// <returns>
        /// The position after last content character of the line, 
        /// not including any line breaks.
        /// </returns>
        internal override int GetContentEndPositionCP()
        {
            EnsureComplexData();
            return _dcp + _cchContent;
        }

        /// <summary>
        /// Retrieves the position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </summary>
        /// <returns>
        /// The position in the line pointing to the beginning of content 
        /// hidden by ellipses.
        /// </returns>
        internal override int GetEllipsesPositionCP()
        {
            EnsureComplexData();
            return _dcp + _cch - _cchEllipses;
        }

        #endregion LineResult Methods

        //-------------------------------------------------------------------
        //
        //  LineResult Properties
        //
        //-------------------------------------------------------------------

        #region LineResult Properties

        /// <summary>
        /// ITextPointer representing the beginning of the Line's contents.
        /// </summary>
        internal override ITextPointer StartPosition
        {
            get
            {
                if (_startPosition == null)
                {
                    _startPosition = _owner.GetTextPosition(_dcp, LogicalDirection.Forward);
                }
                return _startPosition;
            }
        }

        /// <summary>
        /// ITextPointer representing the end of the Line's contents.
        /// </summary>
        internal override ITextPointer EndPosition
        {
            get
            {
                if (_endPosition == null)
                {
                    _endPosition = _owner.GetTextPosition(_dcp + _cch, LogicalDirection.Backward);
                }
                return _endPosition;
            }
        }

        /// <summary>
        /// Character position representing the beginning of the Line's contents.
        /// </summary>
        internal override int StartPositionCP 
        { 
            get 
            { 
                return _dcp + _owner.Paragraph.ParagraphStartCharacterPosition; 
            } 
        }

        /// <summary>
        /// Character position representing the end of the Line's contents.
        /// </summary>
        internal override int EndPositionCP 
        { 
            get 
            { 
                return _dcp + _cch + _owner.Paragraph.ParagraphStartCharacterPosition; 
            } 
        }

        /// <summary>
        /// The bounding rectangle of the line.
        /// </summary>
        internal override Rect LayoutBox 
        { 
            get 
            { 
                return _layoutBox; 
            } 
        }

        /// <summary>
        /// The dominant baseline of the line. 
        /// Distance from the top of the line to the baseline.
        /// </summary>
        internal override double Baseline 
        { 
            get 
            { 
                return _baseline; 
            } 
        }

        #endregion LineResult Properties

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the line.</param>
        /// <param name="dcp">Index of the first character in the line.</param>
        /// <param name="cch">Number of all characters in the line.</param>
        /// <param name="layoutBox">Rectangle of the line within a text paragraph.</param>
        /// <param name="baseline">Distance from the top of the line to the baseline.</param>
        internal TextParaLineResult(TextParaClient owner, int dcp, int cch, Rect layoutBox, double baseline)
        {
            _owner = owner;
            _dcp = dcp;
            _cch = cch;
            _layoutBox = layoutBox;
            _baseline = baseline;
            _cchContent = _cchEllipses = -1;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Last character index in the line.
        /// </summary>
        internal int DcpLast
        {
            get 
            { 
                return _dcp + _cch; 
            }
            set 
            { 
                _cch = value - _dcp; 
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Ensure complex data in line
        /// </summary>
        private void EnsureComplexData()
        {
            if (_cchContent == -1)
            {
                _owner.GetLineDetails(_dcp, out _cchContent, out _cchEllipses);
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Owner of the line.
        /// </summary>
        private readonly TextParaClient _owner;

        /// <summary>
        /// Index of the first character in the line.
        /// </summary>
        private int _dcp;

        /// <summary>
        /// Number of all characters in the line.
        /// </summary>
        private int _cch;

        /// <summary>
        /// Rectangle of the line within a text paragraph.
        /// </summary>
        private readonly Rect _layoutBox;

        /// <summary>
        /// The dominant baseline of the line. Distance from the top of the 
        /// line to the baseline.
        /// </summary>
        private readonly double _baseline;

        /// <summary>
        /// ITextPointer representing the beginning of the Line's contents.
        /// </summary>
        private ITextPointer _startPosition;

        /// <summary>
        /// ITextPointer representing the end of the Line's contents.
        /// </summary>
        private ITextPointer _endPosition;

        /// <summary>
        /// Number of characters of content of the line, not including any line breaks.
        /// </summary>
        private int _cchContent;

        /// <summary>
        /// Number of characters hidden by ellipses.
        /// </summary>
        private int _cchEllipses;

        #endregion Private Fields
    }
}
