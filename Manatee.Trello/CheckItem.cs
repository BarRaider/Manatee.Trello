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
 
	File Name:		CheckItem.cs
	Namespace:		Manatee.Trello
	Class Name:		CheckItem
	Purpose:		Represents an item in a checklist on Trello.com.

***************************************************************************************/
using System;
using System.Linq;
using Manatee.Json;
using Manatee.Json.Enumerations;
using Manatee.Json.Extensions;
using Manatee.Trello.Contracts;
using Manatee.Trello.Implementation;

namespace Manatee.Trello
{
	//   "checkItems":[
	//      {
	//         "state":"incomplete",
	//         "id":"514463bfd02ebee350000d1c",
	//         "name":"Test development",
	//         "pos":16703
	//      },
	/// <summary>
	/// Represents an item in a checklist.
	/// </summary>
	public class CheckItem : JsonCompatibleExpiringObject, IEquatable<CheckItem>
	{
		private static readonly OneToOneMap<CheckItemStateType, string> _stateMap;

		private string _apiState;
		private string _name;
		private Position _position;
		private CheckItemStateType _state = CheckItemStateType.Unknown;

		/// <summary>
		/// Gets or sets the name of the checklist item.
		/// </summary>
		public string Name
		{
			get
			{
				VerifyNotExpired();
				return _name;
			}
			set
			{
				if (_name == value) return;
				Validate.NonEmptyString(value);
				_name = value;
				Parameters.Add("value", value);
				Put("name");
			}
		}
		/// <summary>
		/// Gets or sets the position of the checklist item.
		/// </summary>
		public Position Position
		{
			get
			{
				VerifyNotExpired();
				return _position;
			}
			set
			{
				if (_position == value) return;
				Validate.Position(value);
				_position = value;
				Parameters.Add("value", value);
				Put("pos");
			}
		}
		/// <summary>
		/// Gets or sets the check state of the checklist item.
		/// </summary>
		public CheckItemStateType State
		{
			get
			{
				VerifyNotExpired();
				return _state;
			}
			set
			{
				if (_state == value) return;
				_state = value;
				UpdateApiState();
				Parameters.Add("value", value);
				Put("state");
			}
		}

		internal override string Key { get { return "checkItems"; } }

		static CheckItem()
		{
			_stateMap = new OneToOneMap<CheckItemStateType, string>
						{
							{CheckItemStateType.Incomplete, "incomplete"},
							{CheckItemStateType.Complete, "complete"},
						};
		}
		/// <summary>
		/// Creates a new instance of the CheckItem class.
		/// </summary>
		public CheckItem() {}
		internal CheckItem(TrelloService svc, CheckList owner)
			: base(svc, owner) {}

		/// <summary>
		/// Deletes this checklist item.  This cannot be undone.
		/// </summary>
		public void Delete()
		{
			if (Svc == null) return;
			Svc.DeleteFromCache(Svc.RequestProvider.Create<CheckItem>(new[] {Owner, this}));
		}
		/// <summary>
		/// Builds an object from a JsonValue.
		/// </summary>
		/// <param name="json">The JsonValue representation of the object.</param>
		public override void FromJson(JsonValue json)
		{
			if (json == null) return;
			if (json.Type != JsonValueType.Object) return;
			var obj = json.Object;
			Id = obj.TryGetString("id");
			_name = obj.TryGetString("name");
			_position = new Position(PositionValue.Unknown);
			if (obj.ContainsKey("pos"))
				_position.FromJson(obj["pos"]);
			_apiState = obj.TryGetString("state");
			UpdateState();
			_isInitialized = true;
		}
		/// <summary>
		/// Converts an object to a JsonValue.
		/// </summary>
		/// <returns>
		/// The JsonValue representation of the object.
		/// </returns>
		public override JsonValue ToJson()
		{
			if (!_isInitialized) VerifyNotExpired();
			var json = new JsonObject
						{
							{"id", Id},
							{"name", _name},
			           		{"pos", _position.ToJson()},
							{"state", _apiState}
						};
			return json;
		}
		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(CheckItem other)
		{
			return Id == other.Id;
		}

		internal override bool Match(string id)
		{
			return Id == id;
		}
		internal override void Refresh(ExpiringObject entity)
		{
			var checkItem = entity as CheckItem;
			if (checkItem == null) return;
			_apiState = checkItem._apiState;
			_name = checkItem._name;
			_position = checkItem._position;
			UpdateState();
			_isInitialized = true;
		}

		/// <summary>
		/// Retrieves updated data from the service instance and refreshes the object.
		/// </summary>
		protected override void Get()
		{
			var entity = Svc.Api.Get(Svc.RequestProvider.Create<CheckItem>(new[] {Owner, this}));
			Refresh(entity);
		}
		/// <summary>
		/// Propigates the service instance to the object's owned objects.
		/// </summary>
		protected override void PropigateSerivce() {}

		private void Put(string extension)
		{
			if (Svc == null)
			{
				Parameters.Clear();
				return;
			}
			var request = Svc.RequestProvider.Create<CheckItem>(new[] { ((CheckList)Owner).Card, Owner, this }, this, extension);
			Svc.PutAndCache(request);
		}
		private void UpdateState()
		{
			_state = _stateMap.Any(kvp => kvp.Value == _apiState) ? _stateMap[_apiState] : CheckItemStateType.Unknown;
		}
		private void UpdateApiState()
		{
			if (_stateMap.Any(kvp => kvp.Key == _state))
				_apiState = _stateMap[_state];
		}
	}
}
