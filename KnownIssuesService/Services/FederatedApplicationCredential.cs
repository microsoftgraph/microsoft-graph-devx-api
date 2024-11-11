using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace KnownIssuesService.Services
{
    /// <summary>
    /// TokenCredential implementation to use a managed identity's access token
    /// as a signed client assertion when acquiring tokens for a primary client
    /// application. The primary application must have a federated credential
    /// configured to allow the managed identity to exchange tokens.
    /// </summary>
    public sealed class FederatedApplicationCredential : TokenCredential
    {
        /// <summary>
        /// Constructor implementation for the federated application credential.
        /// </summary>
        /// <param name="tenantId">TenantId where you want to use the credential (not necessarily the home tenant).</param>
        /// <param name="msiClientId">ClientId for the managed identity.</param>
        /// <param name="appClientId">ClientId for the application registration.</param>
        [ExcludeFromCodeCoverage]
        public FederatedApplicationCredential(string tenantId, string msiClientId, string appClientId)
        {
            ManagedIdentity = new ManagedIdentityCredential(msiClientId);
            ClientAssertion = new ClientAssertionCredential(tenantId, appClientId, ComputeAssertionAsync);
        }

        [ExcludeFromCodeCoverage]
        private ManagedIdentityCredential ManagedIdentity
        {
            get;
        }

        [ExcludeFromCodeCoverage]
        private ClientAssertionCredential ClientAssertion
        {
            get;
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return ClientAssertion.GetToken(requestContext, cancellationToken);
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return await ClientAssertion.GetTokenAsync(requestContext, cancellationToken);
        }

        /// <summary>
        /// Get an exchange token from our managed identity to use as an assertion.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A signed assertion to authenticate with AzureAD.</returns>
        [ExcludeFromCodeCoverage]
        private async Task<string> ComputeAssertionAsync(CancellationToken cancellationToken)
        {
            TokenRequestContext msiContext = new(["api://AzureADTokenExchange/.default"]);
            AccessToken msiToken = await ManagedIdentity.GetTokenAsync(msiContext, cancellationToken);
            return msiToken.Token;
        }
    }
  }
