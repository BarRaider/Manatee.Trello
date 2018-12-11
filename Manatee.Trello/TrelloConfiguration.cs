﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using Manatee.Trello.Internal.Caching;
using Manatee.Trello.Internal.Logging;
using Manatee.Trello.Json;
using Manatee.Trello.Rest;

namespace Manatee.Trello
{
	/// <summary>
	/// Exposes a set of run-time options for Manatee.Trello.
	/// </summary>
	public static class TrelloConfiguration
	{
		private static ILog _log;
		private static ISerializer _serializer;
		private static IDeserializer _deserializer;
		private static IRestClientProvider _restClientProvider;
		private static ICache _cache;
		private static IJsonFactory _jsonFactory;
		private static Func<IRestResponse, int, bool> _retryPredicate;
		private static Func<HttpClient> _httpClientFactory;

		/// <summary>
		/// Specifies the serializer for the REST client.
		/// </summary>
		public static ISerializer Serializer
		{
			get { return _serializer ?? (_serializer = DefaultJsonSerializer.Instance); }
			set { _serializer = value; }
		}

		/// <summary>
		/// Specifies the deserializer for the REST client.
		/// </summary>
		public static IDeserializer Deserializer
		{
			get { return _deserializer ?? (_deserializer = DefaultJsonSerializer.Instance); }
			set { _deserializer = value; }
		}

		/// <summary>
		/// Specifies the REST client provider.
		/// </summary>
		public static IRestClientProvider RestClientProvider
		{
			get { return _restClientProvider ?? (_restClientProvider = DefaultRestClientProvider.Instance); }
			set { _restClientProvider = value; }
		}

		/// <summary>
		/// Provides a cache to manage all Trello objects.
		/// </summary>
		public static ICache Cache
		{
			get { return _cache ?? (_cache = new ConcurrentCache()); }
			set { _cache = value; }
		}
		/// <summary>
		/// Specifies whether deleted items should be removed from the cache.  The default is true.
		/// </summary>
		public static bool RemoveDeletedItemsFromCache { get; set; }
		/// <summary>
		/// Provides logging for Manatee.Trello.  The default log writes to the Console window.
		/// </summary>
		public static ILog Log
		{
			get { return _log ?? (_log = new DebugLog()); }
			set { _log = value; }
		}
		/// <summary>
		/// Provides a factory which is used to create instances of JSON objects.
		/// </summary>
		public static IJsonFactory JsonFactory
		{
			get { return _jsonFactory ?? (_jsonFactory = DefaultJsonFactory.Instance); }
			set { _jsonFactory = value; }
		}
		/// <summary>
		/// Specifies whether the service should throw an exception when an error is received from Trello.  Default is true.
		/// </summary>
		public static bool ThrowOnTrelloError { get; set; }
		/// <summary>
		/// Specifies a length of time an object holds changes before it submits them.  The timer is reset with every change.  Default is 100 ms.
		/// </summary>
		/// <remarks>
		/// Setting a value of 0 ms will result in instant upload of changes, dramatically increasing call volume and slowing performance.
		/// </remarks>
		public static TimeSpan ChangeSubmissionTime { get; set; }
		/// <summary>
		/// Specifies a length of time during which a single entity can only be refreshed once.  Default is 5 seconds.
		/// </summary>
		/// <remarks>
		/// Setting a value of 0 will result in immediately consecutive or parallel calls both going through to Trello.
		/// </remarks>
		public static TimeSpan RefreshThrottle { get; set; }
		/// <summary>
		/// Specifies which HTTP response status codes should trigger an automatic retry.
		/// </summary>
		public static IList<HttpStatusCode> RetryStatusCodes { get; }
		/// <summary>
		/// Specifies a maximum number of retries allowed before an error is thrown.
		/// </summary>
		public static int MaxRetryCount { get; set; }
		/// <summary>
		/// Specifies a delay between retry attempts.
		/// </summary>
		public static TimeSpan DelayBetweenRetries { get; set; }
		/// <summary>
		/// Specifies a predicate to execute to determine if a retry should be attempted.  The default simply uses <see cref="MaxRetryCount"/> and <see cref="DelayBetweenRetries"/>.
		/// </summary>
		/// <remarks>
		/// Parameters:
		/// 
		/// - <see cref="IRestResponse"/> - The response object from the REST provider.  Will need to be cast to the appropriate type.
		/// - <see cref="int"/> - The number of retries attempted.
		///
		/// Return value:
		///
		/// - <see cref="bool"/> - True if the call should be retried; false otherwise.
		/// </remarks>
		public static Func<IRestResponse, int, bool> RetryPredicate
		{
			get { return _retryPredicate ?? DefaultRetry; }
			set { _retryPredicate = value; }
		}

		/// <summary>
		/// Gets or sets the method by which the default REST provider creates instances of <see cref="HttpClient"/>.  This can be overridden to provide custom client initialization.
		/// </summary>
		public static Func<HttpClient> HttpClientFactory
		{
			get { return _httpClientFactory ?? (() => new HttpClient()); }
			set { _httpClientFactory = value; }
		}

		/// <summary>
		/// Specifies that changes on one entity will be relationally tracked throughout the system.  This functionality is opt-in; the default is false.
		/// </summary>
		/// <remarks>
		/// When this property is enabled, collections will update automatically for downloaded entities without having to be refreshed.  For instance if a card's <see cref="Card.List"/> property is changed, the list that contains it will remove that card from its collection and the new list will add the card.
		/// </remarks>
		public static bool EnableConsistencyProcessing { get; set; }

		/// <summary>
		/// Specifies that downloading an entity will also download its nested (or child) entities.  The default is true.
		/// </summary>
		/// <remarks>
		/// For example, when this probably is enabled, downloading a board will also attempt to download its lists, cards, members, etc.  This will significantly increase download size, but it will allow for fewer call, which should significantly increase performance.
		/// The fields which are downloaded will still respect the values set in the <code>DownloadedFields</code> static property.  For example, if <see cref="Board.DownloadedFields"/> does not contain the value <see cref="Board.Fields.Cards"/>, the cards will not be downloaded.
		/// Currently, only boards (downloading lists and cards) and lists (downloading cards) are supported.
		/// </remarks>
		public static bool EnableDeepDownloads { get; set; }

		internal static Dictionary<string, Func<IJsonPowerUp, TrelloAuthorization, IPowerUp>> RegisteredPowerUps { get; }

		static TrelloConfiguration()
		{
			ThrowOnTrelloError = true;
			ChangeSubmissionTime = TimeSpan.FromMilliseconds(100);
			RegisteredPowerUps = new Dictionary<string, Func<IJsonPowerUp, TrelloAuthorization, IPowerUp>>();
			RetryStatusCodes = new List<HttpStatusCode>();
			RemoveDeletedItemsFromCache = true;
			RefreshThrottle = TimeSpan.FromSeconds(5);
			EnableDeepDownloads = true;
		}

		/// <summary>
		/// Registers a new power-up implementation.
		/// </summary>
		/// <param name="id">The Trello ID of the power-up.</param>
		/// <param name="factory">A factory function that creates instances of the power-up implementation.</param>
		public static void RegisterPowerUp(string id, Func<IJsonPowerUp, TrelloAuthorization, IPowerUp> factory)
		{
			RegisteredPowerUps[id] = factory;
		}

		private static bool DefaultRetry(IRestResponse response, int callCount)
		{
			var retry = RetryStatusCodes.Contains(response.StatusCode) &&
						callCount <= MaxRetryCount;
			if (retry)
				Thread.Sleep(DelayBetweenRetries);
			return retry;
		}
	}
}
