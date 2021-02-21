using DotnetPlayground.Models;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DotnetPlayground.GraphQL.Extensions
{
    public class UseBloggingContextAttribute : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseDbContext<BloggingContext>();
        }
    }
}
