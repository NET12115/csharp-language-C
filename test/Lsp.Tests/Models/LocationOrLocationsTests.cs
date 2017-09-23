using System;
using FluentAssertions;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServerProtocol.Models;
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

            var deresult = JsonConvert.DeserializeObject<LocationOrLocations>(expected);
            deresult.ShouldBeEquivalentTo(model);
        }
    }
}
