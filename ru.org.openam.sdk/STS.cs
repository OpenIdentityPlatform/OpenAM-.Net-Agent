using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Protocols.WSTrust;
using Microsoft.IdentityModel.SecurityTokenService;
using System.ServiceModel;
using System.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.WSTrust.Bindings;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Description;
using System.Net.Security;
using System.ServiceModel.Channels;
using System.ServiceModel.Security.Tokens;
using System.ServiceModel.Security;
using ru.org.openam.sdk.sts.fixWCF;

namespace ru.org.openam.sdk
{
    class STS
    {
        public static SecurityToken getToken(sts.Token token)
        {
            var textmessageEncoding = new TextMessageEncodingBindingElement();
            textmessageEncoding.WriteEncoding = Encoding.UTF8;
            textmessageEncoding.MessageVersion = MessageVersion.Soap12WSAddressing10;

            var messageSecurity = new AsymmetricSecurityBindingElement();
            messageSecurity.AllowSerializedSigningTokenOnReply = true;
            messageSecurity.MessageSecurityVersion = MessageSecurityVersion.WSSecurity10WSTrust13WSSecureConversation13WSSecurityPolicy12BasicSecurityProfile10;
            messageSecurity.MessageSecurityVersion = MessageSecurityVersion.WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
            messageSecurity.EnableUnsecuredResponse = true;
            messageSecurity.IncludeTimestamp = false;
            messageSecurity.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic128Rsa15;
            messageSecurity.SecurityHeaderLayout = SecurityHeaderLayout.Lax;
            messageSecurity.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
            messageSecurity.LocalClientSettings.DetectReplays = false;
            messageSecurity.LocalServiceSettings.DetectReplays = false;

            var x509SecurityParamter = new X509SecurityTokenParameters(X509KeyIdentifierClauseType.RawDataKeyIdentifier, SecurityTokenInclusionMode.AlwaysToInitiator);
            messageSecurity.RecipientTokenParameters = x509SecurityParamter;
            messageSecurity.RecipientTokenParameters.RequireDerivedKeys = false;

            var initiator = new X509SecurityTokenParameters(X509KeyIdentifierClauseType.RawDataKeyIdentifier, SecurityTokenInclusionMode.AlwaysToRecipient);
            initiator.RequireDerivedKeys = false;
            messageSecurity.InitiatorTokenParameters = initiator;
            
            var binding = new CustomBinding(messageSecurity, textmessageEncoding,new StrippingChannelBindingElement(), new HttpTransportBindingElement());
            WSTrustChannelFactory trustChannelFactory = new WSTrustChannelFactory(binding, new EndpointAddress(new Uri("http://login.staging.rapidsoft.ru:80/auth/sts"), EndpointIdentity.CreateDnsIdentity("test")));
            trustChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindByIssuerName, "test");
            trustChannelFactory.Credentials.ServiceCertificate.SetDefaultCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindByIssuerName, "test");
            trustChannelFactory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            trustChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindByIssuerName, "test");
            trustChannelFactory.Endpoint.Contract.ProtectionLevel = ProtectionLevel.None;
            trustChannelFactory.TrustVersion = System.ServiceModel.Security.TrustVersion.WSTrust13;
            
            WSTrustChannel channel = (WSTrustChannel)trustChannelFactory.CreateChannel();

            RequestSecurityToken rst = new RequestSecurityToken(RequestTypes.Issue);
            rst.OnBehalfOf = new Microsoft.IdentityModel.Tokens.SecurityTokenElement(token, new Microsoft.IdentityModel.Tokens.SecurityTokenHandlerCollection());
            rst.KeyType = WSTrust13Constants.KeyTypes.Asymmetric;

            RequestSecurityTokenResponse rstr = null;
            SecurityToken SecurityToken = channel.Issue(rst, out rstr);

            return SecurityToken;
        }
    }
}

