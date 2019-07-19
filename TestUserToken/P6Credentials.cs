using System;
using System.IdentityModel.Selectors;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Description;

namespace TestUserToken
{
    /// <summary>
    /// Class P6Credentials based on ClientCredentials
    /// </summary>
    public class P6Credentials : ClientCredentials
    {
        public X509Certificate2 TransportCertificate { get; set; }

        public P6Credentials()
        { }

        protected P6Credentials(P6Credentials cc)
            : base(cc)
        { }

        public override SecurityTokenManager CreateSecurityTokenManager()
        {
            return new P6SecurityTokenManager(this);
        }

        protected override ClientCredentials CloneCore()
        {
            return new P6Credentials(this);
        }

        public void SetTransportCertificate(String subjectName, StoreLocation storeLocation, StoreName storeName)
        {
            SetTransportCertificate(storeLocation, storeName, X509FindType.FindBySubjectDistinguishedName, subjectName);
        }

        public void SetTransportCertificate(StoreLocation storeLocation, StoreName storeName, X509FindType x509FindType, String subjectName)
        {
            TransportCertificate = FindCertificate(storeLocation, storeName, x509FindType, subjectName);
        }

        private static X509Certificate2 FindCertificate(StoreLocation location, StoreName name, X509FindType findType, String findValue)
        {
            var store = new X509Store(name, location);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var col = store.Certificates.Find(findType, findValue, true);
                return col[0]; // return first certificate found
            }
            finally
            {
                store.Close();
            }
        }

    }
}
