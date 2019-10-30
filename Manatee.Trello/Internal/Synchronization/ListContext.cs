using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Manatee.Trello.Internal.Caching;
using Manatee.Trello.Internal.DataAccess;
using Manatee.Trello.Internal.Validation;
using Manatee.Trello.Json;

namespace Manatee.Trello.Internal.Synchronization
{
	internal class ListContext : SynchronizationContext<IJsonList>
	{
		private static readonly Dictionary<string, object> Parameters;
		private static readonly List.Fields MemberFields;

		public static Dictionary<string, object> CurrentParameters
		{
			get
			{
				lock (Parameters)
				{
					if (!Parameters.Any())
						GenerateParameters();

					return new Dictionary<string, object>(Parameters);
				}
			}
		}

		public ReadOnlyActionCollection Actions { get; }
		public CardCollection Cards { get; }
		public virtual bool HasValidId => IdRule.Instance.Validate(Data.Id, null) == null;

		static ListContext()
		{
			Parameters = new Dictionary<string, object>();
			MemberFields = List.Fields.Name |
			               List.Fields.IsClosed |
			               List.Fields.Position |
			               List.Fields.IsSubscribed;
			Properties = new Dictionary<string, Property<IJsonList>>
				{
					{
						nameof(List.Board),
						new Property<IJsonList, Board>((d, a) => d.Board?.GetFromCache<Board, IJsonBoard>(a),
						                               (d, o) =>
							                               {
								                               if (o != null) d.Board = o.Json;
							                               })
					},
					{
						nameof(List.Id),
						new Property<IJsonList, string>((d, a) => d.Id, (d, o) => d.Id = o)
					},
					{
						nameof(List.IsArchived),
						new Property<IJsonList, bool?>((d, a) => d.Closed, (d, o) => d.Closed = o)
					},
					{
						nameof(List.IsSubscribed),
						new Property<IJsonList, bool?>((d, a) => d.Subscribed, (d, o) => d.Subscribed = o)
					},
					{
						nameof(List.Name),
						new Property<IJsonList, string>((d, a) => d.Name, (d, o) => d.Name = o)
					},
					{
						nameof(List.Position),
						new Property<IJsonList, Position>((d, a) => Position.GetPosition(d.Pos), (d, o) => d.Pos = Position.GetJson(o))
					},
					{
						nameof(IJsonList.ValidForMerge),
						new Property<IJsonList, bool>((d, a) => d.ValidForMerge, (d, o) => d.ValidForMerge = o, true)
					},
				};
		}

		public ListContext(string id, TrelloAuthorization auth)
			: base(auth)
		{
			Data.Id = id;

			Actions = new ReadOnlyActionCollection(typeof(List), () => Data.Id, auth);
			Actions.Refreshed += (s, e) => OnMerged(new List<string> {nameof(Actions)});
			Cards = new CardCollection(() => Data.Id, auth);
			Cards.Refreshed += (s, e) => OnMerged(new List<string> {nameof(Cards)});
		}

		public static void UpdateParameters()
		{
			lock (Parameters)
			{
				Parameters.Clear();
			}
		}

		private static void GenerateParameters()
		{
			lock (Parameters)
			{
				Parameters.Clear();
				var flags = Enum.GetValues(typeof(List.Fields)).Cast<List.Fields>().ToList();
				var availableFields = (List.Fields)flags.Cast<int>().Sum();

				var memberFields = availableFields & MemberFields & List.DownloadedFields;
				Parameters["fields"] = memberFields.GetDescription();

				var parameterFields = availableFields & List.DownloadedFields & (~MemberFields);
				if (parameterFields.HasFlag(List.Fields.Actions))
				{
					Parameters["actions"] = "all";
					Parameters["actions_format"] = "list";
				}
				if (parameterFields.HasFlag(List.Fields.Cards))
				{
					Parameters["cards"] = "open";
					var fields = CardContext.CurrentParameters["fields"];
					if (Card.DownloadedFields.HasFlag(Card.Fields.List))
						fields += ",idList";
					Parameters["card_fields"] = fields;
					if (Card.DownloadedFields.HasFlag(Card.Fields.Members))
						Parameters["card_members"] = "true";
					if (Card.DownloadedFields.HasFlag(Card.Fields.Attachments))
						Parameters["card_attachments"] = "true";
					if (Card.DownloadedFields.HasFlag(Card.Fields.Stickers))
						Parameters["card_stickers"] = "true";
				}
				if (parameterFields.HasFlag(List.Fields.Board))
				{
					Parameters["board"] = "true";
					Parameters["board_fields"] = BoardContext.CurrentParameters["fields"];
				}
			}
		}

		public override Endpoint GetRefreshEndpoint()
		{
			return EndpointFactory.Build(EntityRequestType.List_Read_Refresh,
			                             new Dictionary<string, object> {{"_id", Data.Id}});
		}

		protected override Dictionary<string, object> GetParameters()
		{
			return CurrentParameters;
		}

		protected override async Task SubmitData(IJsonList json, CancellationToken ct)
		{
			var endpoint = EndpointFactory.Build(EntityRequestType.List_Write_Update, new Dictionary<string, object> {{"_id", Data.Id}});
			var newData = await JsonRepository.Execute(Auth, endpoint, json, ct);
			Merge(newData);
		}

		protected override IEnumerable<string> MergeDependencies(IJsonList json, bool overwrite)
		{
			var properties = new List<string>();

			if (json.Actions != null)
			{
				Actions.Update(json.Actions.Select(a => a.GetFromCache<Action, IJsonAction>(Auth, overwrite)));
				properties.Add(nameof(List.Actions));
			}
			if (json.Cards != null)
			{
				Cards.Update(json.Cards.Select(a => a.GetFromCache<Card, IJsonCard>(Auth, overwrite)));
				properties.Add(nameof(List.Cards));
			}

			return properties;
		}
	}
}