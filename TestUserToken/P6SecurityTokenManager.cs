using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.ServiceModel;
using System.ServiceModel.Security.Tokens;

namespace TestUserToken
{
    /// <summary>
    /// Class P6SecurityTokenManager base on ClientCredentialsSecurityTokenManager
    /// </summary>
    public class P6SecurityTokenManager : ClientCredentialsSecurityTokenManager
    {
        private P6Credentials p6Credentials;

        public P6SecurityTokenManager(P6Credentials cred)
            : base(cred)
        {
            p6Credentials = cred;
        }

        public override SecurityTokenProvider CreateSecurityTokenProvider(SecurityTokenRequirement requirement)
        {
            if (requirement.Properties.ContainsKey(ServiceModelSecurityTokenRequirement.TransportSchemeProperty) && requirement.TokenType == SecurityTokenTypes.X509Certificate)
                return new X509SecurityTokenProvider(p6Credentials.TransportCertificate);

            if (requirement.KeyUsage == SecurityKeyUsage.Signature && requirement.TokenType == SecurityTokenTypes.X509Certificate)
                return new X509SecurityTokenProvider(p6Credentials.ClientCertificate.Certificate);

            return base.CreateSecurityTokenProvider(requirement);
        }


        public override SecurityTokenSerializer CreateSecurityTokenSerializer(SecurityTokenVersion version)
        {
            return new P6TokenSerializer(System.ServiceModel.Security.SecurityVersion.WSSecurity11);
        }
    }
}
