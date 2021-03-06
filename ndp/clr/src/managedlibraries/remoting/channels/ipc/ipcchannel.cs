// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//===========================================================================
//  File:       IpcChannel.cs
//  Author:   Microsoft@Microsoft.Com
//  Summary:    Implements a combined ipc channel 
//
//==========================================================================

using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Globalization;
using System.Security.AccessControl;
using System.Security.Permissions;


namespace System.Runtime.Remoting.Channels.Ipc
{

    public class IpcChannel : IChannelReceiver, IChannelSender, ISecurableChannel
    {
        private IpcClientChannel  _clientChannel = null; // client channel
        private IpcServerChannel  _serverChannel = null; // server channel
    
        private int    _channelPriority = 20;  // channel priority
        private String _channelName = "ipc"; // channel name


        public IpcChannel()
        {
            _clientChannel = new IpcClientChannel();
            // server channel will not be activated.
        } // IpcChannel

        public IpcChannel(String portName) : this()
        {
            _serverChannel = new IpcServerChannel(portName);
        } // IpcChannel

        public IpcChannel(IDictionary properties,
                          IClientChannelSinkProvider clientSinkProvider,
                          IServerChannelSinkProvider serverSinkProvider)
            : this(properties, clientSinkProvider, serverSinkProvider, null)
        {
        }

        public IpcChannel(IDictionary properties, 
                          IClientChannelSinkProvider clientSinkProvider,
                          IServerChannelSinkProvider serverSinkProvider,
                          CommonSecurityDescriptor securityDescriptor)
        {
            Hashtable clientData = new Hashtable();
            Hashtable serverData = new Hashtable();

            bool portFound = false;
        
            // divide properties up for respective channels
            if (properties != null)
            {
                foreach (DictionaryEntry entry in properties)
                {
                    switch ((String)entry.Key)
                    {
                    // general channel properties
                    case "name": _channelName = (String)entry.Value; break;
                    case "priority": _channelPriority = Convert.ToInt32((String)entry.Value, CultureInfo.InvariantCulture); break;
                    case "portName": 
                    {
                        serverData["portName"] = entry.Value; 
                        portFound = true;
                        break;
                    }

                    default: 
                            clientData[entry.Key] = entry.Value;
                            serverData[entry.Key] = entry.Value;
                            break;
                    }
                }                    
            }

            _clientChannel = new IpcClientChannel(clientData, clientSinkProvider);

            if (portFound)
                _serverChannel = new IpcServerChannel(serverData, serverSinkProvider, securityDescriptor);
        } // IpcChannel


        //
        // ISecurableChannel implementation
        //
        public bool IsSecured
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
            get {
                if (_clientChannel != null)
                    return _clientChannel.IsSecured;
                if (_serverChannel != null)
                    return _serverChannel.IsSecured;
                return false;
            }
            [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
            set {
                if (((IList)ChannelServices.RegisteredChannels).Contains(this))
                    throw new InvalidOperationException(CoreChannel.GetResourceString("Remoting_InvalidOperation_IsSecuredCannotBeChangedOnRegisteredChannels"));
                if (_clientChannel != null)
                    _clientChannel.IsSecured = value;
                if (_serverChannel != null)
                    _serverChannel.IsSecured = value;
            }    
        }

        // 
        // IChannel implementation
        //

        public int ChannelPriority
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
            get { return _channelPriority; }    
        } // ChannelPriority

        public String ChannelName
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
            get { return _channelName; }
        } // ChannelName

        // returns channelURI and places object uri into out parameter
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
        public String Parse(String url, out String objectURI)
        {            
            return IpcChannelHelper.ParseURL(url, out objectURI);
        } // Parse
        
        //
        // end of IChannel implementation
        //


        //
        // IChannelSender implementation
        //

        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
        public IMessageSink CreateMessageSink(String url, Object remoteChannelData, 
                                                      out String objectURI)
        {
            return _clientChannel.CreateMessageSink(url, remoteChannelData, out objectURI);
        } // CreateMessageSink

        //
        // end of IChannelSender implementation
        //


        //
        // IChannelReceiver implementation
        //

        public Object ChannelData
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
            get 
            {
                if (_serverChannel != null)
                    return _serverChannel.ChannelData;
                else
                    return null;
            }
        } // ChannelData
      
                
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
        public String[] GetUrlsForUri(String objectURI)
        {
            if (_serverChannel != null)
                return _serverChannel.GetUrlsForUri(objectURI);
            else
                return null;
        } // GetUrlsforURI

        
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
        public void StartListening(Object data)
        {
            if (_serverChannel != null)
                _serverChannel.StartListening(data);
        } // StartListening


        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure, Infrastructure=true)]
        public void StopListening(Object data)
        {
            if (_serverChannel != null)
                _serverChannel.StopListening(data);
        } // StopListening

        //
        // IChannelReceiver implementation
        //

    
    } // class IpcChannel


} // namespace System.Runtime.Remoting.Channels.Ipc


