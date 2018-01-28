using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Models
{
	class RandomData
	{
		private static HashAlgorithm _hasher;

		public byte[] Buffer { get; set; }

		public RandomData()
		{
		}

		public RandomData(int size)
		{
			var rnd = new Random(Environment.TickCount);
			Buffer = new byte[size];

			rnd.NextBytes(Buffer);
		}

		public RandomData(int randomMinLength, int randomMaxLength)
		{
			var rnd = new Random(Environment.TickCount);
			var size = rnd.Next(randomMinLength, randomMaxLength);
			Buffer = new byte[size];

			rnd.NextBytes(Buffer);
		}

		public static string RandomStr()
		{
			string rStr = Path.GetRandomFileName();
			rStr = rStr.Replace(".", ""); // For Removing the .
			return rStr;
		}

		public override string ToString()
		{
			if (_hasher == null)
				_hasher = SHA256.Create();

			string hash_str = null;
			if (Buffer != null && Buffer.Length > 0)
				hash_str = BitConverter.ToString(_hasher.ComputeHash(Buffer)).Replace("-", "").ToLowerInvariant();
			return hash_str;
		}
	}
}
