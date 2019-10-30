using System;
using Manatee.Trello.Internal;
using Manatee.Trello.Internal.Synchronization;
using Manatee.Trello.Internal.Validation;

namespace Manatee.Trello
{
	/// <summary>
	/// Exposes any data associated with an action.
	/// </summary>
	public class ActionData : IActionData
	{
		private readonly Field<Attachment> _attachment;
		private readonly Field<Board> _board;
		private readonly Field<Board> _boardSource;
		private readonly Field<Board> _boardTarget;
		private readonly Field<Card> _card;
		private readonly Field<Card> _cardSource;
		private readonly Field<CheckItem> _checkItem;
		private readonly Field<CheckList> _checkList;
		private readonly Field<Label> _label;
		private readonly Field<DateTime?> _lastEdited;
		private readonly Field<List> _list;
		private readonly Field<List> _listAfter;
		private readonly Field<List> _listBefore;
		private readonly Field<Member> _member;
		private readonly Field<bool?> _wasArchived;
		private readonly Field<string> _oldDescription;
		private readonly Field<List> _oldList;
		private readonly Field<Position> _oldPosition;
		private readonly Field<string> _oldText;
		private readonly Field<Organization> _organization;
		private readonly Field<PowerUpBase> _powerUp;
		private readonly Field<string> _text;
		private readonly Field<string> _value;
		private readonly Field<CustomFieldDefinition> _customField;
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly ActionDataContext _context;

		/// <summary>
		/// Gets an associated attachment.
		/// </summary>
		/// <associated-action-types>
		/// - AddAttachmentToCard
		///	- DeleteAttachmentFromCard
		/// </associated-action-types>
		public IAttachment Attachment => _attachment.Value;
		/// <summary>
		/// Gets an associated board.
		/// </summary>
		/// <associated-action-types>
		/// - AddMemberToBoard
		/// - AddToOrganizationBoard
		/// - CreateBoard
		/// - DeleteBoardInvitation
		/// - MakeAdminOfBoard
		/// - MakeNormalMemberOfBoard
		/// - MakeObserverOfBoard
		/// - RemoveFromOrganizationBoard
		/// - UnconfirmedBoardInvitation
		/// - UpdateBoard
		/// </associated-action-types>
		public IBoard Board => _board.Value;
		/// <summary>
		/// Gets an associated board.
		/// </summary>
		/// <associated-action-types>
		/// - CopyBoard
		/// </associated-action-types>
		public IBoard BoardSource => _boardSource.Value;
		/// <summary>
		/// Gets an associated board.
		/// </summary>
		/// <associated-action-types>
		/// - CopyBoardx
		/// </associated-action-types>
		public IBoard BoardTarget => _boardTarget.Value;
		/// <summary>
		/// Gets an associated card.
		/// </summary>
		/// <associated-action-types>
		/// - AddAttachmentToCard
		/// - AddChecklistToCard
		/// - AddMemberToCard
		/// - CommentCard
		/// - ConvertToCardFromCheckItem
		/// - CopyCommentCard
		/// - CreateCard
		/// - DeleteAttachmentFromCard
		/// - DeleteCard
		/// - EmailCard
		/// - MoveCardFromBoard
		/// - MoveCardToBoard
		/// - RemoveChecklistFromCard
		/// - RemoveMemberFromCard
		/// - UpdateCard
		/// - UpdateCardClosed
		/// - UpdateCardDesc
		/// - UpdateCardIdList
		/// - UpdateCardName
		/// - UpdateCheckItemStateOnCard
		/// </associated-action-types>
		public ICard Card => _card.Value;
		/// <summary>
		/// Gets an associated card.
		/// </summary>
		/// <associated-action-types>
		/// - CopyCard
		/// </associated-action-types>
		public ICard CardSource => _cardSource.Value;
		/// <summary>
		/// Gets an associated checklist item.
		/// </summary>
		/// <associated-action-types>
		/// - ConvertToCardFromCheckItem
		/// - UpdateCheckItemStateOnCard
		/// </associated-action-types>
		public ICheckItem CheckItem => _checkItem.Value;
		/// <summary>
		/// Gets an associated checklist.
		/// </summary>
		/// <associated-action-types>
		/// - AddChecklistToCard
		/// - RemoveChecklistFromCard
		/// - UpdateChecklist
		/// </associated-action-types>
		public ICheckList CheckList => _checkList.Value;
		/// <summary>
		/// Gets an associated custom field definition.
		/// </summary>
		/// <associated-action-types>
		/// - UpdateCustomField
		/// - UpdateCustomFieldItem
		/// </associated-action-types>
		public ICustomFieldDefinition CustomField => _customField.Value;
		/// <summary>
		/// Gets the associated label.
		/// </summary>
		/// <associated-action-types>
		/// - AddLabelToCard
		/// - CreateLabel
		/// - DeleteLabel
		/// - RemoveLabelFromCard
		/// - UpdateLabel
		/// </associated-action-types>
		public ILabel Label => _label.Value;
		/// <summary>
		/// Gets the date/time a comment was last edited.
		/// </summary>
		public DateTime? LastEdited => _lastEdited.Value;
		/// <summary>
		/// Gets an associated list.
		/// </summary>
		/// <associated-action-types>
		/// - CreateList
		/// - MoveListFromBoard
		/// - MoveListToBoard
		/// - UpdateList
		/// - UpdateListClosed
		/// - UpdateListName
		/// </associated-action-types>
		public IList List => _list.Value;
		/// <summary>
		/// Gets the current list.
		/// </summary>
		/// <associated-action-types>
		/// - UpdateCardIdList
		/// </associated-action-types>
		/// <remarks>
		/// For some action types, this information may be in the <see cref="List"/> or <see cref="OldList"/> properties.
		/// </remarks>
		public IList ListAfter => _listAfter.Value;
		/// <summary>
		/// Gets the previous list.
		/// </summary>
		/// <associated-action-types>
		/// - UpdateCardIdList
		/// </associated-action-types>
		/// <remarks>
		/// For some action types, this information may be in the <see cref="List"/> or <see cref="OldList"/> properties.
		/// </remarks>
		public IList ListBefore => _listBefore.Value;
		/// <summary>
		/// Gets an associated member.
		/// </summary>
		/// <associated-action-types>
		/// - AddMemberToBoard
		/// - AddMemberToCard
		/// - AddMemberToOrganization
		/// - MakeNormalMemberOfBoard
		/// - MakeNormalMemberOfOrganization
		/// - MemberJoinedTrello
		/// - RemoveMemberFromCard
		/// - UpdateMember
		/// </associated-action-types>
		public IMember Member => _member.Value;
		/// <summary>
		/// Gets the previous description.
		/// </summary>
		/// <associated-action-types>
		/// - UpdateCard
		/// - UpdateCardDesc
		/// </associated-action-types>
		public string OldDescription => _oldDescription.Value;
		/// <summary>
		/// Gets the previous list.
		/// </summary>
		/// <associated-action-types>
		/// - UpdateCard
		/// - UpdateCardIdList
		/// </associated-action-types>
		/// <remarks>
		/// For some action types, this information may be in the <see cref="ListAfter"/> or <see cref="ListBefore"/> properties.
		/// </remarks>
		public IList OldList => _oldList.Value;
		/// <summary>
		/// Gets the previous position.
		/// </summary>
		/// <associated-action-types>
		/// - UpdateCard
		/// - UpdateList
		/// - UpdateCustomField
		/// </associated-action-types>
		public Position OldPosition => _oldPosition.Value;
		/// <summary>
		/// Gets the previous text value. 
		/// </summary>
		/// <associated-action-types>
		/// - UpdateCard
		/// - CommentCard
		/// </associated-action-types>
		public string OldText => _oldText.Value;
		/// <summary>
		/// Gets an associated organization.
		/// </summary>
		/// <associated-action-types>
		/// - AddMemberToOrganization
		/// - AddToOrganizationBoard
		/// - CreateOrganization
		/// - DeleteOrganizationInvitation
		/// - MakeNormalMemberOfOrganization
		/// - RemoveFromOrganizationBoard
		/// - UnconfirmedOrganizationInvitation
		/// - UpdateOrganization
		/// </associated-action-types>
		public IOrganization Organization => _organization.Value;
		/// <summary>
		/// Gets an associated power-up.
		/// </summary>
		/// <associated-action-types>
		/// - DisablePowerUp
		/// - EnablePowerUp
		/// </associated-action-types>
		public IPowerUp PowerUp => _powerUp.Value;
		/// <summary>
		/// Gets associated text.
		/// </summary>
		/// <associated-action-types>
		/// - CommentCard
		/// </associated-action-types>
		public string Text
		{
			get { return _text.Value; }
			set { _text.Value = value; }
		}
		/// <summary>
		/// Gets whether the object was previously archived.
		/// </summary>
		/// <associated-action-types>
		/// - UpdateCardClosed
		/// - UpdateListClosed
		/// </associated-action-types>
		public bool? WasArchived => _wasArchived.Value;
		/// <summary>
		/// Gets a custom value associate with the action if any.
		/// </summary>
		public string Value => _value.Value;

		internal ActionData(ActionDataContext context)
		{
			_context = context;

			_attachment = new Field<Attachment>(_context, nameof(Attachment));
			_board = new Field<Board>(_context, nameof(Board));
			_boardSource = new Field<Board>(_context, nameof(BoardSource));
			_boardTarget = new Field<Board>(_context, nameof(BoardTarget));
			_card = new Field<Card>(_context, nameof(Card));
			_cardSource = new Field<Card>(_context, nameof(CardSource));
			_checkItem = new Field<CheckItem>(_context, nameof(CheckItem));
			_checkList = new Field<CheckList>(_context, nameof(CheckList));
			_customField = new Field<CustomFieldDefinition>(_context, nameof(CustomField));
			_label = new Field<Label>(_context, nameof(Label));
			_lastEdited = new Field<DateTime?>(_context, nameof(LastEdited));
			_list = new Field<List>(_context, nameof(List));
			_listAfter = new Field<List>(_context, nameof(ListAfter));
			_listBefore = new Field<List>(_context, nameof(ListBefore));
			_member = new Field<Member>(_context, nameof(Member));
			_wasArchived = new Field<bool?>(_context, nameof(WasArchived));
			_oldDescription = new Field<string>(_context, nameof(OldDescription));
			_oldList = new Field<List>(_context, nameof(OldList));
			_oldPosition = new Field<Position>(_context, nameof(OldPosition));
			_oldText = new Field<string>(_context, nameof(OldText));
			_organization = new Field<Organization>(_context, nameof(Organization));
			_powerUp = new Field<PowerUpBase>(_context, nameof(PowerUp));
			_text = new Field<string>(_context, nameof(Text));
			_text.AddRule(OldValueNotNullOrWhiteSpaceRule.Instance);
			_value = new Field<string>(_context, nameof(Value));
		}
	}
}