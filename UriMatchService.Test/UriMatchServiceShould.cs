using System;
using UriMatchService;
using Xunit;

namespace UriMatchService.Test
{
    public class UriMatchServiceShould
    {
        [Theory,
        InlineData("/", "root"),
        InlineData("/baz/fod/burg", ""),
        InlineData("/baz/kit", "kit"),
        InlineData("/baz/fod", "baz"),
        InlineData("/baz/fod/blob", "blob"),
        InlineData("/glah/flid/blob", "goo"),
        InlineData("/settings/{id}", "set"),
        InlineData("/organization/{id}/settings/iteminsights", "org")]
        public void FindPathTemplates(string url, string key)
        {
            var table = new UriTemplateTable();
            table.Add("root", new UriTemplate("/"));
            table.Add("foo", new UriTemplate("/foo/{bar}"));
            table.Add("kit", new UriTemplate("/baz/kit"));
            table.Add("baz", new UriTemplate("/baz/{bar}"));
            table.Add("blob", new UriTemplate("/baz/{bar}/blob"));
            table.Add("goo", new UriTemplate("/{goo}/{bar}/blob"));
            table.Add("set", new UriTemplate("/settings/{id}"));
            table.Add("org", new UriTemplate("/organization/{id}/settings/iteminsights"));

            var result = table.Match(new Uri(url, UriKind.RelativeOrAbsolute));

            if (string.IsNullOrEmpty(key))
            {
                Assert.Null(result);
            }
            else
            {
                Assert.Equal(key, result.Key);
            }

            Assert.NotNull(table["goo"]);
            Assert.Null(table["goo1"]);
        }
    }
}
