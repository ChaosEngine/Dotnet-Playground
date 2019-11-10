using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public enum KindEnum
	{
		MD5 = 0,
		SHA256 = 1
	}

	public class ThinHashes
	{
		[Key]
		[Required]
		public string Key { get; set; }

		[Required]
		public string HashMD5 { get; set; }

		[Required]
		public string HashSHA256 { get; set; }
	}

	public partial class Hashes : ThinHashes
	{
		[Required]
		[HashLength]
		[NotMapped]
		public string Search { get; set; }

		[Required]
		[NotMapped]
		public KindEnum Kind { get; set; }

		public Hashes(ThinHashes th, HashInput hi)
		{
			Key = th.Key;
			HashMD5 = th.HashMD5;
			HashSHA256 = th.HashSHA256;

			Kind = hi.Kind;
			Search = hi.Search;
		}
	}

	public class HashInput
	{
		[Required]
		[HashLength]
		public string Search { get; set; }

		[Required]
		public KindEnum Kind { get; set; }
	}

	public class HashesInfo
	{
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public int ID { get; set; } = 0;
		public int Count { get; set; } = 0;
		public int KeyLength { get; set; }
		public string Alphabet { get; set; }
		public bool IsCalculating { get; set; }
	}

	public sealed class HashesDataTableLoadInput
	{
		[RegularExpression("(Key|HashMD5|HashSHA256)", ErrorMessage = "Characters are not allowed: only Key|HashMD5|HashSHA256")]
		public string Sort { get; set; }

		[Required, RegularExpression("(asc|desc)", ErrorMessage = "Order not allowed: only asc|desc")]
		public string Order { get; set; }

		[RegularExpression("[a-zA-Z0-9].*", ErrorMessage = "Characters are not allowed")]
		public string Search { get; set; }

		[Range(0, int.MaxValue)]
		public int Limit { get; set; } = 20;

		[Range(0, int.MaxValue)]
		public int Offset { get; set; }

		[RegularExpression("(refresh|cached)", ErrorMessage = "ExtraParam not allowed: only refresh|cached")]
		public string ExtraParam { get; set; }

		public HashesDataTableLoadInput()
		{
			//default c'to for MVC (de)serialization
		}

		public HashesDataTableLoadInput(
			string sort,
			string order,
			string search,
			int limit,
			int offset,
			string extraParam)
		{
			Sort = sort;
			Order = order;
			Search = search;
			Limit = limit;
			Offset = offset;
			ExtraParam = extraParam;
		}
	}
}