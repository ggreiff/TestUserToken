using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;

namespace TestUserToken
{

    public class P6BindingCredentials
    {
        public CustomBinding Binding { get; set; }
        public P6Credentials Credentials { get; set; }


        public P6BindingCredentials(String username, String password)
        {


            //
            // Define our P6 P6Credentials
            //
            var customCredentials = new P6Credentials();
            customCredentials.UserName.UserName = username;
            customCredentials.UserName.Password = password;
            Credentials = customCredentials;

            //
            // Create our security that we will use in the binding
            // Special Sauce = WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10
            //
            var security = SecurityBindingElement.CreateMutualCertificateBindingElement();
            security.IncludeTimestamp = true;
            security.AllowInsecureTransport = true;
            security.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic256;
            security.MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
            security.EnableUnsecuredResponse = true;
            


            //
            // Soap encoding and allow 20 meg responses
            //
            var encoding = new TextMessageEncodingBindingElement {MessageVersion = MessageVersion.Soap11};
            var transport = new HttpsTransportBindingElement {MaxReceivedMessageSize = 20000000};

            /*
            var transportSecurityElement = new TransportSecurityBindingElement();
            transportSecurityElement.AllowInsecureTransport = true;
            transportSecurityElement.IncludeTimestamp = true;
            transportSecurityElement.MessageSecurityVersion = MessageSecurityVersion.WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
            transportSecurityElement.
            transportSecurityElement.EndpointSupportingTokenParameters.SetKeyDerivation(false);
            */

            //
            // Define our binding and allow up to 2 mins for P6WS to get back to us.
            //
            var binding = new CustomBinding();
            binding.Elements.Add(security);
            binding.Elements.Add(encoding);
            binding.Elements.Add(transport);
            binding.SendTimeout = new TimeSpan(0, 2, 0);
            Binding = binding;

        }
    }
}
