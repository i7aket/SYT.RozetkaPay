using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Security;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Verifies <see cref="RozetkaPayWebhookSignatureVerifier"/> against the callback signature contract
/// published at https://docs.rozetkapay.com/guides/callbacks/.
/// </summary>
/// <remarks>
/// Every expected signature in this file was produced by an independent Python reference implementation
/// of the documented algorithm (<c>base64url_encode(sha1(password + base64url_encode(body) + password))</c>),
/// never by the production code under test. Non-ASCII payloads are written with <c>\uXXXX</c> escapes so
/// the vectors stay byte-identical regardless of how this source file is stored or read.
/// </remarks>
public class WebhookSignatureVerifierTests
{
    /// <summary>Password from the official documentation example.</summary>
    private const string OfficialPassword = "your_password";

    /// <summary>Callback body from the official documentation example.</summary>
    private const string OfficialBody = "{\"name\": \"john\", \"age\": 21}";

    /// <summary>Signature the official Python example prints for the pair above.</summary>
    private const string OfficialSignature = "rHk7tE6V9feV_lCvZ6ZDuzte7O4=";

    // Ukrainian text plus a BMP emoji. Its inner Base64 contains both '+' and '/' in the standard
    // alphabet, so this vector fails unless the inner encoding is translated to the URL-safe alphabet.
    private const string UkrainianBody =
        "{\"status\":\"success\",\"description\":\"Оплата пройшла успішно ✅\"}";
    private const string UkrainianBodySignature = "Vf5MD_NSLPzpootqQYLax5pOL8U=";

    // Astral-plane emoji (surrogate pairs -> four-byte UTF-8 sequences) plus the hryvnia sign.
    private const string AstralBody =
        "{\"customer\":\"Олександр\",\"note\":\"Дякуємо! 🎉🇺🇦\",\"amount\":\"250.00 ₴\"}";
    private const string AstralBodySignature = "aTqEtIVXOaTTINIG5XRIyMKHo0g=";

    // The same logical text sent two ways: literal "О" characters versus real code points.
    // They are different raw bodies and therefore have different signatures.
    private const string EscapedUnicodeBody =
        "{\"status\":\"success\",\"description\":\"\\u041e\\u043f\\u043b\\u0430\\u0442\\u0430\"}";
    private const string EscapedUnicodeBodySignature = "WYIfNHnltdkB1VEYKJ2uziEEMTg=";
    private const string UnescapedUnicodeBody =
        "{\"status\":\"success\",\"description\":\"Оплата\"}";
    private const string UnescapedUnicodeBodySignature = "YRWATVXy2Qn00LpfGEGjYLWvdzo=";

    /// <summary>Signature of the empty payload under <see cref="OfficialPassword"/>.</summary>
    private const string EmptyPayloadSignature = "OjK59ILqlE2ROqArFz-045qr6dM=";

    // A dummy password whose leading and trailing spaces are part of the value.
    private const string SpacedPassword = "  whitespace_matters  ";
    private const string ExpectedSignatureWithSurroundingWhitespace = "CClYgMOH8cH-gi_LdOs3rCfN4d8=";
    private const string ExpectedSignatureAfterTrimming = "hdF-Cf4ryKsdQQ81Hml96CUG_Bg=";

    [Fact]
    public void Verify_ShouldAcceptTheOfficialDocumentationVector_ForTheByteOverload()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        Assert.True(verifier.Verify(Utf8(OfficialBody), OfficialSignature));
    }

    [Fact]
    public void Verify_ShouldAcceptTheOfficialDocumentationVector_ForTheStringOverload()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        Assert.True(verifier.Verify(OfficialBody, OfficialSignature));
    }

    [Fact]
    public void SignatureHeaderName_ShouldMatchTheDocumentedHeader()
    {
        Assert.Equal("X-ROZETKAPAY-SIGNATURE", RozetkaPayWebhookSignatureVerifier.SignatureHeaderName);
    }

    [Theory]
    [InlineData(UkrainianBody, UkrainianBodySignature)]
    [InlineData(AstralBody, AstralBodySignature)]
    public void Verify_ShouldHandleNonAsciiPayloadsAsUtf8(string body, string signature)
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        // Both overloads must agree, which also pins the string overload to UTF-8 without a BOM.
        Assert.True(verifier.Verify(Utf8(body), signature));
        Assert.True(verifier.Verify(body, signature));
    }

    [Fact]
    public void Verify_ShouldTreatEscapedAndUnescapedUnicodeAsDifferentPayloads()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        Assert.True(verifier.Verify(EscapedUnicodeBody, EscapedUnicodeBodySignature));
        Assert.True(verifier.Verify(UnescapedUnicodeBody, UnescapedUnicodeBodySignature));

        // Swapping the two signatures must fail: the raw bytes differ even though the text does not.
        Assert.False(verifier.Verify(EscapedUnicodeBody, UnescapedUnicodeBodySignature));
        Assert.False(verifier.Verify(UnescapedUnicodeBody, EscapedUnicodeBodySignature));
    }

    [Fact]
    public void Verify_ShouldAcceptAnEmptyPayloadWithItsOwnSignature()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        Assert.True(verifier.Verify(string.Empty, EmptyPayloadSignature));
        Assert.True(verifier.Verify(ReadOnlyMemory<byte>.Empty, EmptyPayloadSignature));
        Assert.True(verifier.Verify(Utf8(string.Empty), EmptyPayloadSignature));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Verify_ShouldNotConfuseAnEmptyPayloadWithAMissingSignature(string? signature)
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        Assert.False(verifier.Verify(string.Empty, signature));
        Assert.False(verifier.Verify(ReadOnlyMemory<byte>.Empty, signature));
    }

    [Theory]
    // Byte-for-byte differences that a parse-then-reserialize implementation would wrongly accept.
    [InlineData("{\"name\":\"john\",\"age\":21}")]
    [InlineData("{\"name\": \"john\", \"age\": 21}\n")]
    [InlineData("{\"name\": \"john\", \"age\": 21} ")]
    [InlineData(" {\"name\": \"john\", \"age\": 21}")]
    [InlineData("{\"age\": 21, \"name\": \"john\"}")]
    [InlineData("{\"name\": \"john\",  \"age\": 21}")]
    public void Verify_ShouldRejectTheOfficialSignatureForAnyMutatedBody(string mutatedBody)
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        Assert.NotEqual(OfficialBody, mutatedBody);
        Assert.False(verifier.Verify(mutatedBody, OfficialSignature));
        Assert.False(verifier.Verify(Utf8(mutatedBody), OfficialSignature));
    }

    [Fact]
    public void Verify_ShouldUseTheExactPasswordIncludingSurroundingWhitespace()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(SpacedPassword);

        Assert.True(verifier.Verify(OfficialBody, ExpectedSignatureWithSurroundingWhitespace));

        // Trimming the secret would produce a different digest, so the trimmed variant must fail.
        Assert.False(verifier.Verify(OfficialBody, ExpectedSignatureAfterTrimming));
    }

    [Fact]
    public void Verify_ShouldRejectASignatureMadeWithADifferentPassword()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        // Signature of the official body under the password "wrong_password".
        Assert.False(verifier.Verify(OfficialBody, "tsd955ksOvixWyar6XLDrwsPoko="));
    }

    [Fact]
    public void Verify_ShouldRejectASignatureMadeForADifferentBody()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        // Signature of {"name": "jane", "age": 21} under the official password.
        Assert.False(verifier.Verify(OfficialBody, "PVHQIn2I0bCbYa1m0Pz6vnN7_WI="));
        Assert.False(verifier.Verify(OfficialBody, UkrainianBodySignature));
    }

    [Theory]
    [InlineData(null, "null header")]
    [InlineData("", "empty")]
    [InlineData(" ", "single space")]
    [InlineData("   ", "whitespace only")]
    [InlineData("\t", "tab only")]
    [InlineData("not-a-signature", "not base64")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O4", "padding removed")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O4==", "extra padding")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O4===", "more extra padding")]
    [InlineData("rHk7tE6V9feV/lCvZ6ZDuzte7O4=", "standard Base64 slash instead of underscore")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O4*", "invalid character")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O4!", "invalid character")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O5=", "non-canonical trailing bits, decodes to same digest")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O6=", "non-canonical trailing bits, decodes to same digest")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O7=", "non-canonical trailing bits, decodes to same digest")]
    [InlineData(" rHk7tE6V9feV_lCvZ6ZDuzte7O4=", "leading space")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O4= ", "trailing space")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O4=\n", "trailing newline")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuz te7O4=", "embedded space")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuz\nte7O4=", "embedded newline")]
    [InlineData("RHk7tE6V9feV_lCvZ6ZDuzte7O4=", "first character case flipped")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7o4=", "case flipped near the end")]
    [InlineData("rhk7te6v9fev_lcvz6zduzte7o4=", "lowercased")]
    [InlineData("RHK7TE6V9FEV_LCVZ6ZDUZTE7O4=", "uppercased")]
    [InlineData("QUFBQUFBQUFBQUFBQUFBQUFBQQ==", "valid base64url decoding to 19 bytes")]
    [InlineData("QUFBQUFBQUFBQUFBQUFBQUFBQUFB", "valid base64url decoding to 21 bytes")]
    [InlineData("QUFBQQ==", "valid base64url decoding to 3 bytes")]
    [InlineData("=", "padding only")]
    [InlineData("====", "padding only, full quantum")]
    [InlineData("rHk7tE6V9feV_lCvZ6ZDuzte7O4=rHk7tE6V9feV_lCvZ6ZDuzte7O4=", "doubled signature")]
    public void Verify_ShouldReturnFalseWithoutThrowingForMalformedSignatures(
        string? malformedSignature,
        string reason)
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        Assert.False(verifier.Verify(OfficialBody, malformedSignature), reason);
        Assert.False(verifier.Verify(Utf8(OfficialBody), malformedSignature), reason);
    }

    [Fact]
    public void Verify_ShouldRejectEverySingleCharacterMutationOfAValidSignature()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        int rejected = 0;

        for (int index = 0; index < OfficialSignature.Length; index++)
        {
            foreach (char replacement in alphabet)
            {
                if (OfficialSignature[index] == replacement)
                {
                    continue;
                }

                char[] mutated = OfficialSignature.ToCharArray();
                mutated[index] = replacement;

                Assert.False(verifier.Verify(OfficialBody, new string(mutated)));
                rejected++;
            }
        }

        // 28 positions x 63 replacements, minus the 27 positions where the original char is in the alphabet.
        Assert.Equal(1765, rejected);
    }

    [Fact]
    public void Verify_ShouldThrowArgumentNullExceptionForANullStringPayload()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);

        Assert.Throws<ArgumentNullException>(() => verifier.Verify((string)null!, OfficialSignature));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullExceptionForANullPassword()
    {
        Assert.Throws<ArgumentNullException>(() => new RozetkaPayWebhookSignatureVerifier(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Constructor_ShouldThrowArgumentExceptionForABlankPassword(string blankPassword)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new RozetkaPayWebhookSignatureVerifier(blankPassword));

        Assert.Equal("password", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldNotLeakTheSecretIntoExceptionMessages()
    {
        const string blankPassword = "  \t ";

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new RozetkaPayWebhookSignatureVerifier(blankPassword));

        // Even a blank password is caller-supplied input; only the parameter name may appear.
        Assert.DoesNotContain(blankPassword, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verifier_ShouldNotExposeThePasswordThroughItsPublicSurfaceOrToString()
    {
        const string dummyPassword = "not-a-real-credential";
        RozetkaPayWebhookSignatureVerifier verifier = new(dummyPassword);

        Assert.DoesNotContain(dummyPassword, verifier.ToString() ?? string.Empty, StringComparison.Ordinal);

        PropertyInfo[] properties = typeof(RozetkaPayWebhookSignatureVerifier)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        Assert.Empty(properties);

        // No public signing entry point: consumers verify callbacks, they never impersonate RozetkaPay.
        Assert.DoesNotContain(
            typeof(RozetkaPayWebhookSignatureVerifier)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
            method => method.Name.Contains("Sign", StringComparison.OrdinalIgnoreCase) ||
                      method.Name.Contains("Compute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Verifier_ShouldNotDependOnALogger()
    {
        // The verifier handles a secret and a raw body; it must not be able to log either.
        foreach (ConstructorInfo constructor in typeof(RozetkaPayWebhookSignatureVerifier).GetConstructors())
        {
            Assert.DoesNotContain(
                constructor.GetParameters(),
                parameter => typeof(ILogger).IsAssignableFrom(parameter.ParameterType) ||
                             parameter.ParameterType.Name.Contains("Logger", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Verify_ShouldBeDeterministicUnderConcurrentUse()
    {
        RozetkaPayWebhookSignatureVerifier verifier = new(OfficialPassword);
        ConcurrentBag<bool> validResults = [];
        ConcurrentBag<bool> invalidResults = [];
        ConcurrentBag<Exception> failures = [];

        IEnumerable<Task> work = Enumerable.Range(0, 256).Select(iteration => Task.Run(() =>
        {
            try
            {
                switch (iteration % 4)
                {
                    case 0:
                        validResults.Add(verifier.Verify(OfficialBody, OfficialSignature));
                        break;
                    case 1:
                        validResults.Add(verifier.Verify(Utf8(UkrainianBody), UkrainianBodySignature));
                        break;
                    case 2:
                        invalidResults.Add(verifier.Verify(OfficialBody, "rHk7tE6V9feV_lCvZ6ZDuzte7O5="));
                        break;
                    default:
                        invalidResults.Add(verifier.Verify(OfficialBody, null));
                        break;
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }));

        await Task.WhenAll(work);

        Assert.Empty(failures);
        Assert.Equal(128, validResults.Count);
        Assert.Equal(128, invalidResults.Count);
        Assert.All(validResults, Assert.True);
        Assert.All(invalidResults, Assert.False);
    }

    [Fact]
    public void AddRozetkaPay_ShouldRegisterTheVerifierAsASingletonBehindItsInterface()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(CreateConfiguration());

        ServiceDescriptor interfaceDescriptor = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(IRozetkaPayWebhookSignatureVerifier));
        ServiceDescriptor concreteDescriptor = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(RozetkaPayWebhookSignatureVerifier));

        Assert.Equal(ServiceLifetime.Singleton, interfaceDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, concreteDescriptor.Lifetime);
    }

    [Fact]
    public void AddRozetkaPay_ShouldResolveTheVerifierInterfaceAndConcreteTypeToTheSameSingleton()
    {
        using ServiceProvider provider = BuildProvider();

        IRozetkaPayWebhookSignatureVerifier viaInterface =
            provider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>();
        RozetkaPayWebhookSignatureVerifier viaConcreteType =
            provider.GetRequiredService<RozetkaPayWebhookSignatureVerifier>();

        Assert.Same(viaConcreteType, viaInterface);

        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        // A singleton must be shared across scopes, and no scoped dependency may be captured.
        Assert.Same(viaInterface, firstScope.ServiceProvider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>());
        Assert.Same(viaInterface, secondScope.ServiceProvider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>());
    }

    [Fact]
    public void AddRozetkaPay_ShouldConfigureTheVerifierWithTheConfiguredPassword()
    {
        using ServiceProvider provider = BuildProvider();

        IRozetkaPayWebhookSignatureVerifier verifier =
            provider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>();

        Assert.True(verifier.Verify(OfficialBody, OfficialSignature));
        Assert.False(verifier.Verify(OfficialBody, UkrainianBodySignature));
    }

    [Fact]
    public void AddRozetkaPay_ShouldNotOverrideAConsumerRegisteredVerifier()
    {
        ServiceCollection services = new();
        services.AddSingleton<IRozetkaPayWebhookSignatureVerifier, AlwaysTrueVerifier>();
        services.AddRozetkaPay(CreateConfiguration());

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        Assert.IsType<AlwaysTrueVerifier>(
            provider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRozetkaPayWebhookSignatureVerifier));

        // The concrete SDK registration is still available for consumers that want the real thing.
        Assert.IsType<RozetkaPayWebhookSignatureVerifier>(
            provider.GetRequiredService<RozetkaPayWebhookSignatureVerifier>());
    }

    [Fact]
    public void AddRozetkaPay_ShouldNotDuplicateVerifierDescriptorsOnRepeatedRegistration()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(CreateConfiguration());
        services.AddRozetkaPay(CreateConfiguration());

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRozetkaPayWebhookSignatureVerifier));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(RozetkaPayWebhookSignatureVerifier));
    }

    [Fact]
    public void AddRozetkaPay_ShouldNotExposeThePasswordThroughTheServiceDescriptor()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(CreateConfiguration());

        ServiceDescriptor descriptor = Assert.Single(
            services, entry => entry.ServiceType == typeof(RozetkaPayWebhookSignatureVerifier));

        Assert.DoesNotContain(OfficialPassword, descriptor.ToString() ?? string.Empty, StringComparison.Ordinal);
    }

    private static ReadOnlyMemory<byte> Utf8(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(CreateConfiguration());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = "https://api-epdev.rozetkapay.com",
            Login = "login",
            Password = OfficialPassword
        };
    }

    private sealed class AlwaysTrueVerifier : IRozetkaPayWebhookSignatureVerifier
    {
        public bool Verify(ReadOnlyMemory<byte> payload, string? signature) => true;

        public bool Verify(string payload, string? signature) => true;
    }
}
