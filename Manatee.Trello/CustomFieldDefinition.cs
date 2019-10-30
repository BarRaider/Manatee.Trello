﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Manatee.Trello.Internal;
using Manatee.Trello.Internal.DataAccess;
using Manatee.Trello.Internal.Synchronization;
using Manatee.Trello.Internal.Validation;
using Manatee.Trello.Json;

namespace Manatee.Trello
{
	/// <summary>
	/// Represents a custom field definition.
	/// </summary>
	public class CustomFieldDefinition : ICustomFieldDefinition, IMergeJson<IJsonCustomFieldDefinition>, IBatchRefresh, IHandleSynchronization
	{
		private readonly Field<IBoard> _board;
		private readonly Field<string> _fieldGroup;
		private readonly Field<string> _name;
		private readonly Field<Position> _position;
		private readonly Field<CustomFieldType?> _type;
		private readonly CustomFieldDefinitionContext _context;

		/// <summary>
		/// Gets the board on which the field is defined.
		/// </summary>
		public IBoard Board => _board.Value;

		/// <summary>
		/// Gets display information for the custom field.
		/// </summary>
		public ICustomFieldDisplayInfo DisplayInfo { get; }

		/// <summary>
		/// Gets an identifier that groups fields across boards.
		/// </summary>
		public string FieldGroup => _fieldGroup.Value;

		/// <summary>
		/// Gets an ID on which matching can be performed.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// Gets or sets the name of the field.
		/// </summary>
		public string Name
		{
			get { return _name.Value; }
			set { _name.Value = value; }
		}

		/// <summary>
		/// Gets drop down options, if applicable.
		/// </summary>
		public IDropDownOptionCollection Options => _context.DropDownOptions;

		/// <summary>
		/// Gets or sets the position of the field.
		/// </summary>
		public Position Position
		{
			get { return _position.Value; }
			set { _position.Value = value; }
		}

		/// <summary>
		/// Gets the data type of the field.
		/// </summary>
		public CustomFieldType? Type => _type.Value;

		internal IJsonCustomFieldDefinition Json
		{
			get { return _context.Data; }
			set { _context.Merge(value); }
		}
		TrelloAuthorization IBatchRefresh.Auth => _context.Auth;

		/// <summary>
		/// Raised when data on the custom field definition is updated.
		/// </summary>
		public event Action<ICustomFieldDefinition, IEnumerable<string>> Updated;

		internal CustomFieldDefinition(IJsonCustomFieldDefinition json, TrelloAuthorization auth)
		{
			Id = json.Id;
			_context = new CustomFieldDefinitionContext(Id, auth);

			_board = new Field<IBoard>(_context, nameof(Board));
			DisplayInfo = new CustomFieldDisplayInfo(_context.DisplayInfo);
			_fieldGroup = new Field<string>(_context, nameof(FieldGroup));
			_name = new Field<string>(_context, nameof(Name));
			_position = new Field<Position>(_context, nameof(Position));
			_type = new Field<CustomFieldType?>(_context, nameof(Type));

			if (auth != TrelloAuthorization.Null)
				TrelloConfiguration.Cache.Add(this);

			_context.Merge(json);
			_context.Synchronized.Add(this);
		}

		/// <summary>
		/// Deletes the field definition.
		/// </summary>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		public async Task Delete(CancellationToken ct = default(CancellationToken))
		{
			await _context.Delete(ct);
			if (TrelloConfiguration.RemoveDeletedItemsFromCache)
				TrelloConfiguration.Cache.Remove(this);
		}

		/// <summary>
		/// Refreshes the custom field definition data.
		/// </summary>
		/// <param name="force">Indicates that the refresh should ignore the value in <see cref="TrelloConfiguration.RefreshThrottle"/> and make the call to the API.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		public Task Refresh(bool force = false, CancellationToken ct = default(CancellationToken))
		{
			return _context.Synchronize(force, ct);
		}

		void IMergeJson<IJsonCustomFieldDefinition>.Merge(IJsonCustomFieldDefinition json, bool overwrite)
		{
			_context.Merge(json, overwrite);
		}

		/// <summary>
		/// Sets a value for a custom field on a card.
		/// </summary>
		/// <param name="card">The card on which to set the value.</param>
		/// <param name="value">The vaue to set.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		/// <returns>The custom field instance.</returns>
		public async Task<ICustomField<double?>> SetValueForCard(ICard card, double? value,
		                                                         CancellationToken ct = default(CancellationToken))
		{
			NotNullRule<ICard>.Instance.Validate(null, card);
			NullableHasValueRule<double>.Instance.Validate(null, value);

			return await _context.SetValueOnCard(card, value, ct);
		}

		/// <summary>
		/// Sets a value for a custom field on a card.
		/// </summary>
		/// <param name="card">The card on which to set the value.</param>
		/// <param name="value">The vaue to set.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		/// <returns>The custom field instance.</returns>
		public async Task<ICustomField<bool?>> SetValueForCard(ICard card, bool? value,
		                                                       CancellationToken ct = default(CancellationToken))
		{
			NotNullRule<ICard>.Instance.Validate(null, card);
			NullableHasValueRule<bool>.Instance.Validate(null, value);

			return await _context.SetValueOnCard(card, value, ct);
		}

		/// <summary>
		/// Sets a value for a custom field on a card.
		/// </summary>
		/// <param name="card">The card on which to set the value.</param>
		/// <param name="value">The vaue to set.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		/// <returns>The custom field instance.</returns>
		public async Task<ICustomField<string>> SetValueForCard(ICard card, string value,
		                                                        CancellationToken ct = default(CancellationToken))
		{
			NotNullRule<ICard>.Instance.Validate(null, card);
			NotNullRule<string>.Instance.Validate(null, value);

			return await _context.SetValueOnCard(card, value, ct);
		}

		/// <summary>
		/// Sets a value for a custom field on a card.
		/// </summary>
		/// <param name="card">The card on which to set the value.</param>
		/// <param name="value">The vaue to set.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		/// <returns>The custom field instance.</returns>
		public async Task<ICustomField<IDropDownOption>> SetValueForCard(ICard card, IDropDownOption value,
		                                                                 CancellationToken ct = default(CancellationToken))
		{
			NotNullRule<ICard>.Instance.Validate(null, card);
			NotNullRule<IDropDownOption>.Instance.Validate(null, value);

			return await _context.SetValueOnCard(card, value, ct);
		}

		/// <summary>
		/// Sets a value for a custom field on a card.
		/// </summary>
		/// <param name="card">The card on which to set the value.</param>
		/// <param name="value">The vaue to set.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		/// <returns>The custom field instance.</returns>
		public async Task<ICustomField<DateTime?>> SetValueForCard(ICard card, DateTime? value,
		                                                           CancellationToken ct = default(CancellationToken))
		{
			NotNullRule<ICard>.Instance.Validate(null, card);
			NullableHasValueRule<DateTime>.Instance.Validate(null, value);

			return await _context.SetValueOnCard(card, value, ct);
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return $"{Name} ({Type})";
		}

		Endpoint IBatchRefresh.GetRefreshEndpoint()
		{
			return _context.GetRefreshEndpoint();
		}

		void IBatchRefresh.Apply(string content)
		{
			var json = TrelloConfiguration.Deserializer.Deserialize<IJsonCustomFieldDefinition>(content);
			_context.Merge(json);
		}

		void IHandleSynchronization.HandleSynchronized(IEnumerable<string> properties)
		{
			Id = _context.Data.Id;
			var handler = Updated;
			handler?.Invoke(this, properties);
		}
	}
}
