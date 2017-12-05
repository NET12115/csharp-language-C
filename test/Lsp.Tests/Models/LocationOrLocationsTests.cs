using System;
using FluentAssertions;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Lsp.Tests.Models
{
    public class LocationOrLocationsTests
    {
        [Theory, JsonFixture]
        public void SimpleTest(string expected)
        {
            var model = new LocationOrLocations();
            var result = Fixture.SerializeObject(model);

            result.Should().Be(expected);

            var deresult = new Serializer(ClientVersion.Lsp3).DeserializeObject<LocationOrLocations>(expected);
            deresult.ShouldBeEquivalentTo(model);
        }
    }
}
