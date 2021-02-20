using System.Collections.Generic;

namespace DotnetPlayground.Models
{
	public sealed record AnnualTimelapseBag
	{
		public string Result { get; set; }

		public IEnumerable<object[]> Product { get; set; }
	}
}
