﻿using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotnetPlayground.Models
{
	public enum BlogActionEnum
	{
		Unknown = -1,
		Edit = 0,
		Delete = 1
	}
	public enum PostActionEnum
	{
		Unknown = -1,
		EditPost = 0,
		DeletePost = 1,
		AddPost = 2,
		GetPosts = 3
	}

	public partial class Post
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Required]
		public int PostId { get; set; }

		[Required]
		public int BlogId { get; set; }

		[Required]
		public string Content { get; set; }

		[Required]
		public string Title { get; set; }

		public virtual Blog Blog { get; set; }
	}

	public partial class Blog
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

		public Blog(Blog parent)
		{
			BlogId = parent.BlogId;
			Url = parent.Url;

			Post = parent.Post;
			if (Post == null)
				Post = new HashSet<Post>();
		}
	}

	public class DecoratedBlog : Blog
	{
		[ProtectionValidation("Protected ID field is invalid")]
		public string ProtectedID { get; set; }

		public DecoratedBlog() : base()
		{
		}

		public DecoratedBlog(Blog blog) : base(blog)
		{
		}

		public DecoratedBlog(Blog blog, IDataProtector protector) : base(blog)
		{
			ProtectedID = protector.Protect(BlogId.ToString());
		}
	}
}
