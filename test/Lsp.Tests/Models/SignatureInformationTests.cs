using System;
using FluentAssertions;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Lsp.Tests.Models
{
    public class SignatureInformationTests
    {
        [Theory, JsonFixture]
        public void SimpleTest(string expected)
        {
            var model = new SignatureInformation() {
                Documentation = "ab",
                Label = "ab",
                Parameters = new[] { new ParameterInformation() {
                    Documentation = "param",
                    Label = "param"
                } }
            };
            var result = Fixture.SerializeObject(model);

            result.Should().Be(expected);

            var deresult = new Serializer(ClientVersion.Lsp3).DeserializeObject<SignatureInformation>(expected);
            deresult.ShouldBeEquivalentTo(model);
        }

        [Theory, JsonFixture]
        public void MarkupContentTest(string expected)
        {
            var model = new SignatureInformation() {
                Documentation = "ab",
                Label = "ab",
                Parameters = new[] { new ParameterInformation() {
                    Documentation = new MarkupContent() {
                        Kind =  MarkupKind.Markdown,
                        Value = "### Value"
                    },
                    Label = "param"
                } }
            };
            var result = Fixture.SerializeObject(model);

            result.Should().Be(expected);

            var deresult = new Serializer(ClientVersion.Lsp3).DeserializeObject<SignatureInformation>(expected);
            deresult.ShouldBeEquivalentTo(model);
        }
    }
}
