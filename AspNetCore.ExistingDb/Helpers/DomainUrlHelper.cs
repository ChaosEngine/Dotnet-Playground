using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb
{
	/*public class DomainUrlHelperFactory : IUrlHelperFactory
	{
		private class DomainUrlHelper : UrlHelper
		{
			private readonly string _domain;

			public DomainUrlHelper(ActionContext actionContext, string domain)
				: base(actionContext)
			{
				_domain = domain;
			}

			//
			// Summary:
			//     Converts a virtual (relative) path to an application absolute path.
			//
			// Parameters:
			//   contentPath:
			//     The virtual path of the content.
			//
			// Returns:
			//     The application absolute path.
			//
			// Remarks:
			//     If the specified content path does not start with the tilde (~) character, this
			//     method returns contentPath unchanged.
			public override string Content(string contentPath)
			{
				//return contentPath.Replace("~/", _domain);
				if (string.IsNullOrEmpty(contentPath))
				{
					return null;
				}
				else if (contentPath[0] == '~')
				{
					var segment = new PathString(contentPath.Substring(1));
					var applicationPath = HttpContext.Request.PathBase;

					var domain_segm = new PathString(_domain);

					var value = applicationPath.Add(domain_segm).Add(segment).Value;

					return value;
				}

				return contentPath;
			}
		}

		private readonly string _domain;

		public DomainUrlHelperFactory(IConfiguration configuration)
		{
			_domain = configuration.AppRootPath();
		}

		IUrlHelper IUrlHelperFactory.GetUrlHelper(ActionContext context)
		{
			return new DomainUrlHelper(context, _domain);
		}
	}*/
}
