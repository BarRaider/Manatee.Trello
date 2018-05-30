﻿using System.Threading;
using System.Threading.Tasks;

namespace Manatee.Trello
{
	/// <summary>
	/// A collection of boards.
	/// </summary>
	public interface IBoardCollection : IReadOnlyBoardCollection
	{
		/// <summary>
		/// Creates a new board.
		/// </summary>
		/// <param name="name">The name of the board to create.</param>
		/// <param name="description">(Optional) A description for the board.</param>
		/// <param name="source">(Optional) A board to use as a template.</param>
		/// <param name="ct">(Optional) A cancellation token for async processing.</param>
		/// <returns>The <see cref="IBoard"/> generated by Trello.</returns>
		Task<IBoard> Add(string name, string description = null, IBoard source = null, CancellationToken ct = default(CancellationToken));
	}
}