using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using OpenAI.Chat;
using Reki.Application.Common;
using Reki.Application.Common.AI;
using Reki.Application.Common.Safety;
using Reki.Infrastructure;
using Reki.Tests.Common;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Reki.Tests.AI;

public sealed class ChatClientRegistrationTests
{
    [Test]
    public async Task GetResponse_ReplacedProvider_PreservesSharedPipelineAsync()
    {
        // Arrange
        IChatClient inner = Substitute.For<IChatClient>();
        ChatOptions? forwarded = null;
        inner.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                forwarded = call.ArgAt<ChatOptions?>(1);
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));
            });
        await using ServiceProvider services = CreateServices(inner);
        IChatClient client = services.GetRequiredService<IChatClient>();
        var options = new ChatOptions
        {
            ModelId = "deepseek-flash",
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ChatInputSafetyContext.OptionKey] = new ChatInputSafetyContext(
                    TestGameData.CreatePlayer(),
                    ["A new post"]),
            },
        };

        // Act
        ChatResponse response = await client.GetResponseAsync([], options);

        // Assert
        await Assert.That(response.Text).IsEqualTo("ok");
        await Assert.That(services.GetRequiredService<IChatClient>()).IsSameReferenceAs(client);
        await Assert.That(services.GetRequiredKeyedService<IChatClient>(
            DependencyInjection.LlmProviderKey)).IsSameReferenceAs(inner);
        await Assert.That(forwarded?.AdditionalProperties?.ContainsKey(
            ChatInputSafetyContext.OptionKey) ?? false).IsFalse();
        var completionOptions = forwarded?.RawRepresentationFactory?.Invoke(inner) as ChatCompletionOptions;
#pragma warning disable SCME0001 // JsonPatch is required to check the DeepSeek provider option.
        bool thinkingDisabled = completionOptions?.Patch.Contains("$.thinking"u8) ?? false;
#pragma warning restore SCME0001
        await Assert.That(thinkingDisabled).IsTrue();
        await Assert.That(client.GetService<OpenTelemetryChatClient>()?.EnableSensitiveData ?? true).IsFalse();
        await Assert.That(options.AdditionalProperties.ContainsKey(ChatInputSafetyContext.OptionKey)).IsTrue();
    }

    [Test]
    public async Task GetResponse_MissingSafetyContext_DoesNotCallReplacedProviderAsync()
    {
        // Arrange
        IChatClient inner = Substitute.For<IChatClient>();
        await using ServiceProvider services = CreateServices(inner);
        IChatClient client = services.GetRequiredService<IChatClient>();

        // Act
        async Task ResponseAsync() => await client.GetResponseAsync([], new ChatOptions());

        // Assert
        await Assert.That(ResponseAsync).Throws<InvalidOperationException>();
        _ = inner.DidNotReceive().GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateServices(IChatClient inner)
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddLlmInfrastructure();
        services.AddSingleton(Substitute.For<IGameStore>());
        services.AddSingleton<PlayerSafetyGuard>();
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(TestGameData.PeriodStart));
        services.RemoveAllKeyed<IChatClient>(DependencyInjection.LlmProviderKey);
        services.AddKeyedSingleton(DependencyInjection.LlmProviderKey, inner);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });
    }
}