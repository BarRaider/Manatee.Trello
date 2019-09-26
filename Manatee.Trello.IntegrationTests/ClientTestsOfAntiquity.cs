﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Manatee.Trello.Json;
using Manatee.Trello.Tests.Common;
using NUnit.Framework;

namespace Manatee.Trello.IntegrationTests
{
	[TestFixture]
	[Ignore("These tests are here for posterity, but they need to be rewritten to work with the newer test suite.")]
	public class ClientTestsOfAntiquity
	{
		private readonly TrelloFactory _factory = new TrelloFactory();

		[Test]
		public void Issue26_NotificationTypeCardDueSoonNotDeserializing()
		{
			var text =
				"{\"id\":\"571ca99c1aa4fb7e9e30bb0b\",\"unread\":false,\"type\":\"cardDueSoon\",\"date\":\"2016-04-24T11:10:19.997Z\",\"data\":{\"board\":{\"name\":\"Team\",\"id\":\"5718d772857c2a4b2a2befb8\"},\"card\":{\"due\":\"2016-04-25T11:00:00.000Z\",\"shortLink\":\"f5sdWFLT\",\"idShort\":19,\"name\":\"AS MRC Training\",\"id\":\"570e55eb131202e342f205ad\"}},\"idMemberCreator\":null}";
			var serializer = DefaultJsonSerializer.Instance;
			//var expected = new ManateeNotification
			//	{
			//		Id = "571ca99c1aa4fb7e9e30bb0b",
			//		Unread = false,
			//		Type = NotificationType.CardDueSoon,
			//		Date = DateTime.Parse("2016-04-24T11:10:19.997Z"),
			//		Data = new ManateeNotificationData
			//			{
			//				Board = new ManateeBoard
			//					{
			//						Name = "Team",
			//						Id = "5718d772857c2a4b2a2befb8"
			//					},
			//				Card = new ManateeCard
			//					{
			//						Due = DateTime.Parse("2016-04-25T11:00:00.000Z"),
			//						ShortUrl = "f5sdWFLT",
			//						IdShort = 19,
			//						Name = "AS MRC Training",
			//						Id = "570e55eb131202e342f205ad"
			//					}
			//			},
			//		MemberCreator = null
			//	};

			var actual = serializer.Deserialize<IJsonNotification>(text);

			//Assert.IsTrue(TheGodComparer.Instance.Equals(expected, actual));
		}

		[Test]
		public async Task Issue30_PartialSearch_True()
		{
			TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

			var board = _factory.Board(TrelloIds.BoardId);
			var searchText = "car";
			var search = _factory.Search(_factory.SearchQuery().Text(searchText), modelTypes: SearchModelType.Cards, context: new[] {board}, isPartial: true);

			await search.Refresh();

			// search will include archived cards as well as matches in card descriptions.
			Assert.AreEqual(6, search.Cards.Count());

			await TrelloProcessor.Flush();
		}

		[Test]
		public async Task Issue30_PartialSearch_False()
		{
			TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

			var board = _factory.Board(TrelloIds.BoardId);
			var searchText = "car";
			var search = _factory.Search(_factory.SearchQuery().Text(searchText), modelTypes: SearchModelType.Cards, context: new[] {board});

			await search.Refresh();

			Assert.AreEqual(0, search.Cards.Count());

			await TrelloProcessor.Flush();
		}

		[Test]
		[Ignore("The new async operation throws exceptions when tasks are cancelled (normal for .Net tasks).")]
		public async Task Issue32_CancelPendingRequests()
		{
			TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

			var cards = new List<Card>
				{
					new Card("KHKms82H"),
					new Card("AgTd8qhF"),
					new Card("R1Kc5KHd"),
					new Card("vlgbqJES"),
					new Card("uVD9TAIK"),
					new Card("prSr36Ny"),
					new Card("hBoTLb9V"),
				};

			var tokenSource = new CancellationTokenSource();

			var nameTasks = cards.Select(async c =>
				{
					await c.Refresh(ct: tokenSource.Token);
					return c.Name;
				}).ToList();

			tokenSource.Cancel();

			var names = await Task.WhenAll(nameTasks.Where(t => !t.IsCanceled));
			Assert.AreEqual(0, names.Count(n => n != null));
		}

#pragma warning disable 1998
		[Test]
		public async Task Issue34_CardsNotDownloading()
		{
			//app key and token, user required to enter token
			TrelloAuthorization.Default.AppKey = "440a184b181002cf00f63713a7f51191";
			TrelloAuthorization.Default.UserToken = "dfd8dd877fa1775db502f891370fb26882a4d8bad41a1cc8cf1a58874b21322b";

			TrelloConfiguration.ThrowOnTrelloError = true;

			Console.WriteLine(await _factory.Me());
			var boardID = "574e95edd8a4fc16207f7079";
			var board = _factory.Board(boardID);
			Console.WriteLine(board);

			//here is where it calls the exception with 'invalid id'
			foreach (var card in board.Cards)
			{
				Console.WriteLine(card);
			}
		}
#pragma warning restore 1998

		[Test]
		public async Task Issue35_DatesReturningAs1DayBefore()
		{
			ICard card = null;
			try
			{
				TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

				var learningBoard = _factory.Board(TrelloIds.BoardId);
				await learningBoard.Lists.Refresh();
				var list = learningBoard.Lists.First();
				var member = list.Board.Members.First();
				card = await list.Cards.Add("test card 2");
				card.DueDate = new DateTime(2016, 07, 21);

				await TrelloProcessor.Flush();

				var cardCopy = _factory.Card(card.Id);
				Assert.AreEqual(new DateTime(2016, 07, 21), cardCopy.DueDate);
			}
			finally
			{
				card?.Delete();
			}
		}

		[Test]
		public async Task Issue36_CardAttachmentByUrlThrows()
		{
			ICard card = null;
			try
			{
				TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

				var list = _factory.List(TrelloIds.ListId);
				card = await list.Cards.Add("attachment test");
				await card.Attachments.Add("http://i.imgur.com/eKgKEOn.jpg", "me");
			}
			finally
			{
				card?.Delete();
			}
		}

		[Test]
		public async Task Issue37_InconsistentDateEncoding()
		{
			ICard card = null;
			try
			{
				TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

				var list = _factory.List(TrelloIds.ListId);
				card = await list.Cards.Add("date encoding test");
				// 2016-12-08T04:45:00.000Z
				var date = Convert.ToDateTime("8/12/2016 5:45:00PM");
				card.DueDate = date;

				await TrelloProcessor.Flush();
			}
			finally
			{
				card?.Delete();
			}
		}

		[Test]
		public async Task Issue45_DueDateAsMinValue()
		{
			ICard card = null;
			try
			{
				TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

				var list = _factory.List(TrelloIds.ListId);
				card = await list.Cards.Add("min date test");
				card.Description = "a description";
				card.DueDate = DateTime.MinValue;

				await TrelloProcessor.Flush();
			}
			finally
			{
				card?.Delete();
			}
		}

		[Test]
		public async Task Issue47_AddCardWithDetails()
		{
			ICard card = null;
			var name = "card detailed creation test";
			var description = "this is a description";
			var position = Position.Top;
			var dueDate = new DateTime(2014, 1, 1);
			try
			{
				TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

				var me = await _factory.Me();
				var members = new IMember[] {me};
				var board = _factory.Board(TrelloIds.BoardId);
				await board.Refresh();
				var labels = new[] {board.Labels.FirstOrDefault(l => l.Color == LabelColor.Blue)};

				var list = _factory.List(TrelloIds.ListId);
				card = await list.Cards.Add(name, description, position, dueDate, true, members, labels);

				var recard = _factory.Card(card.Id);
				await recard.Refresh();

				Assert.AreEqual(name, recard.Name);
				Assert.AreEqual(description, recard.Description);
				Assert.AreEqual(card.Id, list.Cards.First().Id);
				Assert.AreEqual(dueDate, recard.DueDate);
				Assert.IsTrue(recard.IsComplete.Value, "card not complete");
				Assert.IsTrue(recard.Members.Contains(me), "member not found");
				Assert.IsTrue(recard.Labels.Contains(labels[0]), "label not found");

				await TrelloProcessor.Flush();
			}
			finally
			{
				card?.Delete();
			}
		}

		[Test]
		public async Task Issue46_DueComplete()
		{
			ICard card = null;
			var name = "due complete test";
			var description = "this is a description";
			var position = Position.Top;
			var dueDate = new DateTime(2014, 1, 1);
			try
			{
				TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

				var list = _factory.List(TrelloIds.ListId);
				card = await list.Cards.Add(name, description, position, dueDate);

				Assert.AreEqual(false, card.IsComplete);

				card.IsComplete = true;

				await TrelloProcessor.Flush();

				var recard = _factory.Card(card.Id);
				await recard.Refresh();

				Assert.AreEqual(true, recard.IsComplete);

				await TrelloProcessor.Flush();
			}
			finally
			{
				card?.Delete();
			}
		}

		[Test]
		public async Task Issue59_EditComments()
		{
			ICard card = null;
			var name = "edit comment test";
			try
			{
				TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

				var list = _factory.List(TrelloIds.ListId);
				card = await list.Cards.Add(name);
				var comment = await card.Comments.Add("This is a comment");
				comment.Data.Text = "This comment was changed.";

				Thread.Sleep(5);

				await TrelloProcessor.Flush();
			}
			finally
			{
				card?.Delete();
			}
		}

		[Test]
		public async Task Issue60_BoardPreferencesFromSearch()
		{
			TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

			var search = _factory.Search(_factory.SearchQuery().TextInName("Sandbox"), 1, SearchModelType.Boards);
			await search.Refresh();
			var board = search.Boards.FirstOrDefault();

			Assert.IsNotNull(board.Preferences.Background.Color);
		}

		[Test]
		public async Task Issue84_ListNameNotDownloading()
		{
			TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

			var list = _factory.List(TrelloIds.ListId);
			await list.Refresh();

			Assert.IsNotNull(list.Name);
		}

		[Test]
		public async Task Email_BoardDownloadHangsOnNameAfterFetchingFromCollection()
		{
			TrelloAuthorization.Default.AppKey = TrelloIds.AppKey;

			var me = await _factory.Me();
			await me.Refresh();
			var board = me.Boards.FirstOrDefault(b => b.Name == "Sandbox");
			Assert.IsNotNull(board);

			await board.Refresh();
			Assert.AreEqual("Sandbox", board.Name);

			board = me.Boards.FirstOrDefault(b => b.Name.Equals("Sandbox"));
			Assert.IsNotNull(board);

			await board.Refresh();
			Assert.AreEqual("Sandbox", board.Name);
		}
	}
}
