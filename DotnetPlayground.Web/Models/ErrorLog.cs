using System;

namespace DotnetPlayground.Models;

public sealed class ErrorLog
{
	public int Id { get; set; }
	public int? HttpStatus { get; set; }
	public string Url { get; set; }
	public string Message { get; set; }
	public int? Line { get; set; }
	public int? Column { get; set; }
	public DateTime Created { get; set; }
}
