﻿/***************************************************************************************

	Copyright 2013 Greg Dennis

	   Licensed under the Apache License, Version 2.0 (the "License");
	   you may not use this file except in compliance with the License.
	   You may obtain a copy of the License at

		 http://www.apache.org/licenses/LICENSE-2.0

	   Unless required by applicable law or agreed to in writing, software
	   distributed under the License is distributed on an "AS IS" BASIS,
	   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	   See the License for the specific language governing permissions and
	   limitations under the License.
 
	File Name:		MissingSerializerException.cs
	Namespace:		Manatee.Trello.Exceptions
	Class Name:		MissingSerializerException
	Purpose:		Thrown on TrelloService creation when either the ISerializer
					or IDeserializer implementation is null.

***************************************************************************************/

using System;

namespace Manatee.Trello.Exceptions
{
	/// <summary>
	/// Thrown on TrelloService creation when either the ISerializer or IDeserializer
	/// implementation is null.
	/// </summary>
	public class MissingSerializerException : Exception
	{
		/// <summary>
		/// Creates a new instance of the MissingSerializerException class.
		/// </summary>
		public MissingSerializerException()
			: base("Implementations of ISerializer and IDeserializer must be supplied in the configuration. " +
				   "You may create your own or download Manatee.Trello.ManateeJson or Manatee.Trello.NewtonsoftJson from Nuget.") {}
	}
}