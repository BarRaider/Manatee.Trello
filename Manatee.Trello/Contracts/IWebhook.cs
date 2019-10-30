﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Manatee.Trello
{
	/// <summary>
	/// Represents a webhook.
	/// </summary>
	/// <typeparam name="T">The type of object to which the webhook is attached.</typeparam>
	public interface IWebhook<T> : ICacheable, IRefreshable
		where T : class, ICanWebhook
	{
		/// <summary>
		/// Gets or sets a callback URL for the webhook.
		/// </summary>
		string CallBackUrl { get; set; }

		/// <summary>
		/// Gets the creation date of the webhook.
		/// </summary>
		DateTime CreationDate { get; }

		/// <summary>
		/// Gets or sets a description for the webhook.
		/// </summary>
		string Description { get; set; }

		/// <summary>
		/// Gets or sets whether the webhook is active.
		/// </summary>
		bool? IsActive { get; set; }

		/// <summary>
		/// Gets or sets the webhook's target.
		/// </summary>
		T Target { get; set; }

		/// <summary>
		/// Raised when data on the webhook is updated.
		/// </summary>
		event Action<IWebhook<T>, IEnumerable<string>> Updated;

		/// <summary>
		/// Deletes the webhook.
		/// </summary>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		/// <remarks>
		/// This permanently deletes the webhook from Trello's server, however, this object will remain in memory and all properties will remain accessible.
		/// </remarks>
		Task Delete(CancellationToken ct = default);
	}
}