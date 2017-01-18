using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public enum BlogActionEnum
	{
		Edit = 0,
		Delete = 1
	}

	public partial class Blog
    {
		[Required]
		[Key]
		public int BlogId { get; set; }

		[Required]
		public string Url { get; set; }

		public virtual ICollection<Post> Post { get; set; }

		public Blog()
        {
            Post = new HashSet<Post>();
        }
    }
}
