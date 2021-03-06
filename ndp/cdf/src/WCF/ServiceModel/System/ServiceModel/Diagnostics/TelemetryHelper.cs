// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System.ServiceModel
{
    using System;
    using System.Collections.Generic;
    using System.Runtime;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Permissions;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;
    using System.Threading;
    using Microsoft.Win32;

    /// <summary>
    /// Helper to get NetFx version details
    /// </summary>
    internal class TelemetryHelper
    {
        public string GetHostType()
        {
            return AspNetEnvironment.Enabled ? "IISHosted" : "SelfHosted";
        }

        public string GetEndpoints(ServiceDescription description)
        {
            string endpoints = string.Empty;
            if (description != null)
            {
                List<string> list = new List<string>();
                foreach (ServiceEndpoint endpoint in description.Endpoints)
                {
                    if (endpoint != null && endpoint.Binding != null)
                    {
                        list.Add(GetDetails(endpoint.Binding));
                    }
                }

                endpoints = string.Join(";", list);
            }

            return endpoints;
        }

        public string GetServiceId(ServiceDescription description)
        {
            // since ServiceDescription.ConfigurationName can contain customer service class name,
            // which can be treated as PII data, hash it before log into telemetry ETW. 
            // need non-randomized hashcode since we want to get the same hashcode with the same ConfigrationName
            return StringUtil.GetNonRandomizedHashCode(description.ConfigurationName).ToString();
        }

        public string GetAssemblyVersion()
        {
            return ThisAssembly.InformationalVersion;
        }

        private static string GetDetails(Channels.Binding binding)
        {
            string mode = null;
            string credentialType = null;
            string name = null;

            try
            {
                if (binding is HttpBindingBase)
                {
                    GetHttpBindingBaseDetails((HttpBindingBase)binding, ref name, ref mode, ref credentialType);
                }
                else if (binding is MsmqIntegration.MsmqIntegrationBinding)
                {
                    GetMsmqIntegrationBindingDetails((MsmqIntegration.MsmqIntegrationBinding)binding, ref name, ref mode, ref credentialType);
                }
                else if (binding is NetMsmqBinding)
                {
                    GetNetMsmqBindingDetails((NetMsmqBinding)binding, ref name, ref mode, ref credentialType);
                }
                else if (binding is NetNamedPipeBinding)
                {
                    GetNetNamedPipeBindingDetails((NetNamedPipeBinding)binding, ref name, ref mode, ref credentialType);
                }
                else if (binding is NetTcpBinding) // NetTcpContextBinding
                {
                    GetNetTcpBindingDetails((NetTcpBinding)binding, ref name, ref mode, ref credentialType);
                }
                else if (binding is WSDualHttpBinding)
                {
                    GetWSDualHttpBindingDetails((WSDualHttpBinding)binding, ref name, ref mode, ref credentialType);
                }
                else if (binding is WSFederationHttpBinding) // WS2007FederationHttpBinding
                {
                    GetWSFederationHttpBindingDetails((WSFederationHttpBinding)binding, ref name, ref mode, ref credentialType);
                }
                else if (binding is WSHttpBinding) // WS2007HttpBinding, WSHttpContextBinding
                {
                    GetWSHttpBindingDetails((WSHttpBinding)binding, ref name, ref mode, ref credentialType);
                }
                else if (binding is Channels.CustomBinding)
                {
                    GetCustomBindingDetails((Channels.CustomBinding)binding, ref name, ref mode, ref credentialType);
                }
#pragma warning disable CS0618 
                else if (binding is NetPeerTcpBinding)  // depracated
                {
                    GetNetPeerTcpBindingDetails((NetPeerTcpBinding)binding, ref name, ref mode, ref credentialType);
                }
#pragma warning restore CS0618 
                else
                {
                    // Only dump the binding name with a known list while not known type, no details.
                    name = IsKnownType(binding) ? binding.GetType().Name : "UserBinding";
                }
            }
            catch(Exception e)
            {
                // Make sure we won't affect customer logic by collecting telemetry data

                if (Fx.IsFatal(e))
                {
                    throw;
                }

                DiagnosticUtility.TraceHandledException(e, System.Diagnostics.TraceEventType.Warning);
            }

            return $"{binding.Scheme}:{name ?? "unknown"}:{mode ?? "unknown"}:{credentialType ?? "unknown"}";
        }

        private static void GetHttpBindingBaseDetails(HttpBindingBase binding, ref string name, ref string mode, ref string credentialType)
        {
            if (binding is BasicHttpContextBinding)
            {
                name = GetBindingName<BasicHttpContextBinding>(binding);
            }
            else if (binding is BasicHttpBinding)
            {
                name = GetBindingName<BasicHttpBinding>(binding);
            }
            else if (binding is NetHttpBinding)
            {
                name = GetBindingName<NetHttpBinding>(binding);
            }
            else if (binding is NetHttpsBinding)
            {
                name = GetBindingName<NetHttpsBinding>(binding);
            }
            else if (binding is BasicHttpsBinding)
            {
                name = GetBindingName<BasicHttpsBinding>(binding);
            }
            else
            {
                name = GetBindingName<HttpBindingBase>(binding);
            }

            BasicHttpSecurity basicHttpSecurity = binding.BasicHttpSecurity;
            mode = basicHttpSecurity?.Mode.ToString();
            switch (basicHttpSecurity?.Mode)
            {
                case BasicHttpSecurityMode.None:
                    credentialType = "N/A";
                    break;
                case BasicHttpSecurityMode.Transport:
                case BasicHttpSecurityMode.TransportCredentialOnly:
                    credentialType = basicHttpSecurity.Transport?.ClientCredentialType.ToString();
                    break;
                case BasicHttpSecurityMode.Message:
                case BasicHttpSecurityMode.TransportWithMessageCredential:
                    credentialType = $"{basicHttpSecurity.Transport?.ClientCredentialType.ToString()}+{basicHttpSecurity.Message?.ClientCredentialType.ToString()}";
                    break;
            }
        }

        private static void GetMsmqIntegrationBindingDetails(MsmqIntegration.MsmqIntegrationBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            name = GetBindingName<MsmqIntegration.MsmqIntegrationBinding>(binding);

            MsmqIntegration.MsmqIntegrationSecurity msmqIntegrationSecurity = ((MsmqIntegration.MsmqIntegrationBinding)binding).Security;
            mode = msmqIntegrationSecurity?.Mode.ToString();
            switch (msmqIntegrationSecurity?.Mode)
            {
                case MsmqIntegration.MsmqIntegrationSecurityMode.None:
                    credentialType = "N/A";
                    break;
                case MsmqIntegration.MsmqIntegrationSecurityMode.Transport:
                    credentialType = msmqIntegrationSecurity.Transport?.MsmqAuthenticationMode.ToString();
                    break;
                    // No message mode
            }
        }

        private static void GetNetMsmqBindingDetails(NetMsmqBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            name = GetBindingName<NetMsmqBinding>(binding);

            NetMsmqSecurity netMsmqSecurity = binding.Security;
            mode = netMsmqSecurity?.Mode.ToString();
            switch (netMsmqSecurity?.Mode)
            {
                case NetMsmqSecurityMode.None:
                    credentialType = "N/A";
                    break;
                case NetMsmqSecurityMode.Transport:
                case NetMsmqSecurityMode.Message:
                case NetMsmqSecurityMode.Both:
                    credentialType = $"{netMsmqSecurity.Transport?.MsmqAuthenticationMode.ToString()}+{netMsmqSecurity.Message?.ClientCredentialType.ToString()}";
                    break;
            }
        }

        private static void GetNetNamedPipeBindingDetails(NetNamedPipeBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            name = GetBindingName<NetNamedPipeBinding>(binding);

            NetNamedPipeSecurity netNamedPipeSecurity = binding.Security;
            mode = netNamedPipeSecurity?.ToString();
            switch (netNamedPipeSecurity?.Mode)
            {
                case NetNamedPipeSecurityMode.None:
                    credentialType = "N/A";
                    break;
                case NetNamedPipeSecurityMode.Transport:
                    credentialType = netNamedPipeSecurity.Transport?.ProtectionLevel.ToString();
                    break;
                    // No message mode
            }
        }

        private static void GetNetTcpBindingDetails(NetTcpBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            if (binding is NetTcpContextBinding)
            {
                name = GetBindingName<NetTcpContextBinding>(binding);
            }
            else
            {
                name = GetBindingName<NetTcpBinding>(binding);
            }

            NetTcpSecurity netTcpSecurity = binding.Security;
            mode = netTcpSecurity?.Mode.ToString();
            switch (netTcpSecurity?.Mode)
            {
                case SecurityMode.None:
                    credentialType = "N/A";
                    break;
                case SecurityMode.Transport:
                    credentialType = netTcpSecurity.Transport?.ClientCredentialType.ToString();
                    break;
                case SecurityMode.Message:
                    credentialType = netTcpSecurity.Message?.ClientCredentialType.ToString();
                    break;
                case SecurityMode.TransportWithMessageCredential:
                    credentialType = $"{netTcpSecurity.Transport?.ClientCredentialType.ToString()}+{netTcpSecurity.Message?.ClientCredentialType.ToString()}";
                    break;
            }
        }

        private static void GetWSDualHttpBindingDetails(WSDualHttpBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            name = GetBindingName<WSDualHttpBinding>(binding);

            WSDualHttpSecurity wSDualHttpSecurity = binding.Security;
            mode = wSDualHttpSecurity?.Mode.ToString();
            switch (wSDualHttpSecurity?.Mode)
            {
                case WSDualHttpSecurityMode.None:
                    credentialType = "N/A";
                    break;
                case WSDualHttpSecurityMode.Message:
                    credentialType = wSDualHttpSecurity.Message?.ClientCredentialType.ToString();
                    break;
                    // No Transport mode
            }
        }

        private static void GetWSFederationHttpBindingDetails(WSFederationHttpBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            if (binding is WS2007FederationHttpBinding)
            {
                name = GetBindingName<WS2007FederationHttpBinding>(binding);
            }
            else
            {
                name = GetBindingName<WSFederationHttpBinding>(binding);
            }

            WSFederationHttpSecurity wSFederationHttpSecurity = binding.Security;
            mode = wSFederationHttpSecurity?.Mode.ToString();
            switch (wSFederationHttpSecurity?.Mode)
            {
                case WSFederationHttpSecurityMode.None:
                    credentialType = "N/A";
                    break;
                case WSFederationHttpSecurityMode.Message:
                case WSFederationHttpSecurityMode.TransportWithMessageCredential:
                    credentialType = wSFederationHttpSecurity.Message?.IssuedTokenType ?? "null";
                    break;
            }
        }

        private static void GetWSHttpBindingDetails(WSHttpBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            if (binding is WSHttpContextBinding)
            {
                name = GetBindingName<WSHttpContextBinding>(binding);
            }
            else if (binding is WS2007HttpBinding)
            {
                name = GetBindingName<WS2007HttpBinding>(binding);
            }
            else
            {
                name = GetBindingName<WSHttpBinding>(binding);
            }

            WSHttpSecurity wSHttpSecurity = binding.Security;
            mode = wSHttpSecurity?.Mode.ToString();
            switch (wSHttpSecurity?.Mode)
            {
                case SecurityMode.None:
                    credentialType = "N/A";
                    break;
                case SecurityMode.Transport:
                    credentialType = wSHttpSecurity.Transport?.ClientCredentialType.ToString();
                    break;
                case SecurityMode.Message:
                case SecurityMode.TransportWithMessageCredential:
                    credentialType = $"{wSHttpSecurity.Transport?.ClientCredentialType.ToString()}+{wSHttpSecurity.Message?.ClientCredentialType.ToString()}";
                    break;
            }
        }

        private static void GetCustomBindingDetails(Channels.CustomBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            name = GetBindingName<Channels.CustomBinding>(binding);

            Text.StringBuilder sb = new Text.StringBuilder();
            foreach (Channels.BindingElement element in binding.Elements)
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }

                // Only dump the name with the known binding elements
                sb.Append(IsKnownType(element) ? (element.GetType().Name) : "UserBindingElement");
            }
            mode = sb.ToString();
        }

#pragma warning disable CS0618 
        private static void GetNetPeerTcpBindingDetails(NetPeerTcpBinding binding, ref string name, ref string mode, ref string credentialType)
        {
            name = GetBindingName<NetPeerTcpBinding>(binding);

            PeerSecuritySettings peerSecuritySettings = binding.Security;
            mode = peerSecuritySettings?.Mode.ToString();
            switch (peerSecuritySettings?.Mode)
            {
                case SecurityMode.None:
                    credentialType = "N/A";
                    break;
                case SecurityMode.Transport:
                case SecurityMode.TransportWithMessageCredential:
                case SecurityMode.Message:
                    credentialType = peerSecuritySettings.Transport?.CredentialType.ToString();
                    break;
            }
        }
#pragma warning restore CS0618 

        #region Helper for GDPR

        // To avoid GDPR issue, this function will check if the binding type name is a known type name 
        // For user type, we would append "*" at the end of known type name instead of using user type name directly
        private static string GetBindingName<T>(Channels.Binding binding) where T : Channels.Binding
        {
            string name = typeof(T).Name;
            return binding.GetType().Name == name ? name : $"{name}*";
        }

        // Here are the known Microsoft public key tokens for .Net framework and we use them to indentify Microsoft assembly 
        private readonly static byte[][] knownTokens = {
            new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 },
            new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 },
            new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }
        };

        private static bool IsKnownType(object obj)
        {
            byte[] objToken = obj.GetType().Assembly.GetName().GetPublicKeyToken();
            if ((objToken != null) && (objToken.Length == 8))
            {
                foreach (byte[] knownToken in knownTokens)
                {
                    if (((Collections.IStructuralEquatable)objToken).Equals(knownToken, EqualityComparer<byte>.Default))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

#endregion // Helper for GDPR
    }
}
