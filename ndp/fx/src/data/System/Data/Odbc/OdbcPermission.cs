//------------------------------------------------------------------------------
// <copyright file="OdbcPermission.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------
namespace System.Data.Odbc {

    using System;
    using System.Collections;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;

    [Serializable] 
    public sealed class OdbcPermission :  DBDataPermission {

        [ Obsolete("OdbcPermission() has been deprecated.  Use the OdbcPermission(PermissionState.None) constructor.  http://go.microsoft.com/fwlink/?linkid=14202", true) ] // MDAC 86034
        public OdbcPermission() : this(PermissionState.None) {
        }

        public OdbcPermission(PermissionState state) : base(state) {
        }

        [ Obsolete("OdbcPermission(PermissionState state, Boolean allowBlankPassword) has been deprecated.  Use the OdbcPermission(PermissionState.None) constructor.  http://go.microsoft.com/fwlink/?linkid=14202", true) ] // MDAC 86034
        public OdbcPermission(PermissionState state, bool allowBlankPassword) : this(state) {
            AllowBlankPassword = allowBlankPassword;
        }

        private OdbcPermission(OdbcPermission permission) : base(permission) { // for Copy
        }

        internal OdbcPermission(OdbcPermissionAttribute permissionAttribute) : base(permissionAttribute) { // for CreatePermission
        }

        internal OdbcPermission(OdbcConnectionString constr) : base(constr) { // for Open
            if ((null == constr) || constr.IsEmpty) {
                base.Add(ADP.StrEmpty, ADP.StrEmpty, KeyRestrictionBehavior.AllowOnly);
            }
        }

        public override void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior) {
            DBConnectionString constr = new DBConnectionString(connectionString, restrictions, behavior, null, true);
            AddPermissionEntry(constr);
        }

        override public IPermission Copy () {
            return new OdbcPermission(this);
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )]
    [Serializable] 
    public sealed class OdbcPermissionAttribute : DBDataPermissionAttribute {

        public OdbcPermissionAttribute(SecurityAction action) : base(action) {
        }

        override public IPermission CreatePermission() {
            return new OdbcPermission(this);
        }
    }
}
