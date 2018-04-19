using System;
using System.Threading;
using System.Threading.Tasks;
using Manatee.Trello.Internal.DataAccess;
using Manatee.Trello.Internal.Validation;
using Manatee.Trello.Json;

namespace Manatee.Trello
{
	/// <summary>
	/// A collection of organizations.
	/// </summary>
	public class OrganizationCollection : ReadOnlyOrganizationCollection, IOrganizationCollection
	{
		internal OrganizationCollection(Func<string> getOwnerId, TrelloAuthorization auth)
			: base(getOwnerId, auth) {}

		/// <summary>
		/// Creates a new organization.
		/// </summary>
		/// <param name="displayName">The display name of the organization to add.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		/// <returns>The <see cref="IOrganization"/> generated by Trello.</returns>
		/// <remarks>The organization's name will be derived from the display name and can be changed later.</remarks>
		public async Task<IOrganization> Add(string displayName, CancellationToken ct = default(CancellationToken))
		{
			var error = NotNullOrWhiteSpaceRule.Instance.Validate(null, displayName);
			if (error != null)
				throw new ValidationException<string>(displayName, new[] {error});

			var json = TrelloConfiguration.JsonFactory.Create<IJsonOrganization>();
			json.DisplayName = displayName;

			var endpoint = EndpointFactory.Build(EntityRequestType.Member_Write_CreateOrganization);
			var newData = await JsonRepository.Execute(Auth, endpoint, json, ct);

			return new Organization(newData, Auth);
		}
	}
}