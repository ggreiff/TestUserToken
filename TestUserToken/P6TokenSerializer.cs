using System;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.ServiceModel.Security;
using System.Text;

namespace TestUserToken
{
    /// <summary>
    /// Class P6TokenSerializer based on WSSecurityTokenSerializer
    /// </summary>
    public class P6TokenSerializer : WSSecurityTokenSerializer
    {
        public P6TokenSerializer(SecurityVersion sv)
            : base(sv)
        { }

        /// <summary>
        /// Writes the specified security token using the specified XML writer -- Nonce and Created timestamp. Called by the base class.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Xml.XmlWriter" /> to write the security token.</param>
        /// <param name="token">A <see cref="T:System.IdentityModel.Tokens.SecurityToken" /> that represents the security token to write.</param>
        protected override void WriteTokenCore(System.Xml.XmlWriter writer, SecurityToken token)
        {
            const string tokennamespace = "o";

            var userToken = token as UserNameSecurityToken;
            if (userToken == null || writer == null) return;

            var password = userToken.Password;
            var created = DateTime.UtcNow;
            var exprires = created.AddMinutes(1);
            var createdStr = created.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var expriredStr = exprires.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            //
            // generate unique Nonce value and encoded
            //
            var phrase = Guid.NewGuid().ToString();
            var nonce = GetSha1String(phrase);

            //
            // Write out our header with Nonce and created string.
            //
            var sb = new StringBuilder();
            sb.Append(String.Format("<u:Timestamp xmlns:u='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd' u:Id='{0}'>", token.Id)); 
            sb.Append(String.Format("<u:Created>{0}</u:Created>", createdStr));
            sb.Append(String.Format("<u:Expires>{0}</u:Expires></u:Timestamp>", expriredStr));

            sb.Append(String.Format("<{0}:UsernameToken u:Id='{1}' xmlns:u='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd'>", tokennamespace, token.Id));
            sb.Append(String.Format("<{0}:Username>{1}</{0}:Username>", tokennamespace, userToken.UserName));
            sb.Append(String.Format("<{0}:Password Type='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText'>{1}</{0}:Password>", tokennamespace, password));
            sb.Append(String.Format("<{0}:Nonce EncodingType='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary'>{1}</{0}:Nonce>",tokennamespace,nonce));
            sb.Append(String.Format("<u:Created>{0}</u:Created></{1}:UsernameToken>", createdStr, tokennamespace));
            writer.WriteRaw(sb.ToString());
        }

        /// <summary>
        /// Gets the sha1 value for a string.
        /// </summary>
        /// <param name="phrase">The phrase.</param>
        /// <returns>System.String.</returns>
        protected string GetSha1String(string phrase)
        {
            var sha1Hasher = new SHA1CryptoServiceProvider();
            var hashedDataBytes = sha1Hasher.ComputeHash(Encoding.UTF8.GetBytes(phrase));
            return Convert.ToBase64String(hashedDataBytes);
        }

    }
}
