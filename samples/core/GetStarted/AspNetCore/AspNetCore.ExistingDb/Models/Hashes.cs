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
		public int Count { get; set; } = 0;
		public int KeyLength { get; set; }
		public string Alphabet { get; set; }
		public bool IsCalculating { get; set; }
	}
}