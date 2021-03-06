//------------------------------------------------------------------------------
// <copyright file="XmlDownloadManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml {

    using System;
    using System.IO;
    using System.Security;
    using System.Collections;
    using System.Net;
    using System.Net.Cache;
    using System.Runtime.Versioning;

//
// XmlDownloadManager
//
    internal partial class XmlDownloadManager {

        Hashtable connections;

        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        internal Stream GetStream(Uri uri, ICredentials credentials, IWebProxy proxy, 
            RequestCachePolicy cachePolicy) {
            if ( uri.Scheme == "file" ) {
                return new FileStream( uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 );
            }
            else {
                return GetNonFileStream( uri, credentials, proxy, cachePolicy );
            }
        }

        private Stream GetNonFileStream( Uri uri, ICredentials credentials, IWebProxy proxy, 
            RequestCachePolicy cachePolicy ) {
            WebRequest req = WebRequest.Create( uri );
            if ( credentials != null ) {
                req.Credentials = credentials;
            }
            if ( proxy != null ) {
                req.Proxy = proxy;
            }
            if ( cachePolicy != null ) {
                req.CachePolicy = cachePolicy;
            }
            WebResponse resp = req.GetResponse();
            HttpWebRequest webReq = req as HttpWebRequest;
            if ( webReq != null ) {
                lock ( this ) {
                    if ( connections == null ) {
                        connections = new Hashtable();
                    }
                    OpenedHost openedHost = (OpenedHost)connections[webReq.Address.Host];
                    if ( openedHost == null ) {
                        openedHost = new OpenedHost();
                    }

                    if ( openedHost.nonCachedConnectionsCount < webReq.ServicePoint.ConnectionLimit - 1 ) {
                        // we are not close to connection limit -> don't cache the stream
                        if ( openedHost.nonCachedConnectionsCount == 0 ) {
                            connections.Add( webReq.Address.Host, openedHost );
                        }
                        openedHost.nonCachedConnectionsCount++;
                        return new XmlRegisteredNonCachedStream( resp.GetResponseStream(), this, webReq.Address.Host );
                    }
                    else {
                        // cache the stream and save the connection for the next request
                        return new XmlCachedStream( resp.ResponseUri, resp.GetResponseStream() );
                    }
                }
            }
            else {
                return resp.GetResponseStream();
            }
        }

        internal void Remove( string host ) {
            lock ( this ) {
                OpenedHost openedHost = (OpenedHost)connections[host];
                if ( openedHost != null ) {
                    if ( --openedHost.nonCachedConnectionsCount == 0 ) {
                        connections.Remove( host );
                    }
                }
            }
        }
    }

//
// OpenedHost
//
    internal class OpenedHost {
        internal int nonCachedConnectionsCount;
    }

//
// XmlRegisteredNonCachedStream
//
    internal class XmlRegisteredNonCachedStream : Stream {
        protected Stream stream;
        XmlDownloadManager downloadManager;
        string host;

        internal XmlRegisteredNonCachedStream( Stream stream, XmlDownloadManager downloadManager, string host ) {
            this.stream = stream;
            this.downloadManager = downloadManager;
            this.host = host;
        }

        ~XmlRegisteredNonCachedStream() {
            if ( downloadManager != null ) {
                downloadManager.Remove( host );
            }
            stream = null;
            // The base class, Stream, provides its own finalizer
        } 

        protected override void Dispose( bool disposing ) {
            try {
                if ( disposing && stream != null ) {
                    if ( downloadManager != null ) {
                        downloadManager.Remove( host );
                    }
                    stream.Close();
                }
                stream = null;
                GC.SuppressFinalize( this ); // do not call finalizer
            }
            finally {
                base.Dispose( disposing );
            }
        }

        //
        // Stream
        //
        public override IAsyncResult BeginRead( byte[] buffer, int offset, int count, AsyncCallback callback, object state ) {
            return stream.BeginRead( buffer, offset, count, callback, state );
        }

        public override IAsyncResult BeginWrite( byte[] buffer, int offset, int count, AsyncCallback callback, object state ) {
            return stream.BeginWrite( buffer, offset, count, callback, state );
        }

        public override int EndRead( IAsyncResult asyncResult ) {
            return stream.EndRead( asyncResult );
        }

        public override void EndWrite( IAsyncResult asyncResult ) {
            stream.EndWrite( asyncResult );
        }

        public override void Flush() {
            stream.Flush();
        }

        public override int Read( byte[] buffer, int offset, int count ) {
            return stream.Read( buffer, offset, count );
        }

        public override int ReadByte() {
            return stream.ReadByte();
        }

        public override long Seek( long offset, SeekOrigin origin ) {
            return stream.Seek( offset, origin );
        }

        public override void SetLength( long value ) {
            stream.SetLength( value );
        }

        public override void Write( byte[] buffer, int offset, int count ) {
            stream.Write( buffer, offset, count );
        }

        public override void WriteByte( byte value ) {
            stream.WriteByte( value );
        }

        public override Boolean CanRead {
            get { return stream.CanRead; }
        }

        public override Boolean CanSeek {
            get { return stream.CanSeek; }
        }

        public override Boolean CanWrite {
            get { return stream.CanWrite; }
        }

        public override long Length {
            get { return stream.Length; }
        }

        public override long Position {
            get { return stream.Position; }
            set { stream.Position = value; }
        }
    }

//
// XmlCachedStream
//
    internal class XmlCachedStream : MemoryStream {
        private const int MoveBufferSize = 4096;

        private Uri uri;

        internal XmlCachedStream( Uri uri, Stream stream ) 
            : base() {

            this.uri = uri;

            try {
                byte[] bytes = new byte[MoveBufferSize];
                int read = 0;
                while ( ( read = stream.Read( bytes, 0, MoveBufferSize ) ) > 0 ) {
                    this.Write( bytes, 0, read );
                }
                base.Position = 0;
            }
            finally {
                stream.Close();
            }
        }
    }
}
