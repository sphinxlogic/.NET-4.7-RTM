//------------------------------------------------------------------------------
//  Microsoft Avalon
//  Copyright (c) Microsoft Corporation, 2003
//
//  File:       VisualDrawingContext.cs
//
//  History: 
//      Microsoft: 04/19/2003
//          Created it based on the DrawingVisualDrawingContext used in the AvPhat branch.
//      Microsoft: 07/02/2003
//          Renamed to RetainedDrawingContext, which derives from DrawingContext
//      Microsoft: 07/16/2003
//          Renamed again to DrawingVisualDrawingContext, which derives from RenderDataDrawingContext
//
//------------------------------------------------------------------------------

using System;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Threading;

using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Diagnostics;
using MS.Internal;

namespace System.Windows.Media
{
    /// <summary>
    /// VisualDrawingContext - the DrawingContext for Visuals that can create it.
    /// </summary>
    internal class VisualDrawingContext : RenderDataDrawingContext
    {
        #region Constructors
        
        /// <summary>
        /// Creates a drawing context for a DrawingVisual.
        /// The Visual must not be null.
        /// </summary>
        /// <param name="ownerVisual"> The Visual that created the DrawingContext, which must not be null. </param>
        internal VisualDrawingContext(
            Visual ownerVisual
            )
        {
            Debug.Assert(null != ownerVisual);

            _ownerVisual = ownerVisual;
        }

        #endregion Constructors

        #region Protected Methods

        /// <summary>
        /// CloseCore - Implemented be derivees to Close the context.
        /// This will only be called once (if ever) per instance.
        /// </summary>
        /// <param name="renderData"> The render data produced by this RenderDataDrawingContext.  </param>
        protected override void CloseCore(RenderData renderData)
        {
            Debug.Assert(null != _ownerVisual);
                
            _ownerVisual.RenderClose(renderData);

#if DEBUG
            _ownerVisual = null;
#endif
        }

        #endregion Protected Methods

        #region Private Fields

        private Visual _ownerVisual; 

        #endregion Private Fields
    }
}
