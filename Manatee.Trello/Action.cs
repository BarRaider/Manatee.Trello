﻿/***************************************************************************************

	Copyright 2013 Little Crab Solutions

	   Licensed under the Apache License, Version 2.0 (the "License");
	   you may not use this file except in compliance with the License.
	   You may obtain a copy of the License at

		 http://www.apache.org/licenses/LICENSE-2.0

	   Unless required by applicable law or agreed to in writing, software
	   distributed under the License is distributed on an "AS IS" BASIS,
	   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	   See the License for the specific language governing permissions and
	   limitations under the License.
 
	File Name:		Action.cs
	Namespace:		Manatee.Trello
	Class Name:		Action
	Purpose:		Represents an action on Trello.com.

***************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using Manatee.Trello.Contracts;
using Manatee.Trello.Internal;
using Manatee.Trello.Internal.Json;
using Manatee.Trello.Json;

namespace Manatee.Trello
{
	/// <summary>
	/// Actions are generated by Trello to record what users do.
	/// </summary>
	public class Action : ExpiringObject, IEquatable<Action>, IComparable<Action>
	{
		private static readonly OneToOneMap<ActionType, string> _typeMap;
		private static readonly Dictionary<ActionType, Func<Action, string>> _stringDefinitions; 

		private readonly Dictionary<string, ExpiringObject> _entities;
		private IJsonAction _jsonAction;
		private Member _memberCreator;
		private ActionType _type = ActionType.Unknown;
		private bool _isDeleted;

		/// <summary>
		/// Gets the attachment, if one exists, associated with the action.
		/// </summary>
		public Attachment Attachment
		{
			get { return _isDeleted ? null : TryGetEntity<Attachment>("attachment", new[] {"attachment.id"}, EntityRequestType.Attachment_Read_Refresh); }
		}
		/// <summary>
		/// Gets the board, if one exists, associated with the action.
		/// </summary>
		public Board Board
		{
			get { return _isDeleted ? null : TryGetEntity<Board>("board", new[] {"board.id"}, EntityRequestType.Board_Read_Refresh); }
		}
		/// <summary>
		/// Gets the card, if one exists, associated with the action.
		/// </summary>
		public Card Card
		{
			get { return _isDeleted ? null : TryGetEntity<Card>("card", new[] {"card.id"}, EntityRequestType.Card_Read_Refresh); }
		}
		/// <summary>
		/// Gets the short ID of the card, if one exists, associated with the action.
		/// </summary>
		public int? CardShortId
		{
			get { return _isDeleted ? null : (int?) Data.TryGetNumber("card", "idShort"); }
		}
		/// <summary>
		/// Gets the check list, if one exists, associated with the action.
		/// </summary>
		public CheckList CheckList
		{
			get { return _isDeleted ? null : TryGetEntity<CheckList>("checklist", new[] {"checklist.id"}, EntityRequestType.CheckList_Read_Refresh); }
		}
		/// <summary>
		/// Gets the check item, if one exists, associated with the action.
		/// </summary>
		public CheckItem CheckItem
		{
			get { return _isDeleted ? null : TryGetEntity<CheckItem>("checkItem", new[] {"checkItem.id"}, EntityRequestType.CheckItem_Read_Refresh); }
		}
		/// <summary>
		/// Data associated with the action.  Contents depend upon the action's type.
		/// </summary>
		internal IJsonActionData Data
		{
			get { return _isDeleted ? null : _jsonAction.Data; }
		}
		/// <summary>
		/// When the action was performed.
		/// </summary>
		public DateTime? Date { get { return _jsonAction.Date; } }
		/// <summary>
		/// Gets a unique identifier (not necessarily a GUID).
		/// </summary>
		public sealed override string Id
		{
			get { return _jsonAction.Id; }
			internal set { _jsonAction.Id = value; }
		}
		/// <summary>
		/// Gets the list, if one exists, associated with the action.
		/// </summary>
		public List List
		{
			get { return _isDeleted ? null : TryGetEntity<List>("list", new[] {"list.id", "listAfter.id"}, EntityRequestType.List_Read_Refresh); }
		}
		/// <summary>
		/// Gets the member, if one exists, associated with the action.
		/// </summary>
		public Member Member
		{
			get { return _isDeleted ? null : TryGetEntity<Member>("member", new[] {"idMember", "idMemberAdded", "idMemberRemoved"}, EntityRequestType.Member_Read_Refresh); }
		}
		/// <summary>
		/// The member who performed the action.
		/// </summary>
		public Member MemberCreator
		{
			get { return _isDeleted ? null : UpdateById(ref _memberCreator, EntityRequestType.Member_Read_Refresh, _jsonAction.IdMemberCreator); }
		}
		/// <summary>
		/// Gets the organization, if one exists, associated with the action.
		/// </summary>
		public Organization Organization
		{
			get { return _isDeleted ? null : TryGetEntity<Organization>("organization", new[] {"organization"}, EntityRequestType.Organization_Read_Refresh); }
		}
		/// <summary>
		/// Gets the board which was copied, if one exists, associated with the action.
		/// </summary>
		public Board SourceBoard
		{
			get { return _isDeleted ? null : TryGetEntity<Board>("boardSource", new[] {"boardSource.id"}, EntityRequestType.Board_Read_Refresh); }
		}
		/// <summary>
		/// Gets the card which was copied, if one exists, associated with the action.
		/// </summary>
		public Card SourceCard
		{
			get { return _isDeleted ? null : TryGetEntity<Card>("cardSource", new[] {"cardSource.id"}, EntityRequestType.Card_Read_Refresh); }
		}
		/// <summary>
		/// Gets the list from which a card was moved, if one exists, associated with the action.
		/// </summary>
		public List SourceList
		{
			get { return _isDeleted ? null : TryGetEntity<List>("listSource", new[] {"listBefore.id"}, EntityRequestType.List_Read_Refresh); }
		}
		/// <summary>
		/// Gets the text, if one exists, associated with the action.
		/// </summary>
		public string Text
		{
			get { return _isDeleted ? null : Data.TryGetString("text"); }
		}
		/// <summary>
		/// The type of action performed.
		/// </summary>
		public ActionType Type
		{
			get { return _isDeleted ? ActionType.Unknown : _type; }
		}
		/// <summary>
		/// Gets whether this entity represents an actual entity on Trello.
		/// </summary>
		public override bool IsStubbed { get { return _jsonAction is InnerJsonAction; } }

		static Action()
		{
			_typeMap = new OneToOneMap<ActionType, string>
			           	{
			           		{ActionType.AddAttachmentToCard, "addAttachmentToCard"},
			           		{ActionType.AddChecklistToCard, "addChecklistToCard"},
			           		{ActionType.AddMemberToBoard, "addMemberToBoard"},
			           		{ActionType.AddMemberToCard, "addMemberToCard"},
			           		{ActionType.AddMemberToOrganization, "addMemberToOrganization"},
			           		{ActionType.AddToOrganizationBoard, "addToOrganizationBoard"},
			           		{ActionType.CommentCard, "commentCard"},
			           		{ActionType.CopyCommentCard, "copyCommentCard"},
			           		{ActionType.ConvertToCardFromCheckItem, "convertToCardFromCheckItem"},
			           		{ActionType.CopyBoard, "copyBoard"},
			           		{ActionType.CreateBoard, "createBoard"},
			           		{ActionType.CreateCard, "createCard"},
			           		{ActionType.CopyCard, "copyCard"},
			           		{ActionType.CreateList, "createList"},
			           		{ActionType.CreateOrganization, "createOrganization"},
			           		{ActionType.DeleteAttachmentFromCard, "deleteAttachmentFromCard"},
			           		{ActionType.DeleteBoardInvitation, "deleteBoardInvitation"},
							{ActionType.DeleteCard, "deleteCard"},
			           		{ActionType.DeleteOrganizationInvitation, "deleteOrganizationInvitation"},
			           		{ActionType.MakeAdminOfBoard, "makeAdminOfBoard"},
			           		{ActionType.MakeNormalMemberOfBoard, "makeNormalMemberOfBoard"},
			           		{ActionType.MakeNormalMemberOfOrganization, "makeNormalMemberOfOrganization"},
			           		{ActionType.MakeObserverOfBoard, "makeObserverOfBoard"},
			           		{ActionType.MemberJoinedTrello, "memberJoinedTrello"},
			           		{ActionType.MoveCardFromBoard, "moveCardFromBoard"},
			           		{ActionType.MoveListFromBoard, "moveListFromBoard"},
			           		{ActionType.MoveCardToBoard, "moveCardToBoard"},
			           		{ActionType.MoveListToBoard, "moveListToBoard"},
			           		{ActionType.RemoveAdminFromBoard, "removeAdminFromBoard"},
			           		{ActionType.RemoveAdminFromOrganization, "removeAdminFromOrganization"},
			           		{ActionType.RemoveChecklistFromCard, "removeChecklistFromCard"},
			           		{ActionType.RemoveFromOrganizationBoard, "removeFromOrganizationBoard"},
			           		{ActionType.RemoveMemberFromCard, "removeMemberFromCard"},
			           		{ActionType.UnconfirmedBoardInvitation, "unconfirmedBoardInvitation"},
			           		{ActionType.UnconfirmedOrganizationInvitation, "unconfirmedOrganizationInvitation"},
			           		{ActionType.UpdateBoard, "updateBoard"},
			           		{ActionType.UpdateCard, "updateCard"},
			           		{ActionType.UpdateCheckItemStateOnCard, "updateCheckItemStateOnCard"},
			           		{ActionType.UpdateChecklist, "updateChecklist"},
			           		{ActionType.UpdateMember, "updateMember"},
			           		{ActionType.UpdateOrganization, "updateOrganization"},
			           		{ActionType.UpdateCardIdList, "updateCard:idList"},
			           		{ActionType.UpdateCardClosed, "updateCard:closed"},
			           		{ActionType.UpdateCardDesc, "updateCard:desc"},
			           		{ActionType.UpdateCardName, "updateCard:name"},
			           	};
			_stringDefinitions = new Dictionary<ActionType, Func<Action, string>>
				{
					{ActionType.AddAttachmentToCard, a => a.ToString("{0} attached {1} to card {2}.", a.GetString("attachment.name"), a.GetString("card.name"))},
					{ActionType.AddChecklistToCard, a => a.ToString("{0} added checklist {1} to card {2}.", a.GetString("checklist.name"), a.GetString("card.name"))},
					{ActionType.AddMemberToBoard, a => a.ToString("{0} added member {1} to board {2}.", a.TryGetMemberFullName(), a.GetString("board.name"))},
					{ActionType.AddMemberToCard, a => a.ToString("{0} assigned member {1} to card {2}.", a.TryGetMemberFullName(), a.GetString("card.name"))},
					{ActionType.AddMemberToOrganization, a => a.ToString("{0} added member {1} to organization {2}.", a.TryGetMemberFullName(), a.GetString("organization.name"))},
					{ActionType.AddToOrganizationBoard, a => a.ToString("{0} moved board {1} into organization {2}.", a.GetString("board.id"), a.GetString("organization.name"))},
					{ActionType.CommentCard, a => a.ToString("{0} commented on card #{1}: '{2}'.", a.GetString("card.name"), a.GetString("text"))},
					{ActionType.ConvertToCardFromCheckItem, a => a.ToString("{0} converted checkitem {1} to a card.", a.GetString("card.name"))},
					{ActionType.CopyBoard, a => a.ToString("{0} copied board {1} from board {2}.", a.GetString("board.name"), a.GetString("boardSource.name"))},
					{ActionType.CopyCard, a => a.ToString("{0} copied card {1} from card {2}.", a.GetString("card.name"), a.GetString("cardSource.name"))},
					{ActionType.CreateBoard, a => a.ToString("{0} created board {1}.", a.GetString("board.name"))},
					{ActionType.CreateCard, a => a.ToString("{0} created card {1}.", a.GetString("card.name"))},
					{ActionType.CreateList, a => a.ToString("{0} created list {1}.", a.GetString("list.name"))},
					{ActionType.CreateOrganization, a => a.ToString("{0} created organization {1}.", a.GetString("organization.name"))},
					{ActionType.DeleteAttachmentFromCard, a => a.ToString("{0} removed attachment {1} from card {2}.", a.GetString("attachment.name"), a.GetString("card.name"))},
					{ActionType.DeleteCard, a => a.ToString("{0} deleted card {1} from {2}.", a.GetString("card.idShort"), a.GetString("board.name"))},
					{ActionType.MakeAdminOfBoard, a => a.ToString("{0} made member {1} an admin of board {2}.", a.TryGetMemberFullName(), a.GetString("board.name"))},
					{ActionType.MakeNormalMemberOfBoard, a => a.ToString("{0} made member {1} a normal user of board {2}.", a.TryGetMemberFullName(), a.GetString("board.name"))},
					{ActionType.MakeNormalMemberOfOrganization, a => a.ToString("{0} made member {1} a normal user of organization {2}.", a.TryGetMemberFullName(), a.GetString("organization.name"))},
					{ActionType.MakeObserverOfBoard, a => a.ToString("{0} made member {1} an observer of board {2}.", a.TryGetMemberFullName(), a.GetString("board.name"))},
					{ActionType.MoveCardFromBoard, a => a.ToString("{0} moved card {1} from board {2} to board {3}.", a.GetString("card.name"), a.GetString("board.name"), a.GetString("boardTarget.name"))},
					{ActionType.MoveCardToBoard, a => a.ToString("{0} moved card {1} from board {2} to board {3}.", a.GetString("card.name"), a.GetString("boardSource.name"), a.GetString("board.name"))},
					{ActionType.RemoveAdminFromBoard, a => a.ToString("{0} removed member {1} as an admin of board {2}.", a.TryGetMemberFullName(), a.GetString("board.name"))},
					{ActionType.RemoveAdminFromOrganization, a => a.ToString("{0} removed member {1} as an admin of organization {2}.", a.TryGetMemberFullName(), a.GetString("organization.name"))},
					{ActionType.RemoveChecklistFromCard, a => a.ToString("{0} deleted checklist {1} from card {2}.", a.GetString("checklist.name"), a.GetString("card.name"))},
					{ActionType.RemoveFromOrganizationBoard, a => a.ToString("{0} removed board {1} from organization {2}.", a.GetString("board.name"), a.GetString("organization.name"))},
					{ActionType.RemoveMemberFromBoard, a => a.ToString("{0} removed member {1} from board {2}.", a.TryGetMemberFullName(), a.GetString("board.name"))},
					{ActionType.RemoveMemberFromCard, a => a.ToString("{0} removed member {1} from card {2}.", a.TryGetMemberFullName(), a.GetString("card.name"))},
					{ActionType.UpdateBoard, a => a.ToString("{0} updated board {1}.", a.GetString("board.name"))},
					{ActionType.UpdateCard, a => a.ToString("{0} updated card {1}.", a.GetString("card.name"))},
					{ActionType.UpdateCheckItemStateOnCard, a => a.ToString("{0} updated checkitem {1}.", a.GetString("checkItem.name"))},
					{ActionType.UpdateChecklist, a => a.ToString("{0} updated checklist {1}.", a.GetString("checklist.name"))},
					{ActionType.UpdateMember, a => a.ToString("{0} updated their profile.")},
					{ActionType.UpdateOrganization, a => a.ToString("{0} updated organization {1}.", a.GetString("organization.name"))},
					{ActionType.UpdateCardIdList, a => a.ToString("{0} moved card {1} from list {2} to list {3}.", a.GetString("card.name"), a.GetString("listBefore.name"), a.GetString("listAfter.name"))},
					{ActionType.UpdateCardClosed, a => a.ToString("{0} archived card {1}.", a.GetString("card.name"))},
					{ActionType.UpdateCardDesc, a => a.ToString("{0} changed the description of card {1}.", a.GetString("card.name"))},
				};
		}
		/// <summary>
		/// Creates a new instance of the Action class.
		/// </summary>
		public Action()
		{
			_jsonAction = new InnerJsonAction();
			_entities = new Dictionary<string, ExpiringObject>();
		}

		/// <summary>
		/// Deletes this action.  This cannot be undone.
		/// </summary>
		public void Delete()
		{
			if (_isDeleted) return;
			Validator.Writable();
			Parameters["_id"] = Id;
			EntityRepository.Upload(EntityRequestType.Action_Write_Delete, Parameters);
			_isDeleted = true;
		}
		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(Action other)
		{
			return Id == other.Id;
		}
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
		public override bool Equals(object obj)
		{
			if (!(obj is Action)) return false;
			return Equals((Action) obj);
		}
		/// <summary>
		/// Serves as a hash function for a particular type. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <returns>
		/// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public int CompareTo(Action other)
		{
			var diff = Date - other.Date;
			return diff.HasValue ? (int) diff.Value.TotalMilliseconds : 0;
		}
		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			if (_stringDefinitions.ContainsKey(Type))
				return _stringDefinitions[Type](this);
			Log.Info("I don't have action type '{0}' configured yet.  If you can, please find the log entry where " +
					 "the JSON data is being received and email it to littlecrabsolutions@yahoo.com.  I'll try to add " +
					 "it in the next release.", _jsonAction.Type);
			return string.Format("{0} did something, but it's classified.", MemberCreator.FullName);
		}
		/// <summary>
		/// Retrieves updated data from the service instance and refreshes the object.
		/// </summary>
		public sealed override bool Refresh()
		{
			return false;
		}

		internal override void ApplyJson(object obj)
		{
			if (obj == null) return;
			_jsonAction = (IJsonAction)obj;
			UpdateType();
			Expires = DateTime.Now + EntityRepository.EntityDuration;
		}
		internal void ForceDeleted(bool deleted)
		{
			_isDeleted = deleted;
		}

		private void UpdateType()
		{
			_type = _typeMap.Any(kvp => kvp.Value == _jsonAction.Type) ? _typeMap[_jsonAction.Type] : ActionType.Unknown;
		}
		[Obsolete("Use the overload which takes multiple paths instead.")]
		private T TryGetEntity<T>(string index, string path, EntityRequestType request)
			where T : ExpiringObject
		{
			if (_entities.ContainsKey(index))
				return (T)_entities[index];
			var id = _jsonAction.Data.TryGetString(path.Split('.'));
			if (id == null)
			{
				_entities[index] = null;
				return null;
			}
			T entity = null;
			try
			{
				_entities[index] = UpdateById(ref entity, request, id);
			}
			catch { }
			return entity;
		}
		private T TryGetEntity<T>(string index, IEnumerable<string> paths, EntityRequestType request)
			where T : ExpiringObject
		{
			if (_entities.ContainsKey(index))
				return (T)_entities[index];
			string id = null;
			foreach (var path in paths)
			{
				id = _jsonAction.Data.TryGetString(path.Split('.'));
				if (id != null) break;
			}
			if (id == null)
			{
				_entities[index] = null;
				return null;
			}
			T entity = null;
			try
			{
				_entities[index] = UpdateById(ref entity, request, id);
			}
			catch { }
			return entity;
		}
		private string ToString(string format, params string[] parameters)
		{
			var allParameters = new List<object> { MemberCreator };
			allParameters.AddRange(parameters);
			return string.Format(format, allParameters.ToArray());
		}
		private string GetString(string path)
		{
			var split = path.Split('.');
			object value = _jsonAction.Data.TryGetString(split);
			if (value != null) return value.ToString();
			value = _jsonAction.Data.TryGetNumber(split);
			if (value != null) return value.ToString();
			value = _jsonAction.Data.TryGetNumber(split);
			if (value != null) return value.ToString();
			return string.Empty;
		}
		private string TryGetMemberFullName()
		{
			return Member == null ? "?Unknown?" : Member.FullName;
		}
	}
}
