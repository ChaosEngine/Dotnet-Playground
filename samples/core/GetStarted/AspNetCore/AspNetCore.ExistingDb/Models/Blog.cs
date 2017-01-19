using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public enum BlogActionEnum
	{
		Unknown = -1,
		Edit = 0,
		Delete = 1
	}

	public partial class Blog
    {
		[Required]
		[Key]
		public int BlogId { get; set; }

		[Required]
		[RegularExpression(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,4}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)")]
		public string Url { get; set; }

		public virtual ICollection<Post> Post { get; set; }

		public Blog()
        {
            Post = new HashSet<Post>();
        }
    }
}
