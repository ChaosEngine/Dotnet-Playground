using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetPlayground.Pages
{
	public class ImportCsvModel : PageModel
	{
		/// <summary>
		/// Represents the result of a CSV count operation.
		/// </summary>
		/// <param name="RowsCount">The number of rows in the CSV file.</param>
		/// <param name="NonEmptyCellsCount">The number of non-empty cells in the CSV file.</param>
		protected internal record struct CsvResult(int RowsCount, long NonEmptyCellsCount);


		/// <summary>
		/// Gets or sets the CSV data; a two-dimensional array of strings representing the CSV data.
		/// </summary>
		[BindProperty]
		[FromBody]
		[Required]
		public string[][] CsvData { get; set; }

		public void OnGet()
		{
		}

		public IActionResult OnPost()
		{
			if (!ModelState.IsValid)
				return new BadRequestObjectResult(ModelState);

			if (CsvData == null || CsvData.Length == 0)
				return new BadRequestObjectResult("Error: empty or invalid CSV data.");

			// return new BadRequestObjectResult("Error: OMG!!Fake exception.");

			// Process the CSV data here
			// long counter = 0;
			// foreach (string[] row in CsvData)
			// 	foreach (string cell in row)
			// 		if (!string.IsNullOrEmpty(cell))
			// 			counter++;

			long counter = CsvData.Aggregate(0, (row_accumulator, row) =>
			{
				return row_accumulator + row.Aggregate(0, (cell_acc, cell) =>
				{
					return cell_acc + (string.IsNullOrEmpty(cell) ? 0 : 1);
				});
			});

			return new JsonResult(new CsvResult(CsvData.Length, counter));
		}
	}
}
