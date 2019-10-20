// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using JustEat.HttpClientInterception;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MartinCostello.LondonTravel.Skill
{
    public class StatusTests : FunctionTests
    {
        public StatusTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Theory]
        [InlineData("Bakerloo")]
        [InlineData("bakerloo")]
        [InlineData("BAKERLOO")]
        [InlineData("Central")]
        [InlineData("Circle")]
        [InlineData("District")]
        [InlineData("DLR")]
        [InlineData("Docklands")]
        [InlineData("Docklands Light Rail")]
        [InlineData("Docklands Light Railway")]
        [InlineData("Docklands Rail")]
        [InlineData("Docklands Railway")]
        [InlineData("Hammersmith")]
        [InlineData("Hammersmith & City")]
        [InlineData("Hammersmith and City")]
        [InlineData("Jubilee")]
        [InlineData("London Overground")]
        [InlineData("Overground")]
        [InlineData("Met")]
        [InlineData("Metropolitan")]
        [InlineData("Northern")]
        [InlineData("Piccadilly")]
        [InlineData("TfL Rail")]
        [InlineData("Victoria")]
        [InlineData("City")]
        [InlineData("Waterloo")]
        [InlineData("Waterloo & City")]
        [InlineData("Waterloo and City")]
        public async Task Can_Invoke_Function_For_Valid_Line(string id)
        {
            // Arrange
            Interceptor.RegisterBundle(Path.Combine("Bundles", "tfl-line-statuses.json"));

            AlexaFunction function = CreateFunction();
            SkillRequest request = CreateIntentForLine(id);
            ILambdaContext context = CreateContext();

            // Act
            SkillResponse actual = await function.HandlerAsync(request, context);

            // Assert
            AssertLineResponse(actual);
        }

        [Theory]
        [InlineData("crossrail")]
        [InlineData("Elizabeth")]
        public async Task Can_Invoke_Function_For_Elizabeth_Line(string id)
        {
            // Arrange
            AlexaFunction function = CreateFunction();
            SkillRequest request = CreateIntentForLine(id);
            ILambdaContext context = CreateContext();

            // Act
            SkillResponse actual = await function.HandlerAsync(request, context);

            // Assert
            ResponseBody response = AssertResponse(actual);

            response.Card.ShouldBeNull();
            response.OutputSpeech.ShouldNotBeNull();
            response.Reprompt.ShouldNotBeNull();

            var speeches = new[] { response.OutputSpeech, response.Reprompt.OutputSpeech };

            foreach (var speech in speeches)
            {
                speech.Type.ShouldBe("SSML");

                var ssml = speech.ShouldBeOfType<SsmlOutputSpeech>();
                ssml.Ssml.ShouldBe("<speak>Sorry, I cannot tell you about the status of the Elizabeth Line yet.</speak>");
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("not a tube line")]
        public async Task Can_Invoke_Function_For_Invalid_Line(string id)
        {
            // Arrange
            AlexaFunction function = CreateFunction();
            SkillRequest request = CreateIntentForLine(id);
            ILambdaContext context = CreateContext();

            // Act
            SkillResponse actual = await function.HandlerAsync(request, context);

            // Assert
            ResponseBody response = AssertResponse(actual);

            response.Card.ShouldBeNull();
            response.OutputSpeech.ShouldNotBeNull();
            response.Reprompt.ShouldNotBeNull();

            var speeches = new[] { response.OutputSpeech, response.Reprompt.OutputSpeech };

            foreach (var speech in speeches)
            {
                speech.Type.ShouldBe("SSML");

                var ssml = speech.ShouldBeOfType<SsmlOutputSpeech>();
                ssml.Ssml.ShouldBe("<speak>Sorry, I am not sure what line you said. You can ask about the status of any tube line, London Overground, the D.L.R. or T.F.L. Rail.</speak>");
            }
        }

        [Fact]
        public async Task Can_Invoke_Function_When_The_Api_Fails()
        {
            // Arrange
            AlexaFunction function = CreateFunction();
            SkillRequest request = CreateIntentForLine("district");
            ILambdaContext context = CreateContext();

            // Act
            SkillResponse actual = await function.HandlerAsync(request, context);

            // Assert
            ResponseBody response = AssertResponse(actual);

            response.Card.ShouldBeNull();
            response.Reprompt.ShouldBeNull();

            response.OutputSpeech.ShouldNotBeNull();
            response.OutputSpeech.Type.ShouldBe("SSML");

            var ssml = response.OutputSpeech.ShouldBeOfType<SsmlOutputSpeech>();
            ssml.Ssml.ShouldBe("<speak>Sorry, something went wrong.</speak>");
        }

        private void AssertLineResponse(
            SkillResponse actual,
            string expectedSsml = null,
            string expectedCardTitle = null,
            string expectedCardContent = null)
        {
            ResponseBody response = AssertResponse(actual);

            response.Reprompt.ShouldBeNull();

            response.OutputSpeech.ShouldNotBeNull();
            response.OutputSpeech.Type.ShouldBe("SSML");

            if (expectedSsml != null)
            {
                var ssml = response.OutputSpeech.ShouldBeOfType<SsmlOutputSpeech>();
                ssml.Ssml.ShouldBe(expectedSsml);
            }

            response.Card.ShouldNotBeNull();
            var card = response.Card.ShouldBeOfType<StandardCard>();

            card.Type.ShouldBe("Standard");

            if (expectedCardTitle != null)
            {
                card.Title.ShouldBe(expectedCardTitle);
            }

            if (expectedCardContent != null)
            {
                card.Content.ShouldBe(expectedCardContent);
            }
        }

        private SkillRequest CreateIntentForLine(string id)
        {
            return CreateIntentRequest(
                "StatusIntent",
                new Slot() { Name = "LINE", Value = id });
        }
    }
}
