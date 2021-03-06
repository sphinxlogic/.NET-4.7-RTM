//-------------------------------------------------------------------------------------------------
// File: StartupEventArgs.cs
//
// Copyright (C) 2004 by Microsoft Corporation.  All rights reserved.
// 
// Description:
//          This event is fired when the application starts  - once that application?s Run() 
//          method has been called. 
//
//          The developer will typically hook this event if they want to take action at startup time 
//
// History:
//  08/10/04: kusumav   Moved out of Application.cs to its own separate file.
//  05/09/05: hamidm    Created StartupEventArgs.cs and renamed StartingUpCancelEventArgs to StartupEventArgs
//
//---------------------------------------------------------------------------

using System.ComponentModel;

using System.Windows.Interop;
using MS.Internal.PresentationFramework;
using System.Runtime.CompilerServices;
using MS.Internal;
using MS.Internal.AppModel; 

namespace System.Windows
{
    /// <summary>
    /// Event args for Startup event
    /// </summary>
    public class StartupEventArgs : EventArgs
    {
        /// <summary>
        /// constructor
        /// </summary>
        internal StartupEventArgs()
        {
            _performDefaultAction = true;
        }


        /// <summary>
        /// Command Line arguments
        /// </summary>
        public String[] Args
        {
            get
            {
                if (_args == null)
                {
                    _args = GetCmdLineArgs();
                }
                return _args;
            }
        }

        internal bool PerformDefaultAction
        {
            get { return _performDefaultAction; }
            set { _performDefaultAction = value; }
        }


        private string[] GetCmdLineArgs()
        {
            string[] retValue = null;

            if (!BrowserInteropHelper.IsBrowserHosted && 
                 ( ( Application.Current.MimeType != MimeType.Application ) 
                   || ! IsOnNetworkShareForDeployedApps() ))
            {
                string[] args = Environment.GetCommandLineArgs();
                Invariant.Assert(args.Length >= 1);

                int newLength = args.Length - 1;
                newLength = (newLength >=0 ? newLength : 0);
                
                retValue = new string[newLength];
                
                for (int i = 1; i < args.Length; i++)
                {
                    retValue[i-1] = args[i];
                }
            }
            else
            {
                retValue = new string[0];
            }

            return retValue;
        }
        
        //
        // Put this into a separate Method to avoid loading of this code at JIT time. 
        // 

        //
        // Explicitly tell the compiler that we don't want to be inlined. 
        // This will prevent loading of system.deployment unless we are a click-once app. 
        // 
        [MethodImplAttribute (MethodImplOptions.NoInlining )]
        private bool IsOnNetworkShareForDeployedApps()
        {
            return System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed ; 
        }
        
        private String[]    _args;
        private bool        _performDefaultAction;
    }
}
