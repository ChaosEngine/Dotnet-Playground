
using System.Text.Json.Serialization;

namespace DotnetPlayground.Models
{
	// Under influence from:
	// https://dotnetthoughts.net/implementing-content-security-policy-in-aspnetcore/

	public sealed class CspReportRequest
	{
		public const string ContentType = "application/csp-report";
		
		[JsonPropertyName("csp-report")]
		public CspReport CspReport { get; init; }
	}

	public sealed class CspReport
	{
		[JsonPropertyName("document-uri")]
		public string DocumentUri { get; init; }

		[JsonPropertyName("referrer")]
		public string Referrer { get; init; }

		[JsonPropertyName("violated-directive")]
		public string ViolatedDirective { get; init; }

		[JsonPropertyName("effective-directive")]
		public string EffectiveDirective { get; init; }

		[JsonPropertyName("original-policy")]
		public string OriginalPolicy { get; init; }

		[JsonPropertyName("blocked-uri")]
		public string BlockedUri { get; init; }

		[JsonPropertyName("status-code")]
		public int StatusCode { get; init; }

		[JsonPropertyName("line-number")]
		public int LineNumber { get; init; }

		[JsonPropertyName("column-number")]
		public int ColumnNumber { get; init; }

		[JsonPropertyName("source-file")]
		public string SourceFile { get; init; }
	}
}