// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>Microsoft</OWNER>
// 
//
// RoleClaimProvider.cs
//

namespace System.Security.Claims
{
    using System.Collections.Generic;

    /// <summary>
    /// This internal class is used to wrap role claims that can be set on GenericPrincipal.  They need to be kept distinct from other claims.
    /// ClaimsIdentity has a property the holds this type.  Since it is internal, few checks are
    /// made on parameters.
    /// </summary>    

    [System.Runtime.InteropServices.ComVisible(false)]
    internal class RoleClaimProvider
    {
        string m_issuer;
        string[] m_roles;
        ClaimsIdentity m_subject;

        public RoleClaimProvider(string issuer, string[] roles, ClaimsIdentity subject)
        {
            m_issuer = issuer;
            m_roles = roles;
            m_subject = subject;
        }

        public IEnumerable<Claim> Claims
        {
            get
            {
                for (int i = 0; i < m_roles.Length; i++)
                {
                    if (m_roles[i] != null)
                    {
                        yield return new Claim(m_subject.RoleClaimType, m_roles[i], ClaimValueTypes.String, m_issuer, m_issuer, m_subject);
                    }
                }
            }
        }
    }

}
