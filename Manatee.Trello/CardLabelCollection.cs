﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Manatee.Trello.Internal.Caching;
using Manatee.Trello.Internal.DataAccess;
using Manatee.Trello.Internal.Eventing;
using Manatee.Trello.Internal.Synchronization;
using Manatee.Trello.Internal.Validation;
using Manatee.Trello.Json;

namespace Manatee.Trello
{
	/// <summary>
	/// A collection of labels for cards.
	/// </summary>
	public class CardLabelCollection : ReadOnlyCollection<ILabel>,
	                                   ICardLabelCollection,
	                                   IHandle<EntityDeletedEvent<IJsonLabel>>
	{
		private readonly CardContext _context;

		internal CardLabelCollection(CardContext context, TrelloAuthorization auth)
			: base(() => context.Data.Id, auth)
		{
			_context = context;

			EventAggregator.Subscribe(this);
		}

		/// <summary>
		/// Adds a label to the collection.
		/// </summary>
		/// <param name="label">The label to add.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		public async Task Add(ILabel label, CancellationToken ct = default)
		{
			var error = NotNullRule<ILabel>.Instance.Validate(null, label);
			if (error != null)
				throw new ValidationException<ILabel>(label, new[] {error});

			var json = TrelloConfiguration.JsonFactory.Create<IJsonParameter>();
			json.String = label.Id;

			var endpoint = EndpointFactory.Build(EntityRequestType.Card_Write_AddLabel,
			                                     new Dictionary<string, object> {{"_id", OwnerId}});
			await JsonRepository.Execute(Auth, endpoint, json, ct);

			Items.Add(label);
			await _context.Synchronize(true, ct);
		}

		/// <summary>
		/// Removes a label from the collection.
		/// </summary>
		/// <param name="label">The label to add.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		public async Task Remove(ILabel label, CancellationToken ct = default)
		{
			var error = NotNullRule<ILabel>.Instance.Validate(null, label);
			if (error != null)
				throw new ValidationException<ILabel>(label, new[] {error});

			var endpoint = EndpointFactory.Build(EntityRequestType.Card_Write_RemoveLabel,
			                                     new Dictionary<string, object> {{"_id", OwnerId}, {"_labelId", label.Id}});
			await JsonRepository.Execute(Auth, endpoint, ct);

			Items.Remove(label);
			await _context.Synchronize(true, ct);
		}

		internal sealed override async Task PerformRefresh(bool force, CancellationToken ct)
		{
			var endpoint = EndpointFactory.Build(EntityRequestType.Card_Read_Labels,
			                                     new Dictionary<string, object> {{"_id", OwnerId}});
			var newData = await JsonRepository.Execute<List<IJsonLabel>>(Auth, endpoint, ct, AdditionalParameters);

			Items.Clear();
			Items.AddRange(newData.Select(ja =>
				{
					var label = ja.GetFromCache<Label, IJsonLabel>(Auth);
					label.Json = ja;
					return label;
				}));
		}

		void IHandle<EntityDeletedEvent<IJsonLabel>>.Handle(EntityDeletedEvent<IJsonLabel> message)
		{
			var item = Items.FirstOrDefault(c => c.Id == message.Data.Id);
			Items.Remove(item);
		}
	}
}