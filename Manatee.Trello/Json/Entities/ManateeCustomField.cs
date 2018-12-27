﻿using System;
using System.Globalization;
using Manatee.Json;
using Manatee.Json.Serialization;
using Manatee.Trello.Internal;

namespace Manatee.Trello.Json.Entities
{
	internal class ManateeCustomField : IJsonCustomField, IJsonSerializable
	{
		public string Id { get; set; }
		public IJsonCustomFieldDefinition Definition { get; set; }
		public string Text { get; set; }
		public double? Number { get; set; }
		public DateTime? Date { get; set; }
		public bool? Checked { get; set; }
		public IJsonCustomDropDownOption Selected { get; set; }
		public CustomFieldType Type { get; set; }
		public bool ValidForMerge { get; set; }

		public void FromJson(JsonValue json, JsonSerializer serializer)
		{
			switch (json.Type)
			{
				case JsonValueType.Object:
					var obj = json.Object;
					Id = obj.TryGetString("id");
					Definition = obj.Deserialize<IJsonCustomFieldDefinition>(serializer, "idCustomField");
					Selected = obj.Deserialize<IJsonCustomDropDownOption>(serializer, "idValue");
					Type = obj.Deserialize<CustomFieldType?>(serializer, "type") ?? CustomFieldType.Unknown;
					ValidForMerge = true;
					if (Selected != null)
					{
						Type = CustomFieldType.DropDown;
						break;
					}
					var value = obj.TryGetObject("value");
					if (value != null)
					{
						Text = value.TryGetString("text");
						if (Text != null)
						{
							Type = CustomFieldType.Text;
							break;
						}
						var numberString = value.TryGetString("number");
						Number = numberString != null && double.TryParse(numberString, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
							         ? number
							         : (double?) null;
						if (Number != null)
						{
							Type = CustomFieldType.Number;
							break;
						}
						var boolString = value.TryGetString("checked");
						Checked = boolString != null && bool.TryParse(boolString, out var boolean)
							          ? boolean
							          : (bool?) null;
						if (Checked != null)
						{
							Type = CustomFieldType.CheckBox;
							break;
						}
						var dateString = value.TryGetString("date");
						Date = dateString != null && DateTime.TryParse(dateString, out var date)
							       ? date
							       : (DateTime?) null;
						if (Date != null)
							Type = CustomFieldType.DateTime;
					}
					break;
				case JsonValueType.String:
					Id = json.String;
					break;
			}
		}

		public JsonValue ToJson(JsonSerializer serializer)
		{
			var useForClear = false;
			var obj = new JsonObject();
			switch (Type)
			{
				case CustomFieldType.Text:
					if (Text == null)
					{
						useForClear = true;
						break;
					}
					obj["text"] = Text;
					break;
				case CustomFieldType.DropDown:
					if (Selected == null)
					{
						useForClear = true;
						break;
					}
					obj["idValue"] = Selected.Id;
					break;
				case CustomFieldType.CheckBox:
					if (Checked == null)
					{
						useForClear = true;
						break;
					}
					obj["checked"] = Checked.ToLowerString();
					break;
				case CustomFieldType.DateTime:
					if (Date == null)
					{
						useForClear = true;
						break;
					}
					obj["date"] = serializer.Serialize(Date);
					break;
				case CustomFieldType.Number:
					if (Number == null)
					{
						useForClear = true;
						break;
					}
					obj["number"] = string.Format(CultureInfo.InvariantCulture, "{0}", Number);
					break;
				case CustomFieldType.Unknown:
				default:
					throw new ArgumentOutOfRangeException();
			}

			return useForClear ? (JsonValue) string.Empty : obj;
		}
	}
}